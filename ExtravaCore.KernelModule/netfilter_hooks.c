#include "netfilter_hooks.h"


#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds
#define MAX_PENDING_PACKETS 100

static PacketQueue pending_packets_queue;
static DEFINE_SPINLOCK(pending_packets_lock);
static DECLARE_COMPLETION(queue_item_added);
static DECLARE_COMPLETION(queue_item_processed);
static DECLARE_COMPLETION(queue_processor_exited);

// Declaration of the netfilter hooks
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
static struct task_struct *queue_processor_thread;


static packet_processing_callback_t registered_callback = NULL;

void register_packet_processing_callback(packet_processing_callback_t callback) {
    registered_callback = callback;
}

int queue_processor_fn(void *data) {
    while (!kthread_should_stop()) {
        wait_for_completion_interruptible(&queue_item_added);
        reinit_completion(&queue_item_added);

        if (registered_callback) {
            registered_callback(&pending_packets_queue);
        }

        // Notify that we've processed an item
        complete(&queue_item_processed);
    }

    // Notify that the thread is about to exit
    complete(&queue_processor_exited);

    return 0;
}

static inline void lock_pending_packets(void) {
    spin_lock(&pending_packets_lock);
}

static inline void unlock_pending_packets(void) {
    spin_unlock(&pending_packets_lock);
}

static inline void handle_packet_decision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    const char *decision_icons[] = {
        "❓",  // UNDECIDED
        "🚮",  // DROP
        "📨",  // ACCEPT
        "✂️"   // MANIPULATE
    };

    lock_pending_packets();
    pq_pop_packetTrip(&pending_packets_queue);
    unlock_pending_packets();
    complete(&queue_item_processed);

    if(packetTrip){
        free_pending_packetTrip(packetTrip);
    }
    // Construct the log message using the provided decision and reason
    char log_msg[256];
    snprintf(log_msg, sizeof(log_msg), "%s%s Extrava", decision_icons[decision], get_reason_text(reason));
    LOG_INFO("%s", log_msg);
}

static unsigned int nf_common_routing_handler(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct iphdr *iph = ip_hdr(skb);
    
    if (iph->protocol != IPPROTO_ICMP)
        return NF_ACCEPT;

    LOG_DEBUG("ICMP packet received.");
   
    if (!iph) {
        LOG_ERR("IP header is NULL.");
        return NF_DROP;
    }

    LOG_DEBUG("skb pointer: %p", skb);
    if (skb) {
        LOG_DEBUG("skb length: %zu", skb->len);
    }

    // Attempt to add the packet to the queue
    PendingPacketRoundTrip *packetTrip = create_pending_packetTrip(skb);
    if (!packetTrip) {
        // This condition might occur if there's a memory allocation failure.
        handle_packet_decision(NULL, DROP, MEMORY_FAILURE_PACKET);
        return NF_DROP;
    }

    if (!packetTrip->packet || !packetTrip->packet->header) {
        // Either the packet itself or its header failed to be created.
        handle_packet_decision(NULL, DROP, MEMORY_FAILURE_PACKET_HEADER);
        return NF_DROP;
    }

    lock_pending_packets();
    bool added = pq_add_packetTrip(&pending_packets_queue, packetTrip);
    unlock_pending_packets();
    if (!added) {
        handle_packet_decision(packetTrip, DROP, BUFFER_FULL);
        return NF_DROP;
    }
    
    // Notify that a packet has been added
    complete(&queue_item_added);

    // Wait for the packet to be processed or until a timeout
    long timeout_ret = wait_for_completion_interruptible_timeout(&queue_item_processed, PACKET_PROCESSING_TIMEOUT);

    // If timeout or error occurred during waiting
    if (timeout_ret <= 0) {
        DecisionReason reason = (timeout_ret == 0) ? TIMEOUT : ERROR;
        handle_packet_decision(packetTrip, DROP, reason);
        return NF_DROP;
    }

    // Now, wait for the packet properties to be set
    int ret = wait_event_interruptible_timeout(pending_packets_queue.waitQueue, 
                                               packetTrip->packet->headerProcessed &&
                                               packetTrip->packet->dataProcessed &&
                                               packetTrip->responsePacket->headerProcessed &&
                                               packetTrip->responsePacket->dataProcessed,
                                               PACKET_PROCESSING_TIMEOUT);

    if (ret <= 0) {
        DecisionReason reason = (ret == 0) ? TIMEOUT : ERROR;
        handle_packet_decision(packetTrip, DROP, reason);
        return NF_DROP;
    }

    // Process packet based on decision
    if (packetTrip->decision == ACCEPT) {
        handle_packet_decision(packetTrip, ACCEPT, USER_ACCEPT);
        return NF_ACCEPT;
    } else {
        handle_packet_decision(packetTrip, DROP, USER_DROP);
        return NF_DROP;
    }
}


static unsigned int nf_pre_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state)
{
    return nf_common_routing_handler(PRE_ROUTING, priv, skb, state);
}

static unsigned int nf_post_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state)
{
    return nf_common_routing_handler(POST_ROUTING, priv, skb, state);
}

/* To be called by the module initialization in extrava.c initialization function */
static struct nf_hook_ops* setup_individual_hook(nf_hookfn *handler, unsigned int hooknum) {
    struct nf_hook_ops *ops = (struct nf_hook_ops*)kcalloc(1, sizeof(struct nf_hook_ops), GFP_KERNEL);
    
    if (!ops) {
        LOG_ERR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->hook = handler;
    ops->hooknum = hooknum;
    ops->pf = NFPROTO_IPV4;
    ops->priority = NF_IP_PRI_FIRST;

    if (nf_register_net_hook(&init_net, ops)) {
        LOG_ERR("Failed to register netfilter hook with error: %ld.", PTR_ERR(ops));
        kfree(ops);
        return NULL;
    }

    return ops;
}

void pq_cleanup(PacketQueue *queue) {
    lock_pending_packets();
    while (!pq_is_empty(queue)) {
        PendingPacketRoundTrip *packetTrip = pq_pop_packetTrip(queue);
        if (packetTrip) {
            free_pending_packetTrip(packetTrip);
        }
    }
    unlock_pending_packets();
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
        // TODO: Ensure that the thread has finished processing, or ensure safe termination
    }

    pq_cleanup(&pending_packets_queue);  // Function to cleanup any remaining items in the queue
}


void setup_netfilter_hooks(void) {
    // Initialize the packet queue
    pq_initialize(&pending_packets_queue);

    queue_processor_thread = kthread_run(queue_processor_fn, NULL, "queue_processor_thread");
    if (IS_ERR(queue_processor_thread)) {
        LOG_ERR("Failed to create queue processor thread.");
        handle_setup_error(NULL, NULL, NULL);
        return;
    }

    nf_pre_routing_ops = setup_individual_hook((nf_hookfn*)nf_pre_routing_handler, NF_INET_PRE_ROUTING);
    if (!nf_pre_routing_ops) {
        LOG_ERR("Failed to set up pre-routing hook.");
        handle_setup_error(queue_processor_thread, NULL, NULL);
        return;
    }

    nf_post_routing_ops = setup_individual_hook((nf_hookfn*)nf_post_routing_handler, NF_INET_POST_ROUTING);
    if (!nf_post_routing_ops) {
        LOG_ERR("Failed to set up post-routing hook.");
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

    if (queue_processor_thread) {
        kthread_stop(queue_processor_thread);

        // Wait for the thread to finish processing
        wait_for_completion(&queue_processor_exited);
    }

    pq_cleanup(&pending_packets_queue);

    LOG_INFO("Netfilter hooks unregistered.");
}