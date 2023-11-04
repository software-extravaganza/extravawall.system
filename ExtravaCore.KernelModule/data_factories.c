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
static int numSamples = 100000;
static u64 *timeSamples = NULL;
static u64 *sortedTimeSamples = NULL;

static size_t currentSampleIndex = 0;

// Private functions
void safeFree(void* ptr) {
    if (!ptr){
        return;
    }
    kfree(ptr);
}


int SetupTimeSamples(void) {
    if (!timeSamples) {
        timeSamples = kmalloc(numSamples * sizeof(u64), GFP_KERNEL);
        if (!timeSamples) {
            LOG_ERROR("Failed to allocate memory for timeSamples.");
            return -1; 
        }
    }

    if (!sortedTimeSamples) {
        sortedTimeSamples = kmalloc(numSamples * sizeof(u64), GFP_KERNEL);
        if (!sortedTimeSamples) {
            LOG_ERROR("Failed to allocate memory for sortedTimeSamples.");
            kfree(timeSamples);
            return -1; 
        }
    }

    return 0;
}

void CleanupTimeSamples(void) {
    safeFree(timeSamples);
    safeFree(timeSamples);
}

void recordSampleTime(u64 duration) {
    timeSamples[currentSampleIndex] = duration;
    currentSampleIndex = (currentSampleIndex + 1) % numSamples;
}

u64 calculateSampleAverage(void) {
    u64 sum = 0;
    for (size_t i = 0; i < numSamples; i++) {
        sum += timeSamples[i];
    }
    return sum / numSamples;
}

int compareSamples(const void *a, const void *b) {
    return *(u64 *)a - *(u64 *)b;
}

u64 calculateSamplePercentile(int percentile) {
    memcpy(sortedTimeSamples, timeSamples, numSamples * sizeof(u64));
    sort(sortedTimeSamples, numSamples, sizeof(u64), compareSamples, NULL);
    size_t index = (percentile * numSamples) / 100;
    return sortedTimeSamples[index];
}

char* calculateSamplePercentileToString(int percentile) {
    u64 percentileValue = calculateSamplePercentile(percentile);
    return nanosecondsToHumanizedString(percentileValue);
}

char* calculateSampleAverageToString(void) {
    u64 percentileValue = calculateSampleAverage();
    return nanosecondsToHumanizedString(percentileValue);
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

static void _recordSampleTimeForPacketTrip(PendingPacketRoundTrip *packetTrip){
    u64 startNanoseconds, endNanoseconds, duration;
    if (!packetTrip){
        return;
    }

    endNanoseconds = ktime_to_ns(ktime_get());
    startNanoseconds = ktime_to_ns(packetTrip->createdTime);
    duration = endNanoseconds - startNanoseconds;
    recordSampleTime(duration);
}

void FreePendingPacketTrip(PendingPacketRoundTrip *packetTrip) {
    if (!packetTrip){
        return;
    }

    _recordSampleTimeForPacketTrip(packetTrip);

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

void ResetPendingPacketTrip(PendingPacketRoundTrip *packetTrip) {
    if (packetTrip) {
        _recordSampleTimeForPacketTrip(packetTrip);
        packetTrip->slotAssigned = -5;

        // Reset the createdTime
        packetTrip->createdTime = 0;

        // Reset the attempts
        packetTrip->attempts = 0;

        // Reset the entry
        packetTrip->entry = NULL;

        // Reset the packet if it exists
        if (packetTrip->packet) {
            packetTrip->packet->dataProcessed = false;
            packetTrip->packet->headerProcessed = false;
            packetTrip->packet->size = 0;
            packetTrip->packet->type = 0; // Assuming 0 is a default value for RoundTripPacketType
        }

        // Reset the responsePacket if it exists
        if (packetTrip->responsePacket) {
            packetTrip->responsePacket->dataProcessed = false;
            packetTrip->responsePacket->headerProcessed = false;
            packetTrip->responsePacket->size = 0;
            packetTrip->responsePacket->type = 0; // Assuming 0 is a default value for RoundTripPacketType
        }

        // Reset the decision and routingType
        packetTrip->decision = UNDECIDED;
        packetTrip->routingType = NONE_ROUTING;

        // Reset the protocol
        packetTrip->protocol = 0; // Assuming 0 is a default value for protocol
    }
}

PendingPacketRoundTrip* CreatePendingPacketTrip() {
    PendingPacketRoundTrip *packetTrip = NULL;
    // if (!entry) {
    //     LOG_DEBUG("Entry provided is NULL.");
    //     return NULL;
    // }

    packetTrip = kzalloc(sizeof(PendingPacketRoundTrip), GFP_KERNEL);
    if (!packetTrip) {
        LOG_DEBUG("Failed to allocate memory for PendingPacketRoundTrip.");
        return NULL;
    }

    packetTrip->createdTime = ktime_get();
    packetTrip->decision = UNDECIDED;
    // packetTrip->entry = entry;
    // packetTrip->routingType = type;
    // addMetaDataToPacketTrip(packetTrip, type);
    if (!setupPendingPacketTripMainPacket(packetTrip) ||
        !setupPendingPacketTripResponsePacket(packetTrip)) {
        FreePendingPacketTrip(packetTrip);
        packetTrip = NULL;
        return NULL;
    }

    packetTrip->available = true;

    //LOG_DEBUG("Successfully created PendingPacketRoundTrip.");
    return packetTrip;
}

void AddMetaDataToPacketTrip(PendingPacketRoundTrip *packetTrip, RoutingType type) {
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