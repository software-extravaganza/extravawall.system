#include <linux/module.h>
#include <linux/proc_fs.h>
#include <linux/seq_file.h>
#include <linux/uaccess.h>
#include "logger.h"
#include "data_structures.h"
#include "packet_queue.h"
#include "netfilter_hooks.h"
#include "userspace_comm.h"

#define PROC_FILENAME "my_comm"
#define BUF_SIZE 100
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
    while (!kthread_should_stop()) {
        // Sleep until there's work to do
        wait_for_completion_interruptible(&queue_item_added);
        reinit_completion(&queue_item_added);

        // Dequeue a packet for processing
        currentPacketTrip = dequeue_packet_for_processing();
        if (currentPacketTrip) {
            // Communicate with user space and process the packet
                struct nf_queue_entry entry;

                entry.skb = currentPacketTrip->packet->skb;
                entry.state = currentPacketTrip->packet->state;
                nf_reinject(&entry, NF_ACCEPT);
            // if (user_decision == DROP) {
            //     kfree_skb(currentPacketTrip->packet);
            // } else if (user_decision == ACCEPT) {
            //     struct nf_queue_entry entry;

            //     entry.skb = currentPacketTrip->packet;
            //     entry.state = currentPacketTrip->state;
            //     nf_reinject(&entry, NF_ACCEPT);
            // }

            // Ensure you also free any additional memory or structures related to currentPacketTrip
            free_pending_packetTrip(currentPacketTrip);
        }
    }

    // Signal that we're done processing and exiting
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
    complete(&userspace_item_processed);
    processingPacketTrip = false;
}

