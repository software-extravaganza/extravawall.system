#include "ringbuffer_client.h"

int fd = -1;
RingBuffer *buffer = NULL;

void* open_shared_memory(const char* path) {
    fd = open(path, O_RDWR);
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
    return buffer;
}

RingBufferSlot read_slot(int index) {
    if (buffer == NULL) {
        fprintf(stderr, "Shared memory not initialized\n");
        exit(EXIT_FAILURE);
    }
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
