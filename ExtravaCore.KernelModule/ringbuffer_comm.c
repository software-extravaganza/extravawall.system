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
__u64 SystemRingBufferSentCount = 0;
__u64 *debouce_processed_slots;
int debouce_processed_slots_index = 0;
DEFINE_MUTEX(buffer_write_mutex);

// static void _initRingBuffer(RingBuffer *buffer);
static void _initDuplexRingBuffer(DuplexRingBuffer *duplexBuffer);

static void printRingBufferCounters(void){
    LOG_INFO(MESSAGE_BUFFER_DATA, NUM_SLOTS, SystemBufferActiveUsedSlots, SystemBufferActiveFreeSlots, SystemBufferSlotsUsedCounter, SystemBufferSlotsClearedCounter);
}

void read_from_buffer(void *dest, size_t offset, size_t length) {
    mb(); // Ensure memory operations complete before proceeding 

    if (!IsActive() || !IsUserSpaceConnected()) {
        //LOG_ALERT("RingBuf: Inactive or no userspace connection");
        return;
    }

    if (!duplexBuffer) {
        LOG_ALERT("RingBuf: duplexBuffer is NULL");
        return;
    }

    if (!dest) {
        LOG_ALERT("RingBuf: Destination buffer is NULL");
        return;
    }

    if(offset + length > DUPLEX_RING_BUFFER_SIZE){
        LOG_ALERT("RingBuf: offset + length is greater than DUPLEX_RING_BUFFER_SIZE");
        return;
    }

    // Safe to copy directly to void* since memcpy operates on void pointers.
    memcpy(dest, duplexBuffer + offset, length);
}


void write_to_buffer(const void *src, size_t offset, size_t length) {
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

    mutex_lock(&buffer_write_mutex);
    memcpy(duplexBuffer + offset, src, length);
    mb();
    mutex_unlock(&buffer_write_mutex);
    print_data(src, length, PRINT_DEC);
}

struct RingBufferHeader read_ring_buffer_header(uint offset) {
    struct RingBufferHeader header = {0};
    CHECK_INDEX_AND_OFFSET_RETURN(0, offset, header);
    char buffer[RING_BUFFER_HEADER_SIZE]; // Buffer to hold read data: 1 byte for status and 4 bytes for position

    // Read data from the duplexBuffer
    read_from_buffer(&buffer, offset, RING_BUFFER_HEADER_SIZE);

    // Extract status and position
    header.Status = (RingBufferStatus)buffer[0];

    // Be cautious with endianess here. If you are sure about the byte order, you can use memcpy.
    // If duplexBuffer is in a different byte order than your system, you might need to convert.
    memcpy(&header.Position, buffer + 1, RING_BUFFER_HEADER__POSITION_SIZE);

    return header;
}

void write_ring_buffer_header(uint offset, struct RingBufferHeader header) {
    CHECK_INDEX_AND_OFFSET(0, offset);
    char buffer[RING_BUFFER_HEADER_SIZE]; // Buffer to hold data to write: 1 byte for status and 4 bytes for position

    // Assign status - it's just the first byte.
    buffer[0] = (char)header.Status;

    // Copy position - again, be cautious about endianness.
    memcpy(buffer + 1, &header.Position, RING_BUFFER_HEADER__POSITION_SIZE);

    // Write data back to the duplexBuffer at offset 0
    write_to_buffer(&buffer, offset, RING_BUFFER_HEADER_SIZE);
}

SlotStatus read_ring_buffer_slot_status(uint offset, int slot_index) {
    SlotStatus slot_status = EMPTY;
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_STATUS_OFFSET;
    //printk(KERN_INFO "RingBuf: Reading status at offset %d", base_offset);
    read_from_buffer(&slot_status, base_offset, SLOT_HEADER_STATUS_SIZE);
    return slot_status;
}

