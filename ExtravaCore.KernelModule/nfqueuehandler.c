#include <stdio.h>
#include <stdint.h>
#include <linux/netfilter.h>
#include <libnetfilter_queue/libnetfilter_queue.h>

// gcc -shared -fPIC -o libnfqueuehandler.so nfqueuehandler.c -lnetfilter_queue

// Fedora: sudo dnf install libnetfilter_queue-devel
// Debian & Ubuntu: sudo apt install libnetfilter-queue-dev
static int packetHandler(struct nfq_q_handle *qh, struct nfgenmsg *nfmsg, struct nfq_data *nfa, void *data) {
    u_int32_t id = 0;
    struct nfqnl_msg_packet_hdr *ph;

    ph = nfq_get_msg_packet_hdr(nfa);
    if (ph) {
        id = ntohl(ph->packet_id);
    }

    return nfq_set_verdict(qh, id, NF_ACCEPT, 0, NULL);
}

void start_nfqueue() {
    struct nfq_handle *h = nfq_open();
    struct nfq_q_handle *qh;

    nfq_bind_pf(h, AF_INET);
    qh = nfq_create_queue(h,  0, &packetHandler, NULL);
    nfq_set_mode(qh, NFQNL_COPY_PACKET, 0xffff);
    
    // Main loop to receive and handle packets
    // ... (use recv for example)
    
    nfq_destroy_queue(qh);
    nfq_close(h);
}
