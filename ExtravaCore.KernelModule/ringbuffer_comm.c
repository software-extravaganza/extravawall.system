#include "ringbuffer_comm.h"

static int major_num;
static void *shared_memory = NULL;
static char *duplexBuffer = NULL;
static DECLARE_KFIFO(userIndiciesToClear, IndexRange, NUM_SLOTS);
static struct semaphore userIndiciesToClearSemaphore;
const char* MESSAGE_BUFFER_DATA = "(Buffer stats) Total: %ld; Actively Used: %ld; Actively Free: %ld; Total Used: %ld; Total Free: %ld;";

long SystemBufferSlotsUsedCounter = 0;
long SystemBufferSlotsClearedCounter = 0;
long SystemBufferActiveUsedSlots = 0;
long SystemBufferActiveFreeSlots = 0;
// static void _initRingBuffer(RingBuffer *buffer);
static void _initDuplexRingBuffer(DuplexRingBuffer *duplexBuffer);

static void printRingBufferCounters(void){
    LOG_INFO(MESSAGE_BUFFER_DATA, NUM_SLOTS, SystemBufferActiveUsedSlots, SystemBufferActiveFreeSlots, SystemBufferSlotsUsedCounter, SystemBufferSlotsClearedCounter);
}

void read_from_buffer(char *dest, size_t offset, size_t length) {
    if(!IsActive() || !IsUserSpaceConnected()){
        return;
    }

    if(!duplexBuffer){
        LOG_ALERT("RingBuf: duplexBuffer is NULL");
        return;
    }

    if(!dest){
        LOG_ALERT("RingBuf: dest is NULL");
        return;
    }
    
    memcpy(dest, duplexBuffer + offset, length);
}

void write_to_buffer(const char *src, size_t offset, size_t length) {
    if(!IsActive() || !IsUserSpaceConnected()){
        return;
    }

    if(!duplexBuffer){
        LOG_ALERT("RingBuf: duplexBuffer is NULL");
        return;
    }

    if(!src){
        LOG_ALERT("RingBuf: src is NULL");
        return;
    }

    if(offset + length > DUPLEX_RING_BUFFER_ALIGNED_SIZE){
        LOG_ALERT("RingBuf: offset + length is greater than DUPLEX_RING_BUFFER_ALIGNED_SIZE");
        return;
    }

    memcpy(duplexBuffer + offset, src, length);
}

struct RingBufferHeader read_ring_buffer_header(uint offset) {
    struct RingBufferHeader header;
    char buffer[5]; // Buffer to hold read data: 1 byte for status and 4 bytes for position

    // Read data from the duplexBuffer
    read_from_buffer(buffer, offset, 5);

    // Extract status and position
    header.Status = (RingBufferStatus)buffer[0];

    // Be cautious with endianess here. If you are sure about the byte order, you can use memcpy.
    // If duplexBuffer is in a different byte order than your system, you might need to convert.
    memcpy(&header.Position, buffer + 1, sizeof(header.Position));

    return header;
}

void write_ring_buffer_header(uint offset, struct RingBufferHeader header) {
    char buffer[5]; // Buffer to hold data to write: 1 byte for status and 4 bytes for position

    // Assign status - it's just the first byte.
    buffer[0] = (char)header.Status;

    // Copy position - again, be cautious about endianness.
    memcpy(buffer + 1, &header.Position, sizeof(header.Position));

    // Write data back to the duplexBuffer at offset 0
    write_to_buffer((char *)&buffer, offset, 5);
}

SlotStatus read_ring_buffer_slot_status(uint offset, int slot_index) {
    SlotStatus slot_status = EMPTY;
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE;
    //printk(KERN_INFO "RingBuf: Reading status at offset %d", base_offset);
    read_from_buffer((char *)&slot_status, base_offset, SLOT_HEADER_STATUS_SIZE);
    return slot_status;
}

