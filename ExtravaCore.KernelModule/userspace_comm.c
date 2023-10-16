
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
PendingPacketRoundTrip *processedPacketTrip = NULL;


DECLARE_WAIT_QUEUE_HEAD(user_read_queue);
bool is_data_ready_for_user = false;

// Open function for our device
static int dev_usercomm_open(struct inode *inodep, struct file *filep){
   
   return 0;
}

static int dev_usercomm_to_open(struct inode *inodep, struct file *filep){
    if(!mutex_trylock(&netmod_to_mutex)){
        LOG_ALERT("Netmod: Device %s used by another process", DEVICE_NAME);
        return -EBUSY;
    }

    atomic_inc(&device_open_to_count);
    LOG_DEBUG("Netmod: Device %s opened", DEVICE_NAME);
    return dev_usercomm_open(inodep, filep);
}

static int dev_usercomm_from_open(struct inode *inodep, struct file *filep){
    if(!mutex_trylock(&netmod_from_mutex)){
        LOG_ALERT("Netmod: Device %s used by another process", DEVICE_NAME_ACK);
        return -EBUSY;
    }

    atomic_inc(&device_open_from_count);
    LOG_DEBUG("Netmod: Device %s opened", DEVICE_NAME_ACK);
    return dev_usercomm_open(inodep, filep);
}

int packet_processor_thread(void *data) {
    LOG_DEBUG("Packet processor thread started");
    while (!kthread_should_stop()) {
        // Sleep until there's work to do
        LOG_DEBUG("Waiting for new packet trip");
        wait_for_completion_interruptible(&queue_item_added);
        reinit_completion(&queue_item_added);

        // Dequeue a packet for processing
        int queueLength = pq_len_packetTrip(&pending_packets_queue);
        LOG_DEBUG("Processing new packet trip. Current queue size: %d; Size after add: %d", queueLength, queueLength + 1);
        currentPacketTrip = pq_pop_packetTrip(&pending_packets_queue);
        if (currentPacketTrip == NULL || currentPacketTrip->entry == NULL) {
            LOG_ERROR("Invalid packetTrip structure");
            continue;
        }
        LOG_DEBUG("Packet trip info: %p; Packet to eval: %p (size: %d)", currentPacketTrip, currentPacketTrip->entry->skb, currentPacketTrip->entry->skb->len);
        // Communicate with user space and process the packet
        is_data_ready_for_user = true;
        wake_up_interruptible(&user_read_queue);  // Notify the read handler

        // Wait until user has processed the packet
        LOG_DEBUG("Waiting for user space to process packet");
        wait_for_completion_interruptible(&userspace_item_processed);
        
        if(processedPacketTrip == NULL){
            LOG_ERROR("Processed packet trip is null");
            continue;
        }
        
        LOG_DEBUG("User space has processed packet with decision %lld", processedPacketTrip->decision);
        nf_reinject(processedPacketTrip->entry, processedPacketTrip->decision == ACCEPT ? NF_ACCEPT : NF_DROP);
        // if (user_decision == DROP) {
        //     kfree_skb(processedPacketTrip->packet);
        // } else if (user_decision == ACCEPT) {
        //     struct nf_queue_entry entry;

        //     entry.skb = processedPacketTrip->packet;
        //     entry.state = processedPacketTrip->state;
        //     nf_reinject(&entry, NF_ACCEPT);
        // }

        // Ensure you also free any additional memory or structures related to processedPacketTrip
        free_pending_packetTrip(processedPacketTrip);
        processedPacketTrip = NULL;
        reinit_completion(&userspace_item_processed); 
        LOG_DEBUG("Packet trip processed. Done cleaning up and ready for new packet.");
    }

    // Signal that we're done processing and exiting
    LOG_DEBUG("Packet processor thread exiting");
    complete(&queue_processor_exited);
    return 0;
}

