typedef struct {
    unsigned long pkt_id;
    size_t length;
} PacketHeader;

typedef struct {
    unsigned long packet_index;
    enum RoutingDecision directive;
} PacketDirective;

typedef struct {
    struct sk_buff *skb;
    size_t data_len; 
    bool processed;
    enum RoutingDecision decision;
} PendingPacket;