void write_ring_buffer_slot_status(uint offset, int slot_index, SlotStatus slot_status) {
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE;
    write_to_buffer((char *)&slot_status, base_offset, SLOT_HEADER_STATUS_SIZE);
}

SlotStatus read_system_ring_buffer_slot_status(int slot_index){
    return read_ring_buffer_slot_status(0, slot_index);
}

SlotStatus read_user_ring_buffer_slot_status(int slot_index){
    return read_ring_buffer_slot_status(RING_BUFFER_SIZE, slot_index);
}

void write_system_ring_buffer_slot_status(int slot_index, SlotStatus slot_status){
    write_ring_buffer_slot_status(0, slot_index, slot_status);
}

void write_user_ring_buffer_slot_status(int slot_index, SlotStatus slot_status){
    write_ring_buffer_slot_status(RING_BUFFER_SIZE, slot_index, slot_status);
}

RingBufferStatus read_ring_buffer_status(uint offset) {
    RingBufferStatus ring_buffer_status = Inactive;
    read_from_buffer((char *)&ring_buffer_status, offset, SLOT_HEADER_STATUS_SIZE);
    return ring_buffer_status;
}

RingBufferStatus read_system_ring_buffer_status(void){
    return read_ring_buffer_status(0);
}

RingBufferStatus read_user_ring_buffer_status(void){
    return read_ring_buffer_status(RING_BUFFER_SIZE);
}

void write_ring_buffer_status(uint offset, RingBufferStatus ring_buffer_status) {
    write_to_buffer((char *)&ring_buffer_status, offset, SLOT_HEADER_STATUS_SIZE);
}

void write_system_ring_buffer_status(RingBufferStatus ring_buffer_status){
    write_ring_buffer_status(0, ring_buffer_status);
}

void write_user_ring_buffer_status(RingBufferStatus ring_buffer_status){
    write_ring_buffer_status(RING_BUFFER_SIZE, ring_buffer_status);
}

__u32 read_ring_buffer_position(uint offset){
    __u32 position = 0;
    //printk(KERN_INFO "RingBuf: Reading position at offset %d", offset + 1);
    read_from_buffer((char *)&position, offset + RING_BUFFER_HEADER_STATUS_SIZE, RING_BUFFER_HEADER__POSITION_SIZE);
    return position;
}

__u32 read_system_ring_buffer_position(void){
    return read_ring_buffer_position(0);
}

__u32 read_user_ring_buffer_position(void){
    return read_ring_buffer_position(RING_BUFFER_SIZE);
}

void write_ring_buffer_position(int offset, __u32 position){
    //printk(KERN_INFO "RingBuf: Writing position at offset %d", offset + 1);
    write_to_buffer((char *)&position, offset + RING_BUFFER_HEADER_STATUS_SIZE, RING_BUFFER_HEADER__POSITION_SIZE);
}

void write_system_ring_buffer_position(__u32 position){
    write_ring_buffer_position(0, position);
}

void write_user_ring_buffer_position(__u32 position){
    write_ring_buffer_position(RING_BUFFER_SIZE, position);
}

struct RingBufferSlotHeader read_ring_buffer_slot_header(uint offset, int slot_index) {
    struct RingBufferSlotHeader slot_header = {0};

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE;

    // Read the metadata fields into slot_header
    read_from_buffer((char *)&slot_header.Status, base_offset + 0, SLOT_HEADER_STATUS_SIZE);
    read_from_buffer((char *)&slot_header.TotalDataSize, base_offset + 1, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE);
    read_from_buffer((char *)&slot_header.CurrentDataSize, base_offset + 5, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE);
    read_from_buffer((char *)&slot_header.SequenceNumber, base_offset + 7, SLOT_HEADER_SEQUENCE_NUMBER_SIZE);
    read_from_buffer((char *)&slot_header.ClearanceStartIndex, base_offset + 8, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE);
    read_from_buffer((char *)&slot_header.ClearanceEndIndex, base_offset + 10, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE);

    return slot_header;
}