void write_ring_buffer_slot_id(uint offset, int slot_index, __u64 id) {
    CHECK_INDEX_AND_OFFSET(slot_index, offset);
    unsigned char slotId[SLOT_HEADER_ID_SIZE];
    int_to_bytes(&id, slotId, SLOT_HEADER_ID_SIZE);
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_ID_OFFSET;
    LOG_INFO("RingBuf: Writing id (%llu) at offset %d", id, base_offset);
    print_data(&slotId, SLOT_HEADER_ID_SIZE, PRINT_DEC);
    write_to_buffer(&slotId, base_offset, SLOT_HEADER_ID_SIZE);
}

void write_system_ring_buffer_slot_id(int slot_index, __u64 id){
    write_ring_buffer_slot_id(0, slot_index, id);
}

void write_user_ring_buffer_slot_id(int slot_index, __u64 id){
    write_ring_buffer_slot_id(RING_BUFFER_SIZE, slot_index, id);
}

void write_ring_buffer_slot_status(uint offset, int slot_index, SlotStatus slot_status) {
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_STATUS_OFFSET;
    SlotStatus oldStatus = read_ring_buffer_slot_status(offset, slot_index);
    if(oldStatus != slot_status){
        if(slot_status == VALID){
            SystemRingBufferSentCount++;
            write_ring_buffer_slot_id(offset, slot_index, SystemRingBufferSentCount);
        }
        else if(slot_status == EMPTY){
            write_ring_buffer_slot_id(offset, slot_index, 0);
        }

        write_to_buffer(&slot_status, base_offset, SLOT_HEADER_STATUS_SIZE);
    }
}

SlotStatus read_system_ring_buffer_slot_status(int slot_index){
    uint offset = 0;
    CHECK_INDEX_AND_OFFSET_RETURN(slot_index, offset, EMPTY);
    return read_ring_buffer_slot_status(offset, slot_index);
}

SlotStatus read_user_ring_buffer_slot_status(int slot_index){
    uint offset = RING_BUFFER_SIZE;
    CHECK_INDEX_AND_OFFSET_RETURN(slot_index, offset, EMPTY);
    return read_ring_buffer_slot_status(offset, slot_index);
}

void write_system_ring_buffer_slot_status(int slot_index, SlotStatus slot_status){
    uint offset = 0;
    CHECK_INDEX_AND_OFFSET(slot_index, offset);
    write_ring_buffer_slot_status(offset, slot_index, slot_status);
}

void write_user_ring_buffer_slot_status(int slot_index, SlotStatus slot_status){
    uint offset = RING_BUFFER_SIZE;
    CHECK_INDEX_AND_OFFSET(slot_index, offset);
    write_ring_buffer_slot_status(RING_BUFFER_SIZE, slot_index, slot_status);
}

__u64 read_ring_buffer_slot_id(int slot_index, uint offset) {
    CHECK_INDEX_AND_OFFSET_RETURN(0, offset, 0);
    char *slotIdBytes;
    __u64 slotId;
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_ID_OFFSET;
    read_from_buffer(&slotIdBytes, base_offset, SLOT_HEADER_ID_SIZE);
    bytes_to_int(slotIdBytes, &slotId, SLOT_HEADER_ID_SIZE);
    return slotId;
}

__u64 read_system_ring_buffer_slot_id(int slot_index){
    return read_ring_buffer_slot_id(slot_index, 0);
}

__u64 read_user_ring_buffer_slot_id(int slot_index){
    return read_ring_buffer_slot_id(slot_index, RING_BUFFER_SIZE);
}

RingBufferStatus read_ring_buffer_status(uint offset) {
    CHECK_INDEX_AND_OFFSET_RETURN(0, offset, Inactive);
    RingBufferStatus ring_buffer_status = Inactive;
    read_from_buffer(&ring_buffer_status, offset, SLOT_HEADER_STATUS_SIZE);
    return ring_buffer_status;
}

RingBufferStatus read_system_ring_buffer_status(void){
    return read_ring_buffer_status(0);
}

RingBufferStatus read_user_ring_buffer_status(void){
    return read_ring_buffer_status(RING_BUFFER_SIZE);
}

