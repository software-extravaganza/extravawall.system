#include "userspace_comm.h"

// Constants
#define PROC_FILENAME "MY_COMM"
#define BUFFER_SIZE 100
#define COMMUNICATION_VERSION 2
#define DEVICE_TO_USER_SPACE "extrava_to_process"
#define DEVICE_FROM_USER_SPACE "extrava_from_process"
#define CLASS_NAME  "Extravaganza"
#define S32_SIZE (sizeof(s32))
#define TRANSACTION_HEADER_SIZE (S32_SIZE * 4)

const char* MESSAGE_PACKET_PROCESSOR_STARTED = "Packet processor thread started";
const char* MESSAGE_WAITING_NEW_PACKET = "Waiting for new packet trip";
const char* MESSAGE_WAITING_USER_PROCESS = "Waiting for user space to process packet";
const char* MESSAGE_TIMEOUT_USERSPACE = "Packet processor thread timed out on userspace item processed wait queue";
const char* MESSAGE_ERROR_USERSPACE = "Packet processor thread failed on userspace item processed wait queue";
const char* MESSAGE_POPPING_PACKET = "Popping from _injectionPacketsQueue. Current size: %d; Size after pop: %d";
const char* MESSAGE_PACKET_PROCESSED = "User space has processed packet with decision %lld. Reinjecting...";
const char* MESSAGE_REINJECTION_COMPLETE = "Reinjection complete (%lld)";
const char* MESSAGE_CLEANUP_DONE = "Packet trip processed. Done cleaning up and ready for new packet.";
const char* MESSAGE_PROCESSOR_EXITING = "Packet processor thread exiting";

// Private Fields
static DEFINE_MUTEX(_deviceToUserMutex);
static DEFINE_MUTEX(_deviceFromUserMutex);
static struct class* _charClass = NULL;
static wait_queue_head_t _pendingPacketQueue;
static atomic_t _deviceOpenToCount = ATOMIC_INIT(0);
static atomic_t _deviceOpenFromCount = ATOMIC_INIT(0);
static bool _isLoaded = false;
static int _majorNumberFromUser;
static int _majorNumberToUser;
static struct device* _netmodDevice = NULL;
static struct device* _netmodDeviceAck = NULL;
static bool sendUserSpaceReset = false;
static bool getUserSpaceReset = false;


static int _processResponseHeader(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip);
static int _processResponseData(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip);
static void _stopProcessingPacket(PendingPacketRoundTrip* packetTrip);
static void _resetPacketProcessing(void);
static int _openDeviceTo(struct inode *inode, struct file *file);
static int _openDeviceFrom(struct inode *inode, struct file *file);
static int _releaseDeviceTo(struct inode *inode, struct file *file) ;
static int _releaseDeviceFrom(struct inode *inode, struct file *file) ;
static ssize_t _readDeviceTo(struct file* filep, char __user* buf, size_t len, loff_t* offset);
static ssize_t _writeDeviceFrom(struct file* filep, const char __user* userBuffer, size_t length, loff_t* offset);
static void _cleanStaleItemsOnAllQueues(void);
static void _shouldCaptureChangeHandler(bool shouldCapture);

static struct file_operations _operationsToDeviceUser = {
   .open = _openDeviceTo,
   .read = _readDeviceTo,
   .release = _releaseDeviceTo,
};
static struct file_operations _operationsFromDeviceUser = {
   .open = _openDeviceFrom,
   .write = _writeDeviceFrom,
   .release = _releaseDeviceFrom,
};

// Public Fields
long PacketsIngressCounter = 0;
long PacketsQueuedCounter = 0;
long PacketsCapturedCounter = 0;
long PacketsProcessedCounter = 0;
long PacketsAcceptCounter = 0;
long PacketsManipulateCounter = 0;
long PacketsDropCounter = 0;
long PacketsStaleCounter = 0;
long ReadWaitCounter = 0;
long ReadWokeCounter = 0;
long WriteWaitCounter = 0;
long WriteWokeCounter = 0;
long QueueProcessorWaitCounter = 0;
long QueueProcessorWokeCounter = 0;


atomic_t IsProcessingPacketTrip = ATOMIC_INIT(0); // 0 for false, 1 for true

DECLARE_WAIT_QUEUE_HEAD(UserReadWaitQueue);
DECLARE_WAIT_QUEUE_HEAD(UserWriteWaitQueue);
extern bool UserRead;

