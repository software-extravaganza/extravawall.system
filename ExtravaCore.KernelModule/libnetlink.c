// libnetlink.c
#include <sys/socket.h>
#include <linux/netlink.h>
#include <stdio.h>
#include <errno.h>
#include <string.h>
#include <unistd.h>

#define NETLINK_USER 31
#define MAX_PAYLOAD 1500  // Adjusted to match our packet interception length

struct sockaddr_nl src_addr, dest_addr;
struct nlmsghdr *nlh = NULL;
struct iovec iov;
int sock_fd;

#ifdef __cplusplus
extern "C" {
#endif


int connect_to_kernel() {
    sock_fd = socket(PF_NETLINK, SOCK_RAW, NETLINK_USER);
    if(sock_fd < 0) {
        perror("Failed to create netlink socket");
        return -1;
    }

    memset(&src_addr, 0, sizeof(src_addr));
    src_addr.nl_family = AF_NETLINK;
    src_addr.nl_pid = getpid();

    if(bind(sock_fd, (struct sockaddr*)&src_addr, sizeof(src_addr)) < 0) {
        perror("Failed to bind netlink socket");
        close(sock_fd);
        return -1;
    }

    memset(&dest_addr, 0, sizeof(dest_addr));
    dest_addr.nl_family = AF_NETLINK;
    dest_addr.nl_pid = 0; // kernel
    dest_addr.nl_groups = 0; // unicast

    nlh = (struct nlmsghdr *)malloc(NLMSG_SPACE(MAX_PAYLOAD));
    if(!nlh) {
        perror("Failed to allocate memory for netlink message");
        close(sock_fd);
        return -1;
    }

    return 0;
}

int send_data(char *data, int len) {
    strncpy(NLMSG_DATA(nlh), data, len);
    iov.iov_base = (void *)nlh;
    iov.iov_len = nlh->nlmsg_len;
    struct msghdr msg;
    memset(&msg, 0, sizeof(msg));
    msg.msg_name = (void *)&dest_addr;
    msg.msg_namelen = sizeof(dest_addr);
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;

    return sendmsg(sock_fd, &msg, 0);
}

int receive_data_from_kernel(char *buffer, int length) {
    struct msghdr msg;
    memset(&msg, 0, sizeof(msg));
    msg.msg_name = (void *)&src_addr;
    msg.msg_namelen = sizeof(src_addr);
    msg.msg_iov = &iov;
    msg.msg_iovlen = 1;

    int ret = recvmsg(sock_fd, &msg, 0);
    if (ret > 0) {
        strncpy(buffer, NLMSG_DATA(nlh), length);
    }
    return ret;
}

void close_connection() {
    if (nlh) {
        free(nlh);
        nlh = NULL;
    }
    if (sock_fd >= 0) {
        close(sock_fd);
        sock_fd = -1;
    }
}


#ifdef __cplusplus
}
#endif
