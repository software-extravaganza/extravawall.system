#ifndef LOGGER_H
#define LOGGER_H

#include <linux/kernel.h>
#include <linux/stdarg.h>
#include <linux/string.h>

extern int log_level;
typedef void (*log_func_t)(const char*, const char*, const char*, const char*, int, ...);
const char* filenameWithoutExtension(const char* path);

void __LoggerSetLevel(int level);
#define STRINGIFY(x) #x
#define FORCE_EXPAND(macro) macro
#define __FILENAME__ (strrchr("/"__FILE__, '/') + 1)
#define __FILENAME_NO_EXT__ filenameWithoutExtension(__FILE__)
#define LOG_BASE(level, fmt, ...) \
    printk(fmt, ##__VA_ARGS__);

static inline void _logGenericFunc(int num_level, const char* kern_level, const char* fmt, const char* file, const char* func, int line, ...) {
    va_list args;
    char buffer[1024];
    char full_fmt[] = "EXTRAVA [%s➡ %s➡ %d] %s: %s\n";
    char* category;

    //The statement I need help with
    if(kern_level == NULL){
        category = "";
    }
    else if(strcmp(kern_level, KERN_DEBUG) == 0){
        category = "DEBUG";
    }
    else if(strcmp(kern_level, KERN_INFO) == 0){
        category = "INFO";
    }
    else if(strcmp(kern_level, KERN_ERR) == 0){
        category = "ERROR";
    }
    else if(strcmp(kern_level, KERN_WARNING) == 0){
        category = "WARNING";
    }
    else if(strcmp(kern_level, KERN_ALERT) == 0){
        category = "ALERT";
    }
    else{
        category = "UNKNOWN";
    }

    va_start(args, line);
    vsnprintf(buffer, sizeof(buffer), fmt, args);
    va_end(args);
    LOG_BASE(num_level, full_fmt, file, func, line, category, buffer);
}

#define _INTERNAL_LOG_HELPER(num_level, kern_level, fmt, file, func, line, ...) \
    do { \
        if (log_level <= num_level) { \
            _logGenericFunc(num_level, kern_level, fmt, file, func, line, ##__VA_ARGS__); \
        } \
    } while(0)

#define LOG_HELPER(num_level, kern_level, fmt, ...) \
    _INTERNAL_LOG_HELPER(num_level, kern_level, fmt, __FILENAME_NO_EXT__, __func__, __LINE__, ##__VA_ARGS__)

#define LOG_DEBUG_PACKET(fmt, ...) LOG_HELPER(-2, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_DEBUG_ICMP(packetTrip, fmt, ...) \
    do { \
        if(packetTrip && packetTrip->protocol == IPPROTO_ICMP){ \
            LOG_HELPER(-1, KERN_DEBUG, fmt, ##__VA_ARGS__); \
        } \
    } while(0)
#define LOG_DEBUG_ICMP_PROTOCOL(protocol, fmt, ...) \
    do { \
        if(protocol == IPPROTO_ICMP){ \
            LOG_HELPER(-1, KERN_DEBUG, fmt, ##__VA_ARGS__); \
        } \
    } while(0)
#define LOG_DEBUG(fmt, ...) LOG_HELPER(0, KERN_DEBUG, fmt, ##__VA_ARGS__)
#define LOG_INFO(fmt, ...) LOG_HELPER(1, KERN_INFO, fmt, ##__VA_ARGS__)
#define LOG_ERROR(fmt, ...) LOG_HELPER(2, KERN_ERR, fmt, ##__VA_ARGS__)
#define LOG_WARNING(fmt, ...) LOG_HELPER(3, KERN_WARNING, fmt, ##__VA_ARGS__)
#define LOG_ALERT(fmt, ...) LOG_HELPER(4, KERN_ALERT, fmt, ##__VA_ARGS__)

#define _INTERNAL_LOG_DEBUG_PACKET(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(-2, KERN_DEBUG, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_DEBUG_ICMP(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(-1, KERN_DEBUG, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_DEBUG(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(0, KERN_DEBUG, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_INFO(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(1, KERN_INFO, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_ERROR(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(2, KERN_ERR, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_WARNING(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(3, KERN_WARNING, fmt, file, func, line, ##__VA_ARGS__)
#define _INTERNAL_LOG_ALERT(fmt, file, func, line,...) _INTERNAL_LOG_HELPER(4, KERN_ALERT, fmt, file, func, line, ##__VA_ARGS__)

#define DEFINE_LOG_WRAPPER_FUNC(log_macro, log_name, log_enum) \
    static void log_##log_name##_func(const char* format,  const char* name, const char* file, const char* func, int line, ...) { \
        char buffer[1024]; /* or some appropriate size */ \
        va_list args; \
        va_start(args, line); \
        vsnprintf(buffer, sizeof(buffer), format, args); \
        va_end(args); \
        log_macro("%s", file, func, line, buffer); \
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