void write_ring_buffer_status(uint offset, RingBufferStatus ring_buffer_status) {
    write_to_buffer(&ring_buffer_status, offset, SLOT_HEADER_STATUS_SIZE);
}

void write_system_ring_buffer_status(RingBufferStatus ring_buffer_status){
    write_ring_buffer_status(0, ring_buffer_status);
}

void write_user_ring_buffer_status(RingBufferStatus ring_buffer_status){
    write_ring_buffer_status(RING_BUFFER_SIZE, ring_buffer_status);
}

// __u32 read_ring_buffer_position(uint offset){
//     CHECK_INDEX_AND_OFFSET_RETURN(0, offset, 0);
//     char *positionBytes;
//     __u32 position;
    
//     read_from_buffer(&positionBytes, offset + RING_BUFFER_HEADER_STATUS_SIZE, RING_BUFFER_HEADER__POSITION_SIZE);
//     bytes_to_int(positionBytes, &position, RING_BUFFER_HEADER__POSITION_SIZE);
//     return position;
// }

__u32 read_ring_buffer_position(uint offset){
    CHECK_INDEX_AND_OFFSET_RETURN(0, offset, 0);

    unsigned char positionBytes[RING_BUFFER_HEADER__POSITION_SIZE];  // Buffer allocated on stack

    // Ensure that the offset is valid and will not go beyond the buffer size.
    // If the offset is not valid, return a default value or handle the error appropriately.
    if (offset + RING_BUFFER_HEADER_STATUS_SIZE + RING_BUFFER_HEADER__POSITION_SIZE > DUPLEX_RING_BUFFER_SIZE) {
        // Handle invalid offset error
        return 0; // or appropriate error code or handling
    }

    //printk(KERN_INFO "RingBuf: Reading position at offset %d", offset + 1);
    read_from_buffer(positionBytes, offset + RING_BUFFER_HEADER_STATUS_SIZE, RING_BUFFER_HEADER__POSITION_SIZE);

    __u32 position;
    bytes_to_int(positionBytes, &position, sizeof(position));

    return position;
}

__u32 read_system_ring_buffer_position(void){
    return read_ring_buffer_position(0);
}

__u32 read_user_ring_buffer_position(void){
    return read_ring_buffer_position(RING_BUFFER_SIZE);
}

void write_ring_buffer_position(int offset, __u32 position){
    CHECK_INDEX_AND_OFFSET(0, offset);
    //printk(KERN_INFO "RingBuf: Writing position at offset %d", offset + 1);
    unsigned char bufferPosition[RING_BUFFER_HEADER__POSITION_SIZE];
    int_to_bytes(&position, bufferPosition, RING_BUFFER_HEADER__POSITION_SIZE);
    write_to_buffer(&bufferPosition, offset + RING_BUFFER_HEADER_STATUS_SIZE, RING_BUFFER_HEADER__POSITION_SIZE);
}

void write_system_ring_buffer_position(__u32 position){
    write_ring_buffer_position(0, position);
}

void write_user_ring_buffer_position(__u32 position){
    write_ring_buffer_position(RING_BUFFER_SIZE, position);
}

struct RingBufferSlotHeader read_ring_buffer_slot_header(uint offset, int slot_index) {
    struct RingBufferSlotHeader slot_header = {0};
    CHECK_INDEX_AND_OFFSET_RETURN(slot_index, offset, slot_header);
    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE);

    // Read the metadata fields into slot_header
    read_from_buffer(&slot_header.Status, base_offset + SLOT_HEADER_STATUS_OFFSET, SLOT_HEADER_STATUS_SIZE);
    read_from_buffer(&slot_header.Id, base_offset + SLOT_HEADER_ID_OFFSET, SLOT_HEADER_ID_SIZE);
    read_from_buffer(&slot_header.TotalDataSize, base_offset + SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE);
    read_from_buffer(&slot_header.CurrentDataSize, base_offset + SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE);
    read_from_buffer(&slot_header.SequenceNumber, base_offset + SLOT_HEADER_SEQUENCE_NUMBER_OFFSET, SLOT_HEADER_SEQUENCE_NUMBER_SIZE);
    read_from_buffer(&slot_header.ClearanceStartIndex, base_offset + SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE);
    read_from_buffer(&slot_header.ClearanceEndIndex, base_offset + SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE);

    // slot_header.Id = bytesToUint64((uint8_t *)&slot_header.Id);
    // slot_header.TotalDataSize = bytesToUint32((uint8_t *)&slot_header.TotalDataSize);
    // slot_header.CurrentDataSize = bytesToUint16((uint8_t *)&slot_header.CurrentDataSize);
    // slot_header.ClearanceStartIndex = bytesToUint16((uint8_t *)&slot_header.ClearanceStartIndex);
    // slot_header.ClearanceEndIndex = bytesToUint16((uint8_t *)&slot_header.ClearanceEndIndex);

    return slot_header;
}

