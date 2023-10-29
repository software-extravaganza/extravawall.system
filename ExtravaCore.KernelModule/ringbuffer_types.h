#ifndef _RINGBUFFER_TYPES_H_
#define _RINGBUFFER_TYPES_H_

#include <linux/types.h>

#define NUM_SLOTS 2048
#define MAX_PAYLOAD_SIZE 61440

#ifndef PAGE_SIZE
    long get_page_size() {
        return sysconf(_SC_PAGESIZE);
    }

    size_t page_align(size_t size) {
        long page_size = get_page_size();
        return (size + (page_size - 1)) & ~(page_size - 1);
    }
    #define PAGE_ALIGN(size) (((size) + (get_page_size() - 1)) & ~(get_page_size() - 1))
#else
    #define PAGE_ALIGN(size) (((size) + (PAGE_SIZE - 1)) & ~(PAGE_SIZE - 1))
#endif // PAGE_SIZE


#define SLOT_HEADER_STATUS_SIZE sizeof(__u8)
#define SLOT_HEADER_TOTAL_DATA_SIZE_SIZE sizeof(__u32)
#define SLOT_HEADER_CURRENT_DATA_SIZE_SIZE sizeof(__u16)
#define SLOT_HEADER_SEQUENCE_NUMBER_SIZE sizeof(__u8)
#define SLOT_HEADER_CLEARANCE_START_INDEX_SIZE sizeof(__u16)
#define SLOT_HEADER_CLEARANCE_END_INDEX_SIZE sizeof(__u16)
#define SLOT_HEADER_SIZE (SLOT_HEADER_STATUS_SIZE + SLOT_HEADER_TOTAL_DATA_SIZE_SIZE + SLOT_HEADER_CURRENT_DATA_SIZE_SIZE + SLOT_HEADER_SEQUENCE_NUMBER_SIZE + SLOT_HEADER_CLEARANCE_START_INDEX_SIZE + SLOT_HEADER_CLEARANCE_END_INDEX_SIZE)
#define SLOT_DATA_SIZE MAX_PAYLOAD_SIZE
#define SLOT_SIZE (SLOT_HEADER_SIZE + SLOT_DATA_SIZE)

#define RING_BUFFER_HEADER_STATUS_SIZE sizeof(__u8)
#define RING_BUFFER_HEADER__POSITION_SIZE sizeof(__u32)
#define RING_BUFFER_HEADER_SIZE (RING_BUFFER_HEADER_STATUS_SIZE + RING_BUFFER_HEADER__POSITION_SIZE)
#define RING_BUFFER_DATA_SIZE (SLOT_SIZE * NUM_SLOTS)
#define RING_BUFFER_SIZE (RING_BUFFER_HEADER_SIZE + RING_BUFFER_DATA_SIZE)

#define DUPLEX_RING_BUFFER_SIZE (RING_BUFFER_SIZE * 2)
#define DUPLEX_RING_BUFFER_ALIGNED_SIZE PAGE_ALIGN(DUPLEX_RING_BUFFER_SIZE)



typedef struct RingBufferSlotHeader RingBufferSlotHeader;
typedef struct RingBufferHeader RingBufferHeader;
typedef struct DuplexRingBuffer DuplexRingBuffer;
typedef enum {
    EMPTY = 0,
    VALID = 1,
    ADVANCE = 2,
} SlotStatus;

typedef struct {
    char *data;
    size_t size;
} DataBuffer;

struct RingBufferSlotHeader {
    __u16 ClearanceStartIndex;      // Assuming max 65535 Slots in buffer; Matches ushort from C#
    __u16 ClearanceEndIndex;        // Assuming max 65535 Slots in buffer; Matches ushort from C#
    __u16 CurrentDataSize;          // Assuming max slot data size of 65535 bytes; Matches ushort from C#
    __u32 TotalDataSize;            // For larger data that spans multiple Slots; Matches uint from C#
    __u8  SequenceNumber;           // Range from 0 to 255; Matches byte from C#
    __u8 Status;          // Possible values: EMPTY, VALID, ADVANCE; Use bit field to ensure it's stored as __u8
    //char  Data[MAX_PAYLOAD_SIZE];   // Flexible array member for data payload
};

// struct __attribute__((packed)) RingBuffer {
//     __u32 Position; // Matches uint from C#
//     RingBufferSlot Slots[NUM_SLOTS];
// };

typedef enum {
    Inactive = 0,
    Active = 1,
    Full = 2,
    Terminating = 3
} RingBufferStatus;

struct RingBufferHeader {
    RingBufferStatus Status :8; // Use bit field to ensure it's stored as __u8
    __u32 Position; // Matches uint from C#
};


// struct __attribute__((packed)) DuplexRingBuffer {
//     RingBuffer SystemBuffer;
//     RingBuffer UserBuffer;
// };

#endif // _RINGBUFFER_TYPES_H_