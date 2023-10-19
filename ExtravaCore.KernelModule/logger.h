#ifndef LOGGER_H
#define LOGGER_H

#include <linux/kernel.h>
#include <linux/stdarg.h>

extern int log_level;
typedef void (*log_func_t)(const char*, const char*, const char*, int, ...);

#define FORCE_EXPAND(macro) macro
#define LOG_BASE(level, fmt, ...) \
    printk(fmt, ##__VA_ARGS__);

static inline void _logGenericFunc(int num_level, const char* kern_level, const char* fmt, const char* func, int line, ...) {
    va_list args;
    char buffer[1024];
    char full_fmt[] = "%s[%s:%d] %s\n";
    va_start(args, line);
    vsnprintf(buffer, sizeof(buffer), fmt, args);
    va_end(args);
    LOG_BASE(num_level, full_fmt, kern_level, func, line, buffer);
}

#define _INTERNAL_LOG_HELPER(num_level, kern_level, fmt, func, line, ...) \
    do { \
        if (log_level <= num_level) { \
            _logGenericFunc(num_level, kern_level, fmt, func, line, ##__VA_ARGS__); \
        } \
    } while(0)

#define LOG_HELPER(num_level, kern_level, fmt, ...) \
    _INTERNAL_LOG_HELPER(num_level, kern_level, fmt, __func__, __LINE__, ##__VA_ARGS__)

#define LOG_DEBUG_PACKET(fmt, ...) LOG_HELPER(-2, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_DEBUG_ICMP(fmt, ...) LOG_HELPER(-1, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_DEBUG(fmt, ...) LOG_HELPER(0, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_INFO(fmt, ...) LOG_HELPER(1, KERN_INFO, fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) LOG_HELPER(2, KERN_ERR, fmt, ##__VA_ARGS__)
#define LOG_WARNING(fmt, ...) LOG_HELPER(3, KERN_WARNING, fmt, ##__VA_ARGS__)
#define LOG_ALERT(fmt, ...) LOG_HELPER(4, KERN_ALERT, fmt, ##__VA_ARGS__)

#define _INTERNAL_LOG_DEBUG_PACKET(fmt, func, line,...) _INTERNAL_LOG_HELPER(-2, KERN_DEBUG, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_DEBUG_ICMP(fmt, func, line,...) _INTERNAL_LOG_HELPER(-1, KERN_DEBUG, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_DEBUG(fmt, func, line,...) _INTERNAL_LOG_HELPER(0, KERN_DEBUG, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_INFO(fmt, func, line,...) _INTERNAL_LOG_HELPER(1, KERN_INFO, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_ERROR(fmt, func, line,...) _INTERNAL_LOG_HELPER(2, KERN_ERR, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_WARNING(fmt, func, line,...) _INTERNAL_LOG_HELPER(3, KERN_WARNING, fmt, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_ALERT(fmt, func, line,...) _INTERNAL_LOG_HELPER(4, KERN_ALERT, fmt, func, line, ##__VA_ARGS__)

#define DEFINE_LOG_WRAPPER_FUNC(log_macro, log_name, log_enum) \
    static void log_##log_name##_func(const char* format,  const char* name, const char* func, int line, ...) { \
        char buffer[1024]; /* or some appropriate size */ \
        va_list args; \
        va_start(args, line); \
        vsnprintf(buffer, sizeof(buffer), format, args); \
        va_end(args); \
        log_macro("%s", func, line, buffer); \
    }

typedef enum {
    LOG_TYPE_DEBUG_PACKET,
    LOG_TYPE_DEBUG_ICMP,
    LOG_TYPE_DEBUG,
    LOG_TYPE_INFO,
    LOG_TYPE_ERROR,
    LOG_TYPE_WARNING,
    LOG_TYPE_ALERT,
    __LOG_TYPE_ENUM_COUNT
} LogType;

extern log_func_t log_functions[__LOG_TYPE_ENUM_COUNT];

#define CONCAT_LOG_FUNC(log_type) LOG_##log_type

#endif // LOGGER_H
