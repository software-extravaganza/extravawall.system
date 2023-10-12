#include "data_structures.h"

#define IP_BUFFER_SIZE 16  // for IPv4, 16 chars: 255.255.255.255 + a NULL terminator
#define ZERO_MEMORY_BEFORE_FREE 1  // 1 to zero out memory before freeing, 0 otherwise
#define PACKET_HEADER_VERSION 1 // Version of the packet header
#define ARRAY_SIZE_REASON(arr) (sizeof(arr) / sizeof((arr)[0]))

static DecisionReasonInfo reason_infos[] = {
    { MEMORY_FAILURE_PACKET,  " Memory allocation failure for packet" },
    { MEMORY_FAILURE_PACKET_HEADER,  " Memory allocation failure for packet header" },
    { BUFFER_FULL,     " Dropped a packet because the packet buffer is full" },
    { TIMEOUT,         " Dropped a packet that timed out" },
    { ERROR,           " Dropped a packet because of an error" },
    { USER_ACCEPT,     " Decided to accept a packet" },
    { USER_DROP,       " Decided to drop a packet" }
};

void safe_kfree(void* ptr) {
    if (!ptr)
        return;
    kfree(ptr);
}


/* 
 * Zeros out the memory of a given pointer only if the ZERO_MEMORY_BEFORE_FREE is set to 1
 */
void conditional_memory_zero(void* ptr, size_t size) {
    if (!ptr)
        return;
    if (ZERO_MEMORY_BEFORE_FREE) {
        memset(ptr, 0, size);
    }
}

/* 
 * Frees the memory occupied by a packet header 
 */
void free_packet_header(PacketHeader *header) {
    if (!header)
        return;

    kfree(header);
}

/* 
 * Creates and returns a new packet header 
 */
PacketHeader* create_packet_header(u32 version, size_t data_length) {
    PacketHeader *header = kzalloc(sizeof(PacketHeader), GFP_KERNEL);
    if (!header) {
        LOG_ERR("Failed to allocate memory for PacketHeader.");
        return NULL;
    }

    LOG_DEBUG("Successfully created packet header.");
    
    header->version = version;
    header->data_length = data_length;

    return header;
}

/* 
 * Frees the memory occupied by a pending packet 
 */
void free_pending_packetTrip(PendingPacketRoundTrip *packetTrip) {
    if (!packetTrip)
        return;

    if (packetTrip->packet) {
        free_pending_packet(packetTrip->packet);
        packetTrip->packet = NULL;  // Set the pointer to NULL after freeing
    }

    if (packetTrip->responsePacket) {
        free_pending_packet(packetTrip->responsePacket);
        packetTrip->responsePacket = NULL;  // Set the pointer to NULL after freeing
    }

    conditional_memory_zero(packetTrip, sizeof(PendingPacketRoundTrip));
    kfree(packetTrip);
}

/* 
 * Creates and returns a new pending packet round trip 
 */
PendingPacketRoundTrip* allocate_pending_packet_trip(void) {
    PendingPacketRoundTrip *packetTrip = kzalloc(sizeof(PendingPacketRoundTrip), GFP_KERNEL);
    if (!packetTrip) {
        LOG_DEBUG("Failed to allocate memory for PendingPacketRoundTrip.");
    }
    return packetTrip;
}

bool setup_pending_packet_trip_main_packet(PendingPacketRoundTrip *packetTrip, struct sk_buff *skb) {
    packetTrip->packet = create_pending_packet(skb);
    if (!packetTrip->packet) {
        LOG_DEBUG("Failed to create packet.");
        return false;
    }
    return true;
}

bool setup_pending_packet_trip_response_packet(PendingPacketRoundTrip *packetTrip) {
    packetTrip->responsePacket = create_pending_packet(NULL);
    if (!packetTrip->responsePacket) {
        LOG_DEBUG("Failed to create response packet.");
        return false;
    }
    return true;
}

PendingPacketRoundTrip* create_pending_packetTrip(struct sk_buff *skb) {
    if (!skb) {
        LOG_DEBUG("Null socket buffer (skb) provided.");
        return NULL;
    }

    PendingPacketRoundTrip *packetTrip = allocate_pending_packet_trip();
    if (!packetTrip) {
        return NULL;
    }

    packetTrip->decision = UNDECIDED;
    init_completion(&packetTrip->packet_processed);

    if (!setup_pending_packet_trip_main_packet(packetTrip, skb) ||
        !setup_pending_packet_trip_response_packet(packetTrip)) {
        free_pending_packetTrip(packetTrip);
        return NULL;
    }

    LOG_DEBUG("Successfully created PendingPacketRoundTrip.");
    return packetTrip;
}

