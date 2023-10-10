#ifndef LOGGER_H
#define LOGGER_H

#include <linux/kernel.h>

#define LOG_ERR(fmt, ...) \
    printk(KERN_ERR "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_INFO(fmt, ...) \
    printk(KERN_INFO "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_ALERT(fmt, ...) \
    printk(KERN_ALERT "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_WARN(fmt, ...) \
    printk(KERN_WARNING "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_DBG(fmt, ...) \
    printk(KERN_DEBUG "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#endif // LOGGER_H
