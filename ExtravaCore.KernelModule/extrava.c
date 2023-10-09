// Make sure you have linux-headers installed on your system
// Fedora Silverblue: sudo rpm-ostree install kernel-devel-$(uname -r) --apply-live
//      toolbox enter
//      sudo dnf install kernel-devel kernel-headers gcc make kernel-devel-$(uname -r) kmod mokutil pesign openssl
//      sudo ln -sf /usr/src/kernels/6.5.5-200.fc38.x86_64 /lib/modules/6.5.5-200.fc38.x86_64/build
//      openssl req -new -x509 -newkey rsa:2048 -keyout MOK.priv -outform DER -out MOK.der -nodes -days 36500 -subj "/CN=Machine Key/"
//      make
//      sudo /usr/src/kernels/$(uname -r)/scripts/sign-file sha256 ./MOK.priv ./MOK.der extrava.ko
//      OR
//      sudo /usr/src/linux-headers-$(uname -r)/scripts/sign-file sha256 ./MOK.priv ./MOK.der extrava.ko
//      OR
//      sudo kmodsign sha512 ./MOK.priv ./MOK.der extrava.ko
//      sudo mokutil --disable-validation
//      dnf download --source kernel-uek
//      rpm2cpio ./kernel-uek*.rpm | cpio -idmv
//      sudo mokutil --root-pw --import MOK.der
// Fedora Workstation: sudo dnf install kernel-devel-$(uname -r) make
// Red Hat Enterprise Linux & CentOS: sudo yum install kernel-devel-$(uname -r)
// Debian & Ubuntu: sudo apt install linux-headers-$(uname -r)
#include <linux/module.h>
#include <linux/kernel.h>

// includes for netfilter
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <net/netlink.h>
#include <net/net_namespace.h>
#include <linux/ip.h>
#include <linux/tcp.h>
#include <linux/udp.h>
#include <linux/in.h>

// includes for user space communication
#include <linux/module.h>
#include <linux/fs.h>
#include <linux/uaccess.h>
#include <linux/mutex.h>

#define DEVICE_NAME "extrava_to_user"
#define DEVICE_NAME_ACK "extrava_from_user"
#define CLASS_NAME  "extrava"
#define PACKET_PROCESSING_TIMEOUT (5 * HZ)  // 5 seconds
#define MAX_PENDING_PACKETS 100

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Extravaganza Software");
MODULE_DESCRIPTION("ExtravaCore Kernel Module");

enum RoutingType {
    PRE_ROUTING = 0,
    POST_ROUTING = 1
};

enum RoutingDecision {
    ACCEPT = 1,
    DROP = 0,
    MANIPULATE = 2
};

typedef struct {
    unsigned long pkt_id;
    size_t length;
} PacketHeader;

typedef struct {
    unsigned long packet_index;
    enum RoutingDecision directive; // Let's say 1 = ACCEPT, 2 = DROP, 3 = MANIPULATE
} PacketDirective;

typedef struct {
    struct sk_buff *skb;
    size_t data_len; 
    bool processed;
    enum RoutingDecision decision;
} PendingPacket;

static struct class* char_class = NULL;
static int majorNumber_to_user;
static int majorNumber_from_user;
static struct device* netmodDevice = NULL;
static struct device* netmodDeviceAck = NULL;
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
static wait_queue_head_t pending_packet_queue;
static PendingPacket pending_packets[MAX_PENDING_PACKETS];

// This will be a simulation of the network packet data
char packet_data[512];


static DEFINE_MUTEX(netmod_mutex);

void to_human_readable_ip(unsigned int ip, char *buffer) {
    sprintf(buffer, "%pI4", &ip);
}

PendingPacket *create_pending_packet(struct sk_buff *skb, enum RoutingDecision decision) {
    if (!skb) return NULL;

    PendingPacket *packet = kmalloc(sizeof(PendingPacket), GFP_KERNEL);
    if (!packet) return NULL;

    packet->data_len = skb->len; 
    packet->data = kmalloc(packet->data_len, GFP_KERNEL);
    if (!packet->data) {
        kfree(packet);
        return NULL;
    }

    // Copy data from skb to the packet
    skb_copy_bits(skb, 0, packet->data, packet->data_len);

    packet->processed = false;
    packet->decision = decision;

    return packet;
}

void free_pending_packet(PendingPacket *packet) {
    if (packet) {
        if (packet->data) {
            kfree(packet->data);
        }
        kfree(packet);
    }
}

