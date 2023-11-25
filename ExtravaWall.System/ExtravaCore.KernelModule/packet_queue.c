#include "packet_queue.h"

// Constants
#define MAX_PENDING_PACKETS_SIZE (MAX_PENDING_PACKETS * sizeof(PendingPacketRoundTrip *))
#define MESSAGE_INITIALIZATION_ERROR "Failed to initialize packet queue."
#define MESSAGE_PEEKING_PACKET "Peeking packet trip from queue. Current queue length: %d"
#define MESSAGE_PEEK "Peek Length: %d, Peeked: %d"
#define MESSAGE_NULL_PACKET "Pop returning NULL packet trip."

// Public function implementations
PacketQueue* PacketQueueCreate(void) {
    PacketQueue *queue = kzalloc(sizeof(PacketQueue), GFP_KERNEL);
    int initResponse = PacketQueueInitialize(queue);
    if(initResponse < 0){
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        safeFree(queue);
        return NULL;
    }
    
    return queue;
}

int PacketQueueInitialize(PacketQueue *queue) {
    if (!queue) {
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        return -1;
    }

    queue->semaphore = kzalloc(sizeof(struct semaphore), GFP_KERNEL);
    if (!queue->semaphore) {
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        safeFree(queue);
        return -2;
    }

    int queueResult = kfifo_alloc(&queue->queue, MAX_PENDING_PACKETS_SIZE, GFP_KERNEL);
    if (queueResult) {
        LOG_ERROR(MESSAGE_INITIALIZATION_ERROR);
        kfifo_free(&queue->queue);
        safeFree(queue->semaphore);
        safeFree(queue);
        return -4;
    }

    sema_init(queue->semaphore, 1);
    return 0;
}

static void lock(PacketQueue *queue){
    //down(queue->semaphore);
}

static void unlock(PacketQueue *queue){
    //up(queue->semaphore);
}

#define LOCK_WHILE(queue, operation) \
    lock(queue); \
    operation; \
    unlock(queue)

#define LOCK_WHILE_RETURN_INT(queue, operation) \
   (lock(queue), \
    ({ int value = operation; unlock(queue); value; }))

#define LOCK_WHILE_RETURN_BOOL(queue, operation) \
   (lock(queue), \
    ({ bool value = operation; unlock(queue); value; }))

bool PacketQueueIsFull(PacketQueue *queue) {
    lock(queue);
    bool result = kfifo_is_full(&queue->queue);
    unlock(queue);
    return result;
}

bool PacketQueueIsEmpty(PacketQueue *queue) {
    lock(queue);
    bool result = kfifo_is_empty(&queue->queue);
    unlock(queue);
    return result;
}

void PacketQueueCleanup(PacketQueue *queue) {
    if (!queue) {
        return;
    }

    PacketQueueEmpty(queue);
    // Free the internal buffer of the kfifo
    kfifo_free(&queue->queue);

    // Free the dynamically allocated semaphore
    if (queue->semaphore) {
        kfree(queue->semaphore);
        queue->semaphore = NULL;  // Set to NULL to prevent double freeing
    }
}

void PacketQueueEmpty(PacketQueue *queue) {
    PendingPacketRoundTrip *packet;
    LOCK_WHILE(queue, {
        while (kfifo_out(&queue->queue, &packet, sizeof(packet))) {
            if (packet) {
                FreePendingPacketTrip(packet);
                packet = NULL;
            }
        }
    });
}

// Private function implementations
bool PacketQueuePush(PacketQueue *queue, PendingPacketRoundTrip *packet) {
    if(!packet){
        LOG_DEBUG_PACKET("Packet is NULL");
        return false;
    }

    if(PacketQueueIsFull(queue)){
        LOG_DEBUG_PACKET("Queue is full");
        return false;
    }
    
    return LOCK_WHILE_RETURN_BOOL(queue, kfifo_in(&queue->queue, &packet, sizeof(packet)));
}

int PacketQueueLength(PacketQueue *queue) {
    if (!queue) {
        return 0; // or handle the error appropriately
    }
    return LOCK_WHILE_RETURN_INT(queue, kfifo_len(&queue->queue) / sizeof(PendingPacketRoundTrip *));
}

PendingPacketRoundTrip* PacketQueuePeek(PacketQueue *queue) {
    PendingPacketRoundTrip *packet = NULL;
    int length = PacketQueueLength(queue);
    LOG_DEBUG_PACKET(MESSAGE_PEEKING_PACKET, length);
    if(length > 0){
        int response = LOCK_WHILE_RETURN_INT(queue, {
            kfifo_peek(&queue->queue, &packet)
        });

        if(response < 0){
            LOG_DEBUG_PACKET("Failed to peek packet");
            return NULL;
        }

        if (packet) {
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

    int response = LOCK_WHILE_RETURN_INT(queue, {
        kfifo_out(&queue->queue, &packet, sizeof(packet))
    });

    if(response < 0){
        LOG_DEBUG_PACKET("Failed to pop packet");
        return NULL;
    }

    if (packet){
        return packet;
    }

    LOG_DEBUG_PACKET(MESSAGE_NULL_PACKET);
    return NULL;
}
