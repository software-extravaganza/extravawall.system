#ifndef NETFILTER_HOOKS
#define NETFILTER_HOOKS

#include <linux/module.h>
#include <linux/kernel.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <linux/netfilter/nfnetlink_queue.h>
#include <linux/ip.h>
#include <linux/if_ether.h>
#include <linux/kthread.h>
#include <linux/completion.h>
#include <linux/skbuff.h>
#include "logger.h"
#include "helpers.h"
#include "data_structures.h"
#include "packet_queue.h"
#include "type_converters.h"
#include <linux/kfifo.h>

#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds
#define MAX_PENDING_PACKETS 100

typedef void (*packet_processing_callback_t)(void);
typedef int (*packet_processor_thread_handler_t)(void *data);


extern bool read_queue_item_added;
extern bool queue_item_processed;
extern bool queue_processor_exited;
extern bool userspace_item_processed;
extern bool user_read;
extern struct kfifo pending_packets_queue;
extern struct kfifo read1_packets_queue;
extern struct kfifo read2_packets_queue;
extern struct kfifo write_packets_queue;
extern struct kfifo injection_packets_queue;
extern wait_queue_head_t queue_item_processed_wait_queue;
extern wait_queue_head_t userspace_item_ready;
extern wait_queue_head_t userspace_item_processed_wait_queue;
extern wait_queue_head_t read_queue_item_added_wait_queue;
extern wait_queue_head_t user_read_wait_queue;


extern struct task_struct *queue_processor_thread;

int setup_netfilter_hooks(void);
void cleanup_netfilter_hooks(void);
void register_packet_processing_callback(packet_processing_callback_t callback);
int register_queue_processor_thread_handler(packet_processor_thread_handler_t handler);
void hook_drop(struct net *net);
#endif // NETFILTER_HOOKS