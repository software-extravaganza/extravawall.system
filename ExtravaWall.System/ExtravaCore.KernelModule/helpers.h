#ifndef HELPERS_H
#define HELPERS_H

#include <linux/kernel.h>
#include <linux/ktime.h>
#include <linux/slab.h> // For kmalloc/kfree
#include "logger.h"

#define PRINT_HEX 0
#define PRINT_BIN 1
#define PRINT_DEC 2
#define PRINT_OCT 3

// Define a function type for functions that take no arguments and return void
typedef void (*func_ptr_t)(void);

s64 elapsedMilliseconds(ktime_t *timestamp);
char* timeToHumanizedString(ktime_t kt);
char* nanosecondsToHumanizedString(u64 ns);
int is_little_endian(void);
uint16_t to_little_endian_16(uint16_t value);
uint32_t to_little_endian_32(uint32_t value);
char* bytes_to_ascii(const unsigned char *data, size_t len);
char* bytes_to_hex_string(const unsigned char *bytes, size_t size);
void get_random_ascii_chars(char *buf, size_t num_chars);
void get_random_keyboard_chars(char *buf, size_t num_chars);
void print_hex(const void *data, size_t len);
void print_data(const void *data, size_t len, int format);
// static inline void check_null_helper(const char* ptr_name, void* generic_ptr, LogType log_type_enum, void (*code_func)(void)) {
//     typeof(generic_ptr) value = generic_ptr;
//     log_func_t log_func = log_functions[log_type_enum];
//     log_func("%s is NULL.", ptr_name);
//     (void)value;
//     code_func();
// }
#define ARRAY_SIZE_REASON(arr) (sizeof(arr) / sizeof((arr)[0]))
#define TOKENPASTE(x, y) x ## y
#define TOKENPASTE2(x, y) TOKENPASTE(x, y)
#define TOKENPASTE3(x, y) TOKENPASTE2(x, y)
#define UNIQUE_FUNC_NAME(base) TOKENPASTE2(base, __LINE__)
#define DEFINE_VALUE_FROM_PTR(ptr) \
    typeof(ptr) value = (ptr); \
    (void)value;

#define DEFINE_FUNC(prefix, ptr, code) \
    void TOKENPASTE3(auto_func_, prefix)(void) { \
        DEFINE_VALUE_FROM_PTR(ptr) \
        code; \
    }

#define IS_POINTER(p) ((void)sizeof((p) - (p)))

#define CALL_FUNC(prefix) \
    TOKENPASTE3(auto_func_, prefix)()

#define CALL_FUNC_WITH_PREFIX(prefix) \
    CALL_FUNC(prefix)

#define RETURN_FUNC(prefix) \
    TOKENPASTE3(auto_func_, prefix)

#define RETURN_FUNC_WITH_PREFIX(prefix) \
    RETURN_FUNC(prefix)

#define DEFINE_AND_CALL_FUNC(code) \
    void UNIQUE_FUNC_NAME(auto_func)(void) { code; } \
    UNIQUE_FUNC_NAME(auto_func)();

#define DEFINE_FUNC_WITH_PREFIX(prefix, ptr, code) \
    void TOKENPASTE3(auto_func_, prefix)(void) { \
        DEFINE_VALUE_FROM_PTR(ptr) \
        code; \
    }

#define DEFINE_FUNC_WITH_PREFIX_AND_RETURN_TYPE(prefix, ptr, code) \
    DEFINE_FUNC_WITH_PREFIX(__LINE__, ptr, code); \
    RETURN_FUNC_WITH_PREFIX(__LINE__)

// This macro returns the function's address
#define RETURN_FUNC_ADDRESS(prefix) \
    &TOKENPASTE3(auto_func_, prefix)

// The original DEFINE_UNIQUE_FUNC_AND_RETURN_TYPE macro will now define the function and return its address
#define DEFINE_UNIQUE_FUNC_AND_RETURN_TYPE(ptr, code) \
    ({ \
        DEFINE_FUNC_WITH_PREFIX(__LINE__, ptr, code); \
        RETURN_FUNC_ADDRESS(__LINE__); \
    })

