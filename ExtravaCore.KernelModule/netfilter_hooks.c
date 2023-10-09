#include "netfilter_hooks.h"


#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds
#define MAX_PENDING_PACKETS 100

static PacketQueue pending_packets_queue;
static DEFINE_SPINLOCK(pending_packets_lock);
static DECLARE_COMPLETION(queue_item_added);

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
        // Clear the completion so it can be used again.
        reinit_completion(&queue_item_added);

        // Process the queue until empty.
        while (!pq_is_empty(&pending_packets_queue)) {
            if (registered_callback) {
                registered_callback(&pending_packets_queue);
            }
        }

        wake_up(&pending_packets_queue.waitQueue);
    }
    return 0;
}

static inline void lock_pending_packets(void) {
    spin_lock(&pending_packets_lock);
}

static inline void unlock_pending_packets(void) {
    spin_unlock(&pending_packets_lock);
}

static inline void handle_packet_decision(PendingPacket *packet, RoutingDecision decision, DecisionReason reason) {
    const char *decision_icons[] = {
        "❓",  // UNDECIDED
        "🚮",  // DROP
        "📨",  // ACCEPT
        "✂️"   // MANIPULATE
    };

    lock_pending_packets();
    pq_pop_packet(&pending_packets_queue);
    unlock_pending_packets();
    free_pending_packet(packet);

    // Construct the log message using the provided decision and reason
    char log_msg[256];
    snprintf(log_msg, sizeof(log_msg), "%s%s Extrava", decision_icons[decision], get_reason_text(reason));
    //LOG_INFO(log_msg);
}

static unsigned int nf_common_routing_handler(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct iphdr *iph = ip_hdr(skb);

    if (iph->protocol != IPPROTO_ICMP)
        return NF_ACCEPT;

    // Attempt to add the packet to the queue
    PendingPacket *packet = create_pending_packet(skb);
    if (!packet) {
        handle_packet_decision(NULL, DROP, MEMORY_FAILURE);
        return NF_DROP;
    }

    lock_pending_packets();
    bool added = pq_add_packet(&pending_packets_queue, packet);
    unlock_pending_packets();
    if (!added) {
        handle_packet_decision(packet, DROP, BUFFER_FULL);
        return NF_DROP;
    }
    
    complete(&queue_item_added);
    int ret = wait_event_interruptible_timeout(pending_packets_queue.waitQueue, packet->processed, PACKET_PROCESSING_TIMEOUT);

    if (ret <= 0) {
        DecisionReason reason = (ret == 0) ? TIMEOUT : ERROR;
        handle_packet_decision(packet, DROP, reason);
        return NF_DROP;
    }

    // Process packet based on decision
    if (packet->decision == ACCEPT) {
        handle_packet_decision(packet, ACCEPT, USER_ACCEPT);
        return NF_ACCEPT;
    } else {
        handle_packet_decision(packet, DROP, USER_DROP);
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
        LOG_ERR("Failed to register netfilter hook.");
        kfree(ops);
        return NULL;
    }

    return ops;
}


void setup_netfilter_hooks(void) {
    // Initialize the packet queue
    pq_initialize(&pending_packets_queue);
    queue_processor_thread = kthread_run(queue_processor_fn, NULL, "queue_processor_thread");
    if (IS_ERR(queue_processor_thread)) {
        LOG_ERR("Failed to create queue processor thread.");
        return;
    }

    nf_pre_routing_ops = setup_individual_hook((nf_hookfn*)nf_pre_routing_handler, NF_INET_PRE_ROUTING);
    if (!nf_pre_routing_ops) {
        LOG_ERR("Failed to set up pre-routing hook.");
        return;
    }

    nf_post_routing_ops = setup_individual_hook((nf_hookfn*)nf_post_routing_handler, NF_INET_POST_ROUTING);
    if (!nf_post_routing_ops) {
        LOG_ERR("Failed to set up post-routing hook.");
        // Consider cleanup actions if necessary
        return;
    }

    LOG_INFO("Netfilter hooks registered.");
}

void cleanup_netfilter_hooks(void) {
    if(nf_pre_routing_ops) {
		nf_unregister_net_hook(&init_net, nf_pre_routing_ops);
		kfree(nf_pre_routing_ops);
	}

    if(nf_post_routing_ops) {
		nf_unregister_net_hook(&init_net, nf_post_routing_ops);
		kfree(nf_post_routing_ops);
	}

    if (queue_processor_thread) {
        kthread_stop(queue_processor_thread);
    }

    LOG_INFO("Netfilter hooks unregistered.");
}