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


int is_little_endian() {
    uint16_t test = 0x00FF;
    char *byte = (char *)&test;
    return byte[0] == 0xFF;
}

uint16_t to_little_endian_16(uint16_t value) {
    if (!is_little_endian()) {
        return value;
    } else {
        return (value >> 8) | (value << 8);
    }
}

uint32_t to_little_endian_32(uint32_t value) {
    if (!is_little_endian()) {
        return value;
    } else {
        return ((value >> 24) & 0xff) |
               ((value << 8) & 0xff0000) |
               ((value >> 8) & 0xff00) |
               ((value << 24) & 0xff000000);
    }
}

char* bytes_to_ascii(const unsigned char *data, size_t len) {
    char *ascii_str = kmalloc(len + 1, GFP_KERNEL); // Allocate memory for the ASCII string
    if (!ascii_str)
        return NULL;

    int i;
    for (i = 0; i < len; i++) {
        // Check if the byte value is a printable ASCII character
        // Printable characters are from 0x20 (space) to 0x7E (~)
        if (data[i] >= 0x20 && data[i] <= 0x7E)
            ascii_str[i] = data[i];
        else
            ascii_str[i] = '.'; // Replace non-printables with a dot
    }

    ascii_str[len] = '\0'; // Null-terminate the string
    return ascii_str;
}

void get_random_ascii_chars(char *buf, size_t num_chars) {
    size_t i;
    unsigned char rand_byte;

    for (i = 0; i < num_chars; ++i) {
        // Generate one random byte.
        get_random_bytes(&rand_byte, 1);

        // Convert byte to printable ASCII range from 32 to 126
        buf[i] = (rand_byte % 95) + 32;
    }
}

void get_random_keyboard_chars(char *buf, size_t num_chars) {
    static const char keyboard_chars[] =
        "abcdefghijklmnopqrstuvwxyz"
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
        "0123456789"
        "!@#$%^&*()-_=+[]{};:',.<>?";
    size_t i;
    unsigned char rand_index;

    for (i = 0; i < num_chars; ++i) {
        // Generate a random index for the keyboard_chars array
        get_random_bytes(&rand_index, 1);
        rand_index %= sizeof(keyboard_chars) - 1;  // -1 to exclude the null terminator
        buf[i] = keyboard_chars[rand_index];
    }
}