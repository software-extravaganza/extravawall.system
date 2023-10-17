#include "userspace_comm.h"

// Constants
#define PROC_FILENAME "MY_COMM"
#define BUFFER_SIZE 100
#define COMMUNICATION_VERSION 2
#define DEVICE_TO_USER_NAME "DeviceToUser"
#define DEVICE_FROM_USER_NAME "DeviceFromUser"
#define CLASS_NAME  "Extravaganza"

// Private Fields
static DEFINE_MUTEX(_deviceToUserMutex);
static DEFINE_MUTEX(_deviceFromUserMutex);
static struct class* _charClass = NULL;
static int _majorNumberToDeviceUser;
static int _majorNumberFromDeviceUser;
static struct device* _deviceToUser = NULL;
static struct device* _deviceFromUser = NULL;
static wait_queue_head_t _pendingPacketQueue;
static atomic_t _deviceOpenToUserCount = ATOMIC_INIT(0);
static atomic_t _deviceOpenFromUserCount = ATOMIC_INIT(0);
static bool _dataNeedsProcessing = false;
static bool _isLoaded = false;

// Public Fields
bool ProcessingPacketTrip = false;
long PacketsCapturedCounter = 0;
long PacketsProcessedCounter = 0;
long PacketsAcceptCounter = 0;
long PacketsManipulateCounter = 0;
long PacketsDropCounter = 0;

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
int SetupUserSpaceComm(void) {
    _isProcessingPacket = false;
    _dataNeedsProcessing = false;

    _majorNumberToUser = register_chrdev(0, DEVICE_NAME, &fops_netmod_to_user);
    if (_majorNumberToUser < 0) {
        logError("Failed to register a major number for netmod to user");
        return -1;
    }

    _majorNumberFromUser = register_chrdev(0, DEVICE_NAME_ACK, &fops_netmod_from_user);
    if (_majorNumberFromUser < 0) {
        logError("Failed to register a major number for netmod from user");
        unregister_chrdev(_majorNumberToUser, DEVICE_NAME);
        return -1;
    }

    _charClass = class_create(CLASS_NAME);
    if (IS_ERR(_charClass)) {
        unregister_chrdev(_majorNumberToUser, DEVICE_NAME);
        unregister_chrdev(_majorNumberFromUser, DEVICE_NAME_ACK);
        logError("Failed to register device class");
        return PTR_ERR(_charClass);
    }

    _netmodDevice = device_create(_charClass, NULL, MKDEV(_majorNumberToUser, 0), NULL, DEVICE_NAME);
    if (IS_ERR(_netmodDevice)) {
        logError("Failed to create the input device");
    }

    _netmodDeviceAck = device_create(_charClass, NULL, MKDEV(_majorNumberFromUser, 0), NULL, DEVICE_NAME_ACK);
    if (IS_ERR(_netmodDeviceAck)) {
        logError("Failed to create the output device");
    }

    mutex_init(&_netmodToMutex);
    mutex_init(&_netmodFromMutex);
    init_waitqueue_head(&_pendingPacketQueue);
    init_waitqueue_head(&_userReadWaitQueue);

    if (IS_ERR(_netmodDevice) || IS_ERR(_netmodDeviceAck)) {
        cleanupUserSpaceComm();
        cleanupNetfilterHooks();
        return IS_ERR(_netmodDevice) ? PTR_ERR(_netmodDevice) : PTR_ERR(_netmodDeviceAck);
    }

    int threadRegistration = registerQueueProcessorThreadHandler(packet_processor_thread);
    if (IS_ERR(threadRegistration)) {
        logError("Failed to register queue processor thread handler");
        cleanupUserSpaceComm();
        cleanupNetfilterHooks();
        return IS_ERR(threadRegistration);
    }

    _isLoaded = true;
    return 0;
}

