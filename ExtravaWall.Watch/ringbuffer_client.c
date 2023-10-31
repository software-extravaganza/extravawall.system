#include "ringbuffer_client.h"

//sudo setcap "cap_dac_override+ep cap_sys_rawio+ep" <filename>
//getcap <filename> 
//capsh --caps="cap_dac_override+ep cap_sys_rawio+ep"
//capsh --print
//setenforce 0
//getenforce

//int fd = -1;
//char *buffer = NULL;

long get_page_size();
size_t page_align(size_t size);

__s32 get_size_for_slot_header_status() {
    return SLOT_HEADER_STATUS_SIZE;
}

__s32 get_size_for_slot_header_total_data_size() {
    return SLOT_HEADER_TOTAL_DATA_SIZE_SIZE;
}

__s32 get_size_for_slot_header_current_data_size() {
    return SLOT_HEADER_CURRENT_DATA_SIZE_SIZE;
}

__s32 get_size_for_slot_header_sequence_number() {
    return SLOT_HEADER_SEQUENCE_NUMBER_SIZE;
}

__s32 get_size_for_slot_header_clearance_start_index() {
    return SLOT_HEADER_CLEARANCE_START_INDEX_SIZE;
}

__s32 get_size_for_slot_header_clearance_end_index() {
    return SLOT_HEADER_CLEARANCE_END_INDEX_SIZE;
}

__s32 get_size_for_slot_header() {
    return SLOT_HEADER_SIZE;
}

__s32 get_size_for_slot_data() {
    return SLOT_DATA_SIZE;
}

__s32 get_size_for_slot() {
    return SLOT_SIZE;
}

__s32 get_size_for_ring_buffer_header_status() {
    return RING_BUFFER_HEADER_STATUS_SIZE;
}

__s32 get_size_for_ring_buffer_header_position() {
    return RING_BUFFER_HEADER__POSITION_SIZE;
}

__s32 get_size_for_ring_buffer_header() {
    return RING_BUFFER_HEADER_SIZE;
}

__u32 get_size_for_ring_buffer_data() {
    return RING_BUFFER_DATA_SIZE;
}

__u32 get_size_for_ring_buffer() {
    return RING_BUFFER_SIZE;
}

__u32 get_size_for_duplex_ring_buffer() {
    return DUPLEX_RING_BUFFER_SIZE;
}

__u32 get_size_for_duplex_ring_buffer_aligned() {
    return DUPLEX_RING_BUFFER_ALIGNED_SIZE;
}

__u16 get_number_of_slots() {
    return NUM_SLOTS;
}

__u16 get_size_for_slot_header_id(){
    return SLOT_HEADER_ID_SIZE;
}


// void* open_shared_memory(const char* path) {
//     fd = open(path, O_RDWR | O_SYNC, 0600);
//     if (fd < 0) {
//         perror("Failed to open shared memory");
//         return NULL;
//     }

//     size_t buffer_size = DUPLEX_RING_BUFFER_ALIGNED_SIZE;
//     buffer = (char*) mmap(NULL, buffer_size, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
//     if (buffer == MAP_FAILED) {
//         perror("Failed to map shared memory");
//         close(fd);
//         return NULL;
//     }

//     // assert(ret == sizeof(buf));
//     // assert(!memcmp(buf, message, strlen(message)));
//     return buffer;
// }

// static void print_hex(const char *data, size_t len) {
//     int i;
//     printf("Hex data:");
//     for (i = 0; i < len; ++i) {
//         printf( "%02x ", (unsigned char)data[i]);
//     }
//     printf("\n"); // Newline after the entire hex data is printed
// }

// RingBufferSlot read_slot(int index) {
//     if (buffer == NULL) {
//         fprintf(stderr, "Shared memory not initialized\n");
//         exit(EXIT_FAILURE);
//     }

//     RingBufferSlot *readslot = &buffer->Slots[index];
//     print_hex(readslot->Data, sizeof(RingBufferSlot) / 160);
//     return buffer->Slots[index];
// }

// void write_slot(int index, RingBufferSlot slot) {
//     if (buffer == NULL) {
//         fprintf(stderr, "Shared memory not initialized\n");
//         exit(EXIT_FAILURE);
//     }
//     buffer->Slots[index] = slot;
// }

// void close_shared_memory() {
//     if (buffer) {
//         size_t buffer_size = page_align(sizeof(RingBuffer));
//         munmap(buffer, buffer_size);
//         buffer = NULL;
//     }
//     if (fd >= 0) {
//         close(fd);
//         fd = -1;
//     }
// }

// long get_page_size() {
//     return sysconf(_SC_PAGESIZE);
// }

// size_t page_align(size_t size) {
//     long page_size = get_page_size();
//     return (size + (page_size - 1)) & ~(page_size - 1);
// }

// Add additional functions as needed...