static unsigned int nf_common_routing_handler(enum RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state){
    struct iphdr *iph = ip_hdr(skb);
    int i;
    int ret;
 
    if (iph->protocol == IPPROTO_ICMP) {
        // Find an empty slot to store the packet
        for (i = 0; i < MAX_PENDING_PACKETS; i++) {
            if (pending_packets[i].skb == NULL) {
                // Store the data from the packet in our buffer
                size_t data_len = skb->len < sizeof(packet_data) ? skb->len : sizeof(packet_data);
                memset(packet_data, 0, sizeof(packet_data));
                skb_copy_bits(skb, 0, packet_data, data_len);
                pending_packets[i].skb = skb;
                pending_packets[i].processed = false;
                break;
            }
        }

        // If no slot found, drop the packet
        if (i == MAX_PENDING_PACKETS) {
            // 🫗 dropped packed because the buffer is filled
            printk(KERN_INFO "🫗 Extrava dropped a packet because the packet buffer is full\n");
            return NF_DROP;
        }

        // Wait until the packet has been processed by userspace or until timeout
        ret = wait_event_interruptible_timeout(pending_packet_queue, pending_packets[i].processed, PACKET_PROCESSING_TIMEOUT);

        // Check the return value for timeout (ret == 0) or error (ret == -ERESTARTSYS)
        if (ret == 0) {
            // Handle the timeout case (e.g., drop the packet)
            pending_packets[i].skb = NULL;
            // 🚮⏰️ Drop the packet because it timed out
            printk(KERN_INFO "🚮⏰️ Extrava dropped a packet that timed out\n");
            return NF_DROP;
        } else if (ret == -ERESTARTSYS) {
            // Handle the error case (optional: you can treat it as a timeout)
            // 🚮⚠️ Drop the packet because of an error
            printk(KERN_INFO "🚮⚠️ Extrava dropped a packet because of an error\n");
            pending_packets[i].skb = NULL;
            return NF_DROP;
        }

        // Process the packet based on the decision
        if (pending_packets[i].decision == ACCEPT) {
            // Release the slot and accept the packet
            // 📨 decided to Accept the packet
            printk(KERN_INFO "📨 Extrava decided to accepted a packet\n");
            pending_packets[i].skb = NULL;
            return NF_ACCEPT;
        } else {
            // Release the slot and drop the packet
            // 🚮 decided to Drop the packet
            printk(KERN_INFO "📨 Extrava decided to drop a packet\n");
            pending_packets[i].skb = NULL;
            return NF_DROP;
        }
    }

    return NF_ACCEPT; // Default action for non-ICMP packets
}

static unsigned int nf_pre_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state)
{
    return nf_common_routing_handler(PRE_ROUTING, priv, skb, state);
}

static unsigned int nf_post_routing_handler(void *priv, struct sk_buff *skb, const struct nf_hook_state *state)
{
    return nf_common_routing_handler(POST_ROUTING, priv, skb, state);
}

// Open function for our device
static int dev_usercomm_open(struct inode *inodep, struct file *filep){
//    if(!mutex_trylock(&netmod_mutex)){
//       printk(KERN_ALERT "Netmod: Device used by another process");
//       return -EBUSY;
//    }
   return 0;
}

// This will send packets to userspace
static ssize_t dev_usercomm_read(struct file *filep, char __user *buf, size_t len, loff_t *offset) {
    int i;
    int error_count = 0;
    PacketHeader header;

    // Find an unprocessed packet
    for (i = 0; i < MAX_PENDING_PACKETS; i++) {
        if (pending_packets[i].skb != NULL && !pending_packets[i].processed) {
            header.pkt_id = i; // Using the index as an identifier
            header.length = pending_packets[i].data_len;

            // Ensure user buffer has enough space
            if (len < sizeof(PacketHeader) + header.length)
                return -EINVAL;

            // Copy header to user space
            error_count = copy_to_user(buf, &header, sizeof(PacketHeader));
            if (error_count)
                return -EFAULT;

            // Copy packet data to user space
            error_count = copy_to_user(buf + sizeof(PacketHeader), packet_data, header.length);
            if (error_count)
                return -EFAULT;

            // Mark as processed to avoid resending
            pending_packets[i].processed = true;

            return sizeof(PacketHeader) + header.length;
        }
    }

    return 0;  // No packets found
}

// This will receive directives from userspace
static ssize_t dev_usercomm_write(struct file *filep, const char __user *buffer, size_t len, loff_t *offset) {
    PacketDirective directive;
    
    if (len < sizeof(PacketDirective))
        return -EINVAL;

    if (copy_from_user(&directive, buffer, sizeof(PacketDirective)))
        return -EFAULT;

    // Assuming directive.packet_index contains the index of the packet in our "database"
    if (directive.packet_index >= 0 && directive.packet_index < MAX_PENDING_PACKETS) {
        pending_packets[directive.packet_index].decision = directive.directive;
        pending_packets[directive.packet_index].processed = true;
    }

    // Wake up the waiting task(s)
    wake_up_interruptible(&pending_packet_queue);
    
    return sizeof(PacketDirective);
}