void CleanupUserSpaceCommunication(void) {
    _isLoaded = false;
    _isProcessingPacket = false;
    _isDataPending = false;

    // Ensure no one is using the device or waits until they're done
    logMessage("Waiting for user space communication to be released...");
    while (_deviceOpenToCount > 0 || _deviceOpenFromCount > 0) {
        msleep(50); // This will sleep for 50ms before checking again.
    }
    logMessage("User space communication released");

    // Destroy any synchronization primitives associated with the device
    logMessage("Destroying synchronization primitives");
    mutex_destroy(&_netmodToMutex);
    mutex_destroy(&_netmodFromMutex);

    // Remove the device files under /dev
    logMessage("Removing device files");
    device_destroy(_charClass, MKDEV(_majorNumberToUser, 0));
    device_destroy(_charClass, MKDEV(_majorNumberFromUser, 0));

    // Cleanup the device class
    logMessage("Cleaning up device class");
    class_unregister(_charClass);
    class_destroy(_charClass);

    // Unregister the character devices
    logMessage("Unregistering character devices");
    unregister_chrdev(_majorNumberToUser, DEVICE_NAME);
    unregister_chrdev(_majorNumberFromUser, DEVICE_NAME_ACK);
}

int PacketProcessorThread(void *data) {
    LOG_INFO(LOG_PACKET_PROCESSOR_STARTED);
    long timeout = msecs_to_jiffies(TIMEOUT_IN_MSECS); 
    while (!kthread_should_stop()) {
        LOG_DEBUG_PACKET(LOG_WAITING_NEW_PACKET);
        
        wait_event_interruptible(read_queue_item_added_wait_queue, _readQueueItemAdded == true);
        _readQueueItemAdded = false;
        _packetsCapturedCounter++;
        if(_packetsCapturedCounter % 1000 == 0){
            LOG_INFO(LOG_PACKETS_CAPTURED, _packetsCapturedCounter, _packetsProcessedCounter, _packetsAcceptCounter, _packetsManipulateCounter, _packetsDropCounter);
        }

        _userRead = true;
        wake_up_interruptible(&user_read_wait_queue);

        LOG_DEBUG_PACKET(LOG_WAITING_USER_PROCESS);
        long ret = wait_event_interruptible_timeout(userspace_item_processed_wait_queue, _userspaceItemProcessed == true, timeout);
        _userspaceItemProcessed = false;

        if (ret == 0) {
            LOG_DEBUG_PACKET(LOG_TIMEOUT_USERSPACE);
            resetPacketProcessing();
            continue;
        } else if (ret == -ERESTARTSYS) {
            LOG_ERROR(LOG_ERROR_USERSPACE);
            resetPacketProcessing();
            continue;
        }

        int queueLength = pq_len_packetTrip(&injection_packets_queue);
        LOG_DEBUG_PACKET(LOG_POPPING_PACKET, queueLength, queueLength - 1);
        PendingPacketRoundTrip *packetTrip = pq_pop_packetTrip(&injection_packets_queue);
        if(packetTrip == NULL){
            LOG_ERROR("Processed packet trip is null");
            continue;
        }
        
        LOG_DEBUG_PACKET(LOG_PACKET_PROCESSED, packetTrip->decision);
        nf_reinject(packetTrip->entry, packetTrip->decision == ACCEPT ? NF_ACCEPT : NF_DROP);
        if(packetTrip->decision == ACCEPT){
            _packetsAcceptCounter++;
        }
        else if(packetTrip->decision == MANIPULATE){
            _packetsManipulateCounter++;
        }
        else if(packetTrip->decision == DROP){
            _packetsDropCounter++;
        }

        LOG_DEBUG_PACKET(LOG_REINJECTION_COMPLETE, packetTrip->decision);
        freePendingPacketTrip(packetTrip);
        LOG_DEBUG_PACKET(LOG_CLEANUP_DONE);
        _packetsProcessedCounter++;
    }

    LOG_INFO(LOG_PROCESSOR_EXITING);
    _queueProcessorExited = true;
    wake_up_interruptible(&queue_processor_exited_wait_queue);
    return 0;
}

static void stopPacketProcessing(PendingPacketRoundTrip* packetTrip) {
    logDebugPacket(__func__, "Stopping processing for this packet", 0, 0);
    if (packetTrip == NULL || packetTrip->packet == NULL || packetTrip->entry == NULL || packetTrip->entry->skb == NULL) {
        logError(__func__, "Invalid packetTrip structure");
    } else {
        int injectQueueLength = pqLenPacketTrip(&injection_packets_queue);
        logDebugPacket(__func__, "Processing packet trip", injectQueueLength, injectQueueLength + 1);
        pqPushPacketTrip(&injection_packets_queue, packetTrip);
    }

    _userRead = false;
    _processingPacketTrip = false;
    _userspaceItemProcessed = true;
    wakeUpInterruptible(&userspaceItemProcessedWaitQueue);
}

