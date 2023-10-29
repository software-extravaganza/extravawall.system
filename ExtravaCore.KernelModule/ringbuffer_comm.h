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


int InitializeRingBuffers(void);
void FreeRingBuffers(void);
void TestWriteToRingBuffer(void);

extern long SystemBufferSlotsUsedCounter;
extern long SystemBufferSlotsClearedCounter;
extern long SystemBufferActiveUsedSlots;
extern long SystemBufferActiveFreeSlots;

typedef struct {
    uint Start;
    uint End;
} IndexRange;

#endif // _RINGBUFFER_COMM_H_
