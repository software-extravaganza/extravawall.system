#include "netfilter_hooks.h"

PacketQueue pending_packets_queue;
PacketQueue read1_packets_queue;
PacketQueue read2_packets_queue;
PacketQueue write_packets_queue;
PacketQueue injection_packets_queue;
static DEFINE_SPINLOCK(pending_packets_lock);

bool read_queue_item_added = false;
bool queue_item_processed = false;
bool queue_processor_exited = false;
bool userspace_item_processed = false;
bool user_read = false;
DECLARE_WAIT_QUEUE_HEAD(queue_processor_exited_wait_queue);
DECLARE_WAIT_QUEUE_HEAD(queue_item_processed_wait_queue);
//DECLARE_WAIT_QUEUE_HEAD(userspace_item_ready);
DECLARE_WAIT_QUEUE_HEAD(userspace_item_processed_wait_queue);
DECLARE_WAIT_QUEUE_HEAD(read_queue_item_added_wait_queue);

// Declaration of the netfilter hooks
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
static struct nf_hook_ops *nf_local_routing_ops = NULL;
static struct nf_queue_handler *nf_queue_handler_ops = NULL;

static struct kmem_cache *pendingPacket_cache = NULL;
struct task_struct *queue_processor_thread = NULL;
static packet_processing_callback_t registered_callback = NULL;
static bool isActive = false;
static RoutingType lastRoutingType = NONE_ROUTING;

void register_packet_processing_callback(packet_processing_callback_t callback) {
    registered_callback = callback;
}

int register_queue_processor_thread_handler(packet_processor_thread_handler_t handler) {
    queue_processor_thread = kthread_run(handler, NULL, "packet_processor");
    if (IS_ERR(queue_processor_thread)) {
        printk(KERN_ERR "Failed to start packet_processor_thread.\n");
        cleanup_netfilter_hooks();  // Make sure to clean up before exiting
        return PTR_ERR(queue_processor_thread);
    }

    return 0;
}

static inline void handle_packet_decision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    const char *decision_icons[] = {
        "❓",  // UNDECIDED
        "🚮",  // DROP
        "📨",  // ACCEPT
        "✂️"   // MANIPULATE
    };

    if(packetTrip){
        free_pending_packetTrip(packetTrip);
        packetTrip = NULL;
    }

    char log_msg[256];
    snprintf(log_msg, sizeof(log_msg), "%s%s Extrava", decision_icons[decision], get_reason_text(reason));
    LOG_DEBUG_PACKET("%s", log_msg);
}

static int packet_queue_handler(struct nf_queue_entry *entry, unsigned int queuenum)
{
    PendingPacketRoundTrip *packetTrip = create_pending_packetTrip(entry, lastRoutingType);
    if (!packetTrip || !packetTrip->packet) {
        LOG_ERROR("Failed to create pending packet trip.");
        handle_packet_decision(packetTrip, DROP, MEMORY_FAILURE_PACKET);
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
        LOG_DEBUG_PACKET("Ethernet (MAC) header pointer not set in skb");
    }
    else {
        eth_header = (struct ethhdr *)skb_mac_header(skb);
        if (!eth_header) {
            LOG_DEBUG_PACKET("No Ethernet (MAC) header found");
            return NF_DROP;
        }

        LOG_DEBUG_PACKET("Ethernet (MAC) header found. Size: %zu bytes", sizeof(struct ethhdr));
    }

  
    // enqueue the packet for further processing
    // int queueLength = pq_len_packetTrip(&pending_packets_queue);
    // LOG_DEBUG_PACKET("Pushing to pending_packets_queue. Current size: %d; Size after push: %d", queueLength, queueLength + 1);
    // pq_push_packetTrip(&pending_packets_queue, packetTrip);
    
    int queueLength = pq_len_packetTrip(&read1_packets_queue);
    LOG_DEBUG_PACKET("Pushing to read1_packets_queue. Current Size: %d; Size after push: %d", queueLength, queueLength + 1);
    pq_push_packetTrip(&read1_packets_queue, packetTrip);
    // Signal to userspace that there's a packet to process (e.g., via a char device or netlink).
    //registered_callback();

    // wake up your processing thread
    //wake_up_process(queue_processor_thread);
    read_queue_item_added = true;
    wake_up_interruptible(&read_queue_item_added_wait_queue);
    //wake_up_process(queue_processor_thread);
    return 0;
}