#define NAME_OF(x) #x

#define CHECK_NULL(log_type_enum, ptr, ret_val) \
    do { \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_SUCCESS_EXEC(log_type_enum, ptr, ret_val, code) \
    do { \
        func_ptr_t code_func = DEFINE_UNIQUE_FUNC_AND_RETURN_TYPE(ptr, code); \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
            return ret_val; \
        } \
        else { \
            code_func(); \
        } \
    } while(0)

#define CHECK_NULL_FAIL_EXEC(log_type_enum, ptr, ret_val, code) \
    do { \
        func_ptr_t code_func = DEFINE_UNIQUE_FUNC_AND_RETURN_TYPE(ptr, code); \
        if ((ptr) == NULL) { \
            code_func(); \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_ALWAYS_EXEC(log_type_enum, ptr, ret_val, code) \
    do { \
        func_ptr_t code_func = DEFINE_UNIQUE_FUNC_AND_RETURN_TYPE(ptr, code); \
        if ((ptr) == NULL) { \
            log_func("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
        } \
        if ((ptr) == NULL) { \
            code_func(); \
            return ret_val; \
        } \
    } while(0)

#define CHECK_NULL_AND_LOG(fail_log_type_enum, ptr, ret_val, success_log_type_enum, fmt, ...) \
    do { \
        log_func_t log_func_success = log_functions[success_log_type_enum]; \
        log_func_t log_func_fail = log_functions[fail_log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func_fail("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
            return ret_val; \
        } \
        else { \
            log_func_success(fmt, #ptr, __FILENAME_NO_EXT__, __func__, __LINE__, ##__VA_ARGS__); \
        } \
    } while(0)

#define CHECK_NULL_DESC(log_type_enum, ptr, ret_val, description) \
    do { \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL. (%s)", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__, description); \
            return ret_val; \
        } \
    } while(0)

#define TEST_NULL(log_type_enum, ptr) \
    do { \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
        } \
    } while(0)

#define TEST_AND_MAKE_STRING_OR_EMPTY(log_type_enum, ptr, code) \
    ((ptr) ? ({ \
        typeof(ptr) value = (ptr); \
        code; \
    }) : ""); \
    do { \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL.", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__); \
        } \
    } while(0)

#define MAKE_STRING_OR_EMPTY(ptr, code) \
    ((ptr) ? ({ \
        typeof(ptr) value = (ptr); \
        IS_POINTER(ptr); \
        code; \
    }) : "")

#define TEST_NULL_DESC(log_type_enum, ptr, ret_val, description) \
    do { \
        log_func_t log_func = log_functions[log_type_enum]; \
        if ((ptr) == NULL) { \
            log_func("%s is NULL. (%s)", #ptr, __FILENAME_NO_EXT__, __func__, __LINE__, description); \
        } \
    } while(0)


#define CHECK_INDEX_AND_OFFSET_RETURN(index, offset, failReturn) \
    do{ \
        if (index < 0 || index >= NUM_SLOTS){ \
            LOG_ERROR("RingBuf: Index %d is out of range", index); \
            return failReturn; \
        } \
        if(offset < 0 || offset >= DUPLEX_RING_BUFFER_SIZE) { \
            LOG_ERROR("RingBuf: Offset %d is out of range", offset); \
            return failReturn; \
        } \
    }while(0)

#define CHECK_INDEX_AND_OFFSET(index, offset) \
    do{ \
        if (index < 0 || index >= NUM_SLOTS){ \
            LOG_ERROR("RingBuf: Index %d is out of range", index); \
            return; \
        } \
        if(offset < 0 || offset >= DUPLEX_RING_BUFFER_SIZE) { \
            LOG_ERROR("RingBuf: Offset %d is out of range", offset); \
            return; \
        } \
    }while(0)



#endif // HELPERS_H