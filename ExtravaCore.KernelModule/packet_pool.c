#include "packet_pool.h"

static void InitializePool(PacketPool *pool, int cpu, int initialSize) ;
static void FreePool(PacketPool *pool);
PacketPool *pools;
// Initialize all pools
void InitializeAllPools(int initialSize) {
    int cpuCount = num_online_cpus();
    pools = (PacketPool *)kmalloc(cpuCount * sizeof(PacketPool), GFP_KERNEL);

    for (int i = 0; i < cpuCount; i++) {
        InitializePool(&pools[i], i, initialSize);
    }
}

// Initialize a single pool
static void InitializePool(PacketPool *pool, int cpu, int initialSize) {
    if (initialSize < MIN_POOL_SIZE) initialSize = MIN_POOL_SIZE;
    if (initialSize > MAX_POOL_SIZE) initialSize = MAX_POOL_SIZE;

    pool->packets = (PendingPacketRoundTrip *)kmalloc(initialSize * sizeof(PendingPacketRoundTrip), GFP_KERNEL);
    pool->usedCount = 0;
    pool->currentSize = initialSize;

    for (int i = 0; i < initialSize; i++) {
        pool->packets[i] = *CreatePendingPacketTrip();
        // If there are other initializations needed for the PendingPacketRoundTrip structure, do them here.
    }

    LOG_DEBUG("Successfully created PendingPacketRoundTrip Pool on cpu: %d", cpu);
}

// Get a free packet trip from the current CPU's pool
PendingPacketRoundTrip* GetFreePacketTrip(struct nf_queue_entry *entry, RoutingType type) {
    if (!entry) {
        LOG_DEBUG("Entry provided is NULL.");
        return NULL;
    }

    int cpuId = smp_processor_id();
    PacketPool *currentPool = &pools[cpuId];
    if (currentPool->usedCount == currentPool->currentSize) {
        if (currentPool->currentSize < MAX_POOL_SIZE) {
            // Expand the currentPool
            int newSize = currentPool->currentSize * 2;
            if (newSize > MAX_POOL_SIZE) newSize = MAX_POOL_SIZE;

            currentPool->packets = (PendingPacketRoundTrip *)krealloc(currentPool->packets, newSize * sizeof(PendingPacketRoundTrip), GFP_KERNEL);
            for (int i = currentPool->currentSize; i < newSize; i++) {
                ResetPendingPacketTrip(&currentPool->packets[i]);
            }
            currentPool->currentSize = newSize;
        } else {
            return NULL; // Pool is full and can't be expanded
        }
    }

    for (int i = 0; i < currentPool->currentSize; i++) {
        if (currentPool->packets[i].available) {
            currentPool->usedCount++;
            PendingPacketRoundTrip* packetTrip = &currentPool->packets[i];
            packetTrip->available = false;
            packetTrip->entry = entry;
            packetTrip->routingType = type;
            packetTrip->createdTime = ktime_get();
            packetTrip->attempts = 0;
            packetTrip->slotAssigned = -2;
            AddMetaDataToPacketTrip(packetTrip, type);
            return packetTrip;
        }
    }
    return NULL; // Shouldn't reach here
}


// Return or reset a packet trip to the current CPU's pool
void ReturnPacketTrip(PendingPacketRoundTrip *packetTrip) {
    int cpuId = smp_processor_id();
    PacketPool *currentPool = &pools[cpuId];
    ResetPendingPacketTrip(packetTrip);
    packetTrip->available = true;
    currentPool->usedCount--;
}

// Free all pools
void FreeAllPools() {
    int cpuCount = num_online_cpus();
    for (int i = 0; i < cpuCount; i++) {
        FreePool(&pools[i]);
    }
    kfree(pools);
}

// Free a single pool
static void FreePool(PacketPool *pool) {
    for (int i = 0; i < pool->currentSize; i++) {
        FreePendingPacketTrip(&pool->packets[i]);
    }
    kfree(pool->packets);
    pool->currentSize = 0;
    pool->usedCount = 0;
}