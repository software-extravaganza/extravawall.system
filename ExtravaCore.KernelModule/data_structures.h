#ifndef DATA_STRUCTURES
#define DATA_STRUCTURES

#include <linux/kernel.h>
#include <linux/skbuff.h>
#include <linux/kfifo.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <net/netfilter/nf_queue.h>
#include <linux/skbuff.h>
#include <linux/ip.h>
#include <linux/ktime.h>
#include "logger.h"

// Constants
#define IP_BUFFER_SIZE 16  // for IPv4, 16 chars: 255.255.255.255 + a NULL terminator
#define ZERO_MEMORY_BEFORE_FREE 1  // 1 to zero out memory before freeing, 0 otherwise
#define PACKET_HEADER_VERSION 1 // Version of the packet header

/* Enumerations for Routing Types and Decisions */
typedef enum {
    NONE_ROUTING = 0,
    PRE_ROUTING = 1,    // Signifies if the packet is in the pre-routing stage
    POST_ROUTING = 2,   // Signifies if the packet is in the post-routing stage
    LOCAL_ROUTING = 3   // Signifies if the packet is in the local-routing stage
} RoutingType;

typedef enum {
    UNDECIDED = 0,    // Signifies that the packet has not been processed yet
    DROP = 1,         // Signifies that the packet should be dropped
    ACCEPT = 2,       // Signifies that the packet should be accepted
    MANIPULATE = 3    // Signifies that the packet has been manipulated
} RoutingDecision;

typedef enum {
    REQUEST_PACKET,
    RESPONSE_PACKET
} RoundTripPacketType;

typedef enum {
    MEMORY_FAILURE_PACKET,
    MEMORY_FAILURE_PACKET_HEADER,
    BUFFER_FULL,
    TIMEOUT,
    ERROR,
    USER_ACCEPT,
    USER_DROP
} DecisionReason;

typedef struct {
    DecisionReason reason;        // Code to represent the reason
    char* text; // Description of the reason
} DecisionReasonInfo;

/* Pending Packet structure */
typedef struct {
    bool dataProcessed;
    bool headerProcessed;
    RoundTripPacketType type;
} PendingPacket;

typedef struct {
    ktime_t createdTime;
    struct nf_queue_entry *entry;
    PendingPacket *packet;
    PendingPacket *responsePacket;
    RoutingDecision decision;
    RoutingType routingType;
} PendingPacketRoundTrip;

/* Function Declarations */
void ConditionalMemoryZero(void* ptr, size_t size);
PendingPacketRoundTrip* CreatePendingPacketTrip(struct nf_queue_entry *entry, RoutingType type);
void FreePendingPacketTrip(PendingPacketRoundTrip *packetTrip);
PendingPacket* CreatePendingPacket(RoundTripPacketType type);
void FreePendingPacket(PendingPacket *packet);
bool AddDataToPacket(PendingPacket *packet, struct sk_buff *skb);
char* GetReasonText(DecisionReason reason);
char* CreatePacketTypeString(PendingPacket *packet);


#endif // DATA_STRUCTURES