char *read_ring_buffer_slot_data(uint offset, int slot_index, __u16 data_size) {
    if (data_size > SLOT_DATA_SIZE) {
        // Handle error or limit data_size
        return NULL;
    }

    char *data_buffer = kzalloc(data_size, GFP_KERNEL);
    if (!data_buffer) {
        // Allocation failed
        return NULL;
    }

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE + SLOT_HEADER_SIZE; 
    read_from_buffer(data_buffer, base_offset, data_size);

    return data_buffer;
}

void free_ring_buffer_slot_data(char *data){
    if (data) {
        kfree(data);
    }
}

void write_ring_buffer_slot_header(uint offset, int slot_index, struct RingBufferSlotHeader *slot_header) {
    if (!slot_header) {
        return; // Or handle the error as needed
    }

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE;

    // Write the metadata fields from slot_header
    //printk(KERN_INFO "RingBuf: Writing status at offset %d", base_offset + 0);
    write_to_buffer((char *)&slot_header->Status, base_offset + 0, SLOT_HEADER_STATUS_SIZE);
    //printk(KERN_INFO "RingBuf: Writing total data size at offset %d", base_offset + 1);
    write_to_buffer((char *)&slot_header->TotalDataSize, base_offset + 1, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE);
    //printk(KERN_INFO "RingBuf: Writing current data size at offset %d", base_offset + 5);
    write_to_buffer((char *)&slot_header->CurrentDataSize, base_offset + 5, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE);
    //printk(KERN_INFO "RingBuf: Writing sequence number at offset %d", base_offset + 7);
    write_to_buffer((char *)&slot_header->SequenceNumber, base_offset + 7, SLOT_HEADER_SEQUENCE_NUMBER_SIZE);
    //printk(KERN_INFO "RingBuf: Writing clearance start index at offset %d", base_offset + 8);
    write_to_buffer((char *)&slot_header->ClearanceStartIndex, base_offset + 8, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE);
    //printk(KERN_INFO "RingBuf: Writing clearance end index at offset %d", base_offset + 10);
    write_to_buffer((char *)&slot_header->ClearanceEndIndex, base_offset + 10, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE);
}

void write_ring_buffer_slot_data(uint offset, int slot_index, char *data, __u16 data_size) {
    if (!data || data_size > SLOT_DATA_SIZE) {
        LOG_ALERT("RingBuf: Data is NULL or data size is too large");
        return; // Or handle the error as needed
    }

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + slot_index * SLOT_SIZE + SLOT_HEADER_SIZE;
    //printk(KERN_INFO "RingBuf: Writing data at offset %d", base_offset);
    //printk("Data %s", bytes_to_ascii(data, data_size));
    write_to_buffer(data, base_offset, data_size);
}

struct RingBufferHeader read_system_ring_buffer_header(void){
    return read_ring_buffer_header(0);
}

struct RingBufferHeader read_user_ring_buffer_header(void){
    return read_ring_buffer_header(RING_BUFFER_SIZE);
}

void write_system_ring_buffer_header(struct RingBufferHeader header){
    write_ring_buffer_header(0, header);
}

void write_user_ring_buffer_header(struct RingBufferHeader header){
    write_ring_buffer_header(RING_BUFFER_SIZE, header);
}

struct RingBufferSlotHeader read_system_ring_buffer_slot_header(int slot_index){
    return read_ring_buffer_slot_header(0, slot_index);
}

struct RingBufferSlotHeader read_user_ring_buffer_slot_header(int slot_index){
    return read_ring_buffer_slot_header(RING_BUFFER_SIZE, slot_index);
}

char *read_system_ring_buffer_slot_data(int slot_index, __u16 data_size){
    return read_ring_buffer_slot_data(0, slot_index, data_size);
}

char *read_user_ring_buffer_slot_data(int slot_index, __u16 data_size){
    return read_ring_buffer_slot_data(RING_BUFFER_SIZE, slot_index, data_size);
}

