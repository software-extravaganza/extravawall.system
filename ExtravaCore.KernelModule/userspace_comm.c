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

static DEFINE_MUTEX(netmod_mutex);
static struct class* char_class = NULL;
static int majorNumber_to_user;
static int majorNumber_from_user;
static struct device* netmodDevice = NULL;
static struct device* netmodDeviceAck = NULL;
static wait_queue_head_t pending_packet_queue;

// Open function for our device
static int dev_usercomm_open(struct inode *inodep, struct file *filep){
   if(!mutex_trylock(&netmod_mutex)){
      printk(KERN_ALERT "Netmod: Device used by another process");
      return -EBUSY;
   }
   return 0;
}

// This will send packets to userspace
static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    int error_count = 0;
    PendingPacketRoundTrip *packetTrip;
LOG_INFO("DEBUG 12");
    // Try to fetch a packetTrip from pending_packets_queue
    //todo: lock internally on queue?
    packetTrip =  pq_peek_packetTrip(&pending_packets_queue);

    while (!packetTrip || !packetTrip->packet) {
        if (filep->f_flags & O_NONBLOCK)
            return -EAGAIN;

        if (wait_event_interruptible(pending_packet_queue, (packetTrip = pq_peek_packetTrip(&pending_packets_queue))))
            return -ERESTARTSYS; // Signal received, stop waiting
    }

LOG_INFO("DEBUG 13");
    if(!packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        LOG_INFO("DEBUG 14");
        // Ensure user buffer has enough space
        if (len < sizeof(PacketHeader)) {
            LOG_ERR("User buffer is too small to hold packetTrip header %zu < %zu", len, sizeof(PacketHeader));
            return -EINVAL;
        }

        // Copy header to user space
        LOG_INFO("Sending packetTrip header to user space %zu bytes; Version: %d; Length: %d", sizeof(PacketHeader), packetTrip->packet->header->version, packetTrip->packet->header->data_length);
        unsigned char headerVersion[sizeof(int)];
        int_to_bytes(packetTrip->packet->header->version, headerVersion);

        unsigned char headerLength[sizeof(int)];
        int_to_bytes(packetTrip->packet->header->data_length, headerLength);

        unsigned char headerPayload[sizeof(int)*2];
        memcpy(headerPayload, headerVersion, sizeof(int));
        memcpy(headerPayload + sizeof(int), headerLength, sizeof(int));

        error_count = copy_to_user(buf, headerPayload, sizeof(int)*2);
        if (error_count){
            LOG_ERR("Failed to copy packetTrip header to user space");
            return -EFAULT;
        }

        packetTrip->packet->headerProcessed = true;
        return sizeof(PacketHeader);
    }
    else if(packetTrip->packet->headerProcessed && !packetTrip->packet->dataProcessed){
        LOG_INFO("DEBUG 15");
        if (len < packetTrip->packet->header->data_length) {
            LOG_ERR("User buffer is too small to hold packetTrip data %zu < %d", len, packetTrip->packet->header->data_length);
            return -EINVAL;
        }

        // Copy packetTrip data to user space
        //error_count = copy_to_user(buf + sizeof(packetTrip->data), packetTrip->data, header.length);
        error_count = copy_to_user(buf, packetTrip->packet->data, packetTrip->packet->header->data_length);
        if (error_count){
            LOG_ERR("Failed to copy packetTrip data to user space");
            return -EFAULT;
        }

        packetTrip->packet->dataProcessed = true;
        return packetTrip->packet->header->data_length;
    }
    else if(packetTrip->packet->headerProcessed && packetTrip->packet->dataProcessed){
        LOG_WARN("Packet was already processed");
    }
    else{
        LOG_ERR("Packet is in an invalid state");
        return -EFAULT;

    }

    return 0;
}

// This will receive directives from userspace
static ssize_t dev_usercomm_write(struct file *filep, const char __user *buffer, size_t len, loff_t *offset) {
    PendingPacketRoundTrip *packetTrip;
LOG_INFO("DEBUG 16");
    // Try to fetch a packetTrip from pending_packets_queue
    //todo: lock internally on queue?
    packetTrip =  pq_peek_packetTrip(&pending_packets_queue);
    
    
    RoutingDecision decision;

    if (len < sizeof(RoutingDecision))
        return -EINVAL;

    if (copy_from_user(&decision, buffer, sizeof(RoutingDecision)))
        return -EFAULT;

    // Process the decision
    // For example, if you have a decision to drop a packetTrip, remove it from the queue
    // Use the decision.packet_index or other identifiers to locate the packetTrip

    // Wake up the waiting task(s)
    wake_up_interruptible(&pending_packet_queue);

    return sizeof(RoutingDecision);
}

static int dev_usercomm_release(struct inode *inodep, struct file *filep){
   mutex_unlock(&netmod_mutex);
   return 0;
}

static struct file_operations fops_netmod_to_user = {
   .open = dev_usercomm_open,
   .read = dev_usercomm_read,
   .release = dev_usercomm_release,
};

static struct file_operations fops_netmod_from_user = {
   .open = dev_usercomm_open,
   .write = dev_usercomm_write,
   .release = dev_usercomm_release,
};

bool fired = false;
void packet_processor(PendingPacketRoundTrip *packetTrip) {
    if(!fired) {
        fired = true;
        LOG_INFO("Packet processor fired");
    }


    // Wake up any reading process
    wake_up_interruptible(&pending_packet_queue);
}

void setup_user_space_comm(void) {
    // Register devices for user space communication
    majorNumber_to_user = register_chrdev(0, DEVICE_NAME, &fops_netmod_to_user);
    if (majorNumber_to_user<0){
        LOG_ERR("Netmod_to_user failed to register a major number");
        return;
    }

    majorNumber_from_user = register_chrdev(0, DEVICE_NAME_ACK, &fops_netmod_from_user);
    if (majorNumber_from_user<0){
        LOG_ERR("Netmod_from_user failed to register a major number");
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        return;
    }

    char_class = class_create(CLASS_NAME);
    if (IS_ERR(char_class)){
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        unregister_chrdev(majorNumber_from_user, DEVICE_NAME);
        LOG_ERR("Failed to register device class 'in'");
        return;
    }

    netmodDevice = device_create(char_class, NULL, MKDEV(majorNumber_to_user, 0), NULL, DEVICE_NAME);
    if (IS_ERR(netmodDevice)){
        LOG_ERR("Failed to create the 'in' device");
    }

    netmodDeviceAck = device_create(char_class, NULL, MKDEV(majorNumber_from_user, 0), NULL, DEVICE_NAME_ACK);
    if (IS_ERR(netmodDeviceAck)){
        LOG_ERR("Failed to create the 'out' device");
    }

    mutex_init(&netmod_mutex);
    init_waitqueue_head(&pending_packet_queue);

    // Error handling: if either device fails to initialize, cleanup everything and exit
    if(IS_ERR(netmodDevice) || IS_ERR(netmodDeviceAck)) {
        cleanup_user_space_comm();
        cleanup_netfilter_hooks();
        return;  
    }

    register_packet_processing_callback(packet_processor);
}

void cleanup_user_space_comm(void) {
    mutex_destroy(&netmod_mutex);
    device_destroy(char_class, MKDEV(majorNumber_to_user, 0));
    device_destroy(char_class, MKDEV(majorNumber_from_user, 0));
    class_unregister(char_class);
    class_destroy(char_class);
    unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
    unregister_chrdev(majorNumber_from_user, DEVICE_NAME_ACK);
}