static int dev_usercomm_release(struct inode *inodep, struct file *filep){
   //mutex_unlock(&netmod_mutex);
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


static void cleanup_user_space_comm(void) {
    mutex_destroy(&netmod_mutex); 
    device_destroy(char_class, MKDEV(majorNumber_to_user, 0));
    device_destroy(char_class, MKDEV(majorNumber_from_user, 0));
    class_unregister(char_class);
    class_destroy(char_class);
    unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
    unregister_chrdev(majorNumber_from_user, DEVICE_NAME_ACK);
}

static void cleanup_netfilter_hooks(void) {
    if(nf_pre_routing_ops != NULL) {
		nf_unregister_net_hook(&init_net, nf_pre_routing_ops);
		kfree(nf_pre_routing_ops);
	}

    if(nf_post_routing_ops != NULL) {
		nf_unregister_net_hook(&init_net, nf_post_routing_ops);
		kfree(nf_post_routing_ops);
	}
}

// In mini-firewall.c 

static int __init nf_minifirewall_init(void) {
    printk(KERN_INFO "Extrava module initializing ⌛️\n");

    //Pre-routing
	nf_pre_routing_ops = (struct nf_hook_ops*)kcalloc(1,  sizeof(struct nf_hook_ops), GFP_KERNEL);
	if (nf_pre_routing_ops != NULL) {
		nf_pre_routing_ops->hook = (nf_hookfn*)nf_pre_routing_handler;
		nf_pre_routing_ops->hooknum = NF_INET_PRE_ROUTING;
		nf_pre_routing_ops->pf = NFPROTO_IPV4;
		nf_pre_routing_ops->priority = NF_IP_PRI_FIRST; // set the priority
		
		nf_register_net_hook(&init_net, nf_pre_routing_ops);
	}

    //Post-routing
    nf_post_routing_ops = (struct nf_hook_ops*)kcalloc(1, sizeof(struct nf_hook_ops), GFP_KERNEL);
    if (nf_post_routing_ops != NULL) {
        nf_post_routing_ops->hook = (nf_hookfn*)nf_post_routing_handler;
        nf_post_routing_ops->hooknum = NF_INET_POST_ROUTING;
        nf_post_routing_ops->pf = NFPROTO_IPV4;
        nf_post_routing_ops->priority = NF_IP_PRI_FIRST;
        
        nf_register_net_hook(&init_net, nf_post_routing_ops);
    }

    // Register devices for user space communication
    majorNumber_to_user = register_chrdev(0, DEVICE_NAME, &fops_netmod_to_user);
    if (majorNumber_to_user<0){
        printk(KERN_ALERT "Netmod_to_user failed to register a major number\n");
        return majorNumber_to_user;
    }

    majorNumber_from_user = register_chrdev(0, DEVICE_NAME_ACK, &fops_netmod_from_user);
    if (majorNumber_from_user<0){
        printk(KERN_ALERT "Netmod_from_user failed to register a major number\n");
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        return majorNumber_from_user;
    }

    char_class = class_create(CLASS_NAME);
    if (IS_ERR(char_class)){
        unregister_chrdev(majorNumber_to_user, DEVICE_NAME);
        unregister_chrdev(majorNumber_from_user, DEVICE_NAME);
        printk(KERN_ALERT "Failed to register device class 'in'\n");
        return PTR_ERR(char_class);
    }

    netmodDevice = device_create(char_class, NULL, MKDEV(majorNumber_to_user, 0), NULL, DEVICE_NAME);
    if (IS_ERR(netmodDevice)){
        printk(KERN_ALERT "Failed to create the 'in' device\n");
    }

    netmodDeviceAck = device_create(char_class, NULL, MKDEV(majorNumber_from_user, 0), NULL, DEVICE_NAME_ACK);
    if (IS_ERR(netmodDeviceAck)){
        printk(KERN_ALERT "Failed to create the 'out' device\n");
    }

    mutex_init(&netmod_mutex);
    init_waitqueue_head(&pending_packet_queue);

    // Error handling: if either device fails to initialize, cleanup everything and exit
    if(IS_ERR(netmodDevice) || IS_ERR(netmodDeviceAck)) {
        cleanup_user_space_comm();
        cleanup_netfilter_hooks();
        return -1;  // Or any other appropriate error code
    }
    
    printk(KERN_INFO "Extrava module loaded ✔️\n");
	return 0;
}

static void __exit nf_minifirewall_exit(void) {
    printk(KERN_INFO "Extrava module exiting ⌛️\n");
    cleanup_user_space_comm();
    cleanup_netfilter_hooks();
	printk(KERN_INFO "Extrava module unloaded 🛑\n");
}

module_init(nf_minifirewall_init);
module_exit(nf_minifirewall_exit);