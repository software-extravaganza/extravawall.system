// logger.c
#include "logger.h"

void log_special_event(const char *event_detail) {
    LOG_INFO("Special event occurred: %s", event_detail);
}