// Private Functions
static int _releaseDevice(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *deviceMutex, atomic_t *atomicValue) {
    atomic_dec(atomicValue);
    mutex_unlock(deviceMutex);
    LOG_DEBUG("Device %s disconnected from user space", deviceName);
    return 0;
}

static int _openDevice(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *deviceMutex, atomic_t *atomicValue) {
    if(!mutex_trylock(deviceMutex)){
        LOG_INFO("Device %s cannot lock; is being used by another process.", deviceName);
        return -EBUSY;
    }

    atomic_inc(atomicValue);
    LOG_DEBUG("Device %s connected from user space", deviceName);
    return 0;
}

// Public Functions
int SetupUserSpaceCommunication(void) {
    atomic_set(&IsProcessingPacketTrip, 0);
    SetShouldCaptureEventHandler(_shouldCaptureChangeHandler);

    _majorNumberToUser = register_chrdev(0, DEVICE_TO_USER_SPACE, &_operationsToDeviceUser);
    if (_majorNumberToUser < 0) {
        LOG_ERROR("Failed to register a major number for netmod to user");
        return -1;
    }

    _majorNumberFromUser = register_chrdev(0, DEVICE_FROM_USER_SPACE, &_operationsFromDeviceUser);
    if (_majorNumberFromUser < 0) {
        LOG_ERROR("Failed to register a major number for netmod from user");
        unregister_chrdev(_majorNumberToUser, DEVICE_TO_USER_SPACE);
        return -1;
    }

    _charClass = class_create(CLASS_NAME);
    if (IS_ERR(_charClass)) {
        unregister_chrdev(_majorNumberToUser, DEVICE_TO_USER_SPACE);
        unregister_chrdev(_majorNumberFromUser, DEVICE_FROM_USER_SPACE);
        LOG_ERROR("Failed to register device class");
        return PTR_ERR(_charClass);
    }

    _netmodDevice = device_create(_charClass, NULL, MKDEV(_majorNumberToUser, 0), NULL, DEVICE_TO_USER_SPACE);
    if (IS_ERR(_netmodDevice)) {
        LOG_ERROR("Failed to create the input device");
    }

    _netmodDeviceAck = device_create(_charClass, NULL, MKDEV(_majorNumberFromUser, 0), NULL, DEVICE_FROM_USER_SPACE);
    if (IS_ERR(_netmodDeviceAck)) {
        LOG_ERROR("Failed to create the output device");
    }

    mutex_init(&_deviceToUserMutex);
    mutex_init(&_deviceFromUserMutex);
    init_waitqueue_head(&_pendingPacketQueue);
    init_waitqueue_head(&UserReadWaitQueue);
    init_waitqueue_head(&UserWriteWaitQueue);

    if (IS_ERR(_netmodDevice) || IS_ERR(_netmodDeviceAck)) {
        CleanupUserSpaceCommunication();
        CleanupNetfilterHooks();
        return IS_ERR(_netmodDevice) ? PTR_ERR(_netmodDevice) : PTR_ERR(_netmodDeviceAck);
    }

    int threadRegistration = RegisterQueueProcessorThreadHandler(_packetProcessorThread);
    if (threadRegistration != 0) {
        LOG_ERROR("Failed to register queue processor thread handler");
        CleanupUserSpaceCommunication();
        CleanupNetfilterHooks();
        return threadRegistration;
    }

    _isLoaded = true;
    return 0;
}

void CleanupUserSpaceCommunication(void) {
    _isLoaded = false;
    atomic_set(&IsProcessingPacketTrip, 0);
    //_isDataPending = false;

    Deactivate();
    // Ensure no one is using the device or waits until they're done
    LOG_DEBUG("Waiting for user space communication to be released...");
    while (atomic_read(&_deviceOpenToCount) > 0 || atomic_read(&_deviceOpenFromCount) > 0) {
        msleep(50); // This will sleep for 50ms before checking again.
    }
    LOG_DEBUG("User space communication released");

    // Destroy any synchronization primitives associated with the device
    LOG_DEBUG("Destroying synchronization primitives");
    mutex_destroy(&_deviceToUserMutex);
    mutex_destroy(&_deviceFromUserMutex);

    // Remove the device files under /dev
    LOG_DEBUG("Removing device files");
    device_destroy(_charClass, MKDEV(_majorNumberToUser, 0));
    device_destroy(_charClass, MKDEV(_majorNumberFromUser, 0));

    // Cleanup the device class
    LOG_DEBUG("Cleaning up device class");
    class_unregister(_charClass);
    class_destroy(_charClass);

    // Unregister the character devices
    LOG_DEBUG("Unregistering character devices");
    unregister_chrdev(_majorNumberToUser, DEVICE_TO_USER_SPACE);
    unregister_chrdev(_majorNumberFromUser, DEVICE_FROM_USER_SPACE);
}

