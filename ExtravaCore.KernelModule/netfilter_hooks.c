/**
 * This module provides netfilter hooks for packet processing in the Linux kernel.
 * It allows for custom processing of packets at various stages of the network stack.
 */
#include "netfilter_hooks.h"

#define ERROR_HOOK_SETUP -1
#define ERROR_MEMORY_ALLOC -2

// Constants for decision icons
#define UNDECIDED_ICON "❓"
#define DROP_ICON "🚮"
#define ACCEPT_ICON "📨"
#define MANIPULATE_ICON "✂️"

const char *DECISION_ICONS[] = {
    UNDECIDED_ICON,  // UNDECIDED
    DROP_ICON,       // DROP
    ACCEPT_ICON,     // ACCEPT
    MANIPULATE_ICON  // MANIPULATE
};

PacketQueue _pendingPacketsQueue;
PacketQueue _read1PacketsQueue;
PacketQueue _read2PacketsQueue;
PacketQueue _writePacketsQueue;
PacketQueue _injectionPacketsQueue;

bool _readQueueItemAdded = false;
bool _queueItemProcessed = false;
bool _queueProcessorExited = false;
bool _userspaceItemProcessed = false;
bool _userRead = false;
DECLARE_WAIT_QUEUE_HEAD(QueueProcessorExitedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(_queueItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(UserspaceItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(ReadQueueItemAddedWaitQueue);

// Declaration of the netfilter hooks
static struct nf_hook_ops *_preRoutingOps = NULL;
static struct nf_hook_ops *_postRoutingOps = NULL;
static struct nf_hook_ops *_localRoutingOps = NULL;
static struct nf_queue_handler *_queueHandlerOps = NULL;

struct task_struct *_queueProcessorThread = NULL;
static packet_processing_callback_t _registeredCallback = NULL;
static bool _isActive = false;
static RoutingType _lastRoutingType = NONE_ROUTING;

static void _hookDrop(struct net *net);
static int _packetQueueHandler(struct nf_queue_entry *entry, unsigned int queuenum);
static struct nf_queue_handler* _setupQueueHandlerHook(void);

void RegisterPacketProcessingCallback(packet_processing_callback_t callback) {
    _registeredCallback = callback;
}

int RegisterQueueProcessorThreadHandler(packet_processor_thread_handler_t handler) {
    _queueProcessorThread = kthread_run(handler, NULL, "packet_processor");
    if (IS_ERR(_queueProcessorThread)) {
        printk(KERN_ERR "Failed to start packet_processor_thread.\n");
        CleanupNetfilterHooks();  // Make sure to clean up before exiting
        return PTR_ERR(_queueProcessorThread);
    }

    return 0;
}

static void _handlePacketDecision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    char logMessage[256];
    if(packetTrip){
        FreePendingPacketTrip(packetTrip);
        packetTrip = NULL;
    }

    snprintf(logMessage, sizeof(logMessage), "%s%s Extrava", DECISION_ICONS[decision], GetReasonText(reason));
    LOG_DEBUG_ICMP("%s", logMessage);
}

// Refactored function to handle nfRouting
static unsigned int _nfRoutingHandlerCommon(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    _lastRoutingType = NONE_ROUTING;
    CHECK_NULL(LOG_TYPE_ERROR, state, NF_DROP);
    const char* hookName = hookToString(state->hook);
    const char* routeTypeName = routeTypeToString(type);
    CHECK_NULL_AND_LOG(LOG_TYPE_ERROR, skb, NF_DROP, LOG_TYPE_DEBUG_PACKET, "skb pointer: %p length: %u", skb, skb->len);
    struct iphdr *ipHeader = ip_hdr(skb);
    CHECK_NULL(LOG_TYPE_ERROR, ipHeader, NF_DROP);
    const char* protocolName = ipProtocolToString(ipHeader->protocol);
    const char* typeOfService = TOS_TO_STRING(ipHeader->tos);

    if (ipHeader->protocol != IPPROTO_ICMP){ //} || type == LOCAL_ROUTING) {
        return NF_ACCEPT;
    }

    LOG_DEBUG_ICMP("ICMP packet received. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);

    if(!_isActive){
        LOG_DEBUG_ICMP("Extrava is not active. Dropping packet. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
        return NF_DROP;
    }

    struct ethhdr *ethHeader;
    int macHeaderSet = skb_mac_header_was_set(skb);
    if (!macHeaderSet) {
        LOG_DEBUG_ICMP("Ethernet (MAC) header pointer not set in skb. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
    }
    else {
        ethHeader = (struct ethhdr *)skb_mac_header(skb);
        if (!ethHeader) {
            LOG_DEBUG_ICMP("No Ethernet (MAC) header found. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
            return NF_DROP;
        }

        LOG_DEBUG_ICMP("Ethernet (MAC) header found. Size: %zu bytes. Routing Type: %s, Protocol: %s, ToS: %u, Hook Type: %s", sizeof(struct ethhdr), routeTypeName, protocolName, typeOfService, hookName);
    }

    _lastRoutingType = type;
    return NF_QUEUE_NR(0);
}

static unsigned int _nfPreRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return _nfRoutingHandlerCommon(PRE_ROUTING, priv, skb, state);
}

static unsigned int _nfPostRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return _nfRoutingHandlerCommon(POST_ROUTING, priv, skb, state);
}

static unsigned int _nfLocalRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return _nfRoutingHandlerCommon(LOCAL_ROUTING, priv, skb, state);
}

int SetupNetfilterHooks(void) {
    _preRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);
    _postRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);
    _localRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);
    _queueHandlerOps = kzalloc(sizeof(struct nf_queue_handler), GFP_KERNEL);

    if (!_preRoutingOps || !_postRoutingOps || !_localRoutingOps || !_queueHandlerOps) {
        printk(KERN_ERR "Failed to allocate memory for netfilter hooks.\n");
        CleanupNetfilterHooks();
        return ERROR_MEMORY_ALLOC;
    }

    PacketQueueInitialize(&_pendingPacketsQueue);
    PacketQueueInitialize(&_read1PacketsQueue);
    PacketQueueInitialize(&_read2PacketsQueue);
    PacketQueueInitialize(&_writePacketsQueue);
    PacketQueueInitialize(&_injectionPacketsQueue);

    _preRoutingOps->hook = _nfPreRoutingHandler;
    _preRoutingOps->hooknum = NF_INET_PRE_ROUTING;
    _preRoutingOps->pf = PF_INET;
    _preRoutingOps->priority = NF_IP_PRI_FIRST;

    _postRoutingOps->hook = _nfPostRoutingHandler;
    _postRoutingOps->hooknum = NF_INET_POST_ROUTING;
    _postRoutingOps->pf = PF_INET;
    _postRoutingOps->priority = NF_IP_PRI_FIRST;

    _localRoutingOps->hook = _nfLocalRoutingHandler;
    _localRoutingOps->hooknum = NF_INET_LOCAL_OUT;
    _localRoutingOps->pf = PF_INET;
    _localRoutingOps->priority = NF_IP_PRI_FIRST;

    _queueHandlerOps = _setupQueueHandlerHook();
    nf_register_net_hook(&init_net, _preRoutingOps);
    nf_register_net_hook(&init_net, _postRoutingOps);
    nf_register_net_hook(&init_net, _localRoutingOps);
    nf_register_queue_handler(_queueHandlerOps);

    _isActive = true;
    return 0;
}

