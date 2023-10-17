#ifndef LOGGER_H
#define LOGGER_H

#include <linux/kernel.h>

extern int log_level;

#define LOG_BASE(level, kern_level, fmt, ...) \
    if (log_level <= level) printk(kern_level "[%s:%d] " fmt "\n", __func__, __LINE__, ##__VA_ARGS__)

#define LOG_DEBUG_PACKET(fmt, ...) LOG_BASE(-1, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_DEBUG(fmt, ...) LOG_BASE(0, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_INFO(fmt, ...)  LOG_BASE(1, KERN_INFO, fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...)   LOG_BASE(2, KERN_ERR, fmt, ##__VA_ARGS__)
#define LOG_WARN(fmt, ...)  LOG_BASE(3, KERN_WARNING, fmt, ##__VA_ARGS__)
#define LOG_ALERT(fmt, ...) LOG_BASE(4, KERN_ALERT, fmt, ##__VA_ARGS__)

#define LOG_TYPE_ERROR ERROR
#define LOG_TYPE_DEBUG DEBUG
#define LOG_TYPE_INFO INFO
#define LOG_TYPE_WARN WARN
#define LOG_TYPE_ALERT ALERT
#define LOG_TYPE_DEBUG_PACKET DEBUG_PACKET

#define CONCAT_LOG_FUNC(log_type) LOG_##log_type

#endif // LOGGER_H