static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    int error_count = 0;
    //PendingPacketRoundTrip *packetTrip;
    LOG_DEBUG("Read active for %s", DEVICE_NAME);
    //buf[0] = 1;
    
    //return 0;
   // packetTrip =  pq_peek_packetTrip(&pending_packets_queue);

    while (!currentPacketTrip || !currentPacketTrip->packet || (currentPacketTrip->responsePacket && currentPacketTrip->responsePacket->headerProcessed && currentPacketTrip->responsePacket->dataProcessed)) {
        LOG_DEBUG("Blocking read for %s. Waiting for new data...", DEVICE_NAME);
        // Using wait_event_interruptible to sleep until a packet is available
        if (wait_for_completion_interruptible(&userspace_item_ready)) {
            LOG_ERROR("Received signal, stopping wait");
            stopProcessingPacket();
            return -ERESTARTSYS;
        }
        reinit_completion(&userspace_item_ready);

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

    if(!processingPacketTrip){
        LOG_ERROR("Packet is not being processed");
        stopProcessingPacket();
        return -EFAULT;
    }

    if (!currentPacketTrip || !currentPacketTrip->packet || !currentPacketTrip->packet->header) {
        LOG_ERROR("Invalid packetTrip structure");
        stopProcessingPacket();
        return -EFAULT;
    }
    else{
        LOG_DEBUG("Valid packetTrip structure");
    }
    
    LOG_DEBUG("Packet trip info: %p; Packet to eval: %p (size: %d)", currentPacketTrip, currentPacketTrip->packet, currentPacketTrip->packet->header->data_length);
    if(!currentPacketTrip->packet->headerProcessed && !currentPacketTrip->packet->dataProcessed){
        LOG_DEBUG("Neither header nor data has been processed. Processing header...");
        // Ensure user buffer has enough space
        if (len < sizeof(PacketHeader)) {
            LOG_ERROR("User buffer is too small to hold packetTrip header %zu < %zu", len, sizeof(PacketHeader));
            stopProcessingPacket();
            return -EINVAL;
        }

        // Copy header to user space
        LOG_DEBUG("Sending packetTrip header to user space %zu bytes; Version: %d; Length: %d", sizeof(PacketHeader), currentPacketTrip->packet->header->version, currentPacketTrip->packet->header->data_length);
        unsigned char headerVersion[sizeof(int)];
        int_to_bytes(currentPacketTrip->packet->header->version, headerVersion);

        unsigned char headerLength[sizeof(int)];
        int_to_bytes(currentPacketTrip->packet->header->data_length, headerLength);

        unsigned char headerPayload[sizeof(int)*2];
        memcpy(headerPayload, headerVersion, sizeof(int));
        memcpy(headerPayload + sizeof(int), headerLength, sizeof(int));

        error_count = copy_to_user(buf, headerPayload, sizeof(int)*2);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip header to user space");
            stopProcessingPacket();
            return -EFAULT;
        }

        currentPacketTrip->packet->headerProcessed = true;
        LOG_DEBUG("... header has been processed");
        return 0;
    }
    else if(currentPacketTrip->packet->headerProcessed && !currentPacketTrip->packet->dataProcessed){
        LOG_DEBUG("Header has been processed but data has not been processed yet. Processing data...");
        if (len < currentPacketTrip->packet->header->data_length) {
            LOG_ERROR("User buffer is too small to hold packetTrip data %zu < %d", len, currentPacketTrip->packet->header->data_length);
            stopProcessingPacket();
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        //error_count = copy_to_user(buf + sizeof(packetTrip->data), packetTrip->data, header.length);
        error_count = copy_to_user(buf, currentPacketTrip->packet->data, currentPacketTrip->packet->header->data_length);
        if (error_count){
            LOG_ERROR("Failed to copy packetTrip data to user space");
            stopProcessingPacket();
            return -EFAULT;
        }

        currentPacketTrip->packet->dataProcessed = true;
        LOG_DEBUG("... data has been processed");
        return 0;
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
    if(!processingPacketTrip){
        return 0;
    }

    if(!currentPacketTrip->responsePacket->headerProcessed && !currentPacketTrip->responsePacket->dataProcessed){
        LOG_DEBUG("Neither response header nor data has been processed. Processing response header (%zu bytes)...", len);
        PacketHeader responseHeader;

        if (len < sizeof(PacketHeader)){
            LOG_ERROR("Response packet header length is too small %zu < %zu", len, sizeof(PacketHeader));
            stopProcessingPacket();
            return -EINVAL;
        }

        if (copy_from_user(&responseHeader, buffer, sizeof(PacketHeader)) != 0){
            LOG_ERROR("Failed to copy response packet header from user space (%zu bytes)", len);
            stopProcessingPacket();
            return -EFAULT;
        }

        buffer += sizeof(PacketHeader);
        LOG_DEBUG("Received response packet header from user space %zu bytes; Version: %d; Length: %d", sizeof(PacketHeader), responseHeader.version, responseHeader.data_length);
        currentPacketTrip->responsePacket->header = &responseHeader;
        currentPacketTrip->responsePacket->headerProcessed = true;

        s32 decisionInt;
        if (copy_from_user(&decisionInt, buffer, sizeof(s32)) != 0){
            LOG_ERROR("Failed to copy response packet decision from user space (%zu bytes)", len);
            stopProcessingPacket();
            return -EFAULT;
        }
        buffer += sizeof(s32);

        LOG_DEBUG("Received response packet data from user space %zu bytes; Decision: %d", sizeof(s32), decisionInt);
        currentPacketTrip->decision = (s64)decisionInt;
        currentPacketTrip->responsePacket->dataProcessed = true;

        LOG_DEBUG("PACKET FULLY PROCESSED - USER SPACE");
        stopProcessingPacket();

        return 0;
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
    complete(&userspace_item_ready);
}

void setup_user_space_comm(void) {
    processingPacketTrip= false;
    // Register devices for user space communication
    majorNumber_to_user = register_chrdev(0, DEVICE_NAME, &fops_netmod_to_user);
    if (majorNumber_to_user<0){
        LOG_ERROR("Netmod_to_user failed to register a major number");
        return;
    }

    majorNumber_from_user = register_chrdev(0, DEVICE_NAME_ACK, &fops_netmod_from_user);
    if (majorNumber_from_user<0){
        LOG_ERROR("Netmod_from_user failed to register a major number");
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        return;
    }

    char_class = class_create(CLASS_NAME);
    if (IS_ERR(char_class)){
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        unregister_chrdev(majorNumber_from_user, DEVICE_NAME);
        LOG_ERROR("Failed to register device class 'in'");
        return;
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
        return;  
    }

    register_packet_processing_callback(packet_processor);
    init_completion(&userspace_item_ready);
        
    isLoaded = true;
}

void cleanup_user_space_comm(void) {
    isLoaded = false;
    processingPacketTrip= false;
     // Ensure no one is using the device or waits until they're done
    // If you've used try_module_get/module_put in your open and release fops, this can help here.
    while (atomic_read(&device_open_to_count) > 0 || atomic_read(&device_open_from_count) > 0) {
        msleep(50); // This will sleep for 50ms before checking again.
    }

    // Destroy any synchronization primitives associated with the device
    mutex_destroy(&netmod_to_mutex);
    mutex_destroy(&netmod_from_mutex);

    // Remove the device files under /dev
    device_destroy(char_class, MKDEV(majorNumber_to_user, 0));
    device_destroy(char_class, MKDEV(majorNumber_from_user, 0));

    // Cleanup the device class
    class_unregister(char_class);
    class_destroy(char_class);

    // Unregister the character devices
    unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
    unregister_chrdev(majorNumber_from_user, DEVICE_NAME_ACK);
}