#include "userspace_comm.h"

// Constants
#define PROC_FILENAME "MY_COMM"
#define BUFFER_SIZE 100
#define COMMUNICATION_VERSION 2
#define DEVICE_TO_USER_SPACE "extrava_to_process"
#define DEVICE_FROM_USER_SPACE "extrava_from_process"
#define CLASS_NAME  "Extravaganza"
#define S32_SIZE (sizeof(s32))
#define TRANSACTION_HEADER_SIZE (S32_SIZE * 3)

const long TIMEOUT_IN_MSECS = 5000;
const char* MESSAGE_PACKET_PROCESSOR_STARTED = "Packet processor thread started";
const char* MESSAGE_WAITING_NEW_PACKET = "Waiting for new packet trip";
const char* MESSAGE_PACKETS_CAPTURED = "Packets captured: %ld; Packets processed: %ld; Packets accepted: %ld; Packets modified: %ld; Packets dropped: %ld; Packets Stale: %ld";
const char* MESSAGE_WAITING_USER_PROCESS = "Waiting for user space to process packet";
const char* MESSAGE_TIMEOUT_USERSPACE = "Packet processor thread timed out on userspace item processed wait queue";
const char* MESSAGE_ERROR_USERSPACE = "Packet processor thread failed on userspace item processed wait queue";
const char* MESSAGE_POPPING_PACKET = "Popping from packet queue. Current size: %d; Size after pop: %d";
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
static bool _dataNeedsProcessing = false;
static bool _isLoaded = false;
static int _majorNumberFromUser;
static int _majorNumberToUser;
static struct device* _netmodDevice = NULL;
static struct device* _netmodDeviceAck = NULL;

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
static void _cleanUpStaleItemsOnQueue(PacketQueue* queue, const char *queueName);

#define CLEANUP_STALE_ITEMS_ON_QUEUE(queue) _cleanUpStaleItemsOnQueue(&queue, #queue)

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
bool IsProcessingPacketTrip = false;
long PacketsCapturedCounter = 0;
long PacketsProcessedCounter = 0;
long PacketsAcceptCounter = 0;
long PacketsManipulateCounter = 0;
long PacketsDropCounter = 0;
long PacketsStaleCounter = 0;

DECLARE_WAIT_QUEUE_HEAD(UserReadWaitQueue);
extern bool UserRead;

// Private Functions
static int _releaseDevice(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *deviceMutex, atomic_t *atomicValue) {
    atomic_dec(atomicValue);
    mutex_unlock(deviceMutex);
    LOG_INFO("Device %s disconnected from user space", deviceName);
    return 0;
}

static int _openDevice(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *deviceMutex, atomic_t *atomicValue) {
    if(!mutex_trylock(deviceMutex)){
        LOG_INFO("Device %s cannot lock; is being used by another process.", deviceName);
        return -EBUSY;
    }

    atomic_inc(atomicValue);
    LOG_INFO("Device %s connected from user space", deviceName);
    return 0;
}

