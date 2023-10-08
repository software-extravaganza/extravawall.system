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
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <net/netlink.h>
#include <net/net_namespace.h>
#include <linux/ip.h>
#include <linux/tcp.h>

MODULE_LICENSE("MIT/GPL");

#define NETLINK_USER 31
#define MY_GROUP 1

static struct nf_hook_ops nfho;
static struct sock *nl_sk = NULL;
static unsigned long last_error_timestamp = 0;
static int consecutive_failures = 0;
static bool allow_netlink_comm = true; // flag to control Netlink communication
static unsigned long last_heartbeat = 0; // timestamp of the last received heartbeat
static unsigned long last_netlink_attempt = 0; // timestamp of the last Netlink communication attempt
const size_t static_length = sizeof("Payload: ") - 1;

static void cleanup_for_unload(void)
{
    nf_unregister_net_hook(&init_net, &nfho);// unregister the network hook
    netlink_kernel_release(nl_sk);// release the netlink socket
}

static void __exit extrava_exit(void) {
    printk(KERN_INFO "Extrava module exiting ⌛️\n");
    cleanup_for_unload();
    printk(KERN_INFO "Extrava module unloaded 🛑\n");
}

static void do_unload_module(void)
{
    printk(KERN_INFO "Unloading module due to trigger ⌛️\n");
    cleanup_for_unload();
    module_put(THIS_MODULE);
}

unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct nlmsghdr *nlh;
    struct sk_buff *skb_out;
    struct iphdr *ip_header; 
    struct tcphdr *tcp_header; 
    char *user_data; 
    char buffer[1500]; 
    int msg_size, data_len, nlh_len;

    printk(KERN_DEBUG "Entered hook_func\n");

    if (!skb) {
        printk(KERN_DEBUG "No skb, exiting.\n");
        return NF_ACCEPT;
    }

    ip_header = (struct iphdr *)skb_network_header(skb);
    if(ip_header->protocol != IPPROTO_TCP) {
        printk(KERN_DEBUG "Not a TCP packet, exiting.\n");
        return NF_ACCEPT;  // Only interested in TCP packets
    }

    tcp_header = (struct tcphdr *)((__u32 *)ip_header + ip_header->ihl);
    user_data = (char *)((unsigned char *)tcp_header + (tcp_header->doff * 4));
    data_len = ntohs(ip_header->tot_len) - (ip_header->ihl * 4) - (tcp_header->doff * 4);
    
    if (data_len + static_length >= sizeof(buffer)) {
        printk(KERN_WARNING "Data length exceeds buffer capacity. Truncating...\n");
        data_len = sizeof(buffer) - static_length - 1;
    }

    int written = snprintf(buffer, sizeof(buffer), "Payload: %.*s", data_len, user_data);
    if (written >= sizeof(buffer) || written < 0) {
        printk(KERN_WARNING "snprintf truncated output or encountered an error.\n");
        return NF_ACCEPT;
    }

    msg_size = strlen(buffer);
    skb_out = nlmsg_new(NLMSG_ALIGN(msg_size + nlmsg_total_size(0)), 0);
    if (!skb_out) {
        printk(KERN_ERR "Failed to allocate new skb\n");
        return NF_ACCEPT;
    }

    nlh = nlmsg_put(skb_out, 0, 0, NLMSG_DONE, msg_size, 0);  
    nlh_len = nlmsg_len(nlh);
    if (msg_size > nlh_len) {
        printk(KERN_WARNING "Source string too long for destination buffer. %d > %d\n", msg_size, nlh_len);
        kfree_skb(skb_out);  // Free the allocated skb before returning
        return NF_ACCEPT;
    }

    strncpy(nlmsg_data(nlh), buffer, msg_size);
    ((char *)nlmsg_data(nlh))[msg_size] = '\0';

    int ret = nlmsg_unicast(nl_sk, skb_out, 0);
    if (ret < 0) {
        printk(KERN_INFO "Failed to send message. Error: %d\n", ret);
        return NF_ACCEPT; 
    }

    printk(KERN_DEBUG "Packet processed successfully\n");

    return NF_ACCEPT; 
}

