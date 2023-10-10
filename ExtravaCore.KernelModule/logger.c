// logger.c
#include <linux/kernel.h>
#include <linux/module.h>

extern int log_level;
module_param(log_level, int, 0644);
MODULE_PARM_DESC(log_level, "Log level: 0=DEBUG, 1=INFO, 2=ERR, 3=WARN, 4=ALERT; default=1");

#include "logger.h"
void log_special_event(const char *event_detail) {
    LOG_INFO("Special event occurred: %s", event_detail);
}