// Public Functions
int SetupUserSpaceCommunication(void) {
    IsProcessingPacketTrip = false;
    _dataNeedsProcessing = false;

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
    IsProcessingPacketTrip = false;
    //_isDataPending = false;

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
    long timeout = msecs_to_jiffies(TIMEOUT_IN_MSECS); 
    while (!kthread_should_stop()) {
        LOG_DEBUG_ICMP(MESSAGE_WAITING_NEW_PACKET);
        
        wait_event_interruptible(ReadQueueItemAddedWaitQueue, _readQueueItemAdded == true);
        _readQueueItemAdded = false;
        PacketsCapturedCounter++;
        if(PacketsCapturedCounter % 1000 == 0){
            LOG_INFO(MESSAGE_PACKETS_CAPTURED, PacketsCapturedCounter, PacketsProcessedCounter, PacketsAcceptCounter, PacketsManipulateCounter, PacketsDropCounter, PacketsStaleCounter);
        }

        _userRead = true;
        wake_up_interruptible(&UserReadWaitQueue);

        LOG_DEBUG_ICMP(MESSAGE_WAITING_USER_PROCESS);
        long ret = wait_event_interruptible_timeout(UserspaceItemProcessedWaitQueue, _userspaceItemProcessed == true, timeout);
        _userspaceItemProcessed = false;

        if (ret == 0) {
            LOG_DEBUG_ICMP(MESSAGE_TIMEOUT_USERSPACE);
            _resetPacketProcessing();
            continue;
        } else if (ret == -ERESTARTSYS) {
            LOG_ERROR(MESSAGE_ERROR_USERSPACE);
            _resetPacketProcessing();
            continue;
        }

        int queueLength = PacketQueueLength(&_injectionPacketsQueue);
        LOG_DEBUG_ICMP(MESSAGE_POPPING_PACKET, queueLength, queueLength - 1);
        PendingPacketRoundTrip *packetTrip = PacketQueuePop(&_injectionPacketsQueue);
        if(packetTrip == NULL){
            LOG_ERROR("Processed packet trip is null");
            continue;
        }

        LOG_DEBUG_ICMP(MESSAGE_PACKET_PROCESSED, packetTrip->decision);
        nf_reinject(packetTrip->entry, packetTrip->decision == ACCEPT ? NF_ACCEPT : NF_DROP);
        if(packetTrip->decision == ACCEPT){
            PacketsAcceptCounter++;
        }
        else if(packetTrip->decision == MANIPULATE){
            PacketsManipulateCounter++;
        }
        else if(packetTrip->decision == DROP){
            PacketsDropCounter++;
        }

        LOG_DEBUG_ICMP(MESSAGE_REINJECTION_COMPLETE, packetTrip->decision);
        FreePendingPacketTrip(packetTrip);
        LOG_DEBUG_ICMP(MESSAGE_CLEANUP_DONE);
        PacketsProcessedCounter++;
        _cleanStaleItemsOnAllQueues();
    }

    LOG_INFO(MESSAGE_PROCESSOR_EXITING);
    _queueProcessorExited = true;
    wake_up_interruptible(&QueueProcessorExitedWaitQueue);
    return 0;
}

static void _cleanUpStaleItemsOnQueue(PacketQueue* queue, const char *queueName){
    int queueLength = PacketQueueLength(queue);
    int numberCleaned = 0;
    while(queueLength > 0){
        PendingPacketRoundTrip* peekPacketTrip = PacketQueuePeek(queue);
        if(peekPacketTrip == NULL){
            return;
        }

        s64 millisecondsElapsedFromCreation = elapsedMilliseconds(&peekPacketTrip->createdTime);
        if(millisecondsElapsedFromCreation > TIMEOUT_IN_MSECS){
            LOG_DEBUG_ICMP("Packet trip has been in the queue for too long (%lld ms). Removing...", millisecondsElapsedFromCreation);
            PendingPacketRoundTrip* popPacketTrip = PacketQueuePop(queue);
            if(popPacketTrip != peekPacketTrip){
                LOG_ERROR("Peeked packet trip is not the same as popped packet trip. Freeing peeked packet trip...");
            }

            peekPacketTrip = NULL;
            FreePendingPacketTrip(popPacketTrip);
            numberCleaned++;
            PacketsStaleCounter++;
        }
        queueLength = PacketQueueLength(queue);
    }

    if(numberCleaned>0){
        int newQueueLength = PacketQueueLength(queue);
        LOG_DEBUG_ICMP("Cleaned %d stale packet trips from %s. Current size: %d;", numberCleaned, queueName, newQueueLength);
    }
}


static void _cleanStaleItemsOnAllQueues(void){
    CLEANUP_STALE_ITEMS_ON_QUEUE(_read1PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_read2PacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_writePacketsQueue);
    CLEANUP_STALE_ITEMS_ON_QUEUE(_injectionPacketsQueue);
}

