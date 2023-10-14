#include "netfilter_hooks.h"

PacketQueue pending_packets_queue;
static DEFINE_SPINLOCK(pending_packets_lock);

DECLARE_COMPLETION(queue_processor_exited);
DECLARE_COMPLETION(queue_item_processed);
DECLARE_COMPLETION(userspace_item_ready);
DECLARE_COMPLETION(userspace_item_processed);
DECLARE_COMPLETION(queue_item_added);
PendingPacketRoundTrip *currentPacketTrip = NULL;
PendingPacketRoundTrip *processingPacketTrip= NULL;

// Declaration of the netfilter hooks
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
static struct task_struct *queue_processor_thread = NULL;
static struct kmem_cache *pendingPacket_cache = NULL;
unsigned long flags;


static packet_processing_callback_t registered_callback = NULL;

void register_packet_processing_callback(packet_processing_callback_t callback) {
    registered_callback = callback;
}

static inline void lock_pending_packets(void) {
    spin_lock_irqsave(&pending_packets_lock, flags);
}

static inline void unlock_pending_packets(void) {
    spin_unlock_irqrestore(&pending_packets_lock, flags);
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
    LOG_DEBUG("%s", log_msg);
}

static unsigned int nf_common_routing_handler(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct iphdr *iph = ip_hdr(skb);
    if (!iph) {
        LOG_ERROR("IP header is NULL.");
        return NF_DROP;
    }

    if (iph->protocol != IPPROTO_ICMP) {
        return NF_ACCEPT;
    }

    LOG_DEBUG("ICMP packet received.");
    LOG_DEBUG("skb pointer: %p", skb);
    if (skb) {
        LOG_DEBUG("skb length: %u", skb->len);
    }

    currentPacketTrip = create_pending_packetTrip(skb);
    if (!currentPacketTrip || !currentPacketTrip->packet || !currentPacketTrip->packet->header) {
        DecisionReason reason = !currentPacketTrip ? MEMORY_FAILURE_PACKET : MEMORY_FAILURE_PACKET_HEADER;
        handle_packet_decision(currentPacketTrip, DROP, reason);
        currentPacketTrip = NULL;
        return NF_DROP;
    }

    bool is_processed = false;
    if (!registered_callback) {
        LOG_ERROR("No callback registered.");
        handle_packet_decision(currentPacketTrip, DROP, ERROR);
        currentPacketTrip = NULL;
        return NF_DROP;
    } 

    registered_callback();
    // enqueue the packet for further processing
    enqueue_packet_for_processing(currentPacketTrip);

    // wake up your processing thread
    wake_up_process(queue_processor_thread);

    return NF_STOLEN; // we'll decide the fate of the packet later in our thread.
    // wait_for_completion_interruptible(&userspace_item_processed);
    // reinit_completion(&userspace_item_processed);
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
static void handle_setup_error(struct task_struct *thread, struct nf_hook_ops *pre_ops, struct nf_hook_ops *post_ops) {
    if (pre_ops) {
        nf_unregister_net_hook(&init_net, pre_ops);
        kfree(pre_ops);
    }

    if (post_ops) {
        nf_unregister_net_hook(&init_net, post_ops);
        kfree(post_ops);
    }

    if (thread) {
        kthread_stop(thread);
    }

    pq_cleanup(&pending_packets_queue);  // Function to cleanup any remaining items in the queue
}

void setup_netfilter_hooks(void) {
    init_completion(&queue_item_added);
    init_completion(&queue_item_processed);
    init_completion(&queue_processor_exited);
    init_completion(&userspace_item_processed);
    pq_initialize(&pending_packets_queue);

    nf_pre_routing_ops = setup_individual_hook((nf_hookfn*)nf_pre_routing_handler, NF_INET_PRE_ROUTING);
    if (!nf_pre_routing_ops) {
        LOG_ERROR("Failed to set up pre-routing hook.");
        handle_setup_error(queue_processor_thread, NULL, NULL);
        return;
    }

    nf_post_routing_ops = setup_individual_hook((nf_hookfn*)nf_post_routing_handler, NF_INET_POST_ROUTING);
    if (!nf_post_routing_ops) {
        LOG_ERROR("Failed to set up post-routing hook.");
        handle_setup_error(queue_processor_thread, nf_pre_routing_ops, NULL);
        return;
    }

    LOG_INFO("Netfilter hooks registered.");
}

void cleanup_netfilter_hooks(void) {
    if (nf_pre_routing_ops) {
        nf_unregister_net_hook(&init_net, nf_pre_routing_ops);
        kfree(nf_pre_routing_ops);
    }

    if (nf_post_routing_ops) {
        nf_unregister_net_hook(&init_net, nf_post_routing_ops);
        kfree(nf_post_routing_ops);
    }

    pq_cleanup(&pending_packets_queue);

    LOG_INFO("Netfilter hooks unregistered.");
}