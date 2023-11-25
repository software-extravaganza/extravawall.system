#ifndef _PACKET_POOL_H_
#define _PACKET_POOL_H_

#include <linux/skbuff.h>
#include <linux/kfifo.h>
#include <linux/semaphore.h>
#include "data_structures.h"
#include "data_factories.h"
#include "packet_queue.h"

#define MIN_POOL_SIZE 1000
#define MAX_POOL_SIZE 50000

typedef struct {
    PendingPacketRoundTrip *packets; // Dynamic array of packet trips
    int usedCount;                   // Number of used packet trips
    int currentSize;                 // Current size of the pool
} PacketPool;

extern PacketPool *pools;

void InitializeAllPools(int initialSize);
PendingPacketRoundTrip* GetFreePacketTrip(struct nf_queue_entry *entry, __u64 id, RoutingType type) ;
void ReturnPacketTrip(PendingPacketRoundTrip *packetTrip);
void FreeAllPools(void);

#endif // _PACKET_POOL_H_