static unsigned int nf_common_routing_handler(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    lastRoutingType = NONE_ROUTING;
    CHECK_NULL(LOG_TYPE_ERROR, state, NF_DROP);
    char* hookName = TEST_AND_MAKE_STRING_OR_EMPTY(LOG_TYPE_DEBUG_PACKET, state->hook, hook_to_string(value));
    char* routeTypeName = route_type_to_string(type);
    CHECK_NULL_AND_LOG(LOG_TYPE_ERROR, skb, NF_DROP, LOG_TYPE_DEBUG_PACKET, "skb pointer: %p length: %u", skb, skb->len);
    struct iphdr *ip_header = ip_hdr(skb);
    CHECK_NULL(LOG_TYPE_ERROR, ip_header, NF_DROP);
    CHECK_NULL(LOG_TYPE_ERROR, ip_header->protocol, NF_DROP);
    char* protocolName = ip_protocol_to_string(ip_header->protocol);
    TEST_NULL(LOG_TYPE_DEBUG_PACKET, ip_header->tos);
    char* type_of_service = TOS_TO_STRING(ip_header->tos);

    if (ip_header->protocol != IPPROTO_ICMP){ //} || type == LOCAL_ROUTING) {
        return NF_ACCEPT;
    }

    LOG_DEBUG_PACKET("ICMP packet received. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, type_of_service, hookName);

    if(!isActive){
        LOG_DEBUG_PACKET("Extrava is not active. Dropping packet. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, type_of_service, hookName);
        return NF_DROP;
    }

    struct ethhdr *eth_header;
    int mac_header_set = skb_mac_header_was_set(skb);
    if (!mac_header_set) {
        LOG_DEBUG_PACKET("Ethernet (MAC) header pointer not set in skb. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, type_of_service, hookName);
    }
    else {
        eth_header = (struct ethhdr *)skb_mac_header(skb);
        if (!eth_header) {
            LOG_DEBUG_PACKET("No Ethernet (MAC) header found. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, type_of_service, hookName);
            return NF_DROP;
        }

        LOG_DEBUG_PACKET("Ethernet (MAC) header found. Size: %zu bytes. Routing Type: %s, Protocol: %s, ToS: %u, Hook Type: %s", sizeof(struct ethhdr), routeTypeName, protocolName, type_of_service, hookName);
    }

    if (skb_is_nonlinear(skb)) {
        if (!skb_try_make_writable(skb, skb->len)) {
            LOG_ERROR("Failed to make skb writable.");
            // Couldn't make the skb writable, you might need to drop it.
            return NF_DROP;
        }
    }
    
    lastRoutingType = type;
    return NF_QUEUE; // we'll decide the fate of the packet later in our thread.

    

    // wait_for_completion_interruptible(&userspace_item_processed_wait_queue);
    // reinit_completion(&userspace_item_processed_wait_queue);
    // if(!currentPacketTrip){
    //     LOG_ERROR("PacketTrip is NULL after processing");
    //     handle_packet_decision(currentPacketTrip, DROP, ERROR);
    //     currentPacketTrip = NULL;
    //     return NF_DROP;
    // }

    // if (currentPacketTrip->decision == ACCEPT) {
    //     handle_packet_decision(currentPacketTrip, ACCEPT, USER_ACCEPT);
    //     currentPacketTrip = NULL;
    //     return NF_ACCEPT;
    // } else {
    //     handle_packet_decision(currentPacketTrip, DROP, USER_DROP);
    //     currentPacketTrip = NULL;
    //     return NF_DROP;
    // }
}

static unsigned int nf_pre_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state){
    return nf_common_routing_handler(PRE_ROUTING, priv, skb, state);
}

static unsigned int nf_post_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state){
    return nf_common_routing_handler(POST_ROUTING, priv, skb, state);
}

static unsigned int nf_local_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state){
    return nf_common_routing_handler(LOCAL_ROUTING, priv, skb, state);
}

static struct nf_queue_handler* setup_queue_handler_hook(void) {
    struct nf_queue_handler *ops = (struct nf_queue_handler*)kcalloc(1, sizeof(struct nf_queue_handler), GFP_KERNEL);
    if (!ops) {
        LOG_ERROR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->outfn = packet_queue_handler;
    ops->nf_hook_drop = hook_drop;

    return ops;
}

void hook_drop(struct net *net){
    LOG_DEBUG("Cleaning up netfilter queue hooks");
    if(nf_queue_handler_ops){
        nf_unregister_queue_handler();
        kfree(nf_queue_handler_ops);
        nf_queue_handler_ops = NULL;
    }

    if(isActive){
        LOG_DEBUG("Initializing packet queue handler");
        nf_queue_handler_ops = setup_queue_handler_hook();
        nf_register_queue_handler(nf_queue_handler_ops);
    }
}

/* To be called by the module initialization in extrava.c initialization function */
static struct nf_hook_ops* setup_individual_hook(nf_hookfn *handler, unsigned int hooknum) {
    struct nf_hook_ops *ops = (struct nf_hook_ops*)kcalloc(1, sizeof(struct nf_hook_ops), GFP_KERNEL);

    if (!ops) {
        LOG_ERROR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->hook = handler;
    ops->hooknum = hooknum;
    ops->pf = NFPROTO_IPV4;
    ops->priority = NF_IP_PRI_FIRST;

    if (nf_register_net_hook(&init_net, ops)) {
        LOG_ERROR("Failed to register netfilter hook with error: %ld.", PTR_ERR(&ops));
        kfree(ops);
        return NULL;
    }

    return ops;
}

/**
 * Handle errors during netfilter hook setup.
 */
static void handle_setup_error(struct task_struct *thread, struct nf_hook_ops *pre_ops, struct nf_hook_ops *post_ops, struct nf_hook_ops *local_ops) {
    if (pre_ops) {
        nf_unregister_net_hook(&init_net, pre_ops);
        kfree(pre_ops);
    }

    if (post_ops) {
        nf_unregister_net_hook(&init_net, post_ops);
        kfree(post_ops);
    }

    if (local_ops) {
        nf_unregister_net_hook(&init_net, local_ops);
        kfree(local_ops);
    }

    if (thread) {
        kthread_stop(thread);
    }

    pq_cleanup(&pending_packets_queue);  // Function to cleanup any remaining items in the queue
    pq_cleanup(&read1_packets_queue);
    pq_cleanup(&read2_packets_queue);
    pq_cleanup(&write_packets_queue);
    pq_cleanup(&injection_packets_queue);
}


int setup_netfilter_hooks(void) {
    LOG_DEBUG("Initializing packet queues");
    // init_completion(&read_queue_item_added_wait_queue);
    // init_completion(&queue_item_processed_wait_queue);
    // init_completion(&queue_processor_exited_wait_queue);
    // init_completion(&userspace_item_processed_wait_queue);
    read_queue_item_added = false;
    queue_item_processed = false;
    queue_processor_exited = false;
    userspace_item_processed = false;
    user_read = false;
    init_waitqueue_head(&read_queue_item_added_wait_queue);
    init_waitqueue_head(&queue_item_processed_wait_queue);
    init_waitqueue_head(&queue_processor_exited_wait_queue);
    init_waitqueue_head(&userspace_item_processed_wait_queue);



    pq_initialize(&pending_packets_queue);
    pq_initialize(&read1_packets_queue);
    pq_initialize(&read2_packets_queue);
    pq_initialize(&write_packets_queue);
    pq_initialize(&injection_packets_queue);

    LOG_DEBUG("Initializing netfilter pre-routing hooks");
    nf_pre_routing_ops = setup_individual_hook((nf_hookfn*)nf_pre_routing_handler, NF_INET_PRE_ROUTING);
    if (!nf_pre_routing_ops) {
        LOG_ERROR("Failed to set up pre-routing hook.");
        handle_setup_error(queue_processor_thread, NULL, NULL, NULL);
        return PTR_ERR(nf_pre_routing_ops);
    }

    LOG_DEBUG("Initializing netfilter post-routing hooks");
    nf_post_routing_ops = setup_individual_hook((nf_hookfn*)nf_post_routing_handler, NF_INET_POST_ROUTING);
    if (!nf_post_routing_ops) {
        LOG_ERROR("Failed to set up post-routing hook.");
        handle_setup_error(queue_processor_thread, nf_pre_routing_ops, NULL, NULL);
        return PTR_ERR(nf_post_routing_ops);
    }

    LOG_DEBUG("Initializing netfilter post-routing hooks");
    nf_local_routing_ops = setup_individual_hook((nf_hookfn*)nf_local_routing_handler, NF_INET_LOCAL_OUT);
    if (!nf_local_routing_ops) {
        LOG_ERROR("Failed to set up post-routing hook.");
        handle_setup_error(queue_processor_thread, nf_pre_routing_ops, nf_post_routing_ops, NULL);
        return PTR_ERR(nf_local_routing_ops);
    }

    LOG_DEBUG("Initializing packet queue handler");
    nf_queue_handler_ops = setup_queue_handler_hook();
    nf_register_queue_handler(nf_queue_handler_ops);
    isActive = true;

    LOG_INFO("Netfilter hooks registered.");
    return 0;
}

void cleanup_netfilter_hooks(void) {
    isActive = false;

    LOG_DEBUG("Cleaning up netfilter pre-routing hooks");
    if (nf_pre_routing_ops) {
        nf_unregister_net_hook(&init_net, nf_pre_routing_ops);
        kfree(nf_pre_routing_ops);
        nf_pre_routing_ops = NULL;
    }

    LOG_DEBUG("Cleaning up netfilter post-routing hooks");
    if (nf_post_routing_ops) {
        nf_unregister_net_hook(&init_net, nf_post_routing_ops);
        kfree(nf_post_routing_ops);
        nf_post_routing_ops = NULL;
    }

    LOG_DEBUG("Cleaning up netfilter local-routing hooks");
    if (nf_local_routing_ops) {
        nf_unregister_net_hook(&init_net, nf_local_routing_ops);
        kfree(nf_local_routing_ops);
        nf_local_routing_ops = NULL;
    }

    LOG_DEBUG("Cleaning up packet queue");
    pq_cleanup(&pending_packets_queue);
    pq_cleanup(&read1_packets_queue);
    pq_cleanup(&read2_packets_queue);
    pq_cleanup(&write_packets_queue);
    pq_cleanup(&injection_packets_queue);

    LOG_DEBUG("Cleaning up netfilter queue hooks");
    if(nf_queue_handler_ops){
        nf_unregister_queue_handler();
        kfree(nf_queue_handler_ops);
        nf_queue_handler_ops = NULL;
    }

    // Stop the packet_processor_thread
    LOG_DEBUG("Stopping packet processor thread");
    if (queue_processor_thread) {
        kthread_stop(queue_processor_thread);
    }

    LOG_INFO("Netfilter hooks unregistered.");
}