char *read_ring_buffer_slot_data(uint offset, int slot_index, __u16 data_size) {
    CHECK_INDEX_AND_OFFSET_RETURN(slot_index, offset, NULL);
    if (data_size > SLOT_DATA_SIZE) {
        // Handle error or limit data_size
        return NULL;
    }

    char *data_buffer = kzalloc(data_size, GFP_KERNEL);
    if (!data_buffer) {
        // Allocation failed
        return NULL;
    }

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_SIZE; 
    printk(KERN_INFO "RingBuf: Reading data at offset %d and size of %d", base_offset, data_size);

    // todo: fix crash somewhere around here
    //read_from_buffer(&data_buffer, base_offset, data_size);

    return data_buffer;
}

void free_ring_buffer_slot_data(char *data){
    if (data) {
        kfree(data);
    }
}

void write_ring_buffer_slot_header(uint offset, int slot_index, struct RingBufferSlotHeader *slot_header) {
    if (!slot_header) {
        LOG_ALERT("RingBuf: Slot header is NULL");
        return; // Or handle the error as needed
    }

    CHECK_INDEX_AND_OFFSET(slot_index, offset);

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE);

    unsigned char slotId[SLOT_HEADER_ID_SIZE];
    unsigned char slotTotalDataSize[SLOT_HEADER_TOTAL_DATA_SIZE_SIZE];
    unsigned char slotCurrentDataSize[SLOT_HEADER_CURRENT_DATA_SIZE_SIZE];
    unsigned char slotClearanceStartIndex[SLOT_HEADER_CLEARANCE_START_INDEX_SIZE];
    unsigned char slotClearanceEndIndex[SLOT_HEADER_CLEARANCE_END_INDEX_SIZE];

    int_to_bytes(&slot_header->Id, slotId, sizeof(slot_header->Id));
    int_to_bytes(&slot_header->TotalDataSize, slotTotalDataSize, sizeof(slot_header->TotalDataSize));
    int_to_bytes(&slot_header->CurrentDataSize, slotCurrentDataSize, sizeof(slot_header->CurrentDataSize));
    int_to_bytes(&slot_header->ClearanceStartIndex, slotClearanceStartIndex, sizeof(slot_header->ClearanceStartIndex));
    int_to_bytes(&slot_header->ClearanceEndIndex, slotClearanceEndIndex, sizeof(slot_header->ClearanceEndIndex));

    // Write the metadata fields from slot_header
    
    // printk(KERN_INFO "RingBuf: Writing id (%llu) at offset %d", slot_header->Id, base_offset + 1);
    // print_data(&slot_header->Id, SLOT_HEADER_ID_SIZE, PRINT_DEC);
    // write_to_buffer(&slotId, base_offset + 1, SLOT_HEADER_ID_SIZE);

    __u32 bob;
    printk(KERN_INFO "RingBuf: Writing total data size (%u) at offset %d", slot_header->TotalDataSize, base_offset + SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET);
    print_data(&slot_header->TotalDataSize, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE, PRINT_DEC);
    write_to_buffer(&slotTotalDataSize, base_offset + SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE);
    read_from_buffer(&bob, base_offset + SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE);
    print_data(&bob, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE, PRINT_DEC);

    printk(KERN_INFO "RingBuf: Writing current data size (%hu) at offset %d", slot_header->CurrentDataSize, base_offset + SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET);
    print_data(&slot_header->CurrentDataSize, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE, PRINT_DEC);
    write_to_buffer(&slotCurrentDataSize, base_offset + SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE);

    printk(KERN_INFO "RingBuf: Writing sequence number (%d) at offset %d", slot_header->SequenceNumber, base_offset + SLOT_HEADER_SEQUENCE_NUMBER_OFFSET);
    print_data(&slot_header->SequenceNumber, SLOT_HEADER_SEQUENCE_NUMBER_SIZE, PRINT_DEC);
    write_to_buffer(&slot_header->SequenceNumber, base_offset + SLOT_HEADER_SEQUENCE_NUMBER_OFFSET, SLOT_HEADER_SEQUENCE_NUMBER_SIZE);

    printk(KERN_INFO "RingBuf: Writing clearance start index (%hu) at offset %d", slot_header->ClearanceStartIndex, base_offset + SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET );
    print_data(&slot_header->ClearanceStartIndex, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE, PRINT_DEC);
    write_to_buffer(&slotClearanceStartIndex, base_offset + SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE);

    printk(KERN_INFO "RingBuf: Writing clearance end index (%hu) at offset %d", slot_header->ClearanceEndIndex, base_offset + SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET);
    print_data(&slot_header->ClearanceEndIndex, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE, PRINT_DEC);
    write_to_buffer(&slotClearanceEndIndex, base_offset + SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE);

    printk(KERN_INFO "RingBuf: Writing status (%d) at offset %d", slot_header->Status, base_offset + SLOT_HEADER_STATUS_OFFSET);
    print_data(&slot_header->Status, SLOT_HEADER_STATUS_SIZE, PRINT_DEC);
    write_system_ring_buffer_slot_status(slot_index, slot_header->Status);
}

