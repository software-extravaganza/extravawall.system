#ifndef _RINGBUFFER_COMM_H_
#define _RINGBUFFER_COMM_H_
// #define NUM_SLOTS 2048
// #define MAX_PAYLOAD_SIZE 61440 

#include <linux/types.h>
#include <linux/fs.h>
#include <linux/mm.h>
#include <linux/vmalloc.h>
#include <linux/module.h>
#include <linux/semaphore.h>
#include <linux/kfifo.h>
#include <linux/init.h>
#include <linux/string.h> 
#include <linux/random.h>
#include "data_structures.h"
#include "ringbuffer_types.h"
#include "module_control.h"
#include "helpers.h"

#define S32_SIZE (sizeof(s32))

int InitializeRingBuffers(void);
void FreeRingBuffers(void);
void TestWriteToRingBuffer(void);
DataBuffer *ReadFromUserRingBuffer(void) ;
int WriteToSystemRingBuffer(const char *data, size_t size);
__u32 read_system_ring_buffer_position(void);
void write_system_ring_buffer_slot_status(int slot_index, SlotStatus slot_status);
SlotStatus read_system_ring_buffer_slot_status(int slot_index);
void free_data_buffer(DataBuffer *buffer);

extern long SystemBufferSlotsUsedCounter;
extern long SystemBufferSlotsClearedCounter;
extern long SystemBufferActiveUsedSlots;
extern long SystemBufferActiveFreeSlots;

typedef struct {
    uint Start;
    uint End;
} IndexRange;

#endif // _RINGBUFFER_COMM_H_
