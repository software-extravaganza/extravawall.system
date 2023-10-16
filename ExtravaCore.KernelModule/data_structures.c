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
    if (!ptr){
        return;
    }
    kfree(ptr);
}

/* 
 * Zeros out the memory of a given pointer only if the ZERO_MEMORY_BEFORE_FREE is set to 1
 */
void conditional_memory_zero(void* ptr, size_t size) {
    if (!ptr){
        return;
    }

    if (ZERO_MEMORY_BEFORE_FREE) {
        memset(ptr, 0, size);
    }
}

struct nf_queue_entry *create_queue_entry(struct sk_buff *skb, const struct nf_hook_state *state) {
    struct nf_queue_entry *entry = kmalloc(sizeof(struct nf_queue_entry), GFP_ATOMIC);
    if (!entry) {
        return NULL;
    }
    entry->skb = skb;
    entry->state = *state;  // Copying the state
    // Populate other necessary fields for entry if needed
    return entry;
}

/* 
 * Frees the memory occupied by a pending packet 
 */
void free_pending_packetTrip(PendingPacketRoundTrip *packetTrip) {
    if (!packetTrip){
        return;
    }

    if (packetTrip->packet) {
        free_pending_packet(packetTrip->packet);
        packetTrip->packet = NULL;  // Set the pointer to NULL after freeing
    }

    if (packetTrip->responsePacket) {
        free_pending_packet(packetTrip->responsePacket);
        packetTrip->responsePacket = NULL;  // Set the pointer to NULL after freeing
    }

    // If packetTrip->skb is set, free it
    if (packetTrip->entry) {
        //safe_kfree(packetTrip->entry);  // Use kfree_skb to free sk_buffs
        packetTrip->entry = NULL;
    }

    //conditional_memory_zero(packetTrip, sizeof(PendingPacketRoundTrip));
    safe_kfree(packetTrip);
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

bool setup_pending_packet_trip_main_packet(PendingPacketRoundTrip *packetTrip) {
    packetTrip->packet = create_pending_packet(REQUEST_PACKET);
    if (!packetTrip->packet) {
        LOG_DEBUG("Failed to create request packet.");
        return false;
    }
    return true;
}

bool setup_pending_packet_trip_response_packet(PendingPacketRoundTrip *packetTrip) {
    packetTrip->responsePacket = create_pending_packet(RESPONSE_PACKET);
    if (!packetTrip->responsePacket) {
        LOG_DEBUG("Failed to create response packet.");
        return false;
    }
    return true;
}

PendingPacketRoundTrip* create_pending_packetTrip(struct nf_queue_entry *entry) {
    if (!entry) {
        LOG_DEBUG("Entry provided is NULL.");
        return NULL;
    }

    PendingPacketRoundTrip *packetTrip = allocate_pending_packet_trip();
    if (!packetTrip) {
        return NULL;
    }

    packetTrip->entry = entry;
    packetTrip->decision = UNDECIDED;

    //init_completion(&packetTrip->packet_processed);

    if (!setup_pending_packet_trip_main_packet(packetTrip) ||
        !setup_pending_packet_trip_response_packet(packetTrip)) {
        free_pending_packetTrip(packetTrip);
        packetTrip = NULL;
        return NULL;
    }

    LOG_DEBUG("Successfully created PendingPacketRoundTrip.");
    return packetTrip;
}

/* 
 * Frees the memory occupied by a pending packet 
 */
void free_pending_packet(PendingPacket *packet) {
    if (!packet) {
        return;
    }

    safe_kfree(packet);
}

char* createPacketTypeString(PendingPacket *packet) {
    return createPacketTypeStringFromType(packet->type);
}

char* createPacketTypeStringFromType(RoundTripPacketType type) {
    switch (type) {
        case REQUEST_PACKET:
            return "Request";
            break;
        case RESPONSE_PACKET:
            return "Response";
            break;
        default:
            return "Unknown";
            break;
    }
}

/* 
 * Creates and returns a new pending packet 
 */
PendingPacket* create_pending_packet(RoundTripPacketType type) {
    char *packetTypeString = createPacketTypeStringFromType(type);
    PendingPacket *packet = kzalloc(sizeof(PendingPacket), GFP_KERNEL);
    if (!packet) {
        LOG_ERROR("Failed to allocate memory for %s PendingPacket.", packetTypeString);
        return NULL;
    }

    packet->type = type;
    packet->headerProcessed = false;
    packet->dataProcessed = false;

    return packet;
}

/* 
 * Converts an IP address to a human-readable format and stores it in the provided buffer 
 * Ensures that the buffer size is at least IP_BUFFER_SIZE for safe snprintf operation 
 */
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len) {
    if (!buffer) {
        LOG_ERROR("Buffer provided is NULL.");
        return;
    }

    if (buf_len < IP_BUFFER_SIZE) {  
        LOG_ERROR("Buffer size is incorrect. Expected at least %d, got %zu.", IP_BUFFER_SIZE, buf_len);
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