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
DECLARE_WAIT_QUEUE_HEAD(_queueProcessorExitedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(_queueItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(_userspaceItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(_readQueueItemAddedWaitQueue);

// Declaration of the netfilter hooks
static struct nf_hook_ops *_preRoutingOps = NULL;
static struct nf_hook_ops *_postRoutingOps = NULL;
static struct nf_hook_ops *_localRoutingOps = NULL;
static struct nf_queue_handler *_queueHandlerOps = NULL;

static struct kmem_cache *_pendingPacketCache = NULL;
struct task_struct *_queueProcessorThread = NULL;
static packet_processing_callback_t _registeredCallback = NULL;
static bool _isActive = false;
static RoutingType _lastRoutingType = NONE_ROUTING;

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

void HandlePacketDecision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    if(packetTrip){
        free_pending_packetTrip(packetTrip);
        packetTrip = NULL;
    }

    char logMessage[256];
    snprintf(logMessage, sizeof(logMessage), "%s%s Extrava", DECISION_ICONS[decision], get_reason_text(reason));
    LOG_DEBUG_PACKET("%s", logMessage);
}

// Refactored function to handle nfRouting
static unsigned int nfRoutingHandlerCommon(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    _lastRoutingType = NONE_ROUTING;
    CHECK_NULL(LOG_TYPE_ERROR, state, NF_DROP);
    char* hookName = TEST_AND_MAKE_STRING_OR_EMPTY(LOG_TYPE_DEBUG_PACKET, state->hook, hook_to_string(value));
    char* routeTypeName = route_type_to_string(type);
    CHECK_NULL_AND_LOG(LOG_TYPE_ERROR, skb, NF_DROP, LOG_TYPE_DEBUG_PACKET, "skb pointer: %p length: %u", skb, skb->len);
    struct iphdr *ipHeader = ip_hdr(skb);
    CHECK_NULL(LOG_TYPE_ERROR, ipHeader, NF_DROP);
    CHECK_NULL(LOG_TYPE_ERROR, ipHeader->protocol, NF_DROP);
    char* protocolName = ip_protocol_to_string(ipHeader->protocol);
    TEST_NULL(LOG_TYPE_DEBUG_PACKET, ipHeader->tos);
    char* typeOfService = TOS_TO_STRING(ipHeader->tos);

    if (ipHeader->protocol != IPPROTO_ICMP){ //} || type == LOCAL_ROUTING) {
        return NF_ACCEPT;
    }

    LOG_DEBUG_PACKET("ICMP packet received. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);

    if(!_isActive){
        LOG_DEBUG_PACKET("Extrava is not active. Dropping packet. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
        return NF_DROP;
    }

    struct ethhdr *ethHeader;
    int macHeaderSet = skb_mac_header_was_set(skb);
    if (!macHeaderSet) {
        LOG_DEBUG_PACKET("Ethernet (MAC) header pointer not set in skb. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
    }
    else {
        ethHeader = (struct ethhdr *)skb_mac_header(skb);
        if (!ethHeader) {
            LOG_DEBUG_PACKET("No Ethernet (MAC) header found. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
            return NF_DROP;
        }

        LOG_DEBUG_PACKET("Ethernet (MAC) header found. Size: %zu bytes. Routing Type: %s, Protocol: %s, ToS: %u, Hook Type: %s", sizeof(struct ethhdr), routeTypeName, protocolName, typeOfService, hookName);
    }

    _lastRoutingType = type;
    return NF_QUEUE_NR(0);
}

static unsigned int nfPreRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return nfRoutingHandlerCommon(PRE_ROUTING, priv, skb, state);
}

static unsigned int nfPostRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return nfRoutingHandlerCommon(POST_ROUTING, priv, skb, state);
}

static unsigned int nfLocalRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    return nfRoutingHandlerCommon(LOCAL_ROUTING, priv, skb, state);
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

    _preRoutingOps->hook = nfPreRoutingHandler;
    _preRoutingOps->hooknum = NF_INET_PRE_ROUTING;
    _preRoutingOps->pf = PF_INET;
    _preRoutingOps->priority = NF_IP_PRI_FIRST;

    _postRoutingOps->hook = nfPostRoutingHandler;
    _postRoutingOps->hooknum = NF_INET_POST_ROUTING;
    _postRoutingOps->pf = PF_INET;
    _postRoutingOps->priority = NF_IP_PRI_FIRST;

    _localRoutingOps->hook = nfLocalRoutingHandler;
    _localRoutingOps->hooknum = NF_INET_LOCAL_OUT;
    _localRoutingOps->pf = PF_INET;
    _localRoutingOps->priority = NF_IP_PRI_FIRST;

    _queueHandlerOps->name = "extrava";
    _queueHandlerOps->outfn = packetQueueHandler;

    nf_register_net_hook(&init_net, _preRoutingOps);
    nf_register_net_hook(&init_net, _postRoutingOps);
    nf_register_net_hook(&init_net, _localRoutingOps);
    nf_register_queue_handler(&init_net, _queueHandlerOps);

    return 0;
}

void CleanupNetfilterHooks(void) {
    if (_queueHandlerOps) {
        nf_unregister_queue_handler(&init_net, _queueHandlerOps);
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
}