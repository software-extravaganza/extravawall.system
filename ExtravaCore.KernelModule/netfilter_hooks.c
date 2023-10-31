/**
 * This module provides netfilter hooks for packet processing in the Linux kernel.
 * It allows for custom processing of packets at various stages of the network stack.
 */
#include "netfilter_hooks.h"

#define ERROR_HOOK_SETUP -1
#define ERROR_MEMORY_ALLOC -2
#define ERROR_WORKQUEUE_CREATION -3

// Constants for decision icons
#define UNDECIDED_ICON "❓"
#define DROP_ICON "🚮"
#define ACCEPT_ICON "📨"
#define MANIPULATE_ICON "✂️"

//DECLARE_WAIT_QUEUE_HEAD(wq);

const char* MESSAGE_PACKETS_CAPTURED = "(Packets stats) ingress: %ld, queued: %ld, captured: %ld; processed: %ld; accepted: %ld; modified: %ld; dropped: %ld; stale: %ld | (Time stats) average:%s; 90th percentile: %s";
const char* MESSAGE_EVENTS_CAPTURED = "Read Wait: %ld; Read Woke: %ld; Write Wait: %ld; Write Woke: %ld; Queue Processor Wait: %ld; Queue Processor Woke: %ld;";
const char *DECISION_ICONS[] = {
    UNDECIDED_ICON,  // UNDECIDED
    DROP_ICON,       // DROP
    ACCEPT_ICON,     // ACCEPT
    MANIPULATE_ICON  // MANIPULATE
};

struct semaphore newPacketSemaphore;

PacketQueue *_pendingPacketsQueue;
PacketQueue *_read1PacketsQueue;
PacketQueue *_read2PacketsQueue;
PacketQueue *_write1PacketsQueue;
PacketQueue *_write2PacketsQueue;
PacketQueue *_injectionPacketsQueue;
PacketQueue *_completedQueue;

// Define a per-CPU queue
DEFINE_PER_CPU(PacketQueue, cpu_packet_queues);

// Spinlock for synchronization
DEFINE_SPINLOCK(packet_queue_lock);
LIST_HEAD(packetQueueListHead);