int _packetProcessorThread(void *data) {
    LOG_INFO(MESSAGE_PACKET_PROCESSOR_STARTED);
    while (!kthread_should_stop() || IsUnloading() ) {
        LOG_DEBUG_PACKET(MESSAGE_WAITING_NEW_PACKET);
        atomic_set(&_pendingQueueItemAdded, 0);
        _userRead = false;

        int pendingQueueLength = PacketQueueLength(_pendingPacketsQueue);
        if(pendingQueueLength <= 0){
            QueueProcessorWaitCounter++;
            while (1) {
                wait_event_interruptible(PendingQueueItemAddedWaitQueue, atomic_read(&_pendingQueueItemAdded) || !atomic_read(&IsProcessingPacketTrip) || IsUnloading() || kthread_should_stop());
                if ((atomic_read(&_pendingQueueItemAdded) && !atomic_read(&IsProcessingPacketTrip)) || IsUnloading() || kthread_should_stop()) {
                    break;
                }
            }
            QueueProcessorWokeCounter++;
        }

        if(!ShouldCapture() || IsUnloading() || kthread_should_stop()){
            continue;
        }

        down(&newPacketSemaphore);
        pendingQueueLength = PacketQueueLength(_pendingPacketsQueue);
        PendingPacketRoundTrip* pendingPacketTrip = PacketQueuePop(_pendingPacketsQueue);
        LOG_DEBUG_ICMP(pendingPacketTrip, "Popping from _pendingPacketsQueue. Current size: %d; Size after pop: %d", pendingQueueLength, pendingQueueLength - 1);
        if(pendingPacketTrip == NULL){
            LOG_ERROR("Processed packet trip is null from _pendingPacketsQueue");
            continue;
        }
        up(&newPacketSemaphore);

        atomic_set(&IsProcessingPacketTrip, 1);
        int read1QueueLength = PacketQueueLength(_read1PacketsQueue);
        LOG_DEBUG_ICMP(pendingPacketTrip, "Pushing to _read1PacketsQueue. Current size: %d; Size after push: %d", read1QueueLength, read1QueueLength + 1);
        PacketQueuePush(_read1PacketsQueue, pendingPacketTrip);

        PacketsCapturedCounter++;
        _userRead = true;
        _userspaceItemProcessed = false;
        wake_up_interruptible(&UserReadWaitQueue);

        LOG_DEBUG_PACKET(MESSAGE_WAITING_USER_PROCESS);
        long timeout = msecs_to_jiffies(PACKET_PROCESSING_TIMEOUT);
        long ret = wait_event_interruptible_timeout(UserspaceItemProcessedWaitQueue, _userspaceItemProcessed == true || IsUnloading() || kthread_should_stop(), timeout);
        if(!ShouldCapture() || IsUnloading() || kthread_should_stop()){
            continue;
        }

        if (ret == 0) {
            LOG_DEBUG_PACKET(MESSAGE_TIMEOUT_USERSPACE);
            _resetPacketProcessing();
            continue;
        } else if (ret == -ERESTARTSYS) {
            LOG_ERROR(MESSAGE_ERROR_USERSPACE);
            _resetPacketProcessing();
            continue;
        }

        int queueLength = PacketQueueLength(_injectionPacketsQueue);
        if(queueLength <= 0){
            LOG_DEBUG_PACKET("No packets queued in _injectionPacketsQueue");
            continue;
        }

        LOG_DEBUG_PACKET(MESSAGE_POPPING_PACKET, queueLength, queueLength - 1);
        PendingPacketRoundTrip *packetTrip = PacketQueuePop(_injectionPacketsQueue);
        if(packetTrip == NULL){
            LOG_ERROR("Processed packet trip is null from _injectionPacketsQueue");
            continue;
        }

        LOG_DEBUG_ICMP(packetTrip, MESSAGE_PACKET_PROCESSED, packetTrip->decision);
        HandlePacketDecision(packetTrip, packetTrip->decision, (packetTrip->decision == ACCEPT || packetTrip->decision == MANIPULATE) ? USER_ACCEPT : USER_DROP);
    }

    LOG_INFO(MESSAGE_PROCESSOR_EXITING);
    _queueProcessorExited = true;
    wake_up_interruptible(&QueueProcessorExitedWaitQueue);
    return 0;
}

