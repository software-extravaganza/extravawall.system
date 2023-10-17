#ifndef _PACKET_QUEUE_H_
#define _PACKET_QUEUE_H_

#include <linux/skbuff.h>
#include "data_structures.h"

#define MAX_PENDING_PACKETS 100

typedef struct kfifo PacketQueue;

bool pq_push_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip);
PendingPacketRoundTrip* pq_peek_packetTrip(PacketQueue *queue);
PendingPacketRoundTrip* pq_pop_packetTrip(PacketQueue *queue);
void pq_initialize(PacketQueue *queue);
bool pq_is_full(PacketQueue *queue);
bool pq_is_empty(PacketQueue *queue);
bool deep_copy_packet(PendingPacketRoundTrip *dest, const PendingPacketRoundTrip *src);
void pq_cleanup(PacketQueue *queue);
int pq_len_packetTrip(PacketQueue *queue);

#endif // _PACKET_QUEUE_H_
