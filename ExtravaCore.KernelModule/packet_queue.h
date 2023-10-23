#ifndef _PACKET_QUEUE_H_
#define _PACKET_QUEUE_H_

#include <linux/skbuff.h>
#include <linux/kfifo.h>
#include <linux/semaphore.h>
#include "data_structures.h"
#include "data_factories.h"

#define MAX_PENDING_PACKETS 100

typedef struct {
    struct kfifo *queue;
    struct semaphore *semaphore;
} PacketQueue;

bool PacketQueuePush(PacketQueue *queue, PendingPacketRoundTrip *packetTrip);
PendingPacketRoundTrip* PacketQueuePeek(PacketQueue *queue);
PendingPacketRoundTrip* PacketQueuePop(PacketQueue *queue);
PacketQueue* PacketQueueCreate(void);
bool PacketQueueIsFull(PacketQueue *queue);
bool PacketQueueIsEmpty(PacketQueue *queue);
void PacketQueueCleanup(PacketQueue *queue);
void PacketQueueEmpty(PacketQueue *queue);
int PacketQueueLength(PacketQueue *queue);

#endif // _PACKET_QUEUE_H_