// Public functions
void ResetPacketProcessing(void) {
    _processingPacketTrip = false;
    _dataNeedsProcessing = false;
    _userRead = false;
    _readQueueItemAdded = false;
    _userspaceItemProcessed = false;
    _queueItemProcessed = false;
    _queueProcessorExited = false;
}

ssize_t DevUsercommRead(struct file* filep, char __user* buf, size_t len, loff_t* offset) {
    int errorCount = 0;

    printf("Read active for %s\n", DEVICE_NAME);
    printf("Blocking read for %s. Waiting for new data...\n", DEVICE_NAME);
    // Using wait_event_interruptible to sleep until a packet is available
    wait_event_interruptible(user_read_wait_queue, _userRead);

    printf("Woke up from wait_event_interruptible for %s\n", DEVICE_NAME);
    if (filep->f_flags & O_NONBLOCK) {
        printf("Non-blocking read, no packet found for %s\n", DEVICE_NAME);
        stopProcessingPacket(NULL);
        return -EAGAIN;
    }

    _processingPacketTrip = true;

    if(!_userRead){
        printf("Error: Packet is not being processed\n");
        return 0;
    }

    int queue1Length = pqLenPacketTrip(&read1_packets_queue);
    int queue2Length = pqLenPacketTrip(&read2_packets_queue);
    PendingPacketRoundTrip* packetTrip = NULL;
    if(queue2Length > 0){
        printf("Popping from read2_packets_queue. Current size: %d; Size after pop: %d\n", queue2Length, queue2Length - 1);
        packetTrip = pqPopPacketTrip(&read2_packets_queue);
    }
    else{
        printf("Popping from read1_packets_queue. Current size: %d; Size after pop: %d\n", queue1Length, queue1Length - 1);
        packetTrip = pqPopPacketTrip(&read1_packets_queue);
    }

    // Check for null values and handle errors
    // ... (similar to the provided CHECK_NULL_FAIL_EXEC calls)

    printf("Packet trip info: %p; Packet to eval: %p (size: %d)\n", packetTrip, packetTrip->entry->skb, packetTrip->entry->skb->len);
    if(!packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = sizeof(s32)*3;
        printf("Neither header nor data has been processed. Processing header (%d bytes)...\n", transactionSize);
        // Ensure user buffer has enough space
        if (len < transactionSize) {
            printf("Error: User buffer is too small to hold packetTrip header %zu < %zu\n", len, transactionSize);
            stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy header to user space
        printf("Sending packetTrip header to user space; Version: %d; Length: %zu bytes\n", COMM_VERSION, transactionSize);
        unsigned char headerVersion[sizeof(s32)];
        intToBytes(COMM_VERSION, headerVersion);

        unsigned char headerLength[sizeof(s32)];
        intToBytes(packetTrip->entry->skb->len, headerLength);

        unsigned char headerRoutingType[sizeof(s32)];
        intToBytes(packetTrip->routingType, headerRoutingType);

        unsigned char headerPayload[transactionSize];
        memcpy(headerPayload, headerRoutingType, sizeof(s32));
        memcpy(headerPayload + sizeof(s32), headerVersion, sizeof(s32));
        memcpy(headerPayload + (sizeof(s32)*2), headerLength, sizeof(s32));

        errorCount = copy_to_user(buf, headerPayload, transactionSize);
        if (errorCount){
            printf("Error: Failed to copy packetTrip header to user space\n");
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->headerProcessed = true;
        printf("... header has been processed\n");

        int nextQueueLength = pqLenPacketTrip(&read2_packets_queue);
        printf("Pushing to read2_packets_queue. Current size: %d; Size after push: %d\n", nextQueueLength, nextQueueLength + 1);
        pqPushPacketTrip(&read2_packets_queue, packetTrip);
        
        return transactionSize;
    }
 else if(packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = packetTrip->entry->skb->len;
        printf("Header has been processed but data has not been processed yet. Processing data (%d bytes)...\n", transactionSize);
        if (len < transactionSize) {
            printf("Error: User buffer is too small to hold packetTrip data %zu < %d\n", len, transactionSize);
            stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        printf("Sending packetTrip data to user space; Version: %d; Length: %zu bytes\n", COMM_VERSION, transactionSize);
        errorCount = copy_to_user(buf, packetTrip->entry->skb->data, transactionSize);
        if (errorCount){
            printf("Error: Failed to copy packetTrip data to user space\n");
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->dataProcessed = true;
        printf("... data has been processed\n");

        int writeQueueLength = pqLenPacketTrip(&write_packets_queue);
        printf("Pushing to write_packets_queue. Current size: %d; Size after push: %d\n", writeQueueLength, writeQueueLength + 1);
        pqPushPacketTrip(&write_packets_queue, packetTrip);
        
        return transactionSize;
    }
    else if(packetTrip->packet->headerProcessed && packetTrip->packet->dataProcessed){
        printf("Warning: Packet was already processed\n");
    }
    else{
        printf("Error: Packet is in an invalid state\n");
        stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    printf("Packet request sent to user space\n");
    
    return 0;
}

ssize_t DevUsercommWrite(struct file* filep, const char __user* userBuffer, size_t length, loff_t* offset) {

    // Check if processing a packet trip
    if (!_isProcessingPacketTrip) {
        return 0;
    }

    int queueLength = _getQueueLength(&writePacketsQueue);
    _logDebugPacket("Popping from writePacketsQueue. Current size: %d; Size after peek: %d", queueLength, queueLength - 1);
    PendingPacketRoundTrip* packetTrip = _popPacketTrip(&writePacketsQueue);

    // Check for null values and handle errors
    _checkNullAndHandleError(packetTrip, -EFAULT, _stopProcessingPacket);
    _checkNullAndHandleError(packetTrip->packet, -EFAULT, _stopProcessingPacket);
    _checkNullAndHandleError(packetTrip->entry, -EFAULT, _stopProcessingPacket);
    _checkNullAndHandleError(packetTrip->entry->skb, -EFAULT, _stopProcessingPacket);

    _logDebugPacket("Write active for %s Processing packet: %d", DEVICE_NAME_ACK_CONSTANT, packetTrip);

    if (!packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        _processResponseHeader(userBuffer, length, packetTrip);
    } else if (packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed) {
        _logError("Response packet was partially processed");
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    } else if (packetTrip->responsePacket->headerProcessed && packetTrip->responsePacket->dataProcessed) {
        _logWarning("Response packet was already processed");
        _stopProcessingPacket(packetTrip);
        return 0;
    } else {
        _logError("Response packet is in an invalid state");
        _stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    _stopProcessingPacket(packetTrip);
    return 0;
}

static int _openDevice(struct inode *inode, struct file *file, const char* deviceName, struct mutex *mutexLock, atomic_t *atomicValue){
    if(!mutex_trylock(mutexLock)){
        LOG_INFO("Failed to lock device '%s'. It's being used by another process.", deviceName);
        return -EBUSY;
    }

    atomic_inc(atomicValue);
    LOG_INFO("Device '%s' connected from user space.", deviceName);
    return 0;
}

static int _openDeviceTo(struct inode *inode, struct file *file){
    return _openDevice(inode, file, DEVICE_NAME, &_netmodToMutex, &_deviceOpenToCount);
}

static int _openDeviceFrom(struct inode *inode, struct file *file){
    return _openDevice(inode, file, DEVICE_NAME_ACK, &_netmodFromMutex, &_deviceOpenFromCount);
}

// Releases the user communication to device
static int userCommToRelease(struct inode *inode, struct file *file) {
    return userCommRelease(inode, file, DEVICE_NAME, &_netmodToMutex, &_deviceOpenToCount);
}

// Releases the user communication from device
static int userCommFromRelease(struct inode *inode, struct file *file) {
    return userCommRelease(inode, file, DEVICE_NAME_ACK, &_netmodFromMutex, &_deviceOpenFromCount);
}

static struct file_operations _operationsToDeviceUser = {
   .open = OpenDeviceToUser,
   .read = ReadDevice,
   .release = ReleaseDeviceToUser,
};

static struct file_operations _operationsFromDeviceUser = {
   .open = OpenDeviceFromUser,
   .write = WriteDevice,
   .release = ReleaseDeviceFromUser,
};

DECLARE_WAIT_QUEUE_HEAD(UserReadWaitQueue);
extern bool UserRead;
