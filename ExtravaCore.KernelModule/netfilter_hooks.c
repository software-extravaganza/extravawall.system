#include "netfilter_hooks.h"


#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds
#define MAX_PENDING_PACKETS 100

PacketQueue pending_packets_queue;
static DEFINE_SPINLOCK(pending_packets_lock);
static DECLARE_COMPLETION(queue_item_added);
static DECLARE_COMPLETION(queue_item_processed);
static DECLARE_COMPLETION(queue_processor_exited);
wait_queue_head_t waitQueue;


// Declaration of the netfilter hooks
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
static struct task_struct *queue_processor_thread;
static struct kmem_cache *pendingPacket_cache;
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

int queue_processor_fn(void *data) {
    while (!kthread_should_stop()) {
        // Wait until there's a packet to process
        wait_for_completion_interruptible(&queue_item_added);

        // Check if we should stop the thread immediately after waiting.
        if (kthread_should_stop()) {
            break;
        }

        PendingPacketRoundTrip *packetTrip = pq_pop_packetTrip(&pending_packets_queue);

        if (packetTrip) {
            if (registered_callback) {
                registered_callback(packetTrip);
            }
            // Notify that this specific packet has been processed
            complete(&packetTrip->packet_processed);
            wake_up_interruptible(&waitQueue);
            complete(&queue_item_processed);  // Signal that an item has been processed
        }

        // Reinitialize the completion here, before waiting again.
        reinit_completion(&queue_item_added);
    }

    // Notify that the thread is about to exit
    complete(&queue_processor_exited);
    return 0;
}

static inline void handle_packet_decision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    const char *decision_icons[] = {
        "❓",  // UNDECIDED
        "🚮",  // DROP
        "📨",  // ACCEPT
        "✂️"   // MANIPULATE
    };

    pq_pop_packetTrip(&pending_packets_queue);
    complete(&queue_item_processed);

    if(packetTrip){
        free_pending_packetTrip(packetTrip);
    }
    wake_up_interruptible(&waitQueue);
    // Construct the log message using the provided decision and reason
    char log_msg[256];
    snprintf(log_msg, sizeof(log_msg), "%s%s Extrava", decision_icons[decision], get_reason_text(reason));
    LOG_INFO("%s", log_msg);
}

static unsigned int nf_common_routing_handler(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct iphdr *iph = ip_hdr(skb);
    if (!iph) {
        LOG_ERR("IP header is NULL.");
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

    PendingPacketRoundTrip *packetTrip = create_pending_packetTrip(skb);
    if (!packetTrip || !packetTrip->packet || !packetTrip->packet->header) {
        DecisionReason reason = !packetTrip ? MEMORY_FAILURE_PACKET : MEMORY_FAILURE_PACKET_HEADER;
        handle_packet_decision(packetTrip, DROP, reason);
        return NF_DROP;
    }

    lock_pending_packets();  // Lock
    bool added = pq_add_packetTrip(&pending_packets_queue, packetTrip);
    unlock_pending_packets();  // Unlock

    if (!added) {
        handle_packet_decision(packetTrip, DROP, BUFFER_FULL);
        return NF_DROP;
    }

    init_completion(&packetTrip->packet_processed);
    complete(&queue_item_added);

    unsigned long start_time = jiffies;
    unsigned long end_time = start_time + PACKET_PROCESSING_TIMEOUT;
    bool is_processed = false;

    // Avoid using functions that can sleep or block in sections of code that are within an RCU read-side critical section. 
    // While is not elegant but using busy-waiting (spin-waiting) to yield the CPU for a short period of time if the condition isn't met
    // This also give time for the queue processor thread to process the packet
    while (time_before(jiffies, end_time)) {
        // Check the condition
        if (packetTrip->packet->headerProcessed &&
            packetTrip->packet->dataProcessed &&
            packetTrip->responsePacket->headerProcessed &&
            packetTrip->responsePacket->dataProcessed) {
            is_processed = true;
            break;
        }
        // Yield the processor for a short duration
        schedule_timeout_uninterruptible(1);
    }

    if (!is_processed) {
        // Handle timeout case
        DecisionReason reason = TIMEOUT;
        handle_packet_decision(packetTrip, DROP, reason);
        return NF_DROP;
    }

    if (packetTrip->decision == ACCEPT) {
        handle_packet_decision(packetTrip, ACCEPT, USER_ACCEPT);
        return NF_ACCEPT;
    } else {
        handle_packet_decision(packetTrip, DROP, USER_DROP);
        return NF_DROP;
    }
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
        LOG_ERR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->hook = handler;
    ops->hooknum = hooknum;
    ops->pf = NFPROTO_IPV4;
    ops->priority = NF_IP_PRI_FIRST;

    if (nf_register_net_hook(&init_net, ops)) {
        LOG_ERR("Failed to register netfilter hook with error: %ld.", PTR_ERR(&ops));
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
    // Initialize the packet queue
    init_completion(&queue_item_added);
    init_completion(&queue_item_processed);
    init_completion(&queue_processor_exited);
    pq_initialize(&pending_packets_queue);
    init_waitqueue_head(&waitQueue);
    pendingPacket_cache = kmem_cache_create("pendingPacket_cache", sizeof(PendingPacket), 0, 0, NULL);
    if (!pendingPacket_cache) {
        LOG_ERR("Failed to create kmem_cache for pendingPacket_cache.");
        handle_setup_error(queue_processor_thread, nf_pre_routing_ops, nf_post_routing_ops);
        return;
    }

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
        complete(&queue_item_added);  // Signal any waiting instances
        wake_up_interruptible(&waitQueue);
        kthread_stop(queue_processor_thread); // Stop the thread
        wait_for_completion(&queue_processor_exited);  // Wait for the thread to confirm it has exited
    }

    pq_cleanup(&pending_packets_queue);

    LOG_INFO("Netfilter hooks unregistered.");
}