atomic_t _pendingQueueItemAdded = ATOMIC_INIT(0);
bool _readQueueItemAdded = false;
bool _queueItemProcessed = false;
bool _queueProcessorExited = false;
bool _userspaceItemProcessed = false;
bool _userRead = false;
bool _userWrite = false;
DECLARE_WAIT_QUEUE_HEAD(QueueProcessorExitedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(_queueItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(UserspaceItemProcessedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(ReadQueueItemAddedWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(PendingQueueItemAddedWaitQueue);

// Declaration of the netfilter hooks
static struct nf_hook_ops *_preRoutingOps = NULL;
static struct nf_hook_ops *_postRoutingOps = NULL;
static struct nf_hook_ops *_localRoutingOps = NULL;
static struct nf_queue_handler *_queueHandlerOps = NULL;

struct task_struct *_queueProcessorThread = NULL;
static packet_processing_callback_t _registeredCallback = NULL;
static bool _isInitialized = false;
static RoutingType _lastRoutingType = NONE_ROUTING;

// Define a workqueue
static struct workqueue_struct *packet_wq;

static void _hookDrop(struct net *net);
static int _packetQueueHandler(struct nf_queue_entry *entry, unsigned int queuenum);
static struct nf_queue_handler* _setupQueueHandlerHook(void);
static void _cleanCompletedPacketTrips(void);
static int _nfDecisionFromExtravaDecision(RoutingDecision decision, bool count);
static int _nfDecisionFromExtravaDefaultDecision(bool count);
static int interceptIpv4(struct iphdr *ipHeader, RoutingType type, const char* routeTypeName, struct sk_buff *skb, const struct nf_hook_state *state, const char* hookName);
int _netFilterPacketProcessorThread(void *data);

void RegisterPacketProcessingCallback(packet_processing_callback_t callback) {
    _registeredCallback = callback;
}

static void printRingBufferCounters(void){
    LOG_INFO(MESSAGE_PACKETS_CAPTURED, PacketsIngressCounter, PacketsQueuedCounter, PacketsCapturedCounter, PacketsProcessedCounter, PacketsAcceptCounter, PacketsManipulateCounter, PacketsDropCounter, PacketsStaleCounter, calculateSampleAverageToString(), calculateSamplePercentileToString(90));
    LOG_INFO(MESSAGE_EVENTS_CAPTURED, ReadWaitCounter, ReadWokeCounter, WriteWaitCounter, WriteWokeCounter, QueueProcessorWaitCounter, QueueProcessorWokeCounter);
}

int RegisterQueueProcessorThreadHandler(packet_processor_thread_handler_t handler) {
    _queueProcessorThread = kthread_run(handler, NULL, "packet_processor");
    if (IS_ERR(_queueProcessorThread)) {
        printk(KERN_ERR "Failed to start packet_processor_thread.\n");
        CleanupNetfilterHooks();  // Make sure to clean up before exiting
        return PTR_ERR(_queueProcessorThread);
    }

    return 0;
}

void StopQueueProcessorThread(void) {
    if(!_queueProcessorThread){
        LOG_WARNING("Queue processor thread is null. Ignoring stop.");
        return;
    }
    
    kthread_stop(_queueProcessorThread);
}

void HandlePacketDecision(PendingPacketRoundTrip *packetTrip, RoutingDecision decision, DecisionReason reason) {
    char logMessage[256];
    if(!packetTrip){
        LOG_WARNING("Packet trip is null. Ignoring decision.");
        return;
    }

    if(!packetTrip->entry){
        LOG_WARNING("Packet trip entry is null. Ignoring decision.");
        return;
    }
    
    int nfDecision = _nfDecisionFromExtravaDecision(decision, true);
    nf_reinject(packetTrip->entry, nfDecision);

    snprintf(logMessage, sizeof(logMessage), "%s%s Extrava", DECISION_ICONS[decision], GetReasonText(reason));
    LOG_DEBUG_ICMP(packetTrip, "%s", logMessage); 

    //DecommissionPacketTrip(packetTrip);
    //_cleanCompletedPacketTrips();
}

static void _cleanCompletedPacketTrips(void){
    CLEANUP_STALE_ITEMS_ON_QUEUE(_completedQueue);
}

static int _nfDecisionFromExtravaDefaultDecision(bool count){
    if(default_packet_response == UNDECIDED){
        default_packet_response = DROP;
    }

    return _nfDecisionFromExtravaDecision(default_packet_response, count);
}

static void increasePacketsIngressCounter(void){
    PacketsIngressCounter++;
    if(PacketsIngressCounter % 5000 == 0){
        printRingBufferCounters();
    }
}

static int _nfDecisionFromExtravaDecision(RoutingDecision decision, bool count){
    if(decision == ACCEPT){
        if(count){
            PacketsAcceptCounter++;
            PacketsProcessedCounter++;
        }
        return NF_ACCEPT;
    }
    else if(decision == MANIPULATE){
        if(count){
            PacketsManipulateCounter++;
            PacketsProcessedCounter++;
        }
        return NF_ACCEPT;
    }
    else if(decision == UNDECIDED){
        return _nfDecisionFromExtravaDefaultDecision(count);
    }
    else {
        if(count){
            PacketsDropCounter++;
            PacketsProcessedCounter++;
        }
        return NF_DROP;
    }
}

void CleanUpStaleItemsOnQueue(PacketQueue* queue, const char *queueName){
    int queueLength = PacketQueueLength(queue);
    int numberCleaned = 0;
    while(queueLength > 0){
        PendingPacketRoundTrip* popPacketTrip = PacketQueuePop(queue);
        if(!popPacketTrip){
            LOG_WARNING("Packet trip is null. Ignoring clean up.");
            continue;
        }

        ReturnPacketTrip(popPacketTrip);
        numberCleaned++;
        PacketsStaleCounter++;
        queueLength = PacketQueueLength(queue);
    }

    if(numberCleaned>0){
        int newQueueLength = PacketQueueLength(queue);
        LOG_DEBUG_PACKET("Cleaned %d packet trips from %s. Current size: %d;", numberCleaned, queueName, newQueueLength);
    }
}

// Refactored function to handle nfRouting
static unsigned int _nfRoutingHandlerCommon(RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    increasePacketsIngressCounter();
    _lastRoutingType = NONE_ROUTING;
    int defaultDecision = _nfDecisionFromExtravaDefaultDecision(false);
    CHECK_NULL(LOG_TYPE_ERROR, state, defaultDecision);
    const char* hookName = hookToString(state->hook);
    const char* routeTypeName = routeTypeToString(type);
    //CHECK_NULL_AND_LOG(LOG_TYPE_ERROR, skb, defaultDecision, LOG_TYPE_DEBUG_PACKET, "skb pointer: %p length: %u", skb, skb->len);
    struct iphdr *ipHeader = ip_hdr(skb);
    int response = defaultDecision;

    if(!ShouldCapture()){
        if(PacketsProcessedCounter % 10000 == 0 || PacketsProcessedCounter == 0){
            LOG_DEBUG_PACKET("Extrava is not capturing. Default packet response (%d) Routing Type: %s, Hook Type: %s", defaultDecision, routeTypeName, hookName);
        }

        return _nfDecisionFromExtravaDefaultDecision(true);
    }

    if(ipHeader){
        response = interceptIpv4(ipHeader, type, routeTypeName, skb, state, hookName);
    }
    else{
        response = _nfDecisionFromExtravaDecision(ACCEPT, true);
    }

    _lastRoutingType = type;
    return response;
}

static int interceptIpv4(struct iphdr *ipHeader, RoutingType type, const char* routeTypeName, struct sk_buff *skb, const struct nf_hook_state *state, const char* hookName){
    if(!ipHeader){
        LOG_WARNING("IP header is null. Ignoring intercept.");
        return _nfDecisionFromExtravaDefaultDecision(true);
    }

    const char* protocolName = ipProtocolToString(ipHeader->protocol);
    const char* typeOfService = TOS_TO_STRING(ipHeader->tos);
    LOG_DEBUG_ICMP_PROTOCOL(ipHeader->protocol, "IP packet received. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);

    if (force_icmp == 1 && ipHeader->protocol != IPPROTO_ICMP){
        return _nfDecisionFromExtravaDecision(ACCEPT, true);
    }
    else if(ipHeader->protocol == IPPROTO_ICMP){
        LOG_DEBUG_ICMP_PROTOCOL(ipHeader->protocol, "ICMP packet received. Routing Type: %s, Protocol: %s, ToS: %s, Hook Type: %s", routeTypeName, protocolName, typeOfService, hookName);
        return NF_QUEUE_NR(0);
    }
    else if(ipHeader->protocol == IPPROTO_TCP){
        struct tcphdr *tcph = tcp_hdr(skb);
        if (tcph){
            LOG_DEBUG_ICMP_PROTOCOL(ipHeader->protocol, "IP header found. Size: %zu bytes. Routing Type: %s, Protocol: %s, ToS: %u, Hook Type: %s, Dst: %s, Src: %s", sizeof(struct tcphdr), routeTypeName, protocolName, typeOfService, hookName, tcph->dest, tcph->source);
            return NF_QUEUE_NR(0);
        }
    }

    return _nfDecisionFromExtravaDefaultDecision(true);
}

static unsigned int _nfPreRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    int response = _nfRoutingHandlerCommon(PRE_ROUTING, priv, skb, state);
    if(response < 0){
        LOG_ERROR("Error in _nfPreRoutingHandler. Response: %d", response);
    }

    return response;
}

static unsigned int _nfPostRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    int response = _nfRoutingHandlerCommon(POST_ROUTING, priv, skb, state);
    if(response < 0){
        LOG_ERROR("Error in _nfPostRoutingHandler. Response: %d", response);
    }

    return response;
}

static unsigned int _nfLocalRoutingHandler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    int response = _nfRoutingHandlerCommon(LOCAL_ROUTING, priv, skb, state);
    if(response < 0){
        LOG_ERROR("Error in _nfLocalRoutingHandler. Response: %d", response);
    }

    return response;
}

int packet_queue_init(void)
{
    int cpu;

    spin_lock_init(&packet_queue_lock);

    for_each_possible_cpu(cpu) {
        PacketQueue *queue = per_cpu_ptr(&cpu_packet_queues, cpu);
        int initResponse = PacketQueueInitialize(queue);
        if(initResponse < 0){
            LOG_ERROR("Error initializing packet queue. Response: %d", initResponse);
            safeFree(queue);
            return NULL;
        }
    }

    return 0; // Success
}


void packet_queue_cleanup(void)
{
    int cpu;
    for_each_possible_cpu(cpu) {
        PacketQueue *queue = per_cpu_ptr(&cpu_packet_queues, cpu);
        PacketQueueCleanup(queue);
    }
}

int SetupNetfilterHooks(void) {
    LOG_INFO("Starting SetupNetfilterHooks");
    InitializeAllPools(2000);
    int ret = packet_queue_init();
    if (ret) {
        printk(KERN_ERR "Failed to initialize packet queues.\n");
        return ret;
    }
    
    _preRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);
    _postRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);
    _localRoutingOps = kzalloc(sizeof(struct nf_hook_ops), GFP_KERNEL);

    if (!_preRoutingOps || !_postRoutingOps || !_localRoutingOps) {
        LOG_ERROR("Failed to allocate memory for netfilter hooks.");
        CleanupNetfilterHooks();
        return ERROR_MEMORY_ALLOC;
    }

    sema_init(&newPacketSemaphore, 1);
    packet_wq = create_workqueue("packet_wq");
    if (!packet_wq) {
        LOG_ERROR("Failed to create workqueue 'packet_wq'");
        return ERROR_WORKQUEUE_CREATION;
    }
    _pendingPacketsQueue = PacketQueueCreate();
    _read1PacketsQueue = PacketQueueCreate();
    _read2PacketsQueue = PacketQueueCreate();
    _write1PacketsQueue = PacketQueueCreate();
    _write2PacketsQueue = PacketQueueCreate();
    _injectionPacketsQueue = PacketQueueCreate();
    _completedQueue = PacketQueueCreate();

    _preRoutingOps->hook = _nfPreRoutingHandler;
    _preRoutingOps->hooknum = NF_INET_PRE_ROUTING;
    _preRoutingOps->pf = PF_INET;
    _preRoutingOps->priority = NF_IP_PRI_FIRST;

    _postRoutingOps->hook = _nfPostRoutingHandler;
    _postRoutingOps->hooknum = NF_INET_POST_ROUTING;
    _postRoutingOps->pf = PF_INET;
    _postRoutingOps->priority = NF_IP_PRI_FIRST;

    _localRoutingOps->hook = _nfLocalRoutingHandler;
    _localRoutingOps->hooknum = NF_INET_LOCAL_OUT;
    _localRoutingOps->pf = PF_INET;
    _localRoutingOps->priority = NF_IP_PRI_FIRST;

    _queueHandlerOps = _setupQueueHandlerHook();
    LOG_INFO("Registering pre-routing hook");
    nf_register_net_hook(&init_net, _preRoutingOps);

    LOG_INFO("Registering post-routing hook");
    nf_register_net_hook(&init_net, _postRoutingOps);

    LOG_INFO("Registering local-routing hook");
    nf_register_net_hook(&init_net, _localRoutingOps);

    LOG_INFO("Registering queue handler");
    nf_register_queue_handler(_queueHandlerOps);

    init_waitqueue_head(&QueueProcessorExitedWaitQueue);
    init_waitqueue_head(&_queueItemProcessedWaitQueue);
    init_waitqueue_head(&UserspaceItemProcessedWaitQueue);
    init_waitqueue_head(&PendingQueueItemAddedWaitQueue);

    _isInitialized = true;
    RegisterQueueProcessorThreadHandler(_netFilterPacketProcessorThread);
    LOG_INFO("Completed SetupNetfilterHooks");
    return 0;
}

