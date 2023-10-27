#include "ringbuffer_comm.h"

static RingBuffer *ingressBuffer = NULL;
static RingBuffer *egressBuffer = NULL;
static int major_num;
static void *shared_memory = NULL;

static void _initRingBuffer(RingBuffer *buffer);

// static int device_mmap(struct file *filp, struct vm_area_struct *vma) {
//     return remap_pfn_range(vma, vma->vm_start,
//                            virt_to_phys(shared_memory) >> PAGE_SHIFT,
//                            vma->vm_end - vma->vm_start, vma->vm_page_prot);
// }

static int device_mmap(struct file *filp, struct vm_area_struct *vma) {
    unsigned long offset = vma->vm_pgoff << PAGE_SHIFT;  
    unsigned long shared_memory_size = sizeof(RingBuffer);
    if (offset >= shared_memory_size) {
        return -EINVAL; 
    }

    unsigned long size = vma->vm_end - vma->vm_start; 
    if (size > (shared_memory_size - offset)) {
        return -EINVAL;
    }

    unsigned long pfn; 
    /* we can use page_to_pfn on the struct page structure 
    * returned by virt_to_page 
    */ 
    /* pfn = page_to_pfn (virt_to_page (shared_memory + offset)); */ 
    
    /* Or make PAGE_SHIFT bits right-shift on the physical 
    * address returned by virt_to_phys 
    */       
    pfn = virt_to_phys(shared_memory + offset) >> PAGE_SHIFT; 

    vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);
    vm_flags_set(vma, VM_IO);
    vm_flags_set(vma, (VM_DONTEXPAND | VM_DONTDUMP));
    if (remap_pfn_range(vma, vma->vm_start, pfn, size, vma->vm_page_prot)) { 
        return -EAGAIN; 
    } 
    return 0; 
}

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
static struct file_operations fops = {
    .owner = THIS_MODULE, 
    .mmap = device_mmap,
    // ... other file operations if needed
};


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

int InitializeRingBuffers(void){
    major_num = register_chrdev(0, "ringbuffer_device", &fops);

    // Allocate shared memory for both ingress and egress buffers
    shared_memory = vmalloc(sizeof(RingBuffer));
    if (!shared_memory) {
        printk(KERN_ALERT "Failed to allocate shared memory\n");
        return -ENOMEM;
    }

    
    // Assign the ingress and egress buffers to the shared memory
    //ingressBuffer = (RingBuffer *)shared_memory;
    //egressBuffer = (RingBuffer *)((char *)shared_memory + sizeof(RingBuffer));
    egressBuffer = (RingBuffer *)shared_memory;
    LOG_INFO("Memory address stored in ingressBuffer: %x", virt_to_phys(shared_memory));

    // Initialize the ring buffers
    //_initRingBuffer(ingressBuffer);
    _initRingBuffer(egressBuffer);

    // Create device class
    device_class = class_create(CLASS_NAME);
    if (IS_ERR(device_class)) {
        printk(KERN_ALERT "Failed to create class.\n");
        unregister_chrdev(major_num, "ringbuffer_device");
        return PTR_ERR(device_class);
    }

    // Create device
    device_obj = device_create(device_class, NULL, MKDEV(major_num, 0), NULL, "ringbuffer_device");
    if (IS_ERR(device_obj)) {
        printk(KERN_ALERT "Failed to create device.\n");
        class_destroy(device_class);
        unregister_chrdev(major_num, "ringbuffer_device");
        return PTR_ERR(device_obj);
    }

    return 0;
}

void FreeRingBuffers(void){
    device_destroy(device_class, MKDEV(major_num, 0));
    class_destroy(device_class);
    unregister_chrdev(major_num, "ringbuffer_device");
    vfree(shared_memory);
}

static void _initRingBuffer(RingBuffer *buffer) {
    int i;
    
    // Initialize read and write positions
    buffer->Position = 0;
    
    // Initialize all Slots to EMPTY status
    for (i = 0; i < NUM_SLOTS; i++) {
        buffer->Slots[i].CurrentDataSize = 0;
        buffer->Slots[i].Status = EMPTY;
    }
}

