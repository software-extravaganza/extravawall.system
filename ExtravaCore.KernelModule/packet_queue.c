#include "packet_queue.h"
#include <linux/kfifo.h>

// Constants
#define MAX_PENDING_PACKETS_SIZE (MAX_PENDING_PACKETS * sizeof(PendingPacketRoundTrip *))
#define MESSAGE_INITIALIZATION_ERROR "Failed to initialize packet queue."
#define MESSAGE_PEEKING_PACKET "Peeking packet trip from queue. Current queue length: %d"
#define MESSAGE_PEEK "Peek Length: %d, Peeked: %d"
#define MESSAGE_NULL_PACKET "Pop returning NULL packet trip."

// Public function implementations
void PacketQueueInitialize(PacketQueue *queue) {
    int result = kfifo_alloc(queue, MAX_PENDING_PACKETS_SIZE, GFP_KERNEL);
    if (result) {
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        // Handle error
    }
}

bool PacketQueueIsFull(PacketQueue *queue) {
    return kfifo_is_full(queue);
}

bool PacketQueueIsEmpty(PacketQueue *queue) {
    return kfifo_is_empty(queue);
}

void PacketQueueCleanup(PacketQueue *queue) {
    PacketQueueEmpty(queue);
    kfifo_free(queue);
}


void PacketQueueEmpty(PacketQueue *queue) {
    PendingPacketRoundTrip *packet;
    while (kfifo_out(queue, &packet, sizeof(packet))) {
        if (packet) {
            FreePendingPacketTrip(packet);
            packet = NULL;
        }
    }
}

// Private function implementations
bool PacketQueuePush(PacketQueue *queue, PendingPacketRoundTrip *packet) {
    return kfifo_in(queue, &packet, sizeof(packet));
}

int PacketQueueLength(PacketQueue *queue) {
    return kfifo_len(queue) / sizeof(PendingPacketRoundTrip *);
}

PendingPacketRoundTrip* PacketQueuePeek(PacketQueue *queue) {
    PendingPacketRoundTrip *packet = NULL;
    int length = PacketQueueLength(queue);
    LOG_DEBUG_ICMP(MESSAGE_PEEKING_PACKET, length);
    if(length > 0){
        if (kfifo_peek(queue, &packet)) {
            LOG_DEBUG_ICMP(MESSAGE_PEEK, length, 1); // 1 indicates success in peeking
            return packet;
        }
    }

    return NULL;
}

PendingPacketRoundTrip* PacketQueuePop(PacketQueue *queue) {
    PendingPacketRoundTrip *packet = NULL;
    if(PacketQueueLength(queue) <= 0){
        LOG_DEBUG_ICMP("Queue is empty");
        return NULL;
    }

    if (kfifo_out(queue, &packet, sizeof(PendingPacketRoundTrip))){
        return packet;
    }

    LOG_DEBUG_ICMP(MESSAGE_NULL_PACKET);
    return NULL;
}
