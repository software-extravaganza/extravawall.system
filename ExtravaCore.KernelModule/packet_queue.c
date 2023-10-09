#include "packet_queue.h"

// Deep copy of a PendingPacket to another
bool deep_copy_packet(PendingPacket *dest, PendingPacket *src) {
    if (!dest || !src) return false;

    dest->skb = src->skb; // Shallow copy pointer
    dest->data_len = src->data_len;
    dest->processed = src->processed;
    dest->decision = src->decision;

    // Deep copy data if it's non-NULL and length is greater than zero
    if (src->data && src->data_len > 0) {
        dest->data = kmalloc(src->data_len, GFP_KERNEL);
        if (!dest->data) return false; // Memory allocation failure
        memcpy(dest->data, src->data, src->data_len);
    } else {
        dest->data = NULL;
    }

    return true;
}

bool pq_add_packet(PacketQueue *queue, PendingPacket *packet) {
    if (!queue || !packet) return false; 

    if (pq_is_full(queue)) {
        return false; // Queue is full
    }

    if (!deep_copy_packet(&queue->packets[queue->end], packet)) {
        return false; // Failed to deep copy packet data
    }

    // Move 'end' to the next position in a circular manner
    queue->end = (queue->end + 1) % MAX_PENDING_PACKETS;

    return true;
}

struct sk_buff* pq_pop_packet(PacketQueue *queue) {
    if (queue->start == queue->end) {
        return NULL; // Queue is empty
    }

    struct sk_buff* skb = queue->packets[queue->start].skb;
    //free_pending_packet(&queue->packets[queue->start]);

    queue->start = (queue->start + 1) % MAX_PENDING_PACKETS;
    return skb;
}

PendingPacket* pq_peek_packet(PacketQueue *queue) {
    if (pq_is_empty(queue)) {
        return NULL; // Queue is empty
    }
    return &queue->packets[queue->start];
}

bool pq_remove_packet(PacketQueue *queue, PendingPacket *packet) {
    int i, j;

    // Search for the packet in the queue
    for (i = queue->start; i != queue->end; i = (i + 1) % MAX_PENDING_PACKETS) {
        if (&queue->packets[i] == packet) {
            break;
        }
    }

    // If not found, return false
    if (&queue->packets[i] != packet) {
        return false;
    }

    free_pending_packet(&queue->packets[i]);

    // Shift all subsequent packets to fill the gap
    for (j = i; j != queue->end; j = (j + 1) % MAX_PENDING_PACKETS) {
        int nextIndex = (j + 1) % MAX_PENDING_PACKETS;
        queue->packets[j] = queue->packets[nextIndex];
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
