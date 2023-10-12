#include "packet_queue.h"
#include <linux/kfifo.h>


void pq_initialize(PacketQueue *queue) {
    int ret = kfifo_alloc(queue, MAX_PENDING_PACKETS * sizeof(PendingPacketRoundTrip *), GFP_KERNEL);
    if (ret) {
        LOG_ERR("Failed to initialize packet queue.");
        // Handle error
    }
}

bool pq_add_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip) {
    return kfifo_in(queue, &packetTrip, sizeof(packetTrip));
}

bool pq_remove_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip) {
    return kfifo_out(queue, &packetTrip, sizeof(packetTrip));
}

PendingPacketRoundTrip* pq_peek_packetTrip(PacketQueue *queue) {
    PendingPacketRoundTrip *packetTrip;
    kfifo_peek(queue, &packetTrip); 
    return packetTrip;
}

PendingPacketRoundTrip* pq_pop_packetTrip(PacketQueue *queue) {
    PendingPacketRoundTrip *packetTrip;
    if (kfifo_out(queue, &packetTrip, sizeof(packetTrip)))
        return packetTrip;
    else
        return NULL;
}

bool pq_is_full(PacketQueue *queue) {
    return kfifo_is_full(queue);
}

bool pq_is_empty(PacketQueue *queue) {
    return kfifo_is_empty(queue);
}

void pq_cleanup(PacketQueue *queue) {
    PendingPacketRoundTrip *packetTrip;
    while (kfifo_out(queue, &packetTrip, sizeof(packetTrip))) {
        if (packetTrip) {
            free_pending_packetTrip(packetTrip);
        }
    }
    kfifo_free(queue);
}