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
#include "logger.h"

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
    struct nf_queue_entry *entry;
    PendingPacket *packet;
    PendingPacket *responsePacket;
    RoutingDecision decision;
    RoutingType routingType;
} PendingPacketRoundTrip;

/* Function Declarations */
void conditional_memory_zero(void* ptr, size_t size);
PendingPacketRoundTrip* create_pending_packetTrip(struct nf_queue_entry *entry, RoutingType type);
void free_pending_packetTrip(PendingPacketRoundTrip *packetTrip);
PendingPacket* create_pending_packet(RoundTripPacketType type);
void free_pending_packet(PendingPacket *packet);
bool add_data_to_packet(PendingPacket *packet, struct sk_buff *skb);
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len);
char* get_reason_text(DecisionReason reason);
void int_to_bytes(int value, unsigned char bytes[4]);
char* createPacketTypeString(PendingPacket *packet);
char* createPacketTypeStringFromType(RoundTripPacketType type);
struct nf_queue_entry *create_queue_entry(struct sk_buff *skb, const struct nf_hook_state *state);

#endif // DATA_STRUCTURES