int FindContiguousEmptySlots(RingBuffer *buffer, int required_slots) {
    int count = 0;
    int current_position = buffer->Position;
    int start_position = current_position;

    do {
        if (buffer->Slots[current_position].Status == EMPTY) {
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
    } while (start_position != buffer->Position);

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

void WriteToRingBuffer(RingBuffer *buffer, const char *data, size_t size) {
    size_t remaining_size = size;
    size_t bytes_written = 0;
    __u8 sequence_number = 0;
    int required_slots = (size + MAX_PAYLOAD_SIZE - 1) / MAX_PAYLOAD_SIZE; // Calculate the number of Slots required

    // Find the first set of contiguous EMPTY Slots that match the required slot count
    int start_position = FindContiguousEmptySlots(buffer, required_slots);
    if (start_position == -1) {
        printk(KERN_ALERT "Not enough contiguous EMPTY Slots. Cannot write data.\n");
        return;
    }
    buffer->Position = start_position;

    LOG_INFO("Writing to slot %d", buffer->Position);
    RingBufferSlot *slot = &buffer->Slots[buffer->Position];

    while (remaining_size > 0) {
        size_t bytes_to_write = min(remaining_size, MAX_PAYLOAD_SIZE);

        if (slot->Status != EMPTY) {
            printk(KERN_ALERT "Unexpected non-empty slot encountered.\n");
            return;
        }
        print_hex(data + bytes_written, bytes_to_write);
        memcpy(slot->Data, data + bytes_written, bytes_to_write);
        slot->CurrentDataSize = bytes_to_write;
        slot->TotalDataSize = size;

        if (size > MAX_PAYLOAD_SIZE) {
            slot->SequenceNumber = sequence_number++;
        } else {
            slot->SequenceNumber = 0;
        }

        bytes_written += bytes_to_write;
        remaining_size -= bytes_to_write;

        if (remaining_size > 0) {
            slot->Status = ADVANCE;
            buffer->Position = (buffer->Position + 1) % NUM_SLOTS;
            slot = &buffer->Slots[buffer->Position];
        } else {
            slot->Status = VALID;
        }

         RingBufferSlot *readslot = &buffer->Slots[buffer->Position];
         print_hex(readslot->Data, slot->TotalDataSize);
    }
}

RingBufferSlot *ReadFromRingBuffer(RingBuffer *buffer) {
    RingBufferSlot *slot = &buffer->Slots[buffer->Position];
    if (slot->Status == VALID || slot->Status == ADVANCE) {
        return slot;
    }
    return NULL;
}

void AdvanceRingBuffer(RingBuffer *buffer) {
    RingBufferSlot *slot = &buffer->Slots[buffer->Position];
    while (slot->Status == VALID || slot->Status == ADVANCE) {
        slot->Status = EMPTY;
        buffer->Position = (buffer->Position + 1) % NUM_SLOTS;
        slot = &buffer->Slots[buffer->Position];
    }
}

void GenerateRandomData(char *data, size_t size) {
    const char *prefix = "hello ";
    size_t prefix_len = strlen(prefix);

    // Ensure that the size is large enough to hold the prefix
    if (size < prefix_len) {
        return; // or handle error appropriately
    }

    // Copy the prefix to the beginning of the data
    memcpy(data, prefix, prefix_len);

    // Generate the random data after the prefix
    get_random_bytes(data + prefix_len, size - prefix_len);
}

void TestWriteToRingBuffer(void) {
    size_t test_data_size = 200; // Example size, can be any value
    char test_data[test_data_size];

    // Generate random data
    GenerateRandomData(test_data, test_data_size);

    // Write the random data to the ring buffer
    WriteToRingBuffer(egressBuffer, test_data, test_data_size);

    printk(KERN_INFO "Random data of size %zu written to the ring buffer.\n", test_data_size);
}