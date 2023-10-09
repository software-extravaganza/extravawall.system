#ifndef _PACKET_QUEUE_H_
#define _PACKET_QUEUE_H_

#include <linux/skbuff.h>
#include "data_structures.h"

#define MAX_PENDING_PACKETS 100

typedef struct {
    PendingPacket packets[MAX_PENDING_PACKETS];
    int start;
    int end;
    wait_queue_head_t waitQueue; 
} PacketQueue;

bool pq_add_packet(PacketQueue *queue, PendingPacket *packet);
bool pq_remove_packet(PacketQueue *queue, PendingPacket *packet);
PendingPacket* pq_peek_packet(PacketQueue *queue);
struct sk_buff* pq_pop_packet(PacketQueue *queue);
void pq_initialize(PacketQueue *queue);
bool pq_is_full(PacketQueue *queue);
bool pq_is_empty(PacketQueue *queue);

#endif // _PACKET_QUEUE_H_
