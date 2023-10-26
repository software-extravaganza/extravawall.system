#ifndef DATA_FACTORIES_H
#define DATA_FACTORIES_H

#include <linux/kernel.h>
#include <linux/skbuff.h>
#include <linux/kfifo.h>
#include <linux/netfilter.h>
#include <linux/netfilter_ipv4.h>
#include <net/netfilter/nf_queue.h>
#include <linux/skbuff.h>
#include <linux/ip.h>
#include <linux/ktime.h>
#include <linux/sort.h>
#include "logger.h"
#include "data_structures.h"
#include "type_converters.h"

/* Function Declarations */
void ConditionalMemoryZero(void* ptr, size_t size);
PendingPacketRoundTrip* CreatePendingPacketTrip(void);
void FreePendingPacketTrip(PendingPacketRoundTrip *packetTrip);
PendingPacket* CreatePendingPacket(RoundTripPacketType type);
void FreePendingPacket(PendingPacket *packet);
bool AddDataToPacket(PendingPacket *packet, struct sk_buff *skb);
char* GetReasonText(DecisionReason reason);
char* CreatePacketTypeString(PendingPacket *packet);
void safeFree(void* ptr) ;
char* calculateSamplePercentileToString(int percentile);
char* calculateSampleAverageToString(void);
int SetupTimeSamples(void);
void CleanupTimeSamples(void);
void AddMetaDataToPacketTrip(PendingPacketRoundTrip *packetTrip, RoutingType type);
void ResetPendingPacketTrip(PendingPacketRoundTrip *packetTrip);
#endif // DATA_FACTORIES_H