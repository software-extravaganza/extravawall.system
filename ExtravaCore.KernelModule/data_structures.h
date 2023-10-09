#ifndef DATA_STRUCTURES
#define DATA_STRUCTURES

#include <linux/kernel.h>
#include <linux/skbuff.h>
#include "logger.h"

/* Enumerations for Routing Types and Decisions */
typedef enum {
    PRE_ROUTING = 0,  // Signifies if the packet is in the pre-routing stage
    POST_ROUTING = 1  // Signifies if the packet is in the post-routing stage
} RoutingType;

typedef enum {
    UNDECIDED = 0,    // Signifies that the packet has not been processed yet
    DROP = 1,         // Signifies that the packet should be dropped
    ACCEPT = 2,       // Signifies that the packet should be accepted
    MANIPULATE = 3    // Signifies that the packet has been manipulated
} RoutingDecision;

typedef enum {
    MEMORY_FAILURE,
    BUFFER_FULL,
    TIMEOUT,
    ERROR,
    USER_ACCEPT,
    USER_DROP
} DecisionReason;

typedef struct {
    DecisionReason reason;        // Code to represent the reason
    const char* text; // Description of the reason
} DecisionReasonInfo;

/* Packet Header structure */
typedef struct {
    unsigned long pkt_id;
    size_t length;
} PacketHeader;

/* Pending Packet structure */
typedef struct {
    struct sk_buff *skb;
    size_t data_len;
    bool processed;
    RoutingDecision decision;
    void *data;
} PendingPacket;

/* Function Declarations */
void conditional_memory_zero(void* ptr, size_t size);
PendingPacket* create_pending_packet(struct sk_buff *skb);
void free_pending_packet(PendingPacket *packet);
PacketHeader* create_packet_header(unsigned long pkt_id, size_t length);
void free_packet_header(PacketHeader *header);
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len);
char* get_reason_text(DecisionReason reason);

#endif // DATA_STRUCTURES