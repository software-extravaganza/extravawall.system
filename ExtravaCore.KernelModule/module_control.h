#ifndef _MODULE_CONTROL_H_
#define _MODULE_CONTROL_H_

#include <linux/kernel.h>
#include <linux/module.h>
#include "logger.h"
#include "helpers.h"

extern int default_packet_response;
extern bool force_icmp;

typedef void (*captureEventHandler)(bool shouldCapture);

bool ShouldCapture(void);
bool IsUnloading(void);
bool IsActive(void);
void Activate(void);
void Deactivate(void);
void SetInitialized(void);
void SetUserSpaceReadConnected(void);
void SetUserSpaceWriteConnected(void);
bool IsInitialized(void);
void SetUnloading(void);
bool IsUserSpaceConnected(void);
bool IsUserSpaceReadConnected(void);
bool IsUserSpaceWriteConnected(void);
void SetUninitialized(void);
void SetUserSpaceReadDisconnected(void);
void SetUserSpaceWriteDisconnected(void);
void SetLogLevel(int level);
int GetLogLevel(void);
void SetShouldCaptureEventHandler(captureEventHandler handler);

#endif // _MODULE_CONTROL_H_