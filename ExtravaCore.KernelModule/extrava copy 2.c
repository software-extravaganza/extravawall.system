#include <linux/module.h>
#include <linux/kernel.h>
#include <linux/init.h>

/* Import necessary functions and data structures */
extern int init_data_structures(void);
extern void cleanup_data_structures(void);

extern int init_netfilter_hooks(void);
extern void cleanup_netfilter_hooks(void);

extern int init_comm(void);
extern void cleanup_comm(void);

static int __init my_module_init(void) {
    printk(KERN_INFO "Loading My Kernel Module...\n");
    
    if (init_data_structures() < 0) {
        printk(KERN_ALERT "Failed to initialize data structures\n");
        return -1;
    }
    
    if (init_netfilter_hooks() < 0) {
        printk(KERN_ALERT "Failed to register netfilter hooks\n");
        cleanup_data_structures();
        return -1;
    }
    
    if (init_comm() < 0) {
        printk(KERN_ALERT "Failed to initialize user-kernel communication\n");
        cleanup_netfilter_hooks();
        cleanup_data_structures();
        return -1;
    }
    
    printk(KERN_INFO "Module loaded successfully!\n");
    return 0;
}

static void __exit my_module_exit(void) {
    printk(KERN_INFO "Unloading My Kernel Module...\n");
    
    cleanup_comm();
    cleanup_netfilter_hooks();
    cleanup_data_structures();

    printk(KERN_INFO "Module unloaded successfully!\n");
}

module_init(my_module_init);
module_exit(my_module_exit);

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Your Name");
MODULE_DESCRIPTION("A Kernel Module combining Data Structures, Netfilter Hooks and User-Kernel Communication");
