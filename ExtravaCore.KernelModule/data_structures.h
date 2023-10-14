#ifndef DATA_STRUCTURES
#define DATA_STRUCTURES

#include <linux/kernel.h>
#include <linux/skbuff.h>
#include <linux/kfifo.h>
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

/* Packet Header structure */
typedef struct {
    s32 version;
    s32 data_length;
} PacketHeader;

/* Pending Packet structure */
typedef struct {
    bool dataProcessed;
    bool headerProcessed;
    void *data;
    PacketHeader *header;
    RoundTripPacketType type;
} PendingPacket;

typedef struct {
    PendingPacket *packet;
    PendingPacket *responsePacket;
    RoutingDecision decision;
} PendingPacketRoundTrip;

/* Function Declarations */
void conditional_memory_zero(void* ptr, size_t size);
PendingPacketRoundTrip* create_pending_packetTrip(struct sk_buff *skb);
void free_pending_packetTrip(PendingPacketRoundTrip *packetTrip);
PendingPacket* create_pending_packet(RoundTripPacketType type, struct sk_buff *skb) ;
void free_pending_packet(PendingPacket *packet);
PacketHeader* create_packet_header(RoundTripPacketType type, u32 version, size_t data_length);
void free_packet_header(PacketHeader *header);
bool add_data_to_packet(PendingPacket *packet, struct sk_buff *skb);
void to_human_readable_ip(const unsigned int ip, char *buffer, size_t buf_len);
char* get_reason_text(DecisionReason reason);
void int_to_bytes(int value, unsigned char bytes[4]);
char* createPacketTypeString(PendingPacket *packet);
char* createPacketTypeStringFromType(RoundTripPacketType type);

#endif // DATA_STRUCTURES