// This will send packets to userspace
//static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    // int error_count = 0;
    // PendingPacketRoundTrip *packetTrip;
    // LOG_DEBUG("Read active for %s", DEVICE_NAME);
    // // Try to fetch a packetTrip from pending_packets_queue
    // //todo: lock internally on queue?
    // packetTrip =  pq_peek_packetTrip(&pending_packets_queue);
    
    // while (!packetTrip || !packetTrip->packet) {
    //     LOG_DEBUG("No packet found");
    //     if (filep->f_flags & O_NONBLOCK){
    //         LOG_WARN("Non-blocking read, no packet found for %s", DEVICE_NAME);
    //         return -EAGAIN;
    //     }

    //     LOG_DEBUG("Blocking read for %s. Waiting for new data...", DEVICE_NAME);
    //     //int ret = wait_event_interruptible_timeout(pending_packet_queue, (packetTrip = pq_peek_packetTrip(&pending_packets_queue)), PACKET_PROCESSING_TIMEOUT);
    //     //int ret = wait_event_interruptible(pending_packet_queue, (packetTrip = pq_peek_packetTrip(&pending_packets_queue)));
    //     unsigned long start_time = jiffies;
    //     unsigned long end_time = start_time + PACKET_PROCESSING_TIMEOUT;

    //     // Avoid using functions that can sleep or block in sections of code that are within an RCU read-side critical section. 
    //     // While is not elegant but using busy-waiting (spin-waiting) to yield the CPU for a short period of time if the condition isn't met
    //     // This also give time for the queue processor thread to process the packet
    //     while (!data_needs_processing && isLoaded) {  // && !kthread_should_stop()
    //         // Yield the processor for a short duration
    //         schedule_timeout_uninterruptible(1);
    //     }
    //     data_needs_processing = false;

    //     if(!isLoaded){  // || kthread_should_stop()
    //         return -EINVAL;
    //     }

    //     packetTrip =  pq_peek_packetTrip(&pending_packets_queue);
    //     if(packetTrip && packetTrip->packet && packetTrip->packet->header){
    //         LOG_DEBUG("...new packet found available on %s.", DEVICE_NAME);
    //         break;
    //     }

    //     if(packetTrip && !packetTrip->packet){
    //         LOG_WARN("...packet found but packet is not available on %s.", DEVICE_NAME);
    //         return -ERESTARTSYS;
    //     }

    //     if(packetTrip && packetTrip->packet && !packetTrip->packet->header){
    //         LOG_WARN("...packet found but header is not available on %s.", DEVICE_NAME);
    //         return -ERESTARTSYS;
    //     }

        
    //     //LOG_DEBUG("...wait_event_interruptible returned %d", ret);
    //     // Check the return value for timeout (ret == 0) or error (ret == -ERESTARTSYS)
    //     // if (ret == 0) {
    //     //     LOG_WARN("user space communication moving to next packet due to timeout\n");
    //     //     return -ERESTARTSYS;
    //     // } else if (ret == -ERESTARTSYS) {
    //     //     LOG_ERROR("user space communication failed\n");
    //     //     return -ERESTARTSYS;
    //     // }

    //     LOG_DEBUG("...still waiting for packets on %s.", DEVICE_NAME);
    //     //return -ERESTARTSYS; // Signal received, stop waiting
    // }

static void stopProcessingPacket(void){
    LOG_DEBUG("STOPPING PROCESSING FOR THIS PACKET");
    if(processedPacketTrip != NULL){
        free_pending_packetTrip(processedPacketTrip);
        processedPacketTrip = NULL;
    }

    if (currentPacketTrip != NULL){
        processedPacketTrip = currentPacketTrip;
        currentPacketTrip = NULL;
    }
    
    is_data_ready_for_user = false;
    processingPacketTrip = false;
    complete(&userspace_item_processed); 
    
}

