#ifndef _PACKET_QUEUE_H_
#define _PACKET_QUEUE_H_

#include <linux/skbuff.h>
#include "data_structures.h"

#define MAX_PENDING_PACKETS 100

typedef struct {
    PendingPacketRoundTrip packetTrips[MAX_PENDING_PACKETS];
    int start;
    int end;
    wait_queue_head_t waitQueue; 
} PacketQueue;

bool pq_add_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip);
bool pq_remove_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip);
PendingPacketRoundTrip* pq_peek_packetTrip(PacketQueue *queue);
PendingPacketRoundTrip* pq_pop_packet(PacketQueue *queue);
void pq_initialize(PacketQueue *queue);
bool pq_is_full(PacketQueue *queue);
bool pq_is_empty(PacketQueue *queue);
bool deep_copy_packet(PendingPacketRoundTrip *dest, const PendingPacketRoundTrip *src);

#endif // _PACKET_QUEUE_H_
