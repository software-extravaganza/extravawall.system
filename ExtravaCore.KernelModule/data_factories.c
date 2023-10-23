#include "data_factories.h"

// Constants
#define MESSAGE_MEMORY_FAILURE_PACKET "Memory allocation failure for packet"
#define MESSAGE_MEMORY_FAILURE_PACKET_HEADER "Memory allocation failure for packet header"
#define MESSAGE_BUFFER_FULL "Dropped a packet because the packet buffer is full"
#define MESSAGE_TIMEOUT "Dropped a packet that timed out"
#define MESSAGE_ERROR "Dropped a packet because of an error"
#define MESSAGE_USER_ACCEPT "Decided to accept a packet"
#define MESSAGE_USER_DROP "Decided to drop a packet"

// Private members
static DecisionReasonInfo _reasonInfos[] = {
    { MEMORY_FAILURE_PACKET, MESSAGE_MEMORY_FAILURE_PACKET },
    { MEMORY_FAILURE_PACKET_HEADER, MESSAGE_MEMORY_FAILURE_PACKET_HEADER },
    { BUFFER_FULL, MESSAGE_BUFFER_FULL },
    { TIMEOUT, MESSAGE_TIMEOUT },
    { ERROR, MESSAGE_ERROR },
    { USER_ACCEPT, MESSAGE_USER_ACCEPT },
    { USER_DROP, MESSAGE_USER_DROP }
};

static char* createPacketTypeStringFromType(RoundTripPacketType type);
struct nf_queue_entry* CreateQueueEntry(struct sk_buff *skb, const struct nf_hook_state *state);
static void addMetaDataToPacketTrip(PendingPacketRoundTrip *packetTrip, RoutingType type);

// Private functions
void safeFree(void* ptr) {
    if (!ptr){
        return;
    }
    kfree(ptr);
}

static char* createPacketTypeStringFromType(RoundTripPacketType type) {
    switch (type) {
        case REQUEST_PACKET:
            return "Request";
        case RESPONSE_PACKET:
            return "Response";
        default:
            return "Unknown";
    }
}

static bool setupPendingPacketTripMainPacket(PendingPacketRoundTrip *packetTrip) {
    packetTrip->packet = CreatePendingPacket(REQUEST_PACKET);
    if (!packetTrip->packet) {
        LOG_DEBUG("Failed to create request packet.");
        return false;
    }
    return true;
}

static bool setupPendingPacketTripResponsePacket(PendingPacketRoundTrip *packetTrip) {
    packetTrip->responsePacket = CreatePendingPacket(RESPONSE_PACKET);
    if (!packetTrip->responsePacket) {
        LOG_DEBUG("Failed to create response packet.");
        return false;
    }
    return true;
}

// Public functions
void ConditionalMemoryZero(void* ptr, size_t size) {
    if (!ptr){
        return;
    }

    if (ZERO_MEMORY_BEFORE_FREE) {
        memset(ptr, 0, size);
    }
}

void FreePendingPacket(PendingPacket *packet) {
    if (!packet) {
        return;
    }

    safeFree(packet);
}

char* CreatePacketTypeString(PendingPacket *packet) {
    return createPacketTypeStringFromType(packet->type);
}

PendingPacket* CreatePendingPacket(RoundTripPacketType type) {
    PendingPacket *packet = kzalloc(sizeof(PendingPacket), GFP_KERNEL);
    packet->type = type;
    packet->headerProcessed = false;
    packet->dataProcessed = false;

    return packet;
}

char* GetReasonText(DecisionReason reason) {
    for (size_t i = 0; i < ARRAY_SIZE_REASON(_reasonInfos); i++) {
        if (_reasonInfos[i].reason == reason) {
            return _reasonInfos[i].text;
        }
    }
    return " Unknown reason";
}

