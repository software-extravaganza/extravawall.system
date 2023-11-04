#ifndef TYPE_CONVERTERS_H
#define TYPE_CONVERTERS_H

#include <linux/kernel.h>
#include <linux/byteorder/generic.h>
#include <linux/types.h>  // For fixed-width integer types like u16, u32, u64, etc.
#include <linux/byteorder/generic.h>  // For endian conversion functions
#include "logger.h"
#include "helpers.h"
#include "data_structures.h"

#define S32_SIZE (sizeof(s32))
#define U32_SIZE (sizeof(u32))
#define U64_SIZE (sizeof(u64))
#define U16_SIZE (sizeof(u16))
#define IP_DSCP_MASK 0xFC  // 11111100
#define IP_ECN_MASK  0x03  // 00000011

#define STRINGIFY(x) #x
#define TOSTRING(x) STRINGIFY(x)



#define DSCP_TOS_STRING(tos) ({ \
    u8 _dscp_val = (tos) & IP_DSCP_MASK; \
    const char* _dscp_str; \
    switch (_dscp_val) { \
        case 0x00: _dscp_str = "CS0 (Default)"; break; \
        case 0x08: _dscp_str = "CS1"; break; \
        case 0x10: _dscp_str = "CS2"; break; \
        case 0x18: _dscp_str = "CS3"; break; \
        case 0x20: _dscp_str = "CS4"; break; \
        case 0x28: _dscp_str = "CS5"; break; \
        case 0x30: _dscp_str = "CS6"; break; \
        case 0x38: _dscp_str = "CS7"; break; \
        default: _dscp_str = "Unknown DSCP"; break; \
    } \
    _dscp_str; \
})

#define ECN_TOS_STRING(tos) ({ \
    u8 _ecn_val = (tos) & IP_ECN_MASK; \
    const char* _ecn_str; \
    switch (_ecn_val) { \
        case 0x00: _ecn_str = "Not-ECT"; break; \
        case 0x01: _ecn_str = "ECT(1)"; break; \
        case 0x02: _ecn_str = "ECT(0)"; break; \
        case 0x03: _ecn_str = "CE"; break; \
        default: _ecn_str = "Unknown ECN"; break; \
    } \
    _ecn_str; \
})

#define TOS_STRING_FORMAT "DSCP: %s, ECN: %s"
#define TOS_STRING_ARGS(tos_val) DSCP_TOS_STRING(tos_val), ECN_TOS_STRING(tos_val)

#define TOS_TO_STRING(tos_val) ({ \
    static char _tos_combined[128]; \
    if (tos_val == 0) { \
        strcpy(_tos_combined, "None"); \
    } else { \
        snprintf(_tos_combined, sizeof(_tos_combined), TOS_STRING_FORMAT, TOS_STRING_ARGS(tos_val)); \
    } \
    _tos_combined; \
})

const char* ipProtocolToString(unsigned int proto);
const char* hookToString(unsigned int hooknum);
const char* routeTypeToString(unsigned int route_type);
void int_to_bytes(const void *value, unsigned char *bytes, size_t len);
void bytes_to_int(const unsigned char *bytes, void *value, size_t len);

void ipToString(const unsigned int ip, char *buffer, size_t buf_len);

#endif // TYPE_CONVERTERS_H