void CleanupNetfilterHooks(void) {
    printRingBufferCounters();
    _isInitialized = false;
    if (_queueHandlerOps) {
        nf_unregister_queue_handler();
        kfree(_queueHandlerOps);
        _queueHandlerOps = NULL;
    }

    if (_localRoutingOps) {
        nf_unregister_net_hook(&init_net, _localRoutingOps);
        kfree(_localRoutingOps);
        _localRoutingOps = NULL;
    }

    if (_postRoutingOps) {
        nf_unregister_net_hook(&init_net, _postRoutingOps);
        kfree(_postRoutingOps);
        _postRoutingOps = NULL;
    }

    if (_preRoutingOps) {
        nf_unregister_net_hook(&init_net, _preRoutingOps);
        kfree(_preRoutingOps);
        _preRoutingOps = NULL;
    }

    if (packet_wq) {
        flush_workqueue(packet_wq);
        destroy_workqueue(packet_wq);
    }

    packet_queue_cleanup();
    FreeAllPools();
    
    PacketQueueCleanup(_pendingPacketsQueue);
    PacketQueueCleanup(_read1PacketsQueue);
    PacketQueueCleanup(_read2PacketsQueue);
    PacketQueueCleanup(_write1PacketsQueue);
    PacketQueueCleanup(_write2PacketsQueue);
    PacketQueueCleanup(_injectionPacketsQueue);
    PacketQueueCleanup(_completedQueue);
    StopQueueProcessorThread();

    struct PacketTripListNode *listNodeToCleanUp;
    struct list_head *pos, *q;

    list_for_each_safe(pos, q, &packetQueueListHead) {
        listNodeToCleanUp = list_entry(pos, struct PacketTripListNode, list);
        list_del(pos);
        kfree(listNodeToCleanUp);
    }
}

