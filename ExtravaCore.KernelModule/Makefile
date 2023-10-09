basedir = $(PWD)
srcdir = $(basedir)/src
incdir = $(basedir)/include
builddir = $(basedir)/bin

# Module object files.
obj-m += extrava.o

# Individual object files that make up the extrava module.
extrava-objs := main_extrava.o logger.o packet_queue.o data_structures.o netfilter_hooks.o userspace_comm.o

all:
	make -C /lib/modules/$(shell uname -r)/build M=$(basedir) modules

clean:
	make -C /lib/modules/$(shell uname -r)/build M=$(basedir) clean
