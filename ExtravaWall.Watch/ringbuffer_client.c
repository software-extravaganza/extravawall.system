#include "ringbuffer_client.h"

//sudo setcap "cap_dac_override+ep cap_sys_rawio+ep" <filename>
//getcap <filename> 
//capsh --caps="cap_dac_override+ep cap_sys_rawio+ep"
//capsh --print
//setenforce 0
//getenforce

int fd = -1;
RingBuffer *buffer = NULL;

void* open_shared_memory(const char* path) {
    fd = open(path, O_RDWR | O_SYNC, 0600);
    if (fd < 0) {
        perror("Failed to open shared memory");
        return NULL;
    }

    buffer = (RingBuffer*) mmap(NULL, sizeof(RingBuffer), PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
    if (buffer == MAP_FAILED) {
        perror("Failed to map shared memory");
        close(fd);
        return NULL;
    }

    // assert(ret == sizeof(buf));
    // assert(!memcmp(buf, message, strlen(message)));
    return buffer;
}

static void print_hex(const char *data, size_t len) {
    int i;
    printf("Hex data:");
    for (i = 0; i < len; ++i) {
        printf( "%02x ", (unsigned char)data[i]);
    }
    printf("\n"); // Newline after the entire hex data is printed
}

RingBufferSlot read_slot(int index) {
    if (buffer == NULL) {
        fprintf(stderr, "Shared memory not initialized\n");
        exit(EXIT_FAILURE);
    }

    RingBufferSlot *readslot = &buffer->Slots[index];
    print_hex(readslot->Data, sizeof(RingBufferSlot) / 160);
    return buffer->Slots[index];
}

void write_slot(int index, RingBufferSlot slot) {
    if (buffer == NULL) {
        fprintf(stderr, "Shared memory not initialized\n");
        exit(EXIT_FAILURE);
    }
    buffer->Slots[index] = slot;
}

void close_shared_memory() {
    if (buffer) {
        munmap(buffer, sizeof(RingBuffer));
        buffer = NULL;
    }
    if (fd >= 0) {
        close(fd);
        fd = -1;
    }
}

// Add additional functions as needed...
