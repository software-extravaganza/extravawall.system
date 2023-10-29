#include <linux/module.h>

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Extravaganza Software");
MODULE_DESCRIPTION("ExtravaCore Kernel Module");
MODULE_VERSION("0.1");

int log_level = 1;
int default_packet_response = 1;
bool force_icmp = false;

// Make sure you have linux-headers installed on your system
// Fedora Silverblue: sudo rpm-ostree install kernel-devel-$(uname -r) --apply-live
//      toolbox enter
//      sudo dnf install kernel-devel kernel-headers gcc make kernel-devel-$(uname -r) kmod mokutil pesign openssl
//      sudo ln -sf /usr/src/kernels/6.5.5-200.fc38.x86_64 /lib/modules/6.5.5-200.fc38.x86_64/build
//      openssl req -new -x509 -newkey rsa:2048 -keyout MOK.priv -outform DER -out MOK.der -nodes -days 36500 -subj "/CN=Machine Key/"
//      make
//      sudo /usr/src/kernels/$(uname -r)/scripts/sign-file sha256 ./MOK.priv ./MOK.der extrava.ko
//      OR
//      sudo /usr/src/linux-headers-$(uname -r)/scripts/sign-file sha256 ./MOK.priv ./MOK.der extrava.ko
//      OR
//      sudo kmodsign sha512 ./MOK.priv ./MOK.der extrava.ko
//      sudo mokutil --disable-validation
//      dnf download --source kernel-uek
//      rpm2cpio ./kernel-uek*.rpm | cpio -idmv
//      sudo mokutil --root-pw --import MOK.der
// Fedora Workstation: sudo dnf install kernel-devel-$(uname -r) make
// Red Hat Enterprise Linux & CentOS: sudo yum install kernel-devel-$(uname -r)
// Debian & Ubuntu: sudo apt install linux-headers-$(uname -r)
#include <linux/kernel.h>
#include <linux/delay.h>
#include "logger.h"
#include "module_control.h"
#include "netfilter_hooks.h"
#include "userspace_comm.h"
#include "data_factories.h"
#include "ringbuffer_comm.h"

// This will be a simulation of the network packet data
char packet_data[512];

#define SHOW_PARAMATER_VALUE(param) LOG_INFO("📌 %s %d", #param, param)


static struct task_struct *test_thread;

int test_thread_function(void *data) {
    // sleeping for 10 seconds
    msleep(10000);

    LOG_INFO("Size of DuplexRingBuffer: %d", DUPLEX_RING_BUFFER_SIZE);
    LOG_INFO("Size of DuplexRingBuffer page aligned: %d", DUPLEX_RING_BUFFER_ALIGNED_SIZE);
    LOG_INFO("Size of RingBuffer: %d", RING_BUFFER_SIZE);
    LOG_INFO("Size of RingBufferSlot: %d", SLOT_SIZE);
    while (!kthread_should_stop()) {
        TestWriteToRingBuffer();
    }
    return 0;
}


static int __init Initialize(void) {
    SHOW_PARAMATER_VALUE(log_level);
    SHOW_PARAMATER_VALUE(default_packet_response);
    SHOW_PARAMATER_VALUE(force_icmp);
    SetLogLevel(log_level);
    LOG_INFO("⌛️  Extrava module initializing  ⌛️");
    // if(SetupTimeSamples() != 0){
    //     LOG_ERROR("Failed to setup time samples");
    //     return -1;
    // }

    // if(SetupUserSpaceCommunication() != 0){
    //     LOG_ERROR("Failed to setup user space communication");
    //     return -1;
    // }

    // if(SetupNetfilterHooks() != 0){
    //     LOG_ERROR("Failed to setup netfilter hooks");
    //     return -1;
    // }

    if(InitializeRingBuffers() != 0){
        LOG_ERROR("Failed to setup buffer rings");
        return -1;
    }

    SetInitialized();
    LOG_INFO("✔️  Extrava module loaded  ✔️");
    Activate();


    test_thread = kthread_run(test_thread_function, NULL, "test_thread");
    if (IS_ERR(test_thread)) {
        printk(KERN_ALERT "Failed to create test thread.\n");
        return PTR_ERR(test_thread);
    }


	return 0;
}

static void __exit Exit(void) {
    LOG_INFO("⌛️  Extrava module exiting  ⌛️");
    Deactivate();
    //LOG_DEBUG("Cleaning up netfilter hooks");
    // CleanupNetfilterHooks();
    // LOG_DEBUG("Cleaning up user space communication");
    // CleanupUserSpaceCommunication();
    // LOG_DEBUG("Cleaning up statistical data");
    // CleanupTimeSamples();
    LOG_DEBUG("Cleaning up buffer rings");
    FreeRingBuffers();
    kthread_stop(test_thread);
	LOG_INFO("🛑  Extrava module unloaded  🛑");
    SetUninitialized();
}

module_init(Initialize);
module_exit(Exit);

