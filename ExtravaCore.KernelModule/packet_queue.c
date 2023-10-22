#include "packet_queue.h"

// Constants
#define MAX_PENDING_PACKETS_SIZE (MAX_PENDING_PACKETS * sizeof(PendingPacketRoundTrip *))
#define MESSAGE_INITIALIZATION_ERROR "Failed to initialize packet queue."
#define MESSAGE_PEEKING_PACKET "Peeking packet trip from queue. Current queue length: %d"
#define MESSAGE_PEEK "Peek Length: %d, Peeked: %d"
#define MESSAGE_NULL_PACKET "Pop returning NULL packet trip."
DEFINE_SPINLOCK(fifo_lock);

// Public function implementations
void PacketQueueInitialize(PacketQueue *queue) {
    int result = kfifo_alloc(queue, MAX_PENDING_PACKETS_SIZE, GFP_KERNEL);
    if (result) {
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        // Handle error
    }
}

bool PacketQueueIsFull(PacketQueue *queue) {
    spin_lock(&fifo_lock);
    bool result = kfifo_is_full(queue);
    spin_unlock(&fifo_lock);
    return result;
}

bool PacketQueueIsEmpty(PacketQueue *queue) {
    spin_lock(&fifo_lock);
    bool result = kfifo_is_empty(queue);
    spin_unlock(&fifo_lock);
    return result;
}

void PacketQueueCleanup(PacketQueue *queue) {
    PacketQueueEmpty(queue);
    kfifo_free(queue);
}


void PacketQueueEmpty(PacketQueue *queue) {
    spin_lock(&fifo_lock);
    PendingPacketRoundTrip *packet;
    while (kfifo_out(queue, &packet, sizeof(packet))) {
        if (packet) {
            FreePendingPacketTrip(packet);
            packet = NULL;
        }
    }
    spin_unlock(&fifo_lock);
}

// Private function implementations
bool PacketQueuePush(PacketQueue *queue, PendingPacketRoundTrip *packet) {
    spin_lock(&fifo_lock);
    bool result = kfifo_in(queue, &packet, sizeof(packet));
    spin_unlock(&fifo_lock);
    return result;
}

int PacketQueueLength(PacketQueue *queue) {
    spin_lock(&fifo_lock);
    int result = kfifo_len(queue) / sizeof(PendingPacketRoundTrip *);
    spin_unlock(&fifo_lock);
    return result;
}

PendingPacketRoundTrip* PacketQueuePeek(PacketQueue *queue) {
    PendingPacketRoundTrip *packet = NULL;
    int length = PacketQueueLength(queue);
    LOG_DEBUG_PACKET(MESSAGE_PEEKING_PACKET, length);
    if(length > 0){
        spin_lock(&fifo_lock);
        int peekResult = kfifo_peek(queue, &packet);
        spin_unlock(&fifo_lock);
        if (peekResult) {
            LOG_DEBUG_PACKET(MESSAGE_PEEK, length, 1); // 1 indicates success in peeking
            return packet;
        }
    }

    return NULL;
}

PendingPacketRoundTrip* PacketQueuePop(PacketQueue *queue) {
    PendingPacketRoundTrip *packet = NULL;
    if(PacketQueueLength(queue) <= 0){
        LOG_DEBUG_PACKET("Queue is empty");
        return NULL;
    }
    spin_lock(&fifo_lock);
    int popResult = kfifo_out(queue, &packet, sizeof(PendingPacketRoundTrip));
    spin_unlock(&fifo_lock);
    if (popResult){
        return packet;
    }

    LOG_DEBUG_PACKET(MESSAGE_NULL_PACKET);
    return NULL;
}
