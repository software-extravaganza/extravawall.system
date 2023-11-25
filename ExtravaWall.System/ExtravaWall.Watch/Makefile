CC=gcc
CFLAGS=-Wall -Werror -fPIC
BINDIR=lib

# Name of the library
TARGET=$(BINDIR)/ringbuffer_client.so

# Source files
SRC=ringbuffer_client.c

# Object files
OBJ=$(SRC:.c=.o)

# Default target
all: directories $(TARGET)

directories: $(BINDIR)

$(BINDIR):
	mkdir -p $(BINDIR)

# Rule for creating the shared library
$(TARGET): $(OBJ)
	$(CC) -shared -o $@ $^

# Rule for building object files from C source files
%.o: %.c
	$(CC) $(CFLAGS) -c -o $@ $<

# Clean target
clean:
	rm -f $(OBJ) $(TARGET)
	rm -rf $(BINDIR)

# Phony targets
.PHONY: all clean