void CleanupNetfilterHooks(void) {
    _isActive = false;
    if (_queueHandlerOps) {
        nf_unregister_queue_handler();
        kfree(_queueHandlerOps);
        _queueHandlerOps = NULL;
    }

    if (_localRoutingOps) {
        nf_unregister_net_hook(&init_net, _localRoutingOps);
        kfree(_localRoutingOps);
        _localRoutingOps = NULL;
    }

    if (_postRoutingOps) {
        nf_unregister_net_hook(&init_net, _postRoutingOps);
        kfree(_postRoutingOps);
        _postRoutingOps = NULL;
    }

    if (_preRoutingOps) {
        nf_unregister_net_hook(&init_net, _preRoutingOps);
        kfree(_preRoutingOps);
        _preRoutingOps = NULL;
    }

    PacketQueueCleanup(&_pendingPacketsQueue);
    PacketQueueCleanup(&_read1PacketsQueue);
    PacketQueueCleanup(&_read2PacketsQueue);
    PacketQueueCleanup(&_writePacketsQueue);
    PacketQueueCleanup(&_injectionPacketsQueue);
}

static struct nf_queue_handler* _setupQueueHandlerHook(void) {
    struct nf_queue_handler *ops = (struct nf_queue_handler*)kcalloc(1, sizeof(struct nf_queue_handler), GFP_KERNEL);
    if (!ops) {
        LOG_ERROR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->outfn = _packetQueueHandler;
    ops->nf_hook_drop = _hookDrop;

    return ops;
}

static void _hookDrop(struct net *net){
    LOG_DEBUG("Cleaning up netfilter queue hooks");
    if(_queueHandlerOps){
        nf_unregister_queue_handler();
        kfree(_queueHandlerOps);
        _queueHandlerOps = NULL;
    }

    if(_isActive){
        LOG_DEBUG("Initializing packet queue handler");
        _queueHandlerOps = _setupQueueHandlerHook();
        nf_register_queue_handler(_queueHandlerOps);
    }
}

static int _packetQueueHandler(struct nf_queue_entry *entry, unsigned int queuenum)
{
    PendingPacketRoundTrip *packetTrip = CreatePendingPacketTrip(entry, _lastRoutingType);
    if (!packetTrip || !packetTrip->packet) {
        LOG_ERROR("Failed to create pending packet trip.");
        _handlePacketDecision(packetTrip, DROP, MEMORY_FAILURE_PACKET);
        packetTrip = NULL;
        return NF_DROP;
    }

    //bool is_processed = false;
    // if (!registered_callback) {
    //     LOG_ERROR("No callback registered.");
    //     handle_packet_decision(packetTrip, DROP, ERROR);
    //     packetTrip = NULL;
    //     return NF_DROP;
    // }

    struct sk_buff *skb = entry->skb;
    struct ethhdr *eth_header;

    if (!skb) {
        LOG_ERROR("No skb available");
        return NF_DROP;
    }

    int mac_header_set = skb_mac_header_was_set(skb);
    if (!mac_header_set) {
        LOG_DEBUG_ICMP("Ethernet (MAC) header pointer not set in skb");
    }
    else {
        eth_header = (struct ethhdr *)skb_mac_header(skb);
        if (!eth_header) {
            LOG_DEBUG_ICMP("No Ethernet (MAC) header found");
            return NF_DROP;
        }

        LOG_DEBUG_ICMP("Ethernet (MAC) header found. Size: %zu bytes", sizeof(struct ethhdr));
    }

  
    // enqueue the packet for further processing
    // int queueLength = pq_len_packetTrip(&pending_packets_queue);
    // LOG_DEBUG_ICMP("Pushing to pending_packets_queue. Current size: %d; Size after push: %d", queueLength, queueLength + 1);
    // pq_push_packetTrip(&pending_packets_queue, packetTrip);
    
    int queueLength = PacketQueueLength(&_read1PacketsQueue);
    LOG_DEBUG_ICMP("Pushing to _read1PacketsQueue. Current Size: %d; Size after push: %d", queueLength, queueLength + 1);
    PacketQueuePush(&_read1PacketsQueue, packetTrip);
    // Signal to userspace that there's a packet to process (e.g., via a char device or netlink).
    //registered_callback();

    // wake up your processing thread
    //wake_up_process(queue_processor_thread);
    _readQueueItemAdded = true;
    wake_up_interruptible(&ReadQueueItemAddedWaitQueue);

    //wake_up_process(queue_processor_thread);
    return 0;
}