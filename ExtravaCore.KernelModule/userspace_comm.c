#include <linux/module.h>
#include <linux/proc_fs.h>
#include <linux/seq_file.h>
#include <linux/uaccess.h>

#define PROC_FILENAME "my_comm"
#define BUF_SIZE 100

static char proc_data[BUF_SIZE];

static ssize_t my_proc_write(struct file *file, const char __user *buffer, size_t count, loff_t *pos) {
    if(count > BUF_SIZE - 1)
        return -EFAULT;
    if(copy_from_user(proc_data, buffer, count))
        return -EFAULT;

    proc_data[count] = '\0';

    printk(KERN_INFO "Received from userspace: %s\n", proc_data);

    return count;
}

static int my_proc_show(struct seq_file *m, void *v) {
    seq_printf(m, "Last received: %s\n", proc_data);
    return 0;
}

static int my_proc_open(struct inode *inode, struct file *file) {
    return single_open(file, my_proc_show, NULL);
}

static const struct file_operations my_fops = {
    .owner = THIS_MODULE,
    .open = my_proc_open,
    .read = seq_read,
    .write = my_proc_write,
    .llseek = seq_lseek,
    .release = single_release,
};

static int __init init_comm(void) {
    proc_create(PROC_FILENAME, 0666, NULL, &my_fops);
    printk(KERN_INFO "Created /proc/%s entry\n", PROC_FILENAME);
    return 0;
}

static void __exit cleanup_comm(void) {
    remove_proc_entry(PROC_FILENAME, NULL);
    printk(KERN_INFO "Removed /proc/%s entry\n", PROC_FILENAME);
}

module_init(init_comm);
module_exit(cleanup_comm);

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Your Name");
MODULE_DESCRIPTION("User-Kernel Communication via Procfs");