void DecommissionPacketTrip(PendingPacketRoundTrip *packetTrip){
    if(!packetTrip){
        LOG_WARNING("Packet trip is null. Ignoring decommission.");
        return;
    }

    PacketQueuePush(_completedQueue, packetTrip);
}

static struct nf_queue_handler* _setupQueueHandlerHook(void) {
    struct nf_queue_handler *ops = (struct nf_queue_handler*)kcalloc(1, sizeof(struct nf_queue_handler), GFP_KERNEL);
    if (!ops) {
        LOG_ERROR("Failed to allocate memory for nf_hook_ops.");
        return NULL;
    }

    ops->outfn = _packetQueueHandler;
    ops->nf_hook_drop = _hookDrop;


    return ops;
}

static void _hookDrop(struct net *net){
    LOG_DEBUG("Cleaning up netfilter queue hooks");
    
    if(_queueHandlerOps){
        nf_unregister_queue_handler();
        kfree(_queueHandlerOps);
        _queueHandlerOps = NULL;
    }

    if(_isInitialized){
        LOG_DEBUG("Re-initializing packet queue handler due to _isInitialized being true");
        _queueHandlerOps = _setupQueueHandlerHook();
        if (!_queueHandlerOps) {
            LOG_ERROR("Failed to set up queue handler during re-initialization");
            return;
        }
        nf_register_queue_handler(_queueHandlerOps);
    }
}



