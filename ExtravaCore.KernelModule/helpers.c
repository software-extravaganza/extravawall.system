#include "helpers.h"

s64 elapsedMilliseconds(ktime_t *timestamp) {
    ktime_t now = ktime_get();
    return ktime_to_ms(ktime_sub(now, *timestamp));
}