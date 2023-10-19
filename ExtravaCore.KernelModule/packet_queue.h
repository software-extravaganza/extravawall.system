#ifndef _PACKET_QUEUE_H_
#define _PACKET_QUEUE_H_

#include <linux/skbuff.h>
#include "data_structures.h"

#define MAX_PENDING_PACKETS 100

typedef struct kfifo PacketQueue;

bool PacketQueuePush(PacketQueue *queue, PendingPacketRoundTrip *packetTrip);
PendingPacketRoundTrip* PacketQueuePeek(PacketQueue *queue);
PendingPacketRoundTrip* PacketQueuePop(PacketQueue *queue);
void PacketQueueInitialize(PacketQueue *queue);
bool PacketQueueIsFull(PacketQueue *queue);
bool PacketQueueIsEmpty(PacketQueue *queue);
void PacketQueueCleanup(PacketQueue *queue);
int PacketQueueLength(PacketQueue *queue);

#endif // _PACKET_QUEUE_H_
