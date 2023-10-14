#ifndef LOGGER_H
#define LOGGER_H

#include <linux/kernel.h>

extern int log_level;

#define LOG_BASE(level, kern_level, fmt, ...) \
    if (log_level <= level) printk(kern_level "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_DEBUG(fmt, ...) LOG_BASE(0, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_INFO(fmt, ...)  LOG_BASE(1, KERN_INFO, fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...)   LOG_BASE(2, KERN_ERR, fmt, ##__VA_ARGS__)
#define LOG_WARN(fmt, ...)  LOG_BASE(3, KERN_WARNING, fmt, ##__VA_ARGS__)
#define LOG_ALERT(fmt, ...) LOG_BASE(4, KERN_ALERT, fmt, ##__VA_ARGS__)

#endif // LOGGER_H