static void _cleanStaleItemsOnAllQueues(void){
    CLEANUP_STALE_ITEMS_ON_QUEUE(_read1PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_read2PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_write1PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_write2PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_injectionPacketsQueue);
}
static void _stopAndResetProcessingPacket(PendingPacketRoundTrip* packetTrip) { 
    if (packetTrip){
        packetTrip->packet->dataProcessed = false;
        packetTrip->packet->headerProcessed = false;
    }

    _stopProcessingPacket(packetTrip);
    _resetPacketProcessing();
}

static void _stopProcessingPacket(PendingPacketRoundTrip* packetTrip) {
    LOG_DEBUG_ICMP(packetTrip, "Stopping processing for this packet");
    if (packetTrip == NULL || packetTrip->packet == NULL || packetTrip->entry == NULL || packetTrip->entry->skb == NULL) {
        LOG_ERROR("Invalid packetTrip structure");
        _resetPacketProcessing();
    } else {
        int injectQueueLength = PacketQueueLength(_injectionPacketsQueue);
        LOG_DEBUG_ICMP(packetTrip, "Pushing to _injectionPacketsQueue. Current size: %d; Size after push: %d", injectQueueLength, injectQueueLength + 1);
        PacketQueuePush(_injectionPacketsQueue, packetTrip);
    }

    _userRead = false;
    _userWrite = false;
    atomic_set(&IsProcessingPacketTrip, 0);
    _userspaceItemProcessed = true;
    wake_up_interruptible(&UserspaceItemProcessedWaitQueue);
}

// Public functions
static void _resetPacketProcessing(void) {
    sendUserSpaceReset = true;
    int pendingQueueLength = PacketQueueLength(_pendingPacketsQueue);
    if(pendingQueueLength > 0){
        atomic_set(&_pendingQueueItemAdded, 1);
        wake_up_interruptible(&PendingQueueItemAddedWaitQueue);
    }
    // sendUserSpaceReset = true;
    // _readQueueItemAdded = true;
    // wake_up_interruptible(&ReadQueueItemAddedWaitQueue);
    // _userspaceItemProcessed = true;
    // wake_up_interruptible(&UserspaceItemProcessedWaitQueue);
    // IsProcessingPacketTrip = false;
}

