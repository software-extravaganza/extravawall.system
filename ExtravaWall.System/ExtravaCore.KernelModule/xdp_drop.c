#include <linux/bpf.h>
#include "/usr/src/kernels/6.5.5-200.fc38.x86_64/tools/lib/bpf/bpf_helpers.h"

// clang -O2 -target bpf -c xdp_drop.c -o xdp_drop.o -I /usr/src/kernels/$(uname -r)/tools/lib/bpf/
SEC("filter")
int xdp_drop_prog(struct __sk_buff *skb) {
    return XDP_DROP;
}

char _license[] SEC("license") = "GPL";
