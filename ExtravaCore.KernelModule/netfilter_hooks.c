#include <linux/module.h>
#include <linux/kernel.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <linux/ip.h>
#include <linux/skbuff.h>
#include "data_structures.h"

/* Declaration of the netfilter hook */
static struct nf_hook_ops nfho;

/* Function to process packets */
unsigned int hook_func(void *priv, struct sk_buff *skb, const struct nf_hook_state *state) {
    struct iphdr *ip_header;  // IP header structure

    /* Extract IP header from the sk_buff */
    ip_header = (struct iphdr *)skb_network_header(skb);

    if (!ip_header) {
        printk(KERN_INFO "Could not retrieve IP header.\n");
        return NF_ACCEPT;
    }

    /* Add your logic to handle packets here */
    // For demonstration purposes, let's just print the source IP
    printk(KERN_INFO "Received packet from %pI4.\n", &ip_header->saddr);

    /* Return NF_ACCEPT to let the packet pass. Modify based on your logic. */
    return NF_ACCEPT;
}

/* Module initialization function */
static int __init init_nfhook(void) {
    nfho.hook = hook_func;
    nfho.hooknum = NF_INET_PRE_ROUTING;  // Hook at the first IP packet check
    nfho.pf = PF_INET;                   // IPv4
    nfho.priority = NF_IP_PRI_FIRST;     // Set highest priority

    nf_register_net_hook(&init_net, &nfho);

    printk(KERN_INFO "Netfilter hook registered.\n");

    return 0;
}

/* Module cleanup function */
static void __exit cleanup_nfhook(void) {
    nf_unregister_net_hook(&init_net, &nfho);
    printk(KERN_INFO "Netfilter hook unregistered.\n");
}

module_init(init_nfhook);
module_exit(cleanup_nfhook);

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Your Name");
MODULE_DESCRIPTION("Netfilter Hook Example");
