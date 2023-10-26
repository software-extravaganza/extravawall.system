#include "helpers.h"

s64 elapsedMilliseconds(ktime_t *timestamp) {
    ktime_t now = ktime_get();
    return ktime_to_ms(ktime_sub(now, *timestamp));
}


char* nanosecondsToHumanizedString(u64 ns) {
    static char buffer[150]; // Increased buffer size to accommodate the additional text
    u64 sec, ms, us, remainder_ns, total_ns;

    total_ns = ns; // Store the total nanoseconds for later use

    sec = ns / 1000000000;
    ns %= 1000000000;

    ms = ns / 1000000;
    ns %= 1000000;

    us = ns / 1000;
    remainder_ns = ns % 1000;

    snprintf(buffer, sizeof(buffer), "%llu sec, %llu ms, %llu us, %llu ns (total ns: %llu)", 
             sec, ms, us, remainder_ns, total_ns);
    return buffer;
}

char* timeToHumanizedString(ktime_t kt) {
    u64 ns = ktime_to_ns(kt);
    return nanosecondsToHumanizedString(ns);
}
