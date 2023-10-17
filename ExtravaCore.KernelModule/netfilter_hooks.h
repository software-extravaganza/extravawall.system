#ifndef NETFILTER_HOOKS
#define NETFILTER_HOOKS

// Includes
#include <linux/completion.h>
#include <linux/if_ether.h>
#include <linux/ip.h>
#include <linux/kernel.h>
#include <linux/kfifo.h>
#include <linux/kthread.h>
#include <linux/module.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <linux/netfilter/nfnetlink_queue.h>
#include <linux/skbuff.h>
#include "data_structures.h"
#include "helpers.h"
#include "logger.h"
#include "packet_queue.h"
#include "type_converters.h"

// Constants
#define MAX_PENDING_PACKETS 100
#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds

// Typedefs
typedef void (*packet_processing_callback_t)(void);
typedef int (*packet_processor_thread_handler_t)(void *data);

// Extern Variables (alphabetically ordered)
extern PacketQueue _injectionPacketsQueue;
extern PacketQueue _pendingPacketsQueue;
extern PacketQueue _read1PacketsQueue;
extern PacketQueue _read2PacketsQueue;
extern PacketQueue _writePacketsQueue;
extern DECLARE_WAIT_QUEUE_HEAD(_queueItemProcessedWaitQueue);
extern DECLARE_WAIT_QUEUE_HEAD(_queueProcessorExitedWaitQueue);
extern DECLARE_WAIT_QUEUE_HEAD(_readQueueItemAddedWaitQueue);
extern DECLARE_WAIT_QUEUE_HEAD(_userspaceItemProcessedWaitQueue);
extern bool _queueItemProcessed;
extern bool _queueProcessorExited;
extern bool _readQueueItemAdded;
extern bool _userRead;
extern bool _userspaceItemProcessed;
extern struct task_struct *_queueProcessorThread;

// Function Declarations (alphabetically ordered)
void CleanupNetfilterHooks(void);
void HandlePacketDecision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason);
void RegisterPacketProcessingCallback(packet_processing_callback_t callback);
int RegisterQueueProcessorThreadHandler(packet_processor_thread_handler_t handler);
int SetupNetfilterHooks(void);

#endif // NETFILTER_HOOKS