struct nf_queue_entry* CreateQueueEntry(struct sk_buff *skb, const struct nf_hook_state *state) {
    struct nf_queue_entry *entry = kmalloc(sizeof(struct nf_queue_entry), GFP_ATOMIC);
    if (!entry) {
        return NULL;
    }
    entry->skb = skb;
    entry->state = *state;  // Copying the state
    return entry;
}

void FreePendingPacketTrip(PendingPacketRoundTrip *packetTrip) {
    if (!packetTrip){
        return;
    }

    if (packetTrip->packet) {
        FreePendingPacket(packetTrip->packet);
        packetTrip->packet = NULL;  // Set the pointer to NULL after freeing
    }

    if (packetTrip->responsePacket) {
        FreePendingPacket(packetTrip->responsePacket);
        packetTrip->responsePacket = NULL;  // Set the pointer to NULL after freeing
    }

    if (packetTrip->entry) {
        packetTrip->entry = NULL;
    }

    safeFree(packetTrip);
}

PendingPacketRoundTrip* CreatePendingPacketTrip(struct nf_queue_entry *entry, RoutingType type) {
    PendingPacketRoundTrip *packetTrip = NULL;
    if (!entry) {
        LOG_DEBUG("Entry provided is NULL.");
        return NULL;
    }

    packetTrip = kzalloc(sizeof(PendingPacketRoundTrip), GFP_KERNEL);
    if (!packetTrip) {
        LOG_DEBUG("Failed to allocate memory for PendingPacketRoundTrip.");
        return NULL;
    }

    packetTrip->createdTime = ktime_get();
    packetTrip->entry = entry;
    packetTrip->decision = UNDECIDED;
    packetTrip->routingType = type;
    addMetaDataToPacketTrip(packetTrip, type);
    if (!setupPendingPacketTripMainPacket(packetTrip) ||
        !setupPendingPacketTripResponsePacket(packetTrip)) {
        FreePendingPacketTrip(packetTrip);
        packetTrip = NULL;
        return NULL;
    }

    LOG_DEBUG("Successfully created PendingPacketRoundTrip.");
    return packetTrip;
}

static void addMetaDataToPacketTrip(PendingPacketRoundTrip *packetTrip, RoutingType type) {
    if (!packetTrip || !packetTrip->entry) {
        LOG_DEBUG("Packet trip provided is NULL.");
        return;
    }

    struct iphdr *ipHeader = ip_hdr(packetTrip->entry->skb);
    struct ethhdr *ethHeader;
    int macHeaderSet = skb_mac_header_was_set(packetTrip->entry->skb);
    char* typeOfService = "Unknown";
    const char* routeTypeName = routeTypeToString(type);
    const char* protocolName = "Unknown";

    if (ipHeader) {
        packetTrip->protocol = ipHeader->protocol;
        typeOfService = TOS_TO_STRING(ipHeader->tos);
        protocolName = ipProtocolToString(ipHeader->protocol);
    }

    if (!macHeaderSet) {
        LOG_DEBUG_ICMP(packetTrip, "Ethernet (MAC) header pointer not set in skb. Routing Type: %s, Protocol: %s, ToS: %s", routeTypeName, protocolName, typeOfService);
    }
    else {
        ethHeader = (struct ethhdr *)skb_mac_header(packetTrip->entry->skb);
        if (ethHeader) {
            unsigned char *srcMac = ethHeader->h_source;
            unsigned char *destMac = ethHeader->h_dest;
            LOG_DEBUG_ICMP(packetTrip, "Ethernet (MAC) header found. Size: %zu bytes. Src:%pM, Dst:%pM, Routing Type: %s, Protocol: %s, ToS: %u", sizeof(struct ethhdr), srcMac, destMac, routeTypeName, protocolName, typeOfService);
        }
        else{
            LOG_DEBUG_ICMP(packetTrip, "No Ethernet (MAC) header found. Routing Type: %s, Protocol: %s, ToS: %s", routeTypeName, protocolName, typeOfService);
        }
    }
}