void nl_recv_msg(struct sk_buff *skb) {
    struct nlmsghdr *nlh;
    struct sk_buff *skb_out;
    char *msg = "Packet received from user!";
    int msg_size = strlen(msg);
    int res;

    nlh = (struct nlmsghdr *)skb->data;
    printk(KERN_INFO "Message received: %s\n", (char *)nlmsg_data(nlh));

    skb_out = nlmsg_new(msg_size, 0);
    if (!skb_out) {
        printk(KERN_ERR "Failed to allocate new skb\n");
        return;
    } 

    nlh = nlmsg_put(skb_out, 0, 0, NLMSG_DONE, msg_size, 0);  
    strncpy(nlmsg_data(nlh), msg, msg_size);

    res = nlmsg_unicast(nl_sk, skb_out, nlh->nlmsg_pid);
    if (res < 0) {
        printk(KERN_INFO "Error while sending back to user\n");
    }

    // When a message is received from user-space, update the heartbeat
    last_heartbeat = jiffies;
    allow_netlink_comm = true; // reset the flag when we receive a heartbeat
}

static int __init extrava_init(void) {
    printk(KERN_INFO "Extrava module initializing ⌛️\n");
    // Setting up netfilter hook
    nfho.hook = hook_func;
    nfho.pf = PF_INET; 
    nfho.hooknum = NF_INET_PRE_ROUTING; 
    nfho.priority = NF_IP_PRI_FIRST;
    nf_register_net_hook(&init_net, &nfho);

    // Setting up netlink socket
    struct netlink_kernel_cfg cfg = {
        .input = nl_recv_msg,
    };

    nl_sk = netlink_kernel_create(&init_net, NETLINK_USER, &cfg);
    if (!nl_sk) {
        printk(KERN_ALERT "Error creating netlink socket.\n");
        return -10;
    }

    printk(KERN_INFO "Extrava module loaded ✔️\n");
    return 0;
}




module_init(extrava_init);
module_exit(extrava_exit);






// unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
//     struct nlmsghdr *nlh;
//     struct sk_buff *skb_out;
//     struct iphdr *ip_header; 
//     struct tcphdr *tcp_header; 
//     char *user_data; 
//     char buffer[1500]; 
//     int msg_size, data_len, nlh_len;

//     ip_header = (struct iphdr *)skb_network_header(skb);
//     tcp_header= (struct tcphdr *)((__u32 *)ip_header + ip_header->ihl);

//     if (!skb) {
//         return NF_ACCEPT;
//     }

//     unsigned long current_time = jiffies;
    
//     // Check if it's been more than 30 seconds since the last heartbeat
//     if (time_after(current_time, last_heartbeat + 30 * HZ)) {
//         allow_netlink_comm = false;
//     }

//     // Retry mechanism: Attempt Netlink communication every minute even if the flag is turned off
//     if (time_after(current_time, last_netlink_attempt + 60 * HZ)) {
//         allow_netlink_comm = true;
//         last_netlink_attempt = current_time;
//     }

//     if (allow_netlink_comm && ip_header->protocol == IPPROTO_TCP) {
//         user_data = (char *)((unsigned char *)tcp_header + (tcp_header->doff * 4));
//         data_len = ntohs(ip_header->tot_len) - (ip_header->ihl * 4) - (tcp_header->doff * 4);
        
//         if (data_len + static_length > sizeof(buffer)) {
//             printk(KERN_WARNING "Data length exceeds buffer capacity. Truncating...\n");
//             data_len = sizeof(buffer) - static_length - 1;
//             return NF_ACCEPT;
//         }

//         int written = snprintf(buffer, sizeof(buffer), "Payload: %.*s", data_len, user_data);
//         if (written >= sizeof(buffer) || written < 0) {
//             // Either the output was truncated or an error occurred.
//             printk(KERN_WARNING "snprintf truncated output or encountered an error.\n");
//             return NF_ACCEPT;
//         }

