#ifndef NETFILTER_HOOKS
#define NETFILTER_HOOKS

// Includes
#include <linux/completion.h>
#include <linux/if_ether.h>
#include <linux/ip.h>
#include <linux/tcp.h>
#include <linux/kernel.h>
#include <linux/kfifo.h>
#include <linux/kthread.h>
#include <linux/semaphore.h>
#include <linux/module.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <linux/netfilter/nfnetlink_queue.h>
#include <linux/skbuff.h>
#include "data_structures.h"
#include "helpers.h"
#include "logger.h"
#include "module_control.h"
#include "packet_queue.h"
#include "packet_pool.h"
#include "type_converters.h"
#include "ringbuffer_types.h"

// Constants
#define MAX_PENDING_PACKETS 100
#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds

extern DEFINE_PER_CPU(PacketQueue, cpu_packet_queues);
extern spinlock_t packet_queue_lock;

// Typedefs
typedef void (*packet_processing_callback_t)(void);
typedef int (*packet_processor_thread_handler_t)(void *data);

// Extern Variables (alphabetically ordered)
extern int default_packet_response;
extern bool force_icmp;
extern PacketQueue *_injectionPacketsQueue;
extern PacketQueue *_pendingPacketsQueue;
extern PacketQueue *_read1PacketsQueue;
extern PacketQueue *_read2PacketsQueue;
extern PacketQueue *_write1PacketsQueue;
extern PacketQueue *_write2PacketsQueue;
extern PacketQueue *_completedQueue;
extern bool _queueItemProcessed;
extern bool _queueProcessorExited;
extern bool _readQueueItemAdded;
extern atomic_t _pendingQueueItemAdded;
extern bool _userRead;
extern bool _userspaceItemProcessed;
extern long PacketsIngressCounter;
extern long PacketsQueuedCounter;
extern long PacketsCapturedCounter;
extern long PacketsProcessedCounter;
extern long PacketsAcceptCounter;
extern long PacketsManipulateCounter;
extern long PacketsDropCounter;
extern long PacketsStaleCounter;
extern long ReadWaitCounter;
extern long ReadWokeCounter;
extern long WriteWaitCounter;
extern long WriteWokeCounter;
extern long QueueProcessorWokeCounter;
extern long QueueProcessorWaitCounter;

extern long SystemBufferSlotsUsedCounter;
extern long SystemBufferSlotsClearedCounter;
extern long SystemBufferActiveUsedSlots;
extern long SystemBufferActiveFreeSlots;

extern struct task_struct *_queueProcessorThread;

extern atomic_t IsProcessingPacketTrip;

// Function Declarations (alphabetically ordered)
void CleanupNetfilterHooks(void);
void HandlePacketDecision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason);
void RegisterPacketProcessingCallback(packet_processing_callback_t callback);
int RegisterQueueProcessorThreadHandler(packet_processor_thread_handler_t handler);
int SetupNetfilterHooks(void);
void DecommissionPacketTrip(PendingPacketRoundTrip *packetTrip);
void CleanUpStaleItemsOnQueue(PacketQueue* queue, const char *queueName);
void NetFilterShouldCaptureChangeHandler(bool shouldCapture);
//void StopQueueProcessorThread(void) ;

#define CLEANUP_STALE_ITEMS_ON_QUEUE(queue) CleanUpStaleItemsOnQueue(queue, #queue)

#endif // NETFILTER_HOOKS