void write_ring_buffer_slot_data(uint offset, int slot_index, char *data, __u16 data_size) {
    if (!data || data_size > SLOT_DATA_SIZE) {
        LOG_ALERT("RingBuf: Data is NULL or data size is too large");
        return; // Or handle the error as needed
    }

    CHECK_INDEX_AND_OFFSET(slot_index, offset);

    size_t base_offset = offset + RING_BUFFER_HEADER_SIZE + (slot_index * SLOT_SIZE) + SLOT_HEADER_SIZE;
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
    mutex_init(&buffer_write_mutex);
    debouce_processed_slots = kmalloc(10000 * sizeof(__u64), GFP_KERNEL);
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
    kfree(&userIndiciesToClearSemaphore);\
    kfree(&debouce_processed_slots);
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

void print_binary(const char *data, size_t len) {
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

RingBufferSlotHeader *create_ring_buffer_slot_header(void) {
    RingBufferSlotHeader *slot_header = kmalloc(sizeof(RingBufferSlotHeader), GFP_KERNEL);
    if (!slot_header) {
        // Handle allocation failure
        return NULL;
    }
    return slot_header;
}

void free_ring_buffer_slot_header(RingBufferSlotHeader *slot_header) {
    if(slot_header != NULL){
        kfree(slot_header);
    }
}

int WriteToSystemRingBuffer(const char *data, size_t size) {
    LOG_INFO("IsActive: %d; IsUserSpaceConnected: %d", IsActive(), IsUserSpaceConnected());
    if(SystemBufferSlotsUsedCounter % 10000 == 0){
        printRingBufferCounters();
    }
    
    if(!IsActive() || !IsUserSpaceConnected()){
        return -1;
    }

    if (size > U32_MAX) {
        printk(KERN_INFO "Warning: size_t value exceeds __u32 range, truncation will occur\n");
    }

    LOG_INFO("Writing...");
    down(&userIndiciesToClearSemaphore);
    IndexRange indexRange;
    while(kfifo_len(&userIndiciesToClear) > 0){
        if(!IsActive() || !IsUserSpaceConnected()){
            return -1;
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

    LOG_INFO("Writing to slot %d", start_position);
   
    while (remaining_size > 0) {
        if(!IsActive() || !IsUserSpaceConnected()){
            free_ring_buffer_slot_header(slot_header);
            return -1;
        }
        size_t bytes_to_write = min(remaining_size, MAX_PAYLOAD_SIZE);
        if(slot_header == NULL){
            return -2;
        }

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
        } else {
            slot_header->Status = VALID;
        }
        
        write_system_ring_buffer_position((start_position + 1) % NUM_SLOTS);
        write_system_ring_buffer_slot_header(start_position, slot_header);
        SystemBufferSlotsUsedCounter++;
        SystemBufferActiveFreeSlots--;
        SystemBufferActiveUsedSlots++;
    }

    free_ring_buffer_slot_header(slot_header);
    LOG_INFO("Slot written %d", start_position);
    return start_position;
}

DataBuffer *create_data_buffer(size_t size) {
    DataBuffer *buffer = kzalloc(sizeof(DataBuffer), GFP_KERNEL);  // Using kzalloc

    if (!buffer) {
        return NULL;
    }

    buffer->data = kzalloc(size, GFP_KERNEL);  // Using kzalloc for zero-initialization
    if (!buffer->data) {
        kfree(buffer);
        return NULL;
    }

    buffer->size = size;
    return buffer;
}

void free_data_buffer(DataBuffer *buffer) {
    kfree(buffer->data);
    buffer->data = NULL;
    buffer->size = 0;
}

void free_data_buffer_if_needed(DataBuffer *buffer, bool bufferIsSet) {
    if(bufferIsSet){
        free_data_buffer(buffer);
    }
}

// uint next_read_user_position = 0;
// DataBuffer *ReadFromUserRingBuffer(void) {
//     if(!IsActive() || !IsUserSpaceConnected()){
//         return NULL;
//     }

//     //LOG_DEBUG_PACKET("Reading from slot %d", next_read_user_position);
//     SlotStatus status = read_user_ring_buffer_slot_status(next_read_user_position);
//     //LOG_DEBUG_PACKET("SLOT STATUS %d", status);
//     int slotsToLoad = 0;
//     int slotsLoaded = 0;
//     int currentSlot = 0;
//     IndexRange indexRange;
//     indexRange.Start = next_read_user_position;
//     size_t currentOffset = 0; 
//     DataBuffer* buffer = NULL;

//     if (status != VALID && status != ADVANCE) {
//         return NULL;
//     }

//     do {
//         if(!IsActive() || !IsUserSpaceConnected()){
//             if(buffer != NULL){
//                 free_data_buffer(buffer);
//             }
//             return NULL;
//         }
//         currentSlot = next_read_user_position + slotsLoaded;
//         RingBufferSlotHeader slot_header = read_user_ring_buffer_slot_header(currentSlot);
//         if (slot_header.ClearanceEndIndex > slot_header.ClearanceStartIndex && slot_header.ClearanceEndIndex > 0 && slot_header.ClearanceEndIndex < NUM_SLOTS) {
//             for(uint clearSlotIndex = slot_header.ClearanceStartIndex; clearSlotIndex < slot_header.ClearanceEndIndex; clearSlotIndex++){
//                 LOG_INFO("Clearing slot %d", clearSlotIndex);
//                 write_system_ring_buffer_slot_status(clearSlotIndex, EMPTY);
//                 SystemBufferSlotsClearedCounter++;
//                 SystemBufferActiveFreeSlots++;
//                 SystemBufferActiveUsedSlots--;
//             }
//         }

//         if(slotsToLoad == 0){
//             buffer = create_data_buffer(slot_header.TotalDataSize);
//             LOG_DEBUG_PACKET("DATA SIZE %d", slot_header.CurrentDataSize);
//             if(slot_header.Status == ADVANCE){
//                 // Ceiling the slotsToLoad to the number of slots required to load the data
//                 slotsToLoad = (slot_header.TotalDataSize + MAX_PAYLOAD_SIZE - 1) / MAX_PAYLOAD_SIZE;
//             }
//             else{
//                 slotsToLoad = 1;
//             }
//         }
    
//         char *newSlotData = read_user_ring_buffer_slot_data(currentSlot, slot_header.CurrentDataSize);
//         memcpy(buffer->data + currentOffset, newSlotData, slot_header.CurrentDataSize); // Assumes buffer->data is a char*
//         currentOffset += slot_header.CurrentDataSize;
//         slotsLoaded++;
//         if(slotsLoaded < slotsToLoad && slot_header.Status == VALID){
//             LOG_WARNING("Invalid slot status mismatch for multiple slot read");
//         }

//         indexRange.End = next_read_user_position;
//         next_read_user_position = (next_read_user_position + 1) % NUM_SLOTS;
//     }
//     while(slotsLoaded < slotsToLoad);

//     down(&userIndiciesToClearSemaphore);
//     kfifo_put(&userIndiciesToClear, indexRange);
//     up(&userIndiciesToClearSemaphore);
//     return buffer;
// }



bool check_and_add_debounce_slot(__u64 id){
    for(int i = 0; i < debouce_processed_slots_index; i++){
        if(debouce_processed_slots[i] == id){
            return false;
        }
    }

    debouce_processed_slots[debouce_processed_slots_index] = id;
    debouce_processed_slots_index = (debouce_processed_slots_index + 1) % 10000;
    return true;
}

uint next_read_user_position = 0;

DataBuffer *ReadFromUserRingBuffer(void) {
    if (!IsActive() || !IsUserSpaceConnected()) {
        return NULL;
    }

    int currentSlot = -1;
    DataBuffer* buffer = NULL;
    bool bufferSet = false;
    size_t currentOffset = 0;
    bool slotFound = false;
    int startIndex = next_read_user_position;
    int endIndex = startIndex;
    int bytesLoaded = 0;
    long maxTotalDataSize = MAX_PAYLOAD_SIZE * (NUM_SLOTS / 2);
    IndexRange indexRange;
    indexRange.Start = next_read_user_position;

    while (!slotFound && currentSlot != startIndex) {
        if (currentSlot < 0) {
            currentSlot = next_read_user_position;
        }

        if (currentSlot > NUM_SLOTS) {
            LOG_WARNING("Forcing slot to zero (out of range) %d", currentSlot);
            currentSlot = 0;
        }

        RingBufferSlotHeader slot_header = read_user_ring_buffer_slot_header(currentSlot);
        if(slot_header.Id == 0){
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.Id == 0) {
            LOG_ERROR("Skipping slot (id 0) %d", currentSlot);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.ClearanceStartIndex < 0) {
            LOG_ERROR("Skipping slot; ClearanceStartIndex is out of range (%d)", slot_header.ClearanceStartIndex);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.ClearanceEndIndex > NUM_SLOTS) {
            LOG_ERROR("Skipping slot; ClearanceEndIndex is out of range (%d)", slot_header.ClearanceEndIndex);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.ClearanceEndIndex < slot_header.ClearanceStartIndex) {
            LOG_ERROR("Skipping slot; ClearanceEndIndex (%d) smaller than ClearanceStartIndex (%d)", slot_header.ClearanceStartIndex, slot_header.ClearanceEndIndex);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.TotalDataSize > maxTotalDataSize || slot_header.TotalDataSize < 0) {
            LOG_ERROR("Skipping slot; TotalDataSize is out of range (%d)", slot_header.TotalDataSize);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.CurrentDataSize > MAX_PAYLOAD_SIZE || slot_header.CurrentDataSize < 0) {
            LOG_ERROR("Skipping slot; CurrentDataSize is out of range (%d)", slot_header.CurrentDataSize);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        if (slot_header.Status > 3) {
            LOG_ERROR("Skipping slot; Status is out of range (%d)", slot_header.Status);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }
        
        if(!check_and_add_debounce_slot(slot_header.Id)){
            //LOG_DEBUG_PACKET("Skipping slot (debounce) %d", currentSlot);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }

        // Allocate buffer on demand
        if (buffer == NULL && (slot_header.Status == VALID || slot_header.Status == ADVANCE)) {
            buffer = create_data_buffer(slot_header.TotalDataSize);
        }

        if(buffer == NULL){
            LOG_WARNING("Skipping slot (buffer null) %d", currentSlot);
            free_data_buffer_if_needed(buffer, bufferSet);
            return NULL;
        }
        else{
            bufferSet = true;
        }

        // Copy data from valid or advance slots
        if (slot_header.Status == VALID || slot_header.Status == ADVANCE) {
            LOG_INFO("We're in. ClearanceStartIndex: %d; ClearanceEndIndex: %d; CurrentDataSize: %d; TotalDataSize: %d; Status: %d", slot_header.ClearanceStartIndex, slot_header.ClearanceEndIndex, slot_header.CurrentDataSize, slot_header.TotalDataSize, slot_header.Status);
            if (slot_header.ClearanceEndIndex > slot_header.ClearanceStartIndex && slot_header.ClearanceEndIndex > 0 && slot_header.ClearanceEndIndex < NUM_SLOTS) {
                for(uint clearSlotIndex = slot_header.ClearanceStartIndex; clearSlotIndex < slot_header.ClearanceEndIndex; clearSlotIndex++){
                    LOG_INFO("Clearing slot %d", clearSlotIndex);
                    write_system_ring_buffer_slot_status(clearSlotIndex, EMPTY);
                    SystemBufferSlotsClearedCounter++;
                    SystemBufferActiveFreeSlots++;
                    SystemBufferActiveUsedSlots--;
                }
            }

            
            char *newSlotData = read_user_ring_buffer_slot_data(currentSlot, slot_header.CurrentDataSize);
            if(newSlotData == NULL){
                LOG_WARNING("Skipping slot (read data null) %d", currentSlot);
                free_data_buffer_if_needed(buffer, bufferSet);
                return NULL;
            }

            if(buffer->size > MAX_PAYLOAD_SIZE){
                LOG_ERROR("Skipping slot; Read data (user space) buffer size too large) %d", currentSlot);
                free_data_buffer_if_needed(buffer, bufferSet);
                return NULL;
            }

            if(buffer->size + currentOffset > MAX_PAYLOAD_SIZE){
                LOG_ERROR("Skipping slot; Read data (user space) buffer size too large) %d", currentSlot);
                free_data_buffer_if_needed(buffer, bufferSet);
                return NULL;
            }

            if(buffer == NULL){
                LOG_ERROR("Skipping slot; Read data (user space) buffer is null) %d", currentSlot);
                free_data_buffer_if_needed(buffer, bufferSet);
                return NULL;
            }

            if(buffer->data == NULL){
                LOG_ERROR("Skipping slot; Read data (user space) buffer data is null) %d", currentSlot);
                free_data_buffer_if_needed(buffer, bufferSet);
                return NULL;
            }
            
            memcpy(buffer->data + currentOffset, newSlotData, slot_header.CurrentDataSize);
            currentOffset += slot_header.CurrentDataSize;
            bytesLoaded += slot_header.CurrentDataSize;
            endIndex = (currentSlot + 1) % NUM_SLOTS;
            slotFound = slot_header.Status == VALID;
            LOG_INFO("Dude! %d - %d", slot_header.TotalDataSize, buffer->size);
        }

        // Move to next slot
        currentSlot = (currentSlot + 1) % NUM_SLOTS;
        if (currentSlot == next_read_user_position || currentSlot > NUM_SLOTS || currentSlot < 0) {
            break; // Prevent infinite loop
        }
    }

    next_read_user_position = endIndex;
    indexRange.End = next_read_user_position;
    
    if(slotFound){
        down(&userIndiciesToClearSemaphore);
        kfifo_put(&userIndiciesToClear, indexRange);
        up(&userIndiciesToClearSemaphore);
    }

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