//         msg_size = strlen(buffer);

//         skb_out = nlmsg_new(NLMSG_ALIGN(msg_size + nlmsg_total_size(0)), 0);
//         if (!skb_out) {
//             printk(KERN_ERR "Failed to allocate new skb\n");
//             return NF_ACCEPT;
//         }

//         nlh = nlmsg_put(skb_out, 0, 0, NLMSG_DONE, msg_size, 0);  
//         nlh_len = nlmsg_len(nlh);
//         if (msg_size > nlh_len) {
//             printk(KERN_WARNING "Source string too long for destination buffer. %d >= %d\n", msg_size, nlh_len);
//             return NF_ACCEPT;
//         }

//         strncpy(nlmsg_data(nlh), buffer, msg_size);
//         ((char *)nlmsg_data(nlh))[msg_size] = '\0';

//         int ret = nlmsg_unicast(nl_sk, skb_out, 0);
//         if (ret < 0) {
//             printk(KERN_INFO "Failed to send message. Error: %d\n", ret);
//             allow_netlink_comm = false; // Immediately stop after a failed send
//             //do_unload_module();
//             return NF_ACCEPT; 
//         }
//     }

//     return NF_ACCEPT; 
// }

// unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
//     struct nlmsghdr *nlh;
//     struct sk_buff *skb_out;
//     struct iphdr *ip_header; 
//     struct tcphdr *tcp_header; 
//     char *user_data; 
//     char buffer[1500]; 
//     int msg_size, data_len;
//     unsigned long current_time = jiffies;
//     unsigned long time_diff = current_time - last_error_timestamp;
    
//     //printk(KERN_INFO "Packet intercepted at %s.\n", __FUNCTION__);

//     ip_header = (struct iphdr *)skb_network_header(skb);
//     tcp_header= (struct tcphdr *)((__u32 *)ip_header+ ip_header->ihl);

//     if (!skb)
//         return NF_ACCEPT;

//     if (ip_header->protocol == IPPROTO_TCP) { //&& (last_error_timestamp == 0 || time_diff > 10*HZ)
//         user_data = (char *)((unsigned char *)tcp_header + (tcp_header->doff * 4));
//         data_len = ntohs(ip_header->tot_len) - (ip_header->ihl * 4) - (tcp_header->doff * 4);
//         snprintf(buffer, sizeof(buffer), "Payload: %.*s", data_len, user_data);
//         msg_size = strlen(buffer);

//         skb_out = nlmsg_new(msg_size, 0);
//         if (!skb_out) {
//             printk(KERN_ERR "Failed to allocate new skb\n");
//             return NF_ACCEPT;
//         }

//         //nlh = nlmsg_put(skb_out, 0, 0, NLMSG_DONE, msg_size, 0);
//         //strncpy(nlmsg_data(nlh), buffer, msg_size);
//         // if (consecutive_failures < 5 || time_diff > 10 * HZ) {

//         //     int ret = nlmsg_unicast(nl_sk, skb_out, 0);
//         //     if (ret < 0) {
//         //         consecutive_failures++;
//         //         last_error_timestamp = current_time;
//         //         printk(KERN_INFO "Failed to send message. Error: %d\n", ret);
                
//         //         if (consecutive_failures >= 5) {
//         //             printk(KERN_INFO "Too many consecutive failures. Delaying sends.\n");
//         //         }
//         //     } else {
//         //         consecutive_failures = 0; // Reset the counter on successful send
//         //     }
//         // }
//     }

//     return NF_ACCEPT; 
// }

// unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
//     struct iphdr *ip_header; 
//     // Comment out or remove netlink related parts

//     if (!skb)
//         return NF_ACCEPT;

//     // You can still keep the TCP check to see if it's the source of the issue
//     if (ip_header->protocol == IPPROTO_TCP) {
//         // ... But don't do anything with the packet
//     }

//     return NF_ACCEPT; 
// }