/* 
 * Frees the memory occupied by a pending packet 
 */
void free_pending_packet(PendingPacket *packet) {
    if (packet->data) {
        size_t data_length = packet->header ? packet->header->data_length : 0;
        conditional_memory_zero(packet->data, data_length);
        safe_kfree(packet->data);
        packet->data = NULL;  // Set the pointer to NULL after freeing
    }

    if (packet->header) {
        safe_kfree(packet->header);
        packet->header = NULL;  // Set the pointer to NULL after freeing
    }

    conditional_memory_zero(packet, sizeof(PendingPacket));
    safe_kfree(packet);
}


/* 
 * Creates and returns a new pending packet 
 */
PendingPacket* create_pending_packet(struct sk_buff *skb) {
    PendingPacket *packet = kzalloc(sizeof(PendingPacket), GFP_KERNEL);
    if (!packet) {
        LOG_ERR("Failed to allocate memory for PendingPacket.");
        return NULL;
    }

    packet->header = create_packet_header(PACKET_HEADER_VERSION, skb ? skb->len : 0);
    if (!packet->header || !add_data_to_packet(packet, skb)) {
        free_pending_packet(packet);
        return NULL;
    }

    packet->headerProcessed = false;
    packet->dataProcessed = false;

    return packet;
}

bool add_data_to_packet(PendingPacket *packet, struct sk_buff *skb) {
    if (!packet || !packet->header) {
        LOG_ERR("Failed to add data to packet due to NULL arguments.");
        return false;
    }

    if (!skb || skb->len == 0) {  // Check if skb->len is 0
        LOG_DEBUG("No skb provided or skb->len is 0. Skipping data addition.");
        return true;
    }

    size_t new_data_length = skb->len;

    if (packet->data && packet->header->data_length != new_data_length) {
        conditional_memory_zero(packet->data, packet->header->data_length);
        kfree(packet->data);
        packet->data = NULL;
    }

    if (!packet->data) {
        packet->data = kmalloc(new_data_length, GFP_KERNEL);
        if (!packet->data) {
            LOG_ERR("Failed to allocate memory for packet data.");
            return false;
        }
        LOG_DEBUG("Allocated memory for packet data.");
    } else {
        LOG_DEBUG("Memory already allocated for packet data.");
    }

    if (!skb->data) {
        LOG_ERR("Unexpected error: skb->data is NULL.");
        return false;
    }

    LOG_DEBUG("About to copy data from skb to packet.");

    skb_copy_bits(skb, 0, packet->data, new_data_length);
    LOG_DEBUG("Successfully copied data from skb to packet.");

    packet->header->data_length = new_data_length;

    return true;
}



/* 
 * Converts an IP address to a human-readable format and stores it in the provided buffer 
 * Ensures that the buffer size is at least IP_BUFFER_SIZE for safe snprintf operation 
 */
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len) {
    if (!buffer) {
        LOG_ERR("Buffer provided is NULL.");
        return;
    }

    if (buf_len < IP_BUFFER_SIZE) {  
        LOG_ERR("Buffer size is incorrect. Expected at least %d, got %zu.", IP_BUFFER_SIZE, buf_len);
        return;  // added this line
    }

    snprintf(buffer, buf_len, "%pI4", &ip);
    buffer[IP_BUFFER_SIZE - 1] = '\0';  // Ensure null termination (safety measure)
}

char* get_reason_text(DecisionReason reason) {
    for (size_t i = 0; i < ARRAY_SIZE_REASON(reason_infos); i++) {
        if (reason_infos[i].reason == reason) {
            return reason_infos[i].text;
        }
    }
    return " Unknown reason";
}

void int_to_bytes(s32 value, unsigned char bytes[sizeof(s32)]) {
    for (size_t i = 0; i < sizeof(s32); i++) {
        bytes[i] = (value >> (8 * (sizeof(s32) - 1 - i))) & 0xFF;
    }
}