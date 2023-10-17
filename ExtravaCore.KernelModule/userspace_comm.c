
#include "userspace_comm.h"

#define PROC_FILENAME "my_comm"
#define BUF_SIZE 100
#define COMM_VERSION 2
#define DEVICE_NAME "extrava_to_user"
#define DEVICE_NAME_ACK "extrava_from_user"
#define CLASS_NAME  "extrava"

static DEFINE_MUTEX(netmod_to_mutex);
static DEFINE_MUTEX(netmod_from_mutex);
static struct class* char_class = NULL;
static int majorNumber_to_user;
static int majorNumber_from_user;
static struct device* netmodDevice = NULL;
static struct device* netmodDeviceAck = NULL;
static wait_queue_head_t pending_packet_queue;
static atomic_t device_open_to_count = ATOMIC_INIT(0);
static atomic_t device_open_from_count = ATOMIC_INIT(0);
static bool data_needs_processing = false;
static bool isLoaded = false;
bool fired = false;
bool processingPacketTrip = false;
long packets_captured_counter = 0;
long packets_processed_counter = 0;
long packets_accept_counter = 0;
long packets_manipulate_counter = 0;
long packets_drop_counter = 0;

DECLARE_WAIT_QUEUE_HEAD(user_read_wait_queue);
extern bool user_read;

static int dev_usercomm_release(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *mutex_lock, atomic_t *atomic_value){
    atomic_dec(atomic_value);
    mutex_unlock(mutex_lock);
    LOG_INFO("Device %s disconnected from user space", deviceName);
    return 0;
}

static int dev_usercomm_open(struct inode *inodep, struct file *filep, char* deviceName, struct mutex *mutex_lock, atomic_t *atomic_value){
    if(!mutex_trylock(mutex_lock)){
        LOG_INFO("Device %s can not lock; is being used by another process.", deviceName);
        return -EBUSY;
    }

    atomic_inc(atomic_value);
    LOG_INFO("Device %s connected from user space", deviceName);
    return 0;
}

int setup_user_space_comm(void) {
    processingPacketTrip= false;
    data_needs_processing = false;
    // Register devices for user space communication
    majorNumber_to_user = register_chrdev(0, DEVICE_NAME, &fops_netmod_to_user);
    if (majorNumber_to_user<0){
        LOG_ERROR("Netmod_to_user failed to register a major number");
        return -1;
    }

    majorNumber_from_user = register_chrdev(0, DEVICE_NAME_ACK, &fops_netmod_from_user);
    if (majorNumber_from_user<0){
        LOG_ERROR("Netmod_from_user failed to register a major number");
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        return -1;
    }

    char_class = class_create(CLASS_NAME);
    if (IS_ERR(char_class)){
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        unregister_chrdev(majorNumber_from_user, DEVICE_NAME);
        LOG_ERROR("Failed to register device class 'in'");
        return PTR_ERR(char_class);
    }

    netmodDevice = device_create(char_class, NULL, MKDEV(majorNumber_to_user, 0), NULL, DEVICE_NAME);
    if (IS_ERR(netmodDevice)){
        LOG_ERROR("Failed to create the 'in' device");
    }

    netmodDeviceAck = device_create(char_class, NULL, MKDEV(majorNumber_from_user, 0), NULL, DEVICE_NAME_ACK);
    if (IS_ERR(netmodDeviceAck)){
        LOG_ERROR("Failed to create the 'out' device");
    }

    mutex_init(&netmod_to_mutex);
    mutex_init(&netmod_from_mutex);
    init_waitqueue_head(&pending_packet_queue);
    init_waitqueue_head(&user_read_wait_queue);

    // Error handling: if either device fails to initialize, cleanup everything and exit
    if(IS_ERR(netmodDevice) || IS_ERR(netmodDeviceAck)) {
        cleanup_user_space_comm();
        cleanup_netfilter_hooks();
        return IS_ERR(netmodDevice) ? PTR_ERR(netmodDevice) : PTR_ERR(netmodDeviceAck); 
    }

    // register_packet_processing_callback(packet_processor);
    // init_completion(&userspace_item_ready);
    //init_completion(&userspace_item_processed_wait_queue);
    int thread_reg = register_queue_processor_thread_handler(packet_processor_thread);
    if(IS_ERR(thread_reg)){
        LOG_ERROR("Failed to register queue processor thread handler");
        cleanup_user_space_comm();
        cleanup_netfilter_hooks();
        return IS_ERR(thread_reg);
    }

    isLoaded = true;
    return 0;
}


