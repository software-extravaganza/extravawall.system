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

int setup_user_space_comm(void);
void cleanup_user_space_comm(void);

#endif // USERSPACE_COMM