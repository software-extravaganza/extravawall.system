#ifndef _RINGBUFFER_COMM_H_
#define _RINGBUFFER_COMM_H_

#define NUM_SLOTS 2048
#define MAX_PAYLOAD_SIZE 61440 

#include <linux/types.h>
#include <linux/fs.h>
#include <linux/mm.h>
#include <linux/vmalloc.h>
#include <linux/module.h>
#include <linux/init.h>
#include <linux/random.h>
#include "data_structures.h"
#include "ringbuffer_types.h"



int InitializeRingBuffers(void);
void FreeRingBuffers(void);
void TestWriteToRingBuffer(void);

#endif // _RINGBUFFER_COMM_H_
