#ifndef HELPERS_H
#define HELPERS_H

#include <linux/kernel.h>
#include "logger.h"

#define CHECK_NULL(log_type, ptr, ret_val) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_SUCCESS_EXEC(log_type, ptr, ret_val, code) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
            return ret_val; \
        } \
        else { \
            typeof(ptr) value = (ptr); \
            code; \
        } \
    } while(0)

#define CHECK_NULL_FAIL_EXEC(log_type, ptr, ret_val, code) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
            typeof(ptr) value = (ptr); \
            code; \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_ALWAYS_EXEC(log_type, ptr, ret_val, code) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
        } \
        typeof(ptr) value = (ptr); \
        code; \
        if ((ptr) == NULL) { \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_AND_LOG(fail_log_type, ptr, ret_val, success_log_type, fmt, ...) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(fail_log_type)("%s is NULL.", #ptr); \
            return ret_val; \
        } \
        else { \
            CONCAT_LOG_FUNC(success_log_type)(fmt, ##__VA_ARGS__); \
        } \
    } while(0)

#define CHECK_NULL_DESC(log_type, ptr, ret_val, description) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL. (%s)", #ptr, description); \
            return ret_val; \
        } \
    } while(0)

#define TEST_NULL(log_type, ptr) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
        } \
    } while(0)

#define TEST_AND_MAKE_STRING_OR_EMPTY(log_type, ptr, code) \
    ((ptr) ? ({ \
        typeof(ptr) value = (ptr); \
        code; \
    }) : ""); \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL.", #ptr); \
        } \
    } while(0)

#define MAKE_STRING_OR_EMPTY(ptr, code) \
    ((ptr) ? ({ \
        typeof(ptr) value = (ptr); \
        code; \
    }) : "")

#define TEST_NULL_DESC(log_type, ptr, ret_val, description) \
    do { \
        if ((ptr) == NULL) { \
            CONCAT_LOG_FUNC(log_type)("%s is NULL. (%s)", #ptr, description); \
        } \
    } while(0)

#endif // HELPERS_H