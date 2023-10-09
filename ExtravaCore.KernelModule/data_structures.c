/*
 * This module provides data structures and utility functions for handling packets
 * within netfilter hooks and communication with userspace. It offers functionalities
 * for packet buffering, packet decisions, and IP address conversions.
 */
#include "data_structures.h"

#define IP_BUFFER_SIZE 16  // for IPv4, 16 chars: 255.255.255.255 + a NULL terminator
#define ZERO_MEMORY_BEFORE_FREE 1  // 1 to zero out memory before freeing, 0 otherwise

static DecisionReasonInfo reason_infos[] = {
    { MEMORY_FAILURE,  " Memory allocation failure" },
    { BUFFER_FULL,     " Dropped a packet because the packet buffer is full" },
    { TIMEOUT,         " Dropped a packet that timed out" },
    { ERROR,           " Dropped a packet because of an error" },
    { USER_ACCEPT,     " Decided to accept a packet" },
    { USER_DROP,       " Decided to drop a packet" }
};

/* 
 * Zeros out the memory of a given pointer only if the ZERO_MEMORY_BEFORE_FREE is set to 1
 */
void conditional_memory_zero(void* ptr, size_t size) {
    if (ZERO_MEMORY_BEFORE_FREE) {
        memset(ptr, 0, size);
    }
}

/* 
 * Frees the memory occupied by a packet header 
 */
void free_packet_header(PacketHeader *header) {
     if (unlikely(!header)) return;

    conditional_memory_zero(header, sizeof(PacketHeader));  // Clear the memory
    kfree(header);
}

/* 
 * Creates and returns a new packet header 
 */
PacketHeader* create_packet_header(unsigned long pkt_id, size_t length) {
    PacketHeader *header = kzalloc(sizeof(PacketHeader), GFP_KERNEL);
    if (unlikely(!header)) {
        LOG_ERR("Failed to allocate memory for PacketHeader.");
        return NULL;
    }

    header->pkt_id = pkt_id;
    header->length = length;

    return header;
}

/* 
 * Frees the memory occupied by a pending packet 
 */
void free_pending_packet(PendingPacket *packet) {
    if (unlikely(!packet)) return;

    if (packet->data) {
        conditional_memory_zero(packet->data, packet->data_len); // Clear the memory
        kfree(packet->data);
    }
    conditional_memory_zero(packet, sizeof(PendingPacket));  // Clear the memory
    kfree(packet);
}


/* 
 * Creates and returns a new pending packet 
 */
PendingPacket* create_pending_packet(struct sk_buff *skb) {
    if (unlikely(!skb)) {
        LOG_ERR("Null socket buffer (skb) provided to create_pending_packet.");
        return NULL;
    }

    PendingPacket *packet = kzalloc(sizeof(PendingPacket), GFP_KERNEL);
    if (unlikely(!packet)) {
        LOG_ERR("Failed to allocate memory for PendingPacket.");
        return NULL;
    }

    packet->data_len = skb->len;
    packet->data = kmalloc(packet->data_len, GFP_KERNEL);  // Using kmalloc directly
    if (unlikely(!packet->data)) {
        LOG_ERR("Failed to allocate memory for packet data.");
        kfree(packet);
        return NULL;
    }

    skb_copy_bits(skb, 0, packet->data, packet->data_len);
    packet->processed = false;
    packet->decision = UNDECIDED;

    return packet;
}

/* 
 * Converts an IP address to a human-readable format and stores it in the provided buffer 
 */
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len) {
    if (unlikely(!buffer)) {
        LOG_ERR("Buffer provided is NULL.");
        return;
    }

    if (unlikely(buf_len < IP_BUFFER_SIZE)) {  // Ensure enough space for null terminator
        LOG_ERR("Buffer size is incorrect. Expected at least %d, got %zu.", IP_BUFFER_SIZE, buf_len);
        return;
    }

    snprintf(buffer, buf_len, "%pI4", &ip);
    buffer[IP_BUFFER_SIZE-1] = '\0';  // Ensure null termination (safety measure)
}


char* get_reason_text(DecisionReason reason) {
    for (size_t i = 0; i < sizeof(reason_infos) / sizeof(reason_infos[0]); i++) {
        if (reason_infos[i].reason == reason) {
            return reason_infos[i].text;
        }
    }
    return " Unknown reason";
}