// Workqueue function
static void packet_work_func(struct work_struct *work)
{
    if (IsUnloading()){  
        return;
    }

    LOG_DEBUG("Entering packet_work_func.");

    struct packet_work *pw = container_of(work, struct packet_work, work);
    if (!pw) {
        LOG_ERROR("Failed to retrieve packet_work from work_struct.");
        return;
    }

    PendingPacketRoundTrip *packetTrip = pw->packetTrip;
    if (!packetTrip) {
        LOG_ERROR("packetTrip is NULL in packet_work_func.");
        kfree(pw); // Free the work structure
        return;
    }

    int lockTry = 0;
    while(down_trylock(&newPacketSemaphore) && lockTry < 10) {
        lockTry++;
    }

    if(lockTry >= 10){
        LOG_WARNING("Failed to acquire semaphore in packet_work_func.");
        kfree(pw); // Free the work structure
        return;
    }

    int queueLength = PacketQueueLength(_pendingPacketsQueue);
    if (queueLength < 0) {
        LOG_ERROR("Error retrieving _pendingPacketsQueue length.");
    } else {
        LOG_DEBUG("Current _pendingPacketsQueue Size: %d", queueLength);
    }

    PacketQueuePush(_pendingPacketsQueue, packetTrip);
    LOG_DEBUG_ICMP(packetTrip, "Pushed packetTrip to _pendingPacketsQueue.");
    up(&newPacketSemaphore);

    atomic_set(&_pendingQueueItemAdded, 1);
    LOG_DEBUG_ICMP(packetTrip, "Released semaphore in packet_work_func.");
    if (atomic_cmpxchg(&_pendingQueueItemAdded, 0, 1) == 0) {
        wake_up_interruptible(&PendingQueueItemAddedWaitQueue);
        LOG_DEBUG_ICMP(packetTrip, "Woke up PendingQueueItemAddedWaitQueue. IsProcessingPacketTrip is set to %d", atomic_read(&IsProcessingPacketTrip));
    }
    

    kfree(pw); // Free the work structure
    LOG_DEBUG_ICMP(packetTrip, "Exiting packet_work_func.");
}