static ssize_t _readDeviceTo(struct file* filep, char __user* buf, size_t length, loff_t* offset) {
    int errorCount = 0;
    int queue1Length = 0;
    int queue2Length = 0;
    PendingPacketRoundTrip* packetTrip = NULL;
    if(!ShouldCapture()){
        LOG_DEBUG_PACKET("Not capturing");
        return length;
    }

    LOG_DEBUG_PACKET("_readDeviceTo for %s", DEVICE_TO_USER_SPACE);
    // if(sendUserSpaceReset){
    //     unsigned char resetPayload[S32_SIZE];
    //     LOG_DEBUG_ICMP("Resetting user space");
    //     sendUserSpaceReset = false;
    //     //getUserSpaceReset = true;
    //     intToBytes(700, resetPayload);
    //     errorCount = copy_to_user(buf, resetPayload, S32_SIZE);
    //     if (errorCount){
    //         LOG_ERROR("Failed to copy packetTrip header to user space");
    //         _stopProcessingPacket(packetTrip);
    //         return -EFAULT;
    //     }
    //     return S32_SIZE;
    // }
    if(_userRead){
        LOG_DEBUG_PACKET("_userRead active for %s. No blocking to wait for new data.", DEVICE_TO_USER_SPACE);
    }
    else{
        LOG_DEBUG_PACKET("Blocking read for %s. Waiting for new data...", DEVICE_TO_USER_SPACE);
    }

    ReadWaitCounter++;
    // Using wait_event_interruptible to sleep until a packet is available
    int readWaitReturn = wait_event_interruptible(UserReadWaitQueue, _userRead);
    if(readWaitReturn == -ERESTARTSYS){
        LOG_DEBUG_PACKET("Wait event interrupted for %s", DEVICE_TO_USER_SPACE);
        return length;
    }

    ReadWokeCounter++;
    if(!ShouldCapture()){
        LOG_DEBUG_PACKET("Stop capturing");
        return length;
    }

    if(!_userRead){
        LOG_DEBUG_PACKET("Woke up from UserReadWaitQueue for %s", DEVICE_TO_USER_SPACE);
    }

    if (filep->f_flags & O_NONBLOCK) {
        LOG_ERROR("Non-blocking read, no packet found for %s", DEVICE_TO_USER_SPACE);
        _stopProcessingPacket(NULL);
        return length;
    }

    atomic_set(&IsProcessingPacketTrip, 1);
    queue1Length = PacketQueueLength(_read1PacketsQueue);
    queue2Length = PacketQueueLength(_read2PacketsQueue);
    if(queue2Length > 0){
        packetTrip = PacketQueuePop(_read2PacketsQueue);
        LOG_DEBUG_ICMP(packetTrip, "Popping from _read2PacketsQueue. Current size: %d; Size after pop: %d", queue2Length, queue2Length - 1);
    }
    else{
        packetTrip = PacketQueuePop(_read1PacketsQueue);
        LOG_DEBUG_ICMP(packetTrip, "Popping from _read1PacketsQueue. Current size: %d; Size after pop: %d", queue1Length, queue1Length - 1);
    }

    if(!packetTrip){
        if(!ShouldCapture()){
            LOG_DEBUG_PACKET("Not capturing");
            _resetPacketProcessing();
        }
        return length;
    }

    packetTrip->attempts++;
    if(packetTrip->attempts > 4){
        LOG_DEBUG_ICMP(packetTrip, "Packet trip took too many attempts (2 >= x < 5). Dropping...");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    LOG_DEBUG_ICMP(packetTrip, "Packet trip info: %p; Packet to eval: %p (size: %d)", packetTrip, packetTrip->entry->skb, packetTrip->entry->skb->len + S32_SIZE);
    if(!packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        int nextQueueLength = 0;
        unsigned char headerFlags[S32_SIZE];
        unsigned char headerVersion[S32_SIZE];
        unsigned char headerLength[S32_SIZE];
        unsigned char headerRoutingType[S32_SIZE];
        unsigned char headerPayload[TRANSACTION_HEADER_SIZE];

        LOG_DEBUG_ICMP(packetTrip, "Processing {📤}📤📥📥 (Sent header: yes / Sent body: no / Received header: no / Received body: no). Processing header (%lu bytes) - Routing type: %d", TRANSACTION_HEADER_SIZE, packetTrip->routingType);
        // Ensure user buffer has enough space
        if (length < TRANSACTION_HEADER_SIZE) {
            LOG_ERROR("User buffer is too small to hold packetTrip header %zu < %lu", length, TRANSACTION_HEADER_SIZE);
            _stopAndResetProcessingPacket(packetTrip);
            return length;
        }

        // Copy header to user space
        LOG_DEBUG_ICMP(packetTrip, "Sending packetTrip header to user space; Version: %d; Length: %lu bytes", COMMUNICATION_VERSION, TRANSACTION_HEADER_SIZE);
        
        intToBytes(sendUserSpaceReset == true ? 1 : 0, headerFlags);
        intToBytes(COMMUNICATION_VERSION, headerVersion);
        intToBytes(packetTrip->entry->skb->len + S32_SIZE, headerLength);
        intToBytes(packetTrip->routingType, headerRoutingType);
        memcpy(headerPayload, headerFlags, S32_SIZE);
        memcpy(headerPayload + S32_SIZE, headerRoutingType, S32_SIZE);
        memcpy(headerPayload + (S32_SIZE*2), headerVersion, S32_SIZE);
        memcpy(headerPayload + (S32_SIZE*3), headerLength, S32_SIZE);

        errorCount = copy_to_user(buf, headerPayload, TRANSACTION_HEADER_SIZE);
        if (errorCount){
            LOG_ERROR("Failed to copy packetTrip header to user space");
            LOG_ERROR("Failed     {⚠}📤📥📥 (Sent header: failed / Sent body: no / Received header: no / Received body: no).");
            _stopAndResetProcessingPacket(packetTrip);
            return length;
        }
        
        packetTrip->packet->headerProcessed = true;
        LOG_DEBUG_ICMP(packetTrip, "Processed  {🆗}📤📥📥 (Sent header: yes / Sent body: no / Received header: no / Received body: no). Header processed (%lu bytes) - Routing type: %d", TRANSACTION_HEADER_SIZE, packetTrip->routingType);
        if(sendUserSpaceReset){
            packetTrip->packet->dataProcessed = false;
            packetTrip->packet->headerProcessed = false;
            sendUserSpaceReset = false;
            PacketQueuePush(_read1PacketsQueue, packetTrip);
        }
        else{
            nextQueueLength = PacketQueueLength(_read2PacketsQueue);
            LOG_DEBUG_ICMP(packetTrip, "Pushing to read2_packets_queue. Current size: %d; Size after push: %d", nextQueueLength, nextQueueLength + 1);
            PacketQueuePush(_read2PacketsQueue, packetTrip);
        }
        
        return TRANSACTION_HEADER_SIZE;
    }
 else if(packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = packetTrip->entry->skb->len + S32_SIZE;
        unsigned char headerFlags[S32_SIZE];
        int writeQueueLength = 0;
        unsigned char *dataPayload = kmalloc(transactionSize, GFP_KERNEL);
        if (dataPayload == NULL) {
            LOG_ERROR("Failed to allocate memory for packetTrip data");
            return length;
        }

        LOG_DEBUG_ICMP(packetTrip, "Processing 🆗{📤}📥📥 (Sent header: yes / Sent body: no / Received header: no / Received body: no). Processing data (%d bytes) - Routing type: %d", transactionSize, packetTrip->routingType);
        if (length < transactionSize) {
            LOG_ERROR("User buffer is too small to hold packetTrip data %zu < %d", length, transactionSize);
            _stopAndResetProcessingPacket(packetTrip);
            kfree(dataPayload);
            return length;
        }

        // Copy packetTrip data to user space
        LOG_DEBUG_ICMP(packetTrip, "Sending packetTrip data to user space; Version: %d; Length: %d bytes", COMMUNICATION_VERSION, transactionSize);
        intToBytes(sendUserSpaceReset == true ? 1: 0, headerFlags);

        memcpy(dataPayload, headerFlags, S32_SIZE);
        memcpy(dataPayload + S32_SIZE, packetTrip->entry->skb->data, packetTrip->entry->skb->len);
        errorCount = copy_to_user(buf, dataPayload, transactionSize);
        kfree(dataPayload);
        if (errorCount){
            LOG_ERROR("Failed to copy packetTrip data to user space");
            LOG_ERROR("Failed     🆗{⚠}📥📥 (Sent header: yes / Sent body: failed / Received header: no / Received body: no).");
            _stopAndResetProcessingPacket(packetTrip);
            return length;
        }

        packetTrip->packet->dataProcessed = true;
        LOG_DEBUG_ICMP(packetTrip, "Processed  🆗{🆗}📥📥 (Sent header: yes / Sent body: yes / Received header: no / Received body: no). Processed data (%d bytes) - Routing type: %d", transactionSize, packetTrip->routingType);

        if(sendUserSpaceReset){
            packetTrip->packet->dataProcessed = false;
            packetTrip->packet->headerProcessed = false;
            sendUserSpaceReset = false;
            PacketQueuePush(_read1PacketsQueue, packetTrip);
        }
        else{
            writeQueueLength = PacketQueueLength(_write1PacketsQueue);
            LOG_DEBUG_ICMP(packetTrip, "Pushing to _write1PacketsQueue. Current size: %d; Size after push: %d", writeQueueLength, writeQueueLength + 1);
            PacketQueuePush(_write1PacketsQueue, packetTrip);
            _userWrite = true;
            wake_up_interruptible_all(&UserWriteWaitQueue);
        }

        return transactionSize;
    }
    else if(packetTrip->packet->headerProcessed && packetTrip->packet->dataProcessed){
        LOG_DEBUG_ICMP(packetTrip, "Warning: Packet was already processed");
        _stopAndResetProcessingPacket(packetTrip);
    }
    else{
        LOG_ERROR("Packet is in an invalid state");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    LOG_DEBUG_ICMP(packetTrip, "Packet request sent to user space");
    
    return length;
}

static ssize_t _writeDeviceFrom(struct file* filep, const char __user* userBuffer, size_t length, loff_t* offset) {
    PendingPacketRoundTrip* packetTrip = NULL;
    int queue1Length = 0;
    int queue2Length = 0;
    if(!ShouldCapture()){
        LOG_DEBUG_PACKET("Not capturing");
        return length;
    }


    WriteWaitCounter++;
    // Using wait_event_interruptible to sleep until a packet is available
    int  writeWaitReturn = wait_event_interruptible(UserWriteWaitQueue, _userWrite == true);
    if(writeWaitReturn == -ERESTARTSYS){
        LOG_DEBUG_PACKET("Wait event interrupted for %s", DEVICE_FROM_USER_SPACE);
    }

    WriteWokeCounter++;
    // if(getUserSpaceReset){
    //     LOG_DEBUG_ICMP("User space reset");
    //     getUserSpaceReset = false;
    //     return length;
    // }

    // Check if processing a packet trip
    if (!atomic_read(&IsProcessingPacketTrip)) {
        return length;
    }

    queue1Length = PacketQueueLength(_write1PacketsQueue);
    queue2Length = PacketQueueLength(_write2PacketsQueue);
    if(queue2Length > 0){
        packetTrip = PacketQueuePop(_write2PacketsQueue);
        LOG_DEBUG_ICMP(packetTrip, "Popping from _write2PacketsQueue. Current size: %d; Size after pop: %d", queue2Length, queue2Length - 1);
    }else{
        packetTrip = PacketQueuePop(_write1PacketsQueue);
        LOG_DEBUG_ICMP(packetTrip, "Popping from _write1PacketsQueue. Current size: %d; Size after peek: %d", queue1Length, queue1Length - 1);
    }

    // Check for null values and handle errors
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip, length, _stopAndResetProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->packet, length, _stopAndResetProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry, length, _stopAndResetProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry->skb, length, _stopAndResetProcessingPacket(packetTrip));

    LOG_DEBUG_ICMP(packetTrip, "Write active for %s Processing packet: %p", DEVICE_FROM_USER_SPACE, packetTrip);

    if (!packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        return _processResponseHeader(userBuffer, length, packetTrip);
    } else if (packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        return _processResponseData(userBuffer, length, packetTrip);
    } else if (packetTrip->responsePacket->headerProcessed && packetTrip->responsePacket->dataProcessed) {
        LOG_WARNING("Response packet was already processed");
        _stopProcessingPacket(packetTrip);
        return length;
    } else {
        LOG_ERROR("Response packet is in an invalid state");
        _stopProcessingPacket(packetTrip);
        return length;
    }

    _stopProcessingPacket(packetTrip);
    return 0;
}

// Private function to process the response header
static int _processResponseHeader(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip) {
    s32 responseVersion = 0;
    s32 responseLength = 0;
    int readBytes = 0;


    LOG_DEBUG_ICMP(packetTrip, "Processing 🆗🆗{📥}📥 (Sent header: yes / Sent body: yes / Received header: no / Received body: no). Processing header (%zu bytes) - Routing type: %d", length, packetTrip->routingType);

    if (length < sizeof(s32) * 2) {
        LOG_ERROR("Response packet header length is too small %zu < %zu", length, sizeof(s32));
        LOG_ERROR("Failed     🆗🆗{⚠}📥 (Sent header: yes / Sent body: yes / Received header: failed / Received body: no).");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    if (copy_from_user(&responseVersion, userBuffer, sizeof(s32)) != 0) {
        LOG_ERROR("Failed to copy response packet version from user space (%zu bytes)", length);
        LOG_ERROR("Failed     🆗🆗{⚠}📥 (Sent header: yes / Sent body: yes / Received header: failed / Received body: no).");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    userBuffer += sizeof(s32);
    readBytes += sizeof(s32);

    if (copy_from_user(&responseLength, userBuffer, sizeof(s32)) != 0) {
        LOG_ERROR("Failed to copy response packet length from user space (%zu bytes)", length);
        LOG_ERROR("Failed     🆗🆗{⚠}📥 (Sent header: yes / Sent body: yes / Received header: failed / Received body: no).");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    userBuffer += sizeof(s32);
    readBytes += sizeof(s32);

    LOG_DEBUG_ICMP(packetTrip, "Processed  🆗🆗{🆗}📥 (Sent header: yes / Sent body: yes / Received header: yes / Received body: no). Processed header (%d bytes) - Routing type: %d", readBytes, packetTrip->routingType);
    LOG_DEBUG_ICMP(packetTrip, "Header from user space %zu bytes; Version: %d; Length: %d", sizeof(s32) * 2, responseVersion, responseLength);
    packetTrip->responsePacket->headerProcessed = true;
    packetTrip->responsePacket->size = responseLength;
    // if (copy_from_user(&decisionInt, userBuffer, responseLength) != 0) {
    //     LOG_ERROR("Failed to copy response packet decision from user space (%zu bytes)", length);
    //     _stopProcessingPacket(packetTrip);
    //     return -EFAULT;
    // }

    // userBuffer += sizeof(s32);
    // readBytes += sizeof(s32);

    
    // //todo: reset logic?
    // LOG_DEBUG_ICMP(packetTrip, "Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
    // packetTrip->decision = (s64)decisionInt;
    // packetTrip->responsePacket->dataProcessed = true;

    // LOG_DEBUG_ICMP(packetTrip, "PACKET FULLY PROCESSED - USER SPACE (%d bytes)", readBytes);
    // _stopProcessingPacket(packetTrip);
    int write2QueueLength = PacketQueueLength(_write2PacketsQueue);
    LOG_DEBUG_ICMP(packetTrip, "Pushing to _write2PacketsQueue. Current size: %d; Size after push: %d", write2QueueLength, write2QueueLength + 1);
    PacketQueuePush(_write2PacketsQueue, packetTrip);
    return readBytes;
}

// Private function to process the response data
static int _processResponseData(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip) {
    s32 decisionInt;
    int readBytes = 0;

    LOG_DEBUG_ICMP(packetTrip, "Processing 🆗🆗🆗{📥} (Sent header: yes / Sent body: yes / Received header: yes / Received body: no). Processing data (%zu bytes) - Routing type: %d", length, packetTrip->routingType);
    if(length != packetTrip->responsePacket->size){
        LOG_ERROR("Response packet data length expected doesn't match received %zu != %zu", length, packetTrip->responsePacket->size);
        LOG_ERROR("Failed     🆗🆗🆗{⚠} (Sent header: yes / Sent body: yes / Received header: yes / Received body: failed).");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    if (copy_from_user(&decisionInt, userBuffer, packetTrip->responsePacket->size) != 0) {
        LOG_ERROR("Failed to copy response packet data from user space (%zu bytes)", length);
        LOG_ERROR("Failed     🆗🆗🆗{⚠} (Sent header: yes / Sent body: yes / Received header: yes / Received body: failed).");
        _stopAndResetProcessingPacket(packetTrip);
        return length;
    }

    userBuffer += packetTrip->responsePacket->size;
    readBytes += packetTrip->responsePacket->size;

    //todo: reset logic?
    LOG_DEBUG_ICMP(packetTrip, "Processed  🆗🆗🆗{🆗} (Sent header: yes / Sent body: yes / Received header: yes / Received body: yes). Processed data (%d bytes) - Routing type: %d", readBytes, packetTrip->routingType);
    LOG_DEBUG_ICMP(packetTrip, "Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
    packetTrip->decision = (s64)decisionInt;
    packetTrip->responsePacket->dataProcessed = true;

    LOG_DEBUG_ICMP(packetTrip, "PACKET FULLY PROCESSED - USER SPACE (%zu bytes)", length);
    _stopProcessingPacket(packetTrip);
    return readBytes;
}

static void _shouldCaptureChangeHandler(bool shouldCapture){
    if(!IsInitialized()){
        return;
    }
    
    NetFilterShouldCaptureChangeHandler(shouldCapture);
    if(!shouldCapture){
        _resetPacketProcessing();
    }
}


static int _openDeviceTo(struct inode *inode, struct file *file){
    SetUserSpaceReadConnected();
    return _openDevice(inode, file, DEVICE_TO_USER_SPACE, &_deviceToUserMutex, &_deviceOpenToCount);
}

static int _openDeviceFrom(struct inode *inode, struct file *file){
    SetUserSpaceWriteConnected();
    return _openDevice(inode, file, DEVICE_FROM_USER_SPACE, &_deviceFromUserMutex, &_deviceOpenFromCount);
}

static int _releaseDeviceTo(struct inode *inode, struct file *file) {
    SetUserSpaceReadDisconnected();
    return _releaseDevice(inode, file, DEVICE_TO_USER_SPACE, &_deviceToUserMutex, &_deviceOpenToCount);
}

static int _releaseDeviceFrom(struct inode *inode, struct file *file) {
    SetUserSpaceWriteDisconnected();
    return _releaseDevice(inode, file, DEVICE_FROM_USER_SPACE, &_deviceFromUserMutex, &_deviceOpenFromCount);
}
