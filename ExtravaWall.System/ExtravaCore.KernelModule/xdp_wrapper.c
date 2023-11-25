#include <errno.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <linux/if_xdp.h>
#include <linux/if_link.h>
#include <sys/socket.h>
#include <net/if.h>
#include <arpa/inet.h>
#include <unistd.h>
#include <linux/if_link.h> // for struct sockaddr_xdp
#include <net/if.h> // for if_nametoindex
#include <sys/types.h>
#include <ifaddrs.h>

// gcc -shared -fPIC -o libxdpwrapper.so xdp_wrapper.c -Wall

#define MAX_PACKET_SIZE 1500

static int xdp_sock = -1;

// Structure to represent a packet
typedef struct {
    char data[MAX_PACKET_SIZE];
    size_t len;
} Packet;

// Initialize AF_XDP socket
const char* init_socket(const char* ifname) {
    struct sockaddr_xdp addr;
    int ifindex = if_nametoindex(ifname);

    if (ifindex == 0) {
        perror("if_nametoindex");
        return "Error retrieving interface index";
    }

    memset(&addr, 0, sizeof(addr));
    addr.sxdp_family = PF_XDP;
    addr.sxdp_ifindex = ifindex;

    xdp_sock = socket(AF_XDP, SOCK_RAW, 0);
    if (xdp_sock < 0) {
        static char error_msg[256];
        snprintf(error_msg, sizeof(error_msg), "Error creating AF_XDP socket for %s: %s", ifname, strerror(errno));
        return error_msg;
    }

    if (bind(xdp_sock, (struct sockaddr*)&addr, sizeof(addr)) < 0) {
        static char error_msg[256];
        snprintf(error_msg, sizeof(error_msg), "Error binding AF_XDP socket to %s: %s", ifname, strerror(errno));
        close(xdp_sock);
        return error_msg;
    }

    return NULL; // Indicates success.
}

const char* list_interfaces() {
    struct ifaddrs *ifaddr, *ifa;
    static char interfaces[2048];  // Assuming this size is sufficient to hold interface names
    memset(interfaces, 0, sizeof(interfaces));

    if (getifaddrs(&ifaddr) == -1) {
        perror("getifaddrs");
        return "Error retrieving interfaces";
    }

    char* ptr = interfaces;
    for (ifa = ifaddr; ifa != NULL; ifa = ifa->ifa_next) {
        if (ifa->ifa_addr == NULL)
            continue;
        snprintf(ptr, sizeof(interfaces) - (ptr - interfaces), "%s,", ifa->ifa_name);
        ptr = interfaces + strlen(interfaces);
    }

    freeifaddrs(ifaddr);

    if (strlen(interfaces) > 0) {
        interfaces[strlen(interfaces)-1] = '\0';  // Remove the trailing comma
    }

    return interfaces;
}

// Retrieve a packet from the AF_XDP socket
int get_packet(Packet* pkt) {
    ssize_t recv_len = recv(xdp_sock, pkt->data, MAX_PACKET_SIZE, 0);
    if (recv_len <= 0) {
        perror("recv");
        return -1;
    }

    pkt->len = recv_len;
    return 0;
}

// Send a decision for the packet
void send_decision(int decision) {
    // For this basic example, we're not actually sending any decision back.
    // AF_XDP sockets by default will pass the packet up the stack if you don't consume them.
    // If you decide to drop, just don't send it up.
    if (decision == 0) {
        // Drop packet (for this example, we simply do nothing)
    } else {
        // Forward packet (for this example, we don't need to do anything)
    }
}

void cleanup() {
    if (xdp_sock != -1) {
        close(xdp_sock);
    }
}