void cleanup_user_space_comm(void) {
    isLoaded = false;
    processingPacketTrip= false;
    data_needs_processing = false;
    
     // Ensure no one is using the device or waits until they're done
    // If you've used try_module_get/module_put in your open and release fops, this can help here.
    LOG_DEBUG("Waiting for user space communication to be released...");
    while (atomic_read(&device_open_to_count) > 0 || atomic_read(&device_open_from_count) > 0) {
        msleep(50); // This will sleep for 50ms before checking again.
    }
    LOG_DEBUG("...User space communication released");

    // Destroy any synchronization primitives associated with the device
    LOG_DEBUG("Destroying user space communication synchronization primitives");
    mutex_destroy(&netmod_to_mutex);
    mutex_destroy(&netmod_from_mutex);

    // Remove the device files under /dev
    LOG_DEBUG("Removing user space communication device files");
    device_destroy(char_class, MKDEV(majorNumber_to_user, 0));
    device_destroy(char_class, MKDEV(majorNumber_from_user, 0));

    // Cleanup the device class
    LOG_DEBUG("Cleaning up user space communication device class");
    class_unregister(char_class);
    class_destroy(char_class);

    // Unregister the character devices
    LOG_DEBUG("Unregistering user space communication character devices");
    unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
    unregister_chrdev(majorNumber_from_user, DEVICE_NAME_ACK);
}

int packet_processor_thread(void *data) {
    LOG_INFO("Packet processor thread started");
    long timeout = msecs_to_jiffies(5000); 
    while (!kthread_should_stop()) {
        // Sleep until there's work to do
        LOG_DEBUG_PACKET("Waiting for new packet trip");
        
        wait_event_interruptible(read_queue_item_added_wait_queue, read_queue_item_added == true);
        read_queue_item_added = false;
        packets_captured_counter++;
        if(packets_captured_counter % 1000 == 0){
            LOG_INFO("Packets captured: %ld; Packets processed: %ld; Packets accepted: %ld; Packets modified: %ld; Packets dropped: %ld", packets_captured_counter, packets_processed_counter, packets_accept_counter, packets_manipulate_counter, packets_drop_counter);
        }

        // Communicate with user space and process the packet
        user_read = true;
        wake_up_interruptible(&user_read_wait_queue);  // Notify the read handler

        // Wait until user has processed the packet
        LOG_DEBUG_PACKET("Waiting for user space to process packet");
        long ret = wait_event_interruptible_timeout(userspace_item_processed_wait_queue, userspace_item_processed == true, timeout);
        userspace_item_processed = false;

        if (ret == 0) {
            LOG_DEBUG_PACKET("Packet processor thread timed out on userspace_item_processed_wait_queue");
            reset_packet_processing();
            continue;
        } else if (ret == -ERESTARTSYS) {
            LOG_ERROR("Packet processor thread failed on userspace_item_processed_wait_queue");
            reset_packet_processing();
            continue;
        }

        int queueLength = pq_len_packetTrip(&injection_packets_queue);
        LOG_DEBUG_PACKET("Popping from pq_pop_packetTrip. Current size: %d; Size after pop: %d", queueLength, queueLength - 1);
        PendingPacketRoundTrip *packetTrip = pq_pop_packetTrip(&injection_packets_queue);
        if(packetTrip == NULL){
            LOG_ERROR("Processed packet trip is null");
            continue;
        }
        
        LOG_DEBUG_PACKET("User space has processed packet with decision %lld. Reinjecting...", packetTrip->decision);
        nf_reinject(packetTrip->entry, packetTrip->decision == ACCEPT ? NF_ACCEPT : NF_DROP);
        if(packetTrip->decision == ACCEPT){
            packets_accept_counter++;
        }
        else if(packetTrip->decision == MANIPULATE){
            packets_manipulate_counter++;
        }
        else if(packetTrip->decision == DROP){
            packets_drop_counter++;
        }

        LOG_DEBUG_PACKET("Reinjection complete (%lld)", packetTrip->decision);
        // if (user_decision == DROP) {
        //     kfree_skb(processedPacketTrip->packet);
        // } else if (user_decision == ACCEPT) {
        //     struct nf_queue_entry entry;

        //     entry.skb = processedPacketTrip->packet;
        //     entry.state = processedPacketTrip->state;
        //     nf_reinject(&entry, NF_ACCEPT);
        // }

        // Ensure you also free any additional memory or structures related to processedPacketTrip
        free_pending_packetTrip(packetTrip);
        LOG_DEBUG_PACKET("Packet trip processed. Done cleaning up and ready for new packet.");
        packets_processed_counter++;
    }

    // Signal that we're done processing and exiting
    LOG_INFO("Packet processor thread exiting");
    queue_processor_exited = true;
    wake_up_interruptible(&queue_processor_exited_wait_queue);
    return 0;
}