static int _packetQueueHandler(struct nf_queue_entry *entry, unsigned int queuenum)
{
    PendingPacketRoundTrip *packetTrip = GetFreePacketTrip(entry, _lastRoutingType);
    if (!packetTrip || !packetTrip->packet) {
        LOG_ERROR("Failed to create pending packet trip.");
        HandlePacketDecision(packetTrip, DROP, MEMORY_FAILURE_PACKET);
        packetTrip = NULL;
        return NF_DROP;
    }

    
    // Get the current CPU's packet queue
    //PacketQueue *queue = this_cpu_ptr(&cpu_packet_queues);

    LOG_DEBUG_ICMP(packetTrip, "About to queue work to packet_wq");
    struct PacketTripListNode *node;
    node = kmalloc(sizeof(*node), GFP_KERNEL); // GFP_KERNEL for normal kernel allocation
    if (node) {
        node->data = packetTrip;
        list_add_tail(&node->list, &packetQueueListHead); // Add new node to the list
    }
    // Lock, push to queue, and unlock
    // spin_lock(&packet_queue_lock);
    // PacketQueuePush(queue, packetTrip);
    // spin_unlock(&packet_queue_lock);
    LOG_DEBUG_ICMP(packetTrip, "Work queued to cpu queue %p", packetTrip);

    s32 transactionSize = packetTrip->entry->skb->len + S32_SIZE * 6;
    int writeQueueLength = 0;
    unsigned char *dataPayload = kmalloc(transactionSize, GFP_KERNEL);
    unsigned char headerFlags[S32_SIZE];
    unsigned char headerVersion[S32_SIZE];
    unsigned char headerLength[S32_SIZE];
    unsigned char headerRoutingType[S32_SIZE];
    unsigned char packetId[S32_SIZE];
    unsigned char packetQueueNumber[S32_SIZE];
    intToBytes(0, headerFlags);
    intToBytes(3, headerVersion);
    intToBytes(packetTrip->entry->skb->len, headerLength);
    intToBytes(packetTrip->routingType, headerRoutingType);
    intToBytes(packetTrip->entry->id, packetId);
    intToBytes(queuenum, packetQueueNumber);
    memcpy(dataPayload, headerFlags, S32_SIZE);
    memcpy(dataPayload + S32_SIZE, headerRoutingType, S32_SIZE);
    memcpy(dataPayload + (S32_SIZE*2), headerVersion, S32_SIZE);
    memcpy(dataPayload + (S32_SIZE*3), headerLength, S32_SIZE);
    memcpy(dataPayload + (S32_SIZE*4), packetId, S32_SIZE);
    memcpy(dataPayload + (S32_SIZE*5), packetQueueNumber, S32_SIZE);

    memcpy(dataPayload + (S32_SIZE*6), packetTrip->entry->skb->data, packetTrip->entry->skb->len);

    int slotAssigned = WriteToSystemRingBuffer(dataPayload, transactionSize);
    if(slotAssigned >= 0){
        packetTrip->slotAssigned = slotAssigned;
    }
    // Signal or process further as needed
    // atomic_set(&_pendingQueueItemAdded, 1);
    // if (atomic_cmpxchg(&_pendingQueueItemAdded, 0, 1) == 0) {
    //     wake_up_interruptible(&PendingQueueItemAddedWaitQueue);
    //     LOG_DEBUG_ICMP(packetTrip, "Woke up PendingQueueItemAddedWaitQueue. IsProcessingPacketTrip is set to %d", atomic_read(&IsProcessingPacketTrip));
    // }
    return 0;
}

