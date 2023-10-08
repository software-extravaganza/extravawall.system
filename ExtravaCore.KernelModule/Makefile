basedir = $(PWD)
srcdir = $(basedir)/src
incdir = $(basedir)/include
builddir = $(basedir)/bin
obj-m += extrava.o

all:
	make -C /lib/modules/$(shell uname -r)/build M=$(basedir) modules

clean:
	make -C /lib/modules/$(shell uname -r)/build M=$(basedir) clean