void write_system_ring_buffer_slot_header(int slot_index, struct RingBufferSlotHeader *slot_header){
    write_ring_buffer_slot_header(0, slot_index, slot_header);
}

void write_user_ring_buffer_slot_header(int slot_index, struct RingBufferSlotHeader *slot_header){
    write_ring_buffer_slot_header(RING_BUFFER_SIZE, slot_index, slot_header);
}

void write_system_ring_buffer_slot_data(int slot_index, char *data, __u16 data_size){
    write_ring_buffer_slot_data(0, slot_index, data, data_size);
}

void write_user_ring_buffer_slot_data(int slot_index, char *data, __u16 data_size){
    write_ring_buffer_slot_data(RING_BUFFER_SIZE, slot_index, data, data_size);
}

static int device_mmap(struct file *filp, struct vm_area_struct *vma)
{
    unsigned long pfn;
    unsigned long length = vma->vm_end - vma->vm_start;
    unsigned long start = vma->vm_start;
    char *vmalloc_area_ptr = shared_memory;
    unsigned long page;
    int ret;

    if (length > DUPLEX_RING_BUFFER_ALIGNED_SIZE){
        printk(KERN_WARNING "RingBuf: Trying to map more memory than allocated\n");
        return -EIO;
    }
    //vma->vm_flags |= VM_DONTEXPAND | VM_DONTDUMP;
    vm_flags_set(vma, (VM_DONTEXPAND | VM_DONTDUMP));
    while (length > 0) {
        page = vmalloc_to_pfn(vmalloc_area_ptr);
        if((ret = remap_pfn_range(vma, start, page, PAGE_SIZE, PAGE_SHARED)) < 0){
            return ret;
        }
           
        start += PAGE_SIZE;
        vmalloc_area_ptr += PAGE_SIZE;
        length -= PAGE_SIZE;
        // page = vmalloc_to_pfn(vmalloc_area_ptr);
        // if (vmf_insert_pfn(vma, start, page)) {
        //     printk(KERN_WARNING "RingBuf: vmf_insert_pfn failed\n");
        //     return -EAGAIN;
        // }

        // start += PAGE_SIZE;
        // vmalloc_area_ptr += PAGE_SIZE;
        // length -= PAGE_SIZE;
    }

    vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);
    printk(KERN_INFO "RingBuf: Memory mapped to user space\n");
    return 0;
}

// static int device_mmap(struct file *filp, struct vm_area_struct *vma) {
//     return remap_pfn_range(vma, vma->vm_start,
//                            virt_to_phys(shared_memory) >> PAGE_SHIFT,
//                            vma->vm_end - vma->vm_start, vma->vm_page_prot);
// }

// static int device_mmap(struct file *filp, struct vm_area_struct *vma) {
//     unsigned long offset = vma->vm_pgoff << PAGE_SHIFT;  
//     unsigned long shared_memory_size = sizeof(RingBuffer);
//     if (offset >= shared_memory_size) {
//         return -EINVAL; 
//     }

//     unsigned long size = vma->vm_end - vma->vm_start; 
//     if (size > (shared_memory_size - offset)) {
//         return -EINVAL;
//     }

//     unsigned long pfn; 
//     /* we can use page_to_pfn on the struct page structure 
//     * returned by virt_to_page 
//     */ 
//     /* pfn = page_to_pfn (virt_to_page (shared_memory + offset)); */ 
    
//     /* Or make PAGE_SHIFT bits right-shift on the physical 
//     * address returned by virt_to_phys 
//     */       
//     pfn = virt_to_phys(shared_memory + offset) >> PAGE_SHIFT; 

//     //vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);
//     //vm_flags_set(vma, VM_IO);
//     //vm_flags_set(vma, (VM_DONTEXPAND | VM_DONTDUMP));
//     if (remap_pfn_range(vma, vma->vm_start, pfn, size, vma->vm_page_prot)) { 
//         return -EAGAIN; 
//     } 
//     return 0; 
// }

