#include <linux/module.h>

MODULE_LICENSE("GPL");
MODULE_AUTHOR("Extravaganza Software");
MODULE_DESCRIPTION("ExtravaCore Kernel Module");
MODULE_VERSION("0.1");

int log_level = 1;

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
#include "logger.h"
#include "module_control.h"
#include "netfilter_hooks.h"
#include "userspace_comm.h"

// This will be a simulation of the network packet data
char packet_data[512];

static int __init Initialize(void) {
    SetLogLevel(log_level);
    LOG_INFO("⌛️  Extrava module initializing  ⌛️");
    LOG_DEBUG("Initializing packet queue");
    if(SetupUserSpaceCommunication() != 0){
        LOG_ERROR("Failed to setup user space communication");
        return -1;
    }

    LOG_DEBUG("Initializing netfilter hooks");
    if(SetupNetfilterHooks() != 0){
        LOG_ERROR("Failed to setup netfilter hooks");
        return -1;
    }

    SetInitialized();
    LOG_INFO("✔️  Extrava module loaded  ✔️");
    Activate();
	return 0;
}

static void __exit Exit(void) {
    LOG_INFO("⌛️  Extrava module exiting  ⌛️");
    Deactivate();
    LOG_DEBUG("Cleaning up netfilter hooks");
    CleanupNetfilterHooks();
    LOG_DEBUG("Cleaning up user space communication");
    CleanupUserSpaceCommunication();
	LOG_INFO("🛑  Extrava module unloaded  🛑");
    SetUninitialized();
}

module_init(Initialize);
module_exit(Exit);