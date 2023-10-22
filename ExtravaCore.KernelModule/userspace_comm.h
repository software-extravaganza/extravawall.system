#include <linux/module.h>
#include <linux/proc_fs.h>
#include <linux/seq_file.h>
#include <linux/uaccess.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <linux/limits.h>
#include "logger.h"
#include "module_control.h"
#include "data_structures.h"
#include "packet_queue.h"
#include "netfilter_hooks.h"

#ifndef USERSPACE_COMM
#define USERSPACE_COMM
extern bool processingPacketTrip;
extern bool _userWrite;

// Public fields
extern wait_queue_head_t QueueItemProcessedWaitQueue; 
extern wait_queue_head_t UserspaceItemReady; 
extern wait_queue_head_t UserspaceItemProcessedWaitQueue; 
extern wait_queue_head_t UserspaceItemProcessedWaitQueueCleared; 
extern wait_queue_head_t PendingQueueItemAddedWaitQueue; 
extern wait_queue_head_t UserReadWaitQueue; 
extern wait_queue_head_t UserWriteWaitQueue;
extern wait_queue_head_t QueueProcessorExitedWaitQueue; 
extern wait_queue_head_t ReInjectionProcessorExitedWaitQueue; 

int SetupUserSpaceCommunication(void);
void CleanupUserSpaceCommunication(void);
int _packetProcessorThreadHandler(void *data);
int _injectionProcessorThreadHandler(void *data);
#endif // USERSPACE_COMM