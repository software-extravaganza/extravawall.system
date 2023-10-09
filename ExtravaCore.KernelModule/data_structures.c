#include <linux/kernel.h>
#include <linux/slab.h>
#include "data_structures.h"

/* Function to create a new pending packet */
PendingPacket* create_pending_packet(struct sk_buff *skb, size_t data_len) {
    PendingPacket *packet = kmalloc(sizeof(PendingPacket), GFP_KERNEL);
    
    if (!packet) {
        printk(KERN_ERR "Failed to allocate memory for PendingPacket.\n");
        return NULL;
    }

    packet->skb = skb;
    packet->data_len = data_len;
    packet->processed = false;
    packet->decision = UNDECIDED;

    return packet;
}

/* Function to free a pending packet */
void free_pending_packet(PendingPacket *packet) {
    if (packet) {
        kfree(packet);
    }
}

/* Function to create a new packet header */
PacketHeader* create_packet_header(unsigned long pkt_id, size_t length) {
    PacketHeader *header = kmalloc(sizeof(PacketHeader), GFP_KERNEL);

    if (!header) {
        printk(KERN_ERR "Failed to allocate memory for PacketHeader.\n");
        return NULL;
    }

    header->pkt_id = pkt_id;
    header->length = length;

    return header;
}

/* Function to free a packet header