static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    int error_count = 0;
    //PendingPacketRoundTrip *packetTrip;
    LOG_DEBUG("Read active for %s", DEVICE_NAME);
    //buf[0] = 1;
    
    //return 0;
   // packetTrip =  pq_peek_packetTrip(&pending_packets_queue);

    while (!is_data_ready_for_user) {
        LOG_DEBUG("Blocking read for %s. Waiting for new data...", DEVICE_NAME);
        // Using wait_event_interruptible to sleep until a packet is available
        if (wait_event_interruptible(user_read_queue, is_data_ready_for_user)) {
            LOG_ERROR("Received signal, stopping wait");
            stopProcessingPacket();
            return -ERESTARTSYS;
        }

        LOG_DEBUG("Woke up from wait_event_interruptible for %s", DEVICE_NAME);
        if (filep->f_flags & O_NONBLOCK) {
            LOG_WARN("Non-blocking read, no packet found for %s", DEVICE_NAME);
            stopProcessingPacket();
            return -EAGAIN;
        }

        // Re-check packet after being woken up
        //packetTrip = pq_peek_packetTrip(&pending_packets_queue);
        processingPacketTrip = true;
    }

    if(!is_data_ready_for_user){
        LOG_ERROR("Packet is not being processed");
        stopProcessingPacket();
        return -EFAULT;
    }

    if (currentPacketTrip == NULL || currentPacketTrip->packet == NULL || currentPacketTrip->entry == NULL || currentPacketTrip->entry->skb == NULL) {
        LOG_ERROR("Invalid packetTrip structure");
        stopProcessingPacket();
        return -EFAULT;
    }
    else{
        LOG_DEBUG("Valid packetTrip structure");
    }
    
    LOG_DEBUG("Packet trip info: %p; Packet to eval: %p (size: %d)", currentPacketTrip, currentPacketTrip->packet, currentPacketTrip->entry->skb->len);
    if(!currentPacketTrip->packet->headerProcessed && !currentPacketTrip->packet->dataProcessed){
        s32 transactionSize = sizeof(s32)*3;
        LOG_DEBUG("Neither header nor data has been processed. Processing header (%d bytes)...", transactionSize);
        // Ensure user buffer has enough space
        if (len < transactionSize) {
            LOG_ERROR("User buffer is too small to hold packetTrip header %zu < %zu", len, transactionSize);
            stopProcessingPacket();
            return -EINVAL;
        }

        // Copy header to user space
        LOG_DEBUG("Sending packetTrip header to user space; Version: %d; Length: %zu bytes", COMM_VERSION, transactionSize);
        unsigned char headerVersion[sizeof(s32)];
        int_to_bytes(COMM_VERSION, headerVersion);

        unsigned char headerLength[sizeof(s32)];
        int_to_bytes(currentPacketTrip->entry->skb->len, headerLength);

        unsigned char headerRoutingType[sizeof(s32)];
        int_to_bytes(currentPacketTrip->routingType, headerRoutingType);

        unsigned char headerPayload[transactionSize];
        memcpy(headerPayload, headerRoutingType, sizeof(s32));
        memcpy(headerPayload + sizeof(s32), headerVersion, sizeof(s32));
        memcpy(headerPayload + (sizeof(s32)*2), headerLength, sizeof(s32));

        error_count = copy_to_user(buf, headerPayload, transactionSize);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip header to user space");
            stopProcessingPacket();
            return -EFAULT;
        }

        currentPacketTrip->packet->headerProcessed = true;
        LOG_DEBUG("... header has been processed");
        return transactionSize;
    }
    else if(currentPacketTrip->packet->headerProcessed && !currentPacketTrip->packet->dataProcessed){
        s32 transactionSize = currentPacketTrip->entry->skb->len;
        LOG_DEBUG("Header has been processed but data has not been processed yet. Processing data (%d bytes)...", transactionSize);
        if (len < transactionSize) {
            LOG_ERROR("User buffer is too small to hold packetTrip data %zu < %d", len, transactionSize);
            stopProcessingPacket();
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        //error_count = copy_to_user(buf + sizeof(packetTrip->data), packetTrip->data, header.length);
        LOG_DEBUG("Sending packetTrip data to user space; Version: %d; Length: %zu bytes", COMM_VERSION, transactionSize);
        error_count = copy_to_user(buf, currentPacketTrip->entry->skb->data, transactionSize);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip data to user space");
            stopProcessingPacket();
            return -EFAULT;
        }

        currentPacketTrip->packet->dataProcessed = true;
        LOG_DEBUG("... data has been processed");
        return transactionSize;
    }
    else if(currentPacketTrip->packet->headerProcessed && currentPacketTrip->packet->dataProcessed){
        LOG_WARN("Packet was already processed");
    }
    else{
        LOG_ERROR("Packet is in an invalid state");
        stopProcessingPacket();
        return -EFAULT;
    }

    LOG_DEBUG("Packet request sent to user space");
    
    return 0;
}