void reset_packet_processing(void){
    processingPacketTrip = false;
    data_needs_processing = false;
    user_read = false;
    read_queue_item_added = false;
    userspace_item_processed = false;
    queue_item_processed = false;
    queue_processor_exited = false;
}

static void stopProcessingPacket(PendingPacketRoundTrip *packetTrip){
    LOG_DEBUG_PACKET("STOPPING PROCESSING FOR THIS PACKET");
    if (packetTrip == NULL || packetTrip->packet == NULL || packetTrip->entry == NULL || packetTrip->entry->skb == NULL) {
        LOG_ERROR("Invalid packetTrip structure");
    }
    else{
        int injectQueueLength = pq_len_packetTrip(&injection_packets_queue);
        LOG_DEBUG_PACKET("Processing packet trip. Current injection queue size: %d; Size after add: %d", injectQueueLength, injectQueueLength + 1);
        pq_push_packetTrip(&injection_packets_queue, packetTrip);
    }

    user_read = false;
    processingPacketTrip = false;
    userspace_item_processed = true;
    wake_up_interruptible(&userspace_item_processed_wait_queue);
    //wake_up_process(queue_processor_thread);
}

static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    int error_count = 0;
    //PendingPacketRoundTrip *packetTrip;
    LOG_DEBUG_PACKET("Read active for %s", DEVICE_NAME);
    LOG_DEBUG_PACKET("Blocking read for %s. Waiting for new data...", DEVICE_NAME);
    // Using wait_event_interruptible to sleep until a packet is available
    wait_event_interruptible(user_read_wait_queue, user_read);

    LOG_DEBUG_PACKET("Woke up from wait_event_interruptible for %s", DEVICE_NAME);
    if (filep->f_flags & O_NONBLOCK) {
        LOG_WARN("Non-blocking read, no packet found for %s", DEVICE_NAME);
        stopProcessingPacket(NULL);
        return -EAGAIN;
    }

    // Re-check packet after being woken up
    //packetTrip = pq_peek_packetTrip(&pending_packets_queue);
    processingPacketTrip = true;
    

    if(!user_read){
        LOG_ERROR("Packet is not being processed");
        return 0;
    }

    int queue1Length = pq_len_packetTrip(&read1_packets_queue);
    int queue2Length = pq_len_packetTrip(&read2_packets_queue);
    PendingPacketRoundTrip *packetTrip = NULL;
    if(queue2Length > 0){
        LOG_DEBUG_PACKET("Popping from read2_packets_queue. Current size: %d; Size after pop: %d", queue2Length, queue2Length - 1);
        packetTrip = pq_pop_packetTrip(&read2_packets_queue);
    }
    else{
        LOG_DEBUG_PACKET("Popping from read1_packets_queue. Current size: %d; Size after pop: %d", queue1Length, queue1Length - 1);
        packetTrip = pq_pop_packetTrip(&read1_packets_queue);
    }

    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->packet, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry->skb, -EFAULT, stopProcessingPacket(packetTrip));

    LOG_DEBUG_PACKET("Packet trip info: %p; Packet to eval: %p (size: %d)", packetTrip, packetTrip->entry->skb, packetTrip->entry->skb->len);
    if(!packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = sizeof(s32)*3;
        LOG_DEBUG_PACKET("Neither header nor data has been processed. Processing header (%d bytes)...", transactionSize);
        // Ensure user buffer has enough space
        if (len < transactionSize) {
            LOG_ERROR("User buffer is too small to hold packetTrip header %zu < %zu", len, transactionSize);
            stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy header to user space
        LOG_DEBUG_PACKET("Sending packetTrip header to user space; Version: %d; Length: %zu bytes", COMM_VERSION, transactionSize);
        unsigned char headerVersion[sizeof(s32)];
        int_to_bytes(COMM_VERSION, headerVersion);

        unsigned char headerLength[sizeof(s32)];
        int_to_bytes(packetTrip->entry->skb->len, headerLength);

        unsigned char headerRoutingType[sizeof(s32)];
        int_to_bytes(packetTrip->routingType, headerRoutingType);

        unsigned char headerPayload[transactionSize];
        memcpy(headerPayload, headerRoutingType, sizeof(s32));
        memcpy(headerPayload + sizeof(s32), headerVersion, sizeof(s32));
        memcpy(headerPayload + (sizeof(s32)*2), headerLength, sizeof(s32));

        error_count = copy_to_user(buf, headerPayload, transactionSize);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip header to user space");
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->headerProcessed = true;
        LOG_DEBUG_PACKET("... header has been processed");


        int nextQueueLength = pq_len_packetTrip(&read2_packets_queue);
        LOG_DEBUG_PACKET("Pushing to read2_packets_queue. Current size: %d; Size after push: %d", nextQueueLength, nextQueueLength + 1);
        pq_push_packetTrip(&read2_packets_queue, packetTrip);
        
        return transactionSize;
    }
    else if(packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        s32 transactionSize = packetTrip->entry->skb->len;
        LOG_DEBUG_PACKET("Header has been processed but data has not been processed yet. Processing data (%d bytes)...", transactionSize);
        if (len < transactionSize) {
            LOG_ERROR("User buffer is too small to hold packetTrip data %zu < %d", len, transactionSize);
            stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        //error_count = copy_to_user(buf + sizeof(packetTrip->data), packetTrip->data, header.length);
        LOG_DEBUG_PACKET("Sending packetTrip data to user space; Version: %d; Length: %zu bytes", COMM_VERSION, transactionSize);
        error_count = copy_to_user(buf, packetTrip->entry->skb->data, transactionSize);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip data to user space");
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        packetTrip->packet->dataProcessed = true;
        LOG_DEBUG_PACKET("... data has been processed");

        
        int writeQueueLength = pq_len_packetTrip(&write_packets_queue);
        LOG_DEBUG_PACKET("Pushing to write_packets_queue. Current size: %d; Size after push: %d", writeQueueLength, writeQueueLength + 1);
        pq_push_packetTrip(&write_packets_queue, packetTrip);
        
        return transactionSize;
    }
    else if(packetTrip->packet->headerProcessed && packetTrip->packet->dataProcessed){
        LOG_WARN("Packet was already processed");
    }
    else{
        LOG_ERROR("Packet is in an invalid state");
        stopProcessingPacket(packetTrip);
        return -EFAULT;
    }

    LOG_DEBUG_PACKET("Packet request sent to user space");
    
    return 0;
}

static ssize_t dev_usercomm_write(struct file *filep, const char __user *buffer, size_t len, loff_t *offset) {

    // Try to fetch a packetTrip from pending_packets_queue
    if(processingPacketTrip == false){
        return 0;
    }

    int queueLength = pq_len_packetTrip(&write_packets_queue);
    LOG_DEBUG_PACKET("Popping from write_packets_queue. Current size: %d; Size after peek: %d", queueLength, queueLength);
    PendingPacketRoundTrip *packetTrip = pq_pop_packetTrip(&write_packets_queue);
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->packet, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry, -EFAULT, stopProcessingPacket(packetTrip));
    CHECK_NULL_FAIL_EXEC(LOG_TYPE_ERROR, packetTrip->entry->skb, -EFAULT, stopProcessingPacket(packetTrip));

    LOG_DEBUG_PACKET("Write active for %s Processing packet: %d", DEVICE_NAME_ACK, packetTrip);

    if(!packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed){
        LOG_DEBUG_PACKET("Neither response header nor data has been processed. Processing response header (%zu bytes)...", len);
        s32 responseVersion = 0;
        s32 responseLength = 0;
        int readBytes = 0;
        if (len < sizeof(s32) * 2){
            LOG_ERROR("Response packet header length is too small %zu < %zu", len, sizeof(s32));
            stopProcessingPacket(packetTrip);
            return -EINVAL;
        }

        if (copy_from_user(&responseVersion, buffer, sizeof(s32)) != 0){
            LOG_ERROR("Failed to copy response packet version from user space (%zu bytes)", len);
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        if (copy_from_user(&responseLength, buffer, sizeof(s32)) != 0){
            LOG_ERROR("Failed to copy response packet length from user space (%zu bytes)", len);
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }

        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        LOG_DEBUG_PACKET("Received response packet header from user space %zu bytes; Version: %d; Length: %d", sizeof(s32)*2, responseVersion, responseLength);
        packetTrip->responsePacket->headerProcessed = true;

        s32 decisionInt;
        if (copy_from_user(&decisionInt, buffer, responseLength) != 0){
            LOG_ERROR("Failed to copy response packet decision from user space (%zu bytes)", len);
            stopProcessingPacket(packetTrip);
            return -EFAULT;
        }
        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        LOG_DEBUG_PACKET("Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
        packetTrip->decision = (s64)decisionInt;
        packetTrip->responsePacket->dataProcessed = true;

        LOG_DEBUG_PACKET("PACKET FULLY PROCESSED - USER SPACE (%d bytes)", readBytes);
        stopProcessingPacket(packetTrip);
        return readBytes;
    }
    else if(packetTrip->responsePacket->headerProcessed && !packetTrip->responsePacket->dataProcessed){
        // LOG_DEBUG_PACKET("Response header has been processed data but has not been processed yet. Processing response data (%zu bytes)...", len);
        // s32 decisionInt;
        // if (copy_from_user(&decisionInt, buffer, len) != 0){
        //     LOG_ERROR("Failed to copy response packet data from user space (%zu bytes)", len);
        //     complete(&userspace_item_processed_wait_queue);
        //     return -EFAULT;
        // }
        
        LOG_ERROR("Response packet was partially processed");
        stopProcessingPacket(packetTrip);
        return -EFAULT;
    }
    else if (packetTrip->responsePacket->headerProcessed && packetTrip->responsePacket->dataProcessed){
         LOG_WARN("Response packet was already processed");
         stopProcessingPacket(packetTrip);
         return 0;
    }
    else{
        LOG_ERROR("Response packet is in an invalid state");
        stopProcessingPacket(packetTrip);
        return -EFAULT;
    }
    // Process the decision
    // For example, if you have a decision to drop a packetTrip, remove it from the queue
    // Use the decision.packet_index or other identifiers to locate the packetTrip

    // Wake up the waiting task(s)
    //wake_up_interruptible(&userspace_item_ready);
    stopProcessingPacket(packetTrip);
    return 0;
}

static int dev_usercomm_to_open(struct inode *inodep, struct file *filep){
    return dev_usercomm_open(inodep, filep, DEVICE_NAME, &netmod_to_mutex, &device_open_to_count);
}

static int dev_usercomm_from_open(struct inode *inodep, struct file *filep){
    return dev_usercomm_open(inodep, filep, DEVICE_NAME_ACK, &netmod_from_mutex, &device_open_from_count);
}

// This will receive directives from userspac
static int dev_usercomm_to_release(struct inode *inodep, struct file *filep){
    return dev_usercomm_release(inodep, filep, DEVICE_NAME, &netmod_to_mutex, &device_open_to_count);
}

static int dev_usercomm_from_release(struct inode *inodep, struct file *filep){
    return dev_usercomm_release(inodep, filep, DEVICE_NAME_ACK, &netmod_from_mutex, &device_open_from_count);
}

static struct file_operations fops_netmod_to_user = {
   .open = dev_usercomm_to_open,
   .read = dev_usercomm_read,
   .release = dev_usercomm_to_release,
};

static struct file_operations fops_netmod_from_user = {
   .open = dev_usercomm_from_open,
   .write = dev_usercomm_write,
   .release = dev_usercomm_from_release,
};