// static int device_mmap(struct file *filp, struct vm_area_struct *vma) {
//     int ret = 0;
//     struct page *page = NULL;
//     unsigned long size = (unsigned long)(vma->vm_end - vma->vm_start);

//     if (size > MAX_SIZE) {
//         ret = -EINVAL;
//         goto out;  
//     } 
   
//     page = virt_to_page((unsigned long)shared_memory + (vma->vm_pgoff << PAGE_SHIFT)); 
//     ret = remap_pfn_range(vma, vma->vm_start, page_to_pfn(page), size, vma->vm_page_prot);
//     if (ret != 0) {
//         goto out;
//     } 

//     out:
//     return ret;
// }

static struct class *device_class = NULL;
static struct device *device_obj = NULL;



// void alloc_mmap_pages(int npages)
// {
//     int i;
//     char *mem = kmalloc(PAGE_SIZE * npages);

//     if (!mem)
//         return mem;

//     for(i = 0; i < npages * PAGE_SIZE; i += PAGE_SIZE) {
//         SetPageReserved(virt_to_page(((unsigned long)mem) + i));

//     return mem;
// }

// void free_mmap_pages(void *mem, int npages)
// {
//     int i;

//     for(i = 0; i < npages * PAGE_SIZE; i += PAGE_SIZE) {
//         ClearPageReserved(virt_to_page(((unsigned long)mem) + i));

//     kfree(mem);
// }

static int adev_open(struct inode *inodep, struct file *filep) {
    printk(KERN_INFO "RingBuf: Device has been opened\n");
    SetUserSpaceReadConnected();
    SetUserSpaceWriteConnected();
    return 0;
}

static int adev_release(struct inode *inodep, struct file *filep) {
    SetUserSpaceReadDisconnected();
    SetUserSpaceWriteDisconnected();
    printk(KERN_INFO "RingBuf: Device successfully closed\n");
    return 0;
}

static struct file_operations fops = {
    .owner = THIS_MODULE, 
    .open = adev_open,
    .release = adev_release,
    .mmap = device_mmap,
    // ... other file operations if needed
};

#define DEVICE_NAME "ringbuffer_device"
#define CLASS_NAME "ring"

int InitializeRingBuffers(void){
    major_num = register_chrdev(0, DEVICE_NAME, &fops);

    // Create device class
    device_class = class_create(CLASS_NAME);
    if (IS_ERR(device_class)) {
        printk(KERN_ALERT "Failed to create class.\n");
        unregister_chrdev(major_num, DEVICE_NAME);
        return PTR_ERR(device_class);
    }

    // Create device
    device_obj = device_create(device_class, NULL, MKDEV(major_num, 0), NULL, DEVICE_NAME);
    if (IS_ERR(device_obj)) {
        printk(KERN_ALERT "Failed to create device.\n");
        class_destroy(device_class);
        unregister_chrdev(major_num, DEVICE_NAME);
        return PTR_ERR(device_obj);
    }

        // Allocate shared memory for both ingress and egress buffers
    //shared_memory = kmalloc(sizeof(RingBuffer), GFP_KERNEL);
    shared_memory = vmalloc(DUPLEX_RING_BUFFER_ALIGNED_SIZE);
    if (!shared_memory) {
        printk(KERN_ALERT "Failed to allocate shared memory\n");
        return -ENOMEM;
    }

    SystemBufferActiveFreeSlots = NUM_SLOTS;
    memset(shared_memory, 0, DUPLEX_RING_BUFFER_ALIGNED_SIZE);
    
    // Assign the ingress and egress buffers to the shared memory
    //ingressBuffer = (RingBuffer *)shared_memory;
    //egressBuffer = (RingBuffer *)((char *)shared_memory + sizeof(RingBuffer));
    duplexBuffer = (char *)shared_memory;

    sema_init(&userIndiciesToClearSemaphore, 1); 
    LOG_INFO("doubleBuffer active");
       
    // Initialize the ring buffers
    //_initDuplexRingBuffer(duplexBuffer);

    return 0;
}

