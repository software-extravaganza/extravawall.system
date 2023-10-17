#include <linux/module.h>
#include <linux/proc_fs.h>
#include <linux/seq_file.h>
#include <linux/uaccess.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include "logger.h"
#include "data_structures.h"
#include "packet_queue.h"
#include "netfilter_hooks.h"

#ifndef USERSPACE_COMM
#define USERSPACE_COMM
extern bool processingPacketTrip;

extern wait_queue_head_t queue_item_processed_wait_queue;
extern wait_queue_head_t userspace_item_ready;
extern wait_queue_head_t userspace_item_processed_wait_queue;
extern wait_queue_head_t read_queue_item_added_wait_queue;
extern wait_queue_head_t user_read_wait_queue;
extern wait_queue_head_t queue_processor_exited_wait_queue;

int setup_user_space_comm(void);
void cleanup_user_space_comm(void);

#endif // USERSPACE_COMM