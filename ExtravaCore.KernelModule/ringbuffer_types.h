#ifndef _RINGBUFFER_TYPES_H_
#define _RINGBUFFER_TYPES_H_

#include <linux/types.h>


#define NUM_SLOTS 2048
#define MAX_PAYLOAD_SIZE 61440

typedef struct RingBufferSlot RingBufferSlot;
typedef struct RingBuffer RingBuffer;
typedef enum {
    EMPTY = 0,
    VALID = 1,
    ADVANCE = 2,
} SlotStatus;


struct __attribute__((packed)) RingBufferSlot {
    __u16 ClearanceStartIndex;      // Assuming max 65535 Slots in buffer; Matches ushort from C#
    __u16 ClearanceEndIndex;        // Assuming max 65535 Slots in buffer; Matches ushort from C#
    SlotStatus Status : 8;          // Possible values: EMPTY, VALID, ADVANCE; Use bit field to ensure it's stored as __u8
    __u16 CurrentDataSize;          // Assuming max slot data size of 65535 bytes; Matches ushort from C#
    __u32 TotalDataSize;            // For larger data that spans multiple Slots; Matches uint from C#
    __u8  SequenceNumber;           // Range from 0 to 255; Matches byte from C#
    char  Data[MAX_PAYLOAD_SIZE];   // Flexible array member for data payload
};

struct __attribute__((packed)) RingBuffer {
    __u32 Position; // Matches uint from C#
    RingBufferSlot Slots[NUM_SLOTS];
};

#endif // _RINGBUFFER_TYPES_H_