void FreeRingBuffers(void){
    device_destroy(device_class, MKDEV(major_num, 0));
    class_destroy(device_class);
    unregister_chrdev(major_num, DEVICE_NAME);
    vfree(shared_memory);
    kfifo_free(&userIndiciesToClear);
    kfree(&userIndiciesToClearSemaphore);
    //kfree(shared_memory);
}
// static void _initDuplexRingBuffer(DuplexRingBuffer *duplexBuffer) {
//     if(duplexBuffer == NULL){
//         return;
//     }

//     _initRingBuffer(&duplexBuffer->SystemBuffer);
//     _initRingBuffer(&duplexBuffer->UserBuffer);
// }

// static void _initRingBuffer(RingBuffer *buffer) {
//     int i;
    
//     // Initialize read and write positions
//     buffer->Position = 0;
    
//     // Initialize all Slots to EMPTY status
//     for (i = 0; i < NUM_SLOTS; i++) {
//         buffer->Slots[i].CurrentDataSize = 0;
//         buffer->Slots[i].Status = EMPTY;
//     }
// }

int FindContiguousEmptySlots(int required_slots) {
    int count = 0;
    int current_position = read_system_ring_buffer_position();
    int start_position = current_position;

    do { 
        if (read_system_ring_buffer_slot_status(current_position) == EMPTY) {
            count++;
            if (count == required_slots) {
                return start_position; // Found enough contiguous EMPTY Slots
            }
            current_position = (current_position + 1) % NUM_SLOTS;
        } else {
            // Reset count and move to the next position
            count = 0;
            start_position = (start_position + 1) % NUM_SLOTS;
            current_position = start_position;
        }
    } while (start_position != read_system_ring_buffer_position());

    return -1; // Not enough contiguous EMPTY Slots found
}

static void print_binary(const char *data, size_t len) {
    int i, j;
    printk(KERN_INFO "Binary data: ");
    for (i = 0; i < len; ++i) {
        // Optionally print a separator (like a space) between bytes
        if (i > 0) {
            printk(KERN_CONT " ");
        }

        for (j = 7; j >= 0; --j) {
            printk(KERN_CONT "%c", (data[i] & (1 << j)) ? '1' : '0');
        }
    }
    printk(KERN_CONT "\n"); // Newline after the entire binary data is printed
}

static void print_hex(const char *data, size_t len) {
    int i;
    printk(KERN_INFO "Hex data: ");
    for (i = 0; i < len; ++i) {
        printk(KERN_CONT "%02x ", (unsigned char)data[i]);
    }
    printk(KERN_CONT "\n"); // Newline after the entire hex data is printed
}

void print_hex_with_offset(const char *data, size_t offset, size_t len) {
    int i;
    printk(KERN_INFO "Offset %zu - Hex data: ", offset);
    for (i = 0; i < len; ++i) {
        printk(KERN_CONT "%02x ", (unsigned char)data[offset + i]);
    }
    printk(KERN_CONT "\n"); // Newline after the hex data
}

RingBufferSlotHeader *create_ring_buffer_slot_header(void) {
    RingBufferSlotHeader *slot_header = kmalloc(sizeof(RingBufferSlotHeader), GFP_KERNEL);
    if (!slot_header) {
        // Handle allocation failure
        return NULL;
    }
    return slot_header;
}

void free_ring_buffer_slot_header(RingBufferSlotHeader *slot_header) {
    kfree(slot_header);
}