ktime_t last_time;
bool processed_packet_last_round = false;
int _netFilterPacketProcessorThread(void *data) {
    __u32 last_ring_buffer_position = -1;
    last_time = ktime_get();
    while (!kthread_should_stop() || IsUnloading() ) {
        if(!processed_packet_last_round && ktime_get() - last_time < ktime_set(0, 1000000)){ // Less than 1ms and now new packets processed, wait a millisecond
            msleep(1);
            continue;
        }

        last_time = ktime_get();
        // Clean stale packets
        struct PacketTripListNode *current_node = NULL;
        struct list_head *stalePacketToRemove = NULL;
        struct list_head *pos, *q;
        list_for_each_safe(pos, q, &packetQueueListHead) {
            current_node = list_entry(pos, struct PacketTripListNode, list);
            if(last_time - current_node->data->createdTime > ktime_set(0, 1000000000) && current_node->data->slotAssigned >= 0){ // Greater than 1000ms then it's stale
                write_system_ring_buffer_slot_status(current_node->data->slotAssigned, EMPTY);
                HandlePacketDecision(current_node->data, _nfDecisionFromExtravaDefaultDecision(false), TIMEOUT);
                list_del(pos);
                SystemBufferSlotsClearedCounter++;
                SystemBufferActiveFreeSlots++;
                SystemBufferActiveUsedSlots--;
                SlotStatus slotStatus = read_system_ring_buffer_slot_status(current_node->data->slotAssigned);
                LOG_DEBUG_ICMP(current_node->data, "Removed stale packet trip from queue. Slot status: %d", slotStatus);
            }
        }
       
        //LOG_DEBUG_PACKET("Reading from user space ring buffer.");
        last_ring_buffer_position = read_system_ring_buffer_position();
        DataBuffer* response = ReadFromUserRingBuffer();
        processed_packet_last_round = response != NULL && response->size > 0 && response->data != NULL;
        if(!processed_packet_last_round){
            continue;
        }

        __u32 packetId = bytesToUint(response->data);
        __u32 packetQueueNumber = bytesToUint(response->data + S32_SIZE);
        __u32 routingDecision = bytesToUint(response->data + S32_SIZE*2);
        free_data_buffer(response);
        LOG_DEBUG_PACKET("Received response. Decision: %d, Packet Id: %d, Queue Number: %d", routingDecision, packetId, packetQueueNumber);

        current_node = NULL;
        struct PacketTripListNode *nodeFound = NULL;
        list_for_each_entry(current_node, &packetQueueListHead, list) {
           if(current_node->data->entry->id == packetId){
                nodeFound = current_node;
                break;
           }
        }

        if(nodeFound == NULL){
            continue;
        }

        LOG_DEBUG_ICMP(current_node->data, "Received decision on packet. Decision: %d", routingDecision);
        list_del(&nodeFound->list);
        nodeFound->data->decision = routingDecision;
        HandlePacketDecision(nodeFound->data, routingDecision, USER_ACCEPT);
    }

    return 0;
}


void NetFilterShouldCaptureChangeHandler(bool shouldCapture){
    printRingBufferCounters();
    if(!shouldCapture){
        LOG_DEBUG("Emptying queues");
        PacketQueueEmpty(_pendingPacketsQueue);
        PacketQueueEmpty(_read1PacketsQueue);
        PacketQueueEmpty(_read2PacketsQueue);
        PacketQueueEmpty(_write1PacketsQueue);
        PacketQueueEmpty(_write2PacketsQueue);
        PacketQueueEmpty(_injectionPacketsQueue);
        PacketQueueEmpty(_completedQueue);
    }
}