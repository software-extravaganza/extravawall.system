// logger.c
#include <linux/kernel.h>
#include <linux/module.h>

extern int log_level;
module_param(log_level, int, 0644);
MODULE_PARM_DESC(log_level, "Log level: -2=DEBUG_PACKET, -1=DEBUG_ICMP, 0=DEBUG, 1=INFO, 2=ERR, 3=WARN, 4=ALERT; default=1");

#include "logger.h"
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_DEBUG_PACKET, debugPacket, LOG_TYPE_DEBUG_PACKET)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_DEBUG_ICMP, debugIcmp, LOG_TYPE_DEBUG_ICMP)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_DEBUG, debug, LOG_TYPE_DEBUG)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_INFO, info, LOG_TYPE_INFO)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_ERROR, error, LOG_TYPE_ERROR)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_WARNING, warning, LOG_TYPE_WARNING)
DEFINE_LOG_WRAPPER_FUNC(_INTERNAL_LOG_ALERT, alert, LOG_TYPE_ALERT)

log_func_t log_functions[__LOG_TYPE_ENUM_COUNT] = {
    [LOG_TYPE_DEBUG_PACKET] = log_debugPacket_func,
    [LOG_TYPE_DEBUG_ICMP] = log_debugIcmp_func,
    [LOG_TYPE_DEBUG] = log_debug_func,
    [LOG_TYPE_INFO] = log_info_func,
    [LOG_TYPE_ERROR] = log_error_func,
    [LOG_TYPE_WARNING] = log_warning_func,
    [LOG_TYPE_ALERT] = log_alert_func,
    // ... other log functions ...
};

void log_special_event(const char *event_detail) {
    LOG_INFO("Special event occurred: %s", event_detail);
}