void WriteToSystemRingBuffer(const char *data, size_t size) {
    if(!IsActive() || !IsUserSpaceConnected()){
        return;
    }

    down(&userIndiciesToClearSemaphore);
    IndexRange indexRange;
    while(kfifo_len(&userIndiciesToClear) > 0){
        if(!IsActive() || !IsUserSpaceConnected()){
            return;
        }
        kfifo_out(&userIndiciesToClear, &indexRange, sizeof(IndexRange));
        
    }
    up(&userIndiciesToClearSemaphore);

    size_t remaining_size = size;
    size_t bytes_written = 0;
    __u8 sequence_number = 0;
    int required_slots = (size + MAX_PAYLOAD_SIZE - 1) / MAX_PAYLOAD_SIZE; // Calculate the number of Slots required
    RingBufferSlotHeader* slot_header = create_ring_buffer_slot_header();
    // Find the first set of contiguous EMPTY Slots that match the required slot count
    int start_position = FindContiguousEmptySlots(required_slots);
    if (start_position == -1) {
        start_position = 0;
    }

    write_system_ring_buffer_position(start_position);

    //LOG_INFO("Writing to slot %d", start_position);
   
    while (remaining_size > 0) {
        if(!IsActive() || !IsUserSpaceConnected()){
            free_ring_buffer_slot_header(slot_header);
            return;
        }
        size_t bytes_to_write = min(remaining_size, MAX_PAYLOAD_SIZE);
        slot_header->CurrentDataSize = bytes_to_write; //to_little_endian_16(bytes_to_write);
        slot_header->TotalDataSize = size; //to_little_endian_32(size);
        slot_header->ClearanceStartIndex = 0; //to_little_endian_16(0);
        slot_header->ClearanceEndIndex = 0; //to_little_endian_16(0);

        if (size > MAX_PAYLOAD_SIZE) {
            slot_header->SequenceNumber = sequence_number++;
        } else {
            slot_header->SequenceNumber = 0;
        }

        write_system_ring_buffer_slot_data(start_position, data + bytes_written, bytes_to_write);
        bytes_written += bytes_to_write;
        remaining_size -= bytes_to_write;
        if (remaining_size > 0) {
            slot_header->Status = ADVANCE;
            write_system_ring_buffer_position((start_position + 1) % NUM_SLOTS);
        } else {
            slot_header->Status = VALID;
        }
        write_system_ring_buffer_slot_header(start_position, slot_header);
        SystemBufferSlotsUsedCounter++;
        SystemBufferActiveFreeSlots--;
        SystemBufferActiveUsedSlots++;
    }

    free_ring_buffer_slot_header(slot_header);
    
    if(SystemBufferSlotsUsedCounter % 10000 == 0){
        printRingBufferCounters();
    }
}

DataBuffer *create_data_buffer(size_t size) {
    DataBuffer *buffer = kmalloc(sizeof(DataBuffer), GFP_KERNEL);

    if (buffer) {
        buffer->data = kmalloc(size, GFP_KERNEL);  // Allocate memory for the data.
        if (!buffer->data) {
            // Handle allocation failure for buffer->data
            kfree(buffer);
            return NULL;
        }
        buffer->size = size;
    }

    return buffer;
}

void free_data_buffer(DataBuffer *buffer) {
    kfree(buffer->data);
    buffer->data = NULL;
    buffer->size = 0;
}

