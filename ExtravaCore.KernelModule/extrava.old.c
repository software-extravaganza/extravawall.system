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
#include <linux/init.h>
#include <linux/module.h>
#include <linux/kernel.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <net/sock.h>
#include <linux/netlink.h>

MODULE_LICENSE("MIT");
#define NETLINK_USER 31
#define REGISTER 1
#define DEREGISTER 2

static struct sock *nl_sk = NULL;
static struct nf_hook_ops nfho;
static int pid = 0;



static void *shared_mem_area;

static int my_mmap(struct file *filp, struct vm_area_struct *vma)
{
    return remap_vmalloc_range(vma, shared_mem_area, vma->vm_pgoff);
}

static int my_open(struct inode *inode, struct file *filp)
{
    // You might allocate or initialize shared memory here
    return 0;
}

static int my_close(struct inode *inode, struct file *filp)
{
    // Cleanup if necessary
    return 0;
}

static const struct file_operations my_fops = {
    .open = my_open,
    .release = my_close,
    .mmap = my_mmap,
};



static void send_msg_to_userspace(char *message) {
    struct sk_buff *skb_out;
    struct nlmsghdr *nlh;
    int msg_size = strlen(message);

    if (pid == 0) {
        return; // No registered user-space application
    }

    skb_out = nlmsg_new(msg_size, GFP_ATOMIC);
    if (!skb_out) {
        printk(KERN_ERR "Failed to allocate new skb\n");
        return;
    }

    nlh = nlmsg_put(skb_out, 0, 0, NLMSG_DONE, msg_size, 0);
    strncpy(nlmsg_data(nlh), message, msg_size);

    if (nlmsg_unicast(nl_sk, skb_out, pid) < 0) {
        printk(KERN_INFO "Error sending to user-space application\n");
    }
}

static unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    send_msg_to_userspace("Packet received");
    return NF_ACCEPT;
}

static void kernel_recv_msg(struct sk_buff *skb) {
    struct nlmsghdr *nlh;
    int32_t *data;

    nlh = (struct nlmsghdr *)skb->data;
    data = (int32_t *)nlmsg_data(nlh);

    switch (*data) {
        case REGISTER:
            pid = nlh->nlmsg_pid;
            printk(KERN_INFO "Registered user-space with PID: %d\n", pid);
            break;
        case DEREGISTER:
            pid = 0;
            printk(KERN_INFO "Deregistered user-space\n");
            break;
    }
}

static int __init extrava_init(void) {
    struct netlink_kernel_cfg cfg = {
        .input = kernel_recv_msg,
    };

    printk(KERN_INFO "Extrava module initializing\n");

    // Register character device
    // Allocate shared memory using vmalloc or similar
    shared_mem_area = vmalloc(MEM_SIZE);

    nl_sk = netlink_kernel_create(&init_net, NETLINK_USER, &cfg);
    if (!nl_sk) {
        printk(KERN_ALERT "Error creating netlink socket.\n");
        return -10;
    }

    nfho.hook = hook_func;
    nfho.hooknum = NF_INET_PRE_ROUTING;
    nfho.pf = PF_INET;
    nfho.priority = NF_IP_PRI_FIRST;
    nf_register_net_hook(&init_net, &nfho);

    printk(KERN_INFO "Extrava module loaded\n");
    return 0;
}

static void __exit extrava_exit(void) {
    printk(KERN_INFO "Extrava module exiting\n");
    // Cleanup: Unregister char device, free shared memory
    vfree(shared_mem_area);
    
    nf_unregister_net_hook(&init_net, &nfho);
    netlink_kernel_release(nl_sk);
    printk(KERN_INFO "Extrava module unloaded\n");
}

module_init(extrava_init);
module_exit(extrava_exit);