// This will receive directives from userspace
static ssize_t dev_usercomm_write(struct file *filep, const char __user *buffer, size_t len, loff_t *offset) {
    PendingPacketRoundTrip *packetTrip;
    
    // Try to fetch a packetTrip from pending_packets_queue
    //todo: lock internally on queue?
    //packetTrip =  pq_peek_packetTrip(&pending_packets_queue);
    if(processingPacketTrip == NULL){
        return 0;
    }

    LOG_DEBUG("Write active for %s Processing packet: %d", DEVICE_NAME_ACK, processingPacketTrip);

    if(!currentPacketTrip->responsePacket->headerProcessed && !currentPacketTrip->responsePacket->dataProcessed){
        LOG_DEBUG("Neither response header nor data has been processed. Processing response header (%zu bytes)...", len);
        s32 responseVersion = 0;
        s32 responseLength = 0;
        int readBytes = 0;
        if (len < sizeof(s32) * 2){
            LOG_ERROR("Response packet header length is too small %zu < %zu", len, sizeof(s32));
            stopProcessingPacket();
            return -EINVAL;
        }

        if (copy_from_user(&responseVersion, buffer, sizeof(s32)) != 0){
            LOG_ERROR("Failed to copy response packet version from user space (%zu bytes)", len);
            stopProcessingPacket();
            return -EFAULT;
        }

        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        if (copy_from_user(&responseLength, buffer, sizeof(s32)) != 0){
            LOG_ERROR("Failed to copy response packet length from user space (%zu bytes)", len);
            stopProcessingPacket();
            return -EFAULT;
        }

        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        LOG_DEBUG("Received response packet header from user space %zu bytes; Version: %d; Length: %d", sizeof(s32)*2, responseVersion, responseLength);
        currentPacketTrip->responsePacket->headerProcessed = true;

        s32 decisionInt;
        if (copy_from_user(&decisionInt, buffer, responseLength) != 0){
            LOG_ERROR("Failed to copy response packet decision from user space (%zu bytes)", len);
            stopProcessingPacket();
            return -EFAULT;
        }
        buffer += sizeof(s32);
        readBytes += sizeof(s32);

        LOG_DEBUG("Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
        currentPacketTrip->decision = (s64)decisionInt;
        currentPacketTrip->responsePacket->dataProcessed = true;

        LOG_DEBUG("PACKET FULLY PROCESSED - USER SPACE (%d bytes)", readBytes);
        stopProcessingPacket();

        return readBytes;
    }
    else if(currentPacketTrip->responsePacket->headerProcessed && !currentPacketTrip->responsePacket->dataProcessed){
        // LOG_DEBUG("Response header has been processed data but has not been processed yet. Processing response data (%zu bytes)...", len);
        // s32 decisionInt;
        // if (copy_from_user(&decisionInt, buffer, len) != 0){
        //     LOG_ERROR("Failed to copy response packet data from user space (%zu bytes)", len);
        //     complete(&userspace_item_processed);
        //     return -EFAULT;
        // }
        
        LOG_ERROR("Response packet was partially processed");
        stopProcessingPacket();
        return -EFAULT;
    }
    else if (currentPacketTrip->responsePacket->headerProcessed && currentPacketTrip->responsePacket->dataProcessed){
         LOG_WARN("Response packet was already processed");
         stopProcessingPacket();
         return 0;
    }
    else{
        LOG_ERROR("Response packet is in an invalid state");
        stopProcessingPacket();
        return -EFAULT;
    }
    // Process the decision
    // For example, if you have a decision to drop a packetTrip, remove it from the queue
    // Use the decision.packet_index or other identifiers to locate the packetTrip

    // Wake up the waiting task(s)
    //wake_up_interruptible(&userspace_item_ready);
    stopProcessingPacket();
    return 0;
}

static int dev_usercomm_release(struct inode *inodep, struct file *filep){
   
   return 0;
}

static int dev_usercomm_to_release(struct inode *inodep, struct file *filep){
    atomic_dec(&device_open_to_count);
    mutex_unlock(&netmod_to_mutex);
    return dev_usercomm_release(inodep, filep);
}

static int dev_usercomm_from_release(struct inode *inodep, struct file *filep){
    atomic_dec(&device_open_from_count);
    mutex_unlock(&netmod_from_mutex);
    return dev_usercomm_release(inodep, filep);
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

bool fired = false;
void packet_processor(void) {
    LOG_DEBUG("Processing new packet trip");
    
    data_needs_processing = true;
    complete(&queue_item_added);
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

    // Error handling: if either device fails to initialize, cleanup everything and exit
    if(IS_ERR(netmodDevice) || IS_ERR(netmodDeviceAck)) {
        cleanup_user_space_comm();
        cleanup_netfilter_hooks();
        return IS_ERR(netmodDevice) ? PTR_ERR(netmodDevice) : PTR_ERR(netmodDeviceAck); 
    }

    register_packet_processing_callback(packet_processor);
    init_completion(&userspace_item_ready);
    init_completion(&userspace_item_processed);
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