uint next_read_user_position = 0;
DataBuffer *ReadFromUserRingBuffer(void) {
    if(!IsActive() || !IsUserSpaceConnected()){
        return NULL;
    }

    //LOG_DEBUG_PACKET("Reading from slot %d", next_read_user_position);
    SlotStatus status = read_user_ring_buffer_slot_status(next_read_user_position);
    //LOG_DEBUG_PACKET("SLOT STATUS %d", status);
    int slotsToLoad = 0;
    int slotsLoaded = 0;
    int currentSlot = 0;
    IndexRange indexRange;
    indexRange.Start = next_read_user_position;
    size_t currentOffset = 0; 
    DataBuffer* buffer = NULL;

    if (status != VALID && status != ADVANCE) {
        return NULL;
    }

    do {
        if(!IsActive() || !IsUserSpaceConnected()){
            if(buffer != NULL){
                free_data_buffer(buffer);
            }
            return NULL;
        }
        currentSlot = next_read_user_position + slotsLoaded;
        RingBufferSlotHeader slot_header = read_user_ring_buffer_slot_header(currentSlot);
        if(slotsToLoad == 0){
            buffer = create_data_buffer(slot_header.TotalDataSize);
            //LOG_DEBUG_PACKET("DATA SIZE %d", slot_header.CurrentDataSize);
            for(uint clearSlotIndex = slot_header.ClearanceStartIndex; clearSlotIndex < slot_header.ClearanceEndIndex; clearSlotIndex++){
                //LOG_INFO("Clearing slot %d", index);
                write_system_ring_buffer_slot_status(clearSlotIndex, EMPTY);
                SystemBufferSlotsClearedCounter++;
                SystemBufferActiveFreeSlots++;
                SystemBufferActiveUsedSlots--;
            }

            if(slot_header.Status == ADVANCE){
                // Ceiling the slotsToLoad to the number of slots required to load the data
                slotsToLoad = (slot_header.TotalDataSize + MAX_PAYLOAD_SIZE - 1) / MAX_PAYLOAD_SIZE;
            }
            else{
                slotsToLoad = 1;
            }
        }
    
        char *newSlotData = read_user_ring_buffer_slot_data(currentSlot, slot_header.CurrentDataSize);
        memcpy(buffer->data + currentOffset, newSlotData, slot_header.CurrentDataSize); // Assumes buffer->data is a char*
        currentOffset += slot_header.CurrentDataSize;
        slotsLoaded++;
        if(slotsLoaded < slotsToLoad && slot_header.Status == VALID){
            LOG_WARNING("Invalid slot status mismatch for multiple slot read");
        }

        indexRange.End = next_read_user_position;
        next_read_user_position = (next_read_user_position + 1) % NUM_SLOTS;
    }
    while(slotsLoaded < slotsToLoad);

    down(&userIndiciesToClearSemaphore);
    kfifo_put(&userIndiciesToClear, indexRange);
    up(&userIndiciesToClearSemaphore);
    return buffer;
}

void AdvanceSystemRingBuffer(void) {
    uint position = read_system_ring_buffer_position();
    SlotStatus status = read_system_ring_buffer_slot_status(position);
    while (status == VALID || status == ADVANCE) {
        write_system_ring_buffer_slot_status(position, EMPTY);
        write_system_ring_buffer_position((read_system_ring_buffer_position() + 1) % NUM_SLOTS);
    }
}

void GenerateRandomData(char *data, size_t size) {
    const char *prefix = "hello ";
    size_t prefix_len = strlen(prefix);

    // Ensure that the size is large enough to hold the prefix
    if (size <= prefix_len) {  // Adjusted to <= to include room for '\0'
        return; // or handle error appropriately
    }

    // Copy the prefix to the beginning of the data
    memcpy(data, prefix, prefix_len);

    // Generate the random data after the prefix
    // Adjusted to use full remaining buffer size, minus 1 for '\0'
    get_random_keyboard_chars(data + prefix_len, size - prefix_len - 1);
    
    // Null-terminate the resulting string
    data[size - 1] = '\0';
}

void TestWriteToRingBuffer(void) {
    
    size_t test_data_size = 200; // Example size, can be any value
    char test_data[test_data_size];

    // Generate random data
    GenerateRandomData(test_data, test_data_size);
    //printk("Text generated: %s", bytes_to_ascii(test_data, test_data_size));
    // Write the random data to the ring buffer
    WriteToSystemRingBuffer(test_data, test_data_size);

    //printk(KERN_INFO "Random data of size %zu written to the ring buffer.\n", test_data_size);
    DataBuffer *data_buffer = ReadFromUserRingBuffer();
    if (data_buffer == NULL) {
        //printk(KERN_INFO "No client response data available.\n");
        return;
    }
    
    //printk(KERN_INFO "Client response data (%d): %s", data_buffer->size, bytes_to_ascii(data_buffer->data, data_buffer->size));
    free_data_buffer(data_buffer);
}