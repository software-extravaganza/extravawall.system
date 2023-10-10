#include "packet_queue.h"

// Deep copy of a PendingPacket to another
bool deep_copy_packet(PendingPacketRoundTrip *dest, const PendingPacketRoundTrip *src) {
    if (!dest || !src || !src->packet || !src->packet->header || !src->packet->data) return false;

    dest->packet = create_pending_packet(src->packet->skb);
    if (!dest->packet) {
        return false; // Failed to allocate memory for the packet
    }

    dest->packet->skb = src->packet->skb; // Shallow copy pointer
    dest->packet->headerProcessed = src->packet->headerProcessed;
    dest->packet->dataProcessed = src->packet->dataProcessed;
    dest->decision = src->decision;

    // Allocate and copy the header
    dest->packet->header = create_packet_header(src->packet->header->version, src->packet->header->data_length);
    if (!dest->packet->header) {
        return false; // Failed to allocate memory for the header
    }

    // Allocate and copy the data
    dest->packet->data = kmalloc(src->packet->header->data_length, GFP_KERNEL);
    if (!dest->packet->data) {
        free_packet_header(dest->packet->header);
        return false; // Memory allocation failure
    }

    memcpy(dest->packet->data, src->packet->data, src->packet->header->data_length);

    return true;
}

bool pq_add_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip) {
    if (!queue || !packetTrip || !packetTrip->packet || !packetTrip->packet->header) return false; 

    if (pq_is_full(queue)) {
        return false; // Queue is full
    }

    if (!deep_copy_packet(&queue->packetTrips[queue->end], packetTrip)) {
        return false; // Failed to deep copy packet data
    }

    // Move 'end' to the next position in a circular manner
    queue->end = (queue->end + 1) % MAX_PENDING_PACKETS;

    return true;
}

PendingPacketRoundTrip* pq_pop_packet(PacketQueue *queue) {
    if (queue->start == queue->end) {
        return NULL; // Queue is empty
    }

    PendingPacketRoundTrip* poppedTrip = &queue->packetTrips[queue->start];
    //free_pending_packet(&queue->packets[queue->start]);

    queue->start = (queue->start + 1) % MAX_PENDING_PACKETS;
    return poppedTrip;
}

PendingPacketRoundTrip* pq_peek_packetTrip(PacketQueue *queue) {
    if (pq_is_empty(queue)) {
        return NULL; // Queue is empty
    }
    return &queue->packetTrips[queue->start];
}

bool pq_remove_packetTrip(PacketQueue *queue, PendingPacketRoundTrip *packetTrip) {
    int i, j;

    // Search for the packet in the queue
    for (i = queue->start; i != queue->end; i = (i + 1) % MAX_PENDING_PACKETS) {
        if (&queue->packetTrips[i] == packetTrip) {
            break;
        }
    }

    // If not found, return false
    if (&queue->packetTrips[i] != packetTrip) {
        return false;
    }

    free_pending_packetTrip(&queue->packetTrips[i]);

    // Shift all subsequent packets to fill the gap
    for (j = i; j != queue->end; j = (j + 1) % MAX_PENDING_PACKETS) {
        int nextIndex = (j + 1) % MAX_PENDING_PACKETS;
        queue->packetTrips[j] = queue->packetTrips[nextIndex];
    }

    // Adjust the end position
    queue->end = (queue->end - 1 + MAX_PENDING_PACKETS) % MAX_PENDING_PACKETS;

    return true;
}

void pq_initialize(PacketQueue *queue) {
    memset(queue, 0, sizeof(PacketQueue));
    init_waitqueue_head(&queue->waitQueue);
}

bool pq_is_full(PacketQueue *queue) {
    return ((queue->end + 1) % MAX_PENDING_PACKETS == queue->start);
}

bool pq_is_empty(PacketQueue *queue) {
    return (queue->start == queue->end);
}