static void _stopProcessingPacket(PendingPacketRoundTrip* packetTrip) {
    LOG_DEBUG_ICMP("Stopping processing for this packet");
    if (packetTrip == NULL || packetTrip->packet == NULL || packetTrip->entry == NULL || packetTrip->entry->skb == NULL) {
        LOG_ERROR("Invalid packetTrip structure");
    } else {
        int injectQueueLength = PacketQueueLength(&_injectionPacketsQueue);
        LOG_DEBUG_ICMP("Pushing to _injectionPacketsQueue. Current size: %d; Size after push: %d", injectQueueLength, injectQueueLength + 1);
        PacketQueuePush(&_injectionPacketsQueue, packetTrip);
    }

    _userRead = false;
    IsProcessingPacketTrip = false;
    _userspaceItemProcessed = true;
    wake_up_interruptible(&UserspaceItemProcessedWaitQueue);
}

// Public functions
static void _resetPacketProcessing(void) {
    IsProcessingPacketTrip = false;
    _dataNeedsProcessing = false;
    _userRead = false;
    _readQueueItemAdded = false;
    _userspaceItemProcessed = false;
    _queueItemProcessed = false;
    _queueProcessorExited = false;
    _cleanStaleItemsOnAllQueues();
}

static ssize_t _readDeviceTo(struct file* filep, char __user* buf, size_t len, loff_t* offset) {
    int errorCount = 0;
    int queue1Length = 0;
    int queue2Length = 0;
    PendingPacketRoundTrip* packetTrip = NULL;
    LOG_DEBUG_ICMP("Read active for %s", DEVICE_TO_USER_SPACE);
    LOG_DEBUG_ICMP("Blocking read for %s. Waiting for new data...", DEVICE_TO_USER_SPACE);
    // Using wait_event_interruptible to sleep until a packet is available
    wait_event_interruptible(UserReadWaitQueue, _userRead);

    LOG_DEBUG_ICMP("Woke up from UserReadWaitQueue for %s", DEVICE_TO_USER_SPACE);
    if (filep->f_flags & O_NONBLOCK) {
        LOG_DEBUG_ICMP("Non-blocking read, no packet found for %s", DEVICE_TO_USER_SPACE);
        _stopProcessingPacket(NULL);
        return -EAGAIN;
    }

    IsProcessingPacketTrip = true;
    queue1Length = PacketQueueLength(&_read1PacketsQueue);
    queue2Length = PacketQueueLength(&_read2PacketsQueue);
    if(queue2Length > 0){
        LOG_DEBUG_ICMP("Popping from _read2PacketsQueue. Current size: %d; Size after pop: %d", queue2Length, queue2Length - 1);
        packetTrip = PacketQueuePop(&_read2PacketsQueue);
    }
    else{
        LOG_DEBUG_ICMP("Popping from _read1PacketsQueue. Current size: %d; Size after pop: %d", queue1Length, queue1Length - 1);
        packetTrip = PacketQueuePop(&_read1PacketsQueue);
    }

    CHECK_NULL(LOG_TYPE_ERROR, packetTrip, -EAGAIN);
    LOG_DEBUG_ICMP("Packet trip info: %p; Packet to eval: %p (size: %d)", packetTrip, packetTrip->entry->skb, packetTrip->entry->skb->len);
    if(!packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        int nextQueueLength = 0;
        unsigned char headerVersion[S32_SIZE];
        unsigned char headerLength[S32_SIZE];
        unsigned char headerRoutingType[S32_SIZE];
        unsigned char headerPayload[TRANSACTION_HEADER_SIZE];

        LOG_DEBUG_ICMP("Neither header nor data has been processed. Processing header (%lu bytes)...", TRANSACTION_HEADER_SIZE);
        // Ensure user buffer has enough space
        if (len < TRANSACTION_HEADER_SIZE) {
            LOG_DEBUG_ICMP("Error: User buffer is too small to hold packetTrip header %zu < %lu", len, TRANSACTION_HEADER_SIZE);
            _stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy header to user space
        LOG_DEBUG_ICMP("Sending packetTrip header to user space; Version: %d; Length: %lu bytes", COMMUNICATION_VERSION, TRANSACTION_HEADER_SIZE);
        
        intToBytes(COMMUNICATION_VERSION, headerVersion);
        intToBytes(packetTrip->entry->skb->len, headerLength);
        intToBytes(packetTrip->routingType, headerRoutingType);
        memcpy(headerPayload, headerRoutingType, sizeof(s32));
        memcpy(headerPayload + sizeof(s32), headerVersion, sizeof(s32));
        memcpy(headerPayload + (sizeof(s32)*2), headerLength, sizeof(s32));

        errorCount = copy_to_user(buf, headerPayload, TRANSACTION_HEADER_SIZE);
        if (errorCount){
            LOG_DEBUG_ICMP("Error: Failed to copy packetTrip header to user space");
            _stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->headerProcessed = true;
        LOG_DEBUG_ICMP("... header has been processed");

        nextQueueLength = PacketQueueLength(&_read2PacketsQueue);
        LOG_DEBUG_ICMP("Pushing to read2_packets_queue. Current size: %d; Size after push: %d", nextQueueLength, nextQueueLength + 1);
        PacketQueuePush(&_read2PacketsQueue, packetTrip);
        
        return TRANSACTION_HEADER_SIZE;
    }
 else if(packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = packetTrip->entry->skb->len;
        int writeQueueLength = 0;

        LOG_DEBUG_ICMP("Header has been processed but data has not been processed yet. Processing data (%d bytes)...", transactionSize);
        if (len < transactionSize) {
            LOG_DEBUG_ICMP("Error: User buffer is too small to hold packetTrip data %zu < %d", len, transactionSize);
            _stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        LOG_DEBUG_ICMP("Sending packetTrip data to user space; Version: %d; Length: %d bytes", COMMUNICATION_VERSION, transactionSize);
        errorCount = copy_to_user(buf, packetTrip->entry->skb->data, transactionSize);
        if (errorCount){
            LOG_DEBUG_ICMP("Error: Failed to copy packetTrip data to user space");
            _stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->dataProcessed = true;
        LOG_DEBUG_ICMP("... data has been processed");

        writeQueueLength = PacketQueueLength(&_writePacketsQueue);
        LOG_DEBUG_ICMP("Pushing to write_packets_queue. Current size: %d; Size after push: %d", writeQueueLength, writeQueueLength + 1);
        PacketQueuePush(&_writePacketsQueue, packetTrip);
        
        return transactionSize;
    }
    else if(packetTrip->packet->headerProcessed && packetTrip->packet->dataProcessed){
        LOG_DEBUG_ICMP("Warning: Packet was already processed");
    }
    else{
        LOG_DEBUG_ICMP("Error: Packet is in an invalid state");
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    LOG_DEBUG_ICMP("Packet request sent to user space");
    
    return 0;
}

static ssize_t _writeDeviceFrom(struct file* filep, const char __user* userBuffer, size_t length, loff_t* offset) {
    PendingPacketRoundTrip* packetTrip = NULL;
    int queueLength = 0;
    // Check if processing a packet trip
    if (!IsProcessingPacketTrip) {
        return 0;
    }

    queueLength = PacketQueueLength(&_writePacketsQueue);
    LOG_DEBUG_ICMP("Popping from writePacketsQueue. Current size: %d; Size after peek: %d", queueLength, queueLength - 1);
    packetTrip = PacketQueuePop(&_writePacketsQueue);

    // Check for null values and handle errors
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip, -EFAULT, _stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->packet, -EFAULT, _stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry, -EFAULT, _stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry->skb, -EFAULT, _stopProcessingPacket(packetTrip));

    LOG_DEBUG_ICMP("Write active for %s Processing packet: %p", DEVICE_FROM_USER_SPACE, packetTrip);

    if (!packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        return _processResponseHeader(userBuffer, length, packetTrip);
    } else if (packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        return _processResponseData(userBuffer, length, packetTrip);
    } else if (packetTrip->responsePacket->headerProcessed && packetTrip->responsePacket->dataProcessed) {
        LOG_WARNING("Response packet was already processed");
        _stopProcessingPacket(packetTrip);
        return 0;
    } else {
        LOG_ERROR("Response packet is in an invalid state");
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    _stopProcessingPacket(packetTrip);
    return 0;
}

// Private function to process the response header
static int _processResponseHeader(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip) {
    s32 decisionInt;
    s32 responseVersion = 0;
    s32 responseLength = 0;
    int readBytes = 0;

    LOG_DEBUG_ICMP("Neither response header nor data has been processed. Processing response header (%zu bytes)...", length);

    if (length < sizeof(s32) * 2) {
        LOG_ERROR("Response packet header length is too small %zu < %zu", length, sizeof(s32));
        _stopProcessingPacket(packetTrip);
        return -EINVAL;
    }

    if (copy_from_user(&responseVersion, userBuffer, sizeof(s32)) != 0) {
        LOG_ERROR("Failed to copy response packet version from user space (%zu bytes)", length);
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    userBuffer += sizeof(s32);
    readBytes += sizeof(s32);

    if (copy_from_user(&responseLength, userBuffer, sizeof(s32)) != 0) {
        LOG_ERROR("Failed to copy response packet length from user space (%zu bytes)", length);
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    userBuffer += sizeof(s32);
    readBytes += sizeof(s32);

    LOG_DEBUG_ICMP("Received response packet header from user space %zu bytes; Version: %d; Length: %d", sizeof(s32) * 2, responseVersion, responseLength);
    packetTrip->responsePacket->headerProcessed = true;

    if (copy_from_user(&decisionInt, userBuffer, responseLength) != 0) {
        LOG_ERROR("Failed to copy response packet decision from user space (%zu bytes)", length);
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    userBuffer += sizeof(s32);
    readBytes += sizeof(s32);

    LOG_DEBUG_ICMP("Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
    packetTrip->decision = (s64)decisionInt;
    packetTrip->responsePacket->dataProcessed = true;

    LOG_DEBUG_ICMP("PACKET FULLY PROCESSED - USER SPACE (%d bytes)", readBytes);
    _stopProcessingPacket(packetTrip);
    return readBytes;
}

// Private function to process the response data
static int _processResponseData(const char __user* userBuffer, size_t length, PendingPacketRoundTrip* packetTrip) {
    s32 decisionInt;
    LOG_DEBUG_ICMP("Response header has been processed but data has not been processed yet. Processing response data (%zu bytes)...", length);
    
    if (copy_from_user(&decisionInt, userBuffer, length) != 0) {
        LOG_ERROR("Failed to copy response packet data from user space (%zu bytes)", length);
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    LOG_DEBUG_ICMP("Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
    packetTrip->decision = (s64)decisionInt;
    packetTrip->responsePacket->dataProcessed = true;

    LOG_DEBUG_ICMP("PACKET FULLY PROCESSED - USER SPACE (%zu bytes)", length);
    _stopProcessingPacket(packetTrip);
    return length;
}


static int _openDeviceTo(struct inode *inode, struct file *file){
    return _openDevice(inode, file, DEVICE_TO_USER_SPACE, &_deviceToUserMutex, &_deviceOpenToCount);
}

static int _openDeviceFrom(struct inode *inode, struct file *file){
    return _openDevice(inode, file, DEVICE_FROM_USER_SPACE, &_deviceFromUserMutex, &_deviceOpenFromCount);
}

static int _releaseDeviceTo(struct inode *inode, struct file *file) {
    return _releaseDevice(inode, file, DEVICE_TO_USER_SPACE, &_deviceToUserMutex, &_deviceOpenToCount);
}

static int _releaseDeviceFrom(struct inode *inode, struct file *file) {
    return _releaseDevice(inode, file, DEVICE_FROM_USER_SPACE, &_deviceFromUserMutex, &_deviceOpenFromCount);
}
