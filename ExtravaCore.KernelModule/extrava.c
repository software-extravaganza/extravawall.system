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

#define DEVICE_NAME_IN "mychardev_in"
#define DEVICE_NAME_OUT "mychardev_out"
#define CLASS_NAME  "mychar"
#define DEVICE_NAME "netmod_to_user"
#define DEVICE_NAME_ACK "netmod_from_user"
#define CLASS_NAME  "netmod"

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Extravaganza Software");
MODULE_DESCRIPTION("ExtravaCore Kernel Module");

static int major_num_in, major_num_out;
static struct class* char_class = NULL;
static int    majorNumber;
static struct class*  netmodClass  = NULL;
static struct device* netmodDevice = NULL;
static struct device* netmodDeviceAck = NULL;
static wait_queue_head_t my_queue;

// This will be a simulation of the network packet data
char packet_data[512];

enum RoutingType {
    PRE_ROUTING,
    POST_ROUTING
};

static DEFINE_MUTEX(netmod_mutex);

typedef struct {
    unsigned long pkt_id;
    size_t length;
} PacketHeader;

typedef struct {
    unsigned long pkt_id;
    int directive; // Let's say 1 = ACCEPT, 2 = DROP, 3 = MANIPULATE
} PacketDirective;

// Callbacks for mychardev_in
static ssize_t dev_in_read(struct file *file, char __user *buffer, size_t len, loff_t *offset);

// Callbacks for mychardev_out
static ssize_t dev_out_write(struct file *file, const char __user *buffer, size_t len, loff_t *offset);

static struct file_operations fops_in = {
    .read = dev_in_read,
};

static struct file_operations fops_out = {
    .write = dev_out_write,
};

void to_human_readable_ip(unsigned int ip, char *buffer) {
    sprintf(buffer, "%pI4", &ip);
}

static unsigned int nf_common_routing_handler(enum RoutingType type, void *priv, struct sk_buff *skb, const struct nf_hook_state *state)
{
    struct iphdr *iph;   // IP header
    struct udphdr *udph; // UDP header
    char routing_str[12]; // To store "Pre" or "Post"

    if(!skb) return NF_ACCEPT;

    // Identify the type of routing
    strcpy(routing_str, (type == PRE_ROUTING) ? "Pre" : "Post");

    iph = ip_hdr(skb); // retrieve the IP headers from the packet
    if(iph->protocol == IPPROTO_UDP) { 
        udph = udp_hdr(skb);
        if(ntohs(udph->dest) == 53) {
            return NF_ACCEPT; // accept UDP packet
        }
    }
    else if (iph->protocol == IPPROTO_TCP) {
        return NF_ACCEPT; // accept TCP packet
    }
    else if (iph->protocol == IPPROTO_ICMP) {
        char dip[16]; // Buffer to store human readable IP address
        to_human_readable_ip(iph->daddr, dip);
        char sip[16]; // Buffer to store human readable IP address
        to_human_readable_ip(iph->saddr, sip);
        printk(KERN_INFO "Drop ICMP packet from %s to %s (%s-routing)\n", sip, dip, routing_str);
        return NF_DROP;
    }
    return NF_ACCEPT;
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
   if(!mutex_trylock(&netmod_mutex)){
      printk(KERN_ALERT "Netmod: Device used by another process");
      return -EBUSY;
   }
   return 0;
}

// This will send packets to userspace
static ssize_t dev_usercomm_read(struct file *filep, char *buffer, size_t len, loff_t *offset){
   PacketHeader header = {12345, sizeof(packet_data)}; // Sample data
   copy_to_user(buffer, &header, sizeof(PacketHeader));
   copy_to_user(buffer + sizeof(PacketHeader), packet_data, sizeof(packet_data));
   return sizeof(PacketHeader) + sizeof(packet_data);
}

// This will receive directives from userspace
static ssize_t dev_usercomm_write(struct file *filep, const char *buffer, size_t len, loff_t *offset){
   PacketDirective directive;
   copy_from_user(&directive, buffer, sizeof(PacketDirective));

   if(directive.directive == 1) {
      // ACCEPT logic here
   } else if(directive.directive == 2) {
      // DROP logic here
   } else if(directive.directive == 3) {
      // MANIPULATE logic here
   }

   return sizeof(PacketDirective);
}

static int dev_usercomm_release(struct inode *inodep, struct file *filep){
   mutex_unlock(&netmod_mutex);
   return 0;
}

static struct file_operations fops = {
   .open = dev_usercomm_open,
   .read = dev_usercomm_read,
   .write = dev_usercomm_write,
   .release = dev_usercomm_release,
};

// In mini-firewall.c 
static struct nf_hook_ops *nf_pre_routing_ops = NULL;
static struct nf_hook_ops *nf_post_routing_ops = NULL;
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
    majorNumber = register_chrdev(0, DEVICE_NAME, &fops);
    if (majorNumber<0){
        printk(KERN_ALERT "Netmod failed to register a major number\n");
        return majorNumber;
    }

    netmodClass = class_create(CLASS_NAME);
    if (IS_ERR(netmodClass)){
        unregister_chrdev(majorNumber, DEVICE_NAME);
        printk(KERN_ALERT "Failed to register device class\n");
        return PTR_ERR(netmodClass);
    }

    netmodDevice = device_create(netmodClass, NULL, MKDEV(majorNumber, 0), NULL, DEVICE_NAME);
    netmodDeviceAck = device_create(netmodClass, NULL, MKDEV(majorNumber, 1), NULL, DEVICE_NAME_ACK);
    if (IS_ERR(netmodDevice) || IS_ERR(netmodDeviceAck)){
        class_destroy(netmodClass);
        unregister_chrdev(majorNumber, DEVICE_NAME);
        printk(KERN_ALERT "Failed to create the device\n");
        return PTR_ERR(netmodDevice);
    }

    mutex_init(&netmod_mutex);
    init_waitqueue_head(&my_queue);

    printk(KERN_INFO "Extrava module loaded ✔️\n");
	return 0;
}

static void __exit nf_minifirewall_exit(void) {
    printk(KERN_INFO "Extrava module exiting ⌛️\n");
    // clean up devices for user space communication
    mutex_destroy(&netmod_mutex); 
    device_destroy(netmodClass, MKDEV(majorNumber, 0));
    device_destroy(netmodClass, MKDEV(majorNumber, 1));
    class_unregister(netmodClass);
    class_destroy(netmodClass);
    unregister_chrdev(majorNumber, DEVICE_NAME);

    // clean up netfilter hooks
	if(nf_pre_routing_ops != NULL) {
		nf_unregister_net_hook(&init_net, nf_pre_routing_ops);
		kfree(nf_pre_routing_ops);
	}

    if(nf_post_routing_ops != NULL) {
		nf_unregister_net_hook(&init_net, nf_post_routing_ops);
		kfree(nf_post_routing_ops);
	}
	printk(KERN_INFO "Extrava module unloaded 🛑\n");
}

module_init(nf_minifirewall_init);
module_exit(nf_minifirewall_exit);