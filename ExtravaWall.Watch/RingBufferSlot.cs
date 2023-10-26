
using System;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ExtravaWall.Watch;

public static class NativeMethods {
    // Constants for mmap
    public const int PROT_READ = 0x1;
    public const int PROT_WRITE = 0x2;
    public const int MAP_SHARED = 0x01;
    public const int O_RDWR = 2;

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int open(string pathname, int flags);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern IntPtr mmap(IntPtr addr, uint length, int prot, int flags, int fd, uint offset);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int munmap(IntPtr addr, uint length);
}

public class SharedMemory : IDisposable {
    public IntPtr sharedMemoryPtr;
    private uint sharedMemorySize;

    public SharedMemory(string devicePath, uint size) {
        sharedMemorySize = size;

        // Open the device
        int fd = NativeMethods.open(devicePath, NativeMethods.O_RDWR);
        if (fd < 0) {
            throw new InvalidOperationException("Failed to open device.");
        }

        // Map the shared memory
        sharedMemoryPtr = NativeMethods.mmap(IntPtr.Zero, sharedMemorySize, NativeMethods.PROT_READ | NativeMethods.PROT_WRITE, NativeMethods.MAP_SHARED, fd, 0);
        if (sharedMemoryPtr == (IntPtr)(-1)) {
            throw new InvalidOperationException("Failed to map shared memory.");
        }

        // Close the device
        NativeMethods.close(fd);
    }

    public RingBuffer? GetIngressBuffer() {
        if (sharedMemoryPtr == IntPtr.Zero) {
            return null;
        }
        Console.WriteLine($"Size of RingBuffer: {Marshal.SizeOf(typeof(RingBuffer))}");
        Console.WriteLine($"Size of RingBufferSlot: {Marshal.SizeOf(typeof(RingBufferSlot))}");
        Console.WriteLine("Memory address of sharedMemoryPtr: " + sharedMemoryPtr.ToString("X"));

        unsafe {
            if (sharedMemoryPtr == IntPtr.Zero) {
                throw new InvalidOperationException("Shared memory pointer is null.");
            }

            RingBuffer* buffer = (RingBuffer*)sharedMemoryPtr.ToPointer();
            if (buffer == null) {
                throw new InvalidOperationException("RingBuffer pointer is null.");
            }
            byte[] ringBuffer = *buffer + RingBufferReader.RING_BUFFER_HEADER_SIZE + RingBufferReader.RING_BUFFER_SLOT_HEADER_SIZE;
            return null;
            // Attempt to read a slot
            try {
                // RingBufferSlot firstSlot = buffer->Slots[0];
                if (sharedMemoryPtr == IntPtr.Zero) {
                    return null;
                }
                //return (RingBuffer?)Marshal.PtrToStructure(sharedMemoryPtr, typeof(RingBuffer));
                // Use firstSlot here...
            } catch (Exception ex) {
                Console.WriteLine($"Error accessing slot: {ex.Message}");
            }
        }

        return null;
    }

    public RingBuffer? GetEgressBuffer() {
        if (sharedMemoryPtr == IntPtr.Zero) {
            return null;
        }

        IntPtr egressPtr = IntPtr.Add(sharedMemoryPtr, Marshal.SizeOf(typeof(RingBuffer)));
        return (RingBuffer?)Marshal.PtrToStructure(egressPtr, typeof(RingBuffer));
    }

    public void Dispose() {
        if (sharedMemoryPtr != IntPtr.Zero) {
            NativeMethods.munmap(sharedMemoryPtr, sharedMemorySize);
            sharedMemoryPtr = IntPtr.Zero;
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RingBufferSlot {
     public ushort ClearanceStartIndex;  // Matches __u16 from C
     public ushort ClearanceEndIndex;    // Matches __u16 from C
     public SlotStatus Status;                 // Matches __u8 from C
     public ushort CurrentDataSize;      // Matches __u16 from C
     public uint TotalDataSize;          // Matches __u32 from C
     public byte SequenceNumber;         // Matches __u8 from C
    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)RingBufferReader.MAX_PAYLOAD_SIZE)]
    // public byte[] Data;
    public fixed byte Data[(int)RingBufferReader.MAX_PAYLOAD_SIZE];

    public RingBufferSlot() {
        // Unsafe.SkipInit(out this.ClearanceStartIndex);
        // Unsafe.SkipInit(out this.ClearanceEndIndex);
        // Unsafe.SkipInit(out this.Status);
        // Unsafe.SkipInit(out this.CurrentDataSize);
        // Unsafe.SkipInit(out this.TotalDataSize);
        // Unsafe.SkipInit(out this.SequenceNumber);
        //Unsafe.SkipInit(out this.Data);
    }
}

public enum SlotStatus : byte {
    EMPTY = 0,
    VALID = 1,
    ADVANCE = 2
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RingBuffer {
    public uint Position;

    // Example size, should match the C definition
    //[MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)RingBufferReader.NUM_SLOTS)]
    //public RingBufferSlot[] Slots;
    public fixed byte Slots[(int)1];

    public RingBufferSlot GetSlot(uint index) {
        fixed (byte* slotsPtr = Slots) {
            return *(RingBufferSlot*)(slotsPtr + index * sizeof(RingBufferSlot));
        }

        //return Slots[index];
    }
    // You can also add methods here for operations on the buffer if needed.
    public RingBuffer() {
        //Unsafe.SkipInit(out this.Position);
        //Unsafe.SkipInit(out this.Slots);
    }
}

public class RingBufferReader {
    public const uint NUM_SLOTS = 2048U;
    public const uint MAX_PAYLOAD_SIZE = 61440U;
    public const uint RING_BUFFER_SLOT_HEADER_SIZE = 11U;
    public const uint RING_BUFFER_SLOT_SIZE = MAX_PAYLOAD_SIZE + RING_BUFFER_SLOT_HEADER_SIZE;
    public const uint RING_BUFFER_HEADER_SIZE = 4U;
    public const uint RING_BUFFER_SIZE = RING_BUFFER_HEADER_SIZE + (RING_BUFFER_SLOT_SIZE * NUM_SLOTS);

    //public const int RING_BUFFER_SLOT_SIZE = sizeof(RingBufferSlot);

    private RingBuffer ingressBuffer;
    private RingBuffer egressBuffer;
    private SharedMemory sharedMemory;

    public RingBufferReader(SharedMemory sharedMemory) {
        this.sharedMemory = sharedMemory;
        var ingressBufferLoaded = sharedMemory.GetIngressBuffer();
        //var egressBufferLoaded = sharedMemory.GetEgressBuffer();

        if (ingressBufferLoaded is null) {
            throw new InvalidOperationException("Ingress buffer is null.");
        }

        // if (egressBufferLoaded is null) {
        //     throw new InvalidOperationException("Egress buffer is null.");
        // }

        ingressBuffer = ingressBufferLoaded.Value;
        //egressBuffer = egressBufferLoaded.Value;
    }

    // public unsafe RingBufferSlot GetSlot(IntPtr ringBufferPtr, int slotIndex) {
    //     long slotSize = sizeof(RingBufferSlot) - RingBufferReader.MAX_PAYLOAD_SIZE; // Size of the slot structure minus the Data array
    //     IntPtr slotPtr = IntPtr.Add(ringBufferPtr, Marshal.SizeOf(typeof(RingBuffer)) + slotIndex * slotSize);

    //     // Marshal the slot data except for the Data array
    //     RingBufferSlot slot = (RingBufferSlot)Marshal.PtrToStructure(slotPtr, typeof(RingBufferSlot));

    //     // Manually copy the Data array
    //     byte* dataPtr = (byte*)(slotPtr + slotSize - RingBufferReader.MAX_PAYLOAD_SIZE).ToPointer();
    //     for (int i = 0; i < RingBufferReader.MAX_PAYLOAD_SIZE; i++) {
    //         slot.Data[i] = dataPtr[i];
    //     }

    //     return slot;
    // }

    public byte[] Read() {
        List<byte> dataRead = new List<byte>();
        uint startIndex = ingressBuffer.Position;
        uint endIndex = startIndex;
        uint? currentIndex = null;
        bool slotFound = false;
        while (!slotFound && currentIndex != startIndex) {
            if (currentIndex == null) {
                currentIndex = ingressBuffer.Position;
            }

            unsafe {

                //RingBufferSlot slot = GetSlot(sharedMemory.sharedMemoryPtr, 1);
                RingBufferSlot slot = ingressBuffer.GetSlot(currentIndex.Value);
                // if (slot.Status == SlotStatus.EMPTY) {
                //     currentIndex = (currentIndex + 1) % RingBufferReader.NUM_SLOTS;
                //     continue;
                // }

                //byte[] data = new byte[RingBufferReader.MAX_PAYLOAD_SIZE];
                //Marshal.Copy((IntPtr)slot.Data, data, 0, RingBufferReader.MAX_PAYLOAD_SIZE);
                // if (slot.Data.Any(b => b != 0)) {
                //     Console.WriteLine("Data found");
                //     slotFound = true;
                // }

                //dataRead.AddRange(data.Take((int)slot.CurrentDataSize));

                if (slot.Status == SlotStatus.VALID) {
                    endIndex = ingressBuffer.Position;
                    AdvanceRingBuffer();
                    break;
                } else if (slot.Status == SlotStatus.ADVANCE) {
                    endIndex = ingressBuffer.Position;
                    AdvanceRingBuffer();
                }
                currentIndex = (currentIndex + 1) % RingBufferReader.NUM_SLOTS;
            }
        }

        // After reading, send clearance indices
        SendClearanceIndices(startIndex, endIndex);

        return dataRead.ToArray();
    }

    private void SendClearanceIndices(uint startIndex, uint endIndex) {
        // Update the egress buffer's clearance indices to indicate which slots have been read
        for (uint i = 0; i < 61451; i++) {
            var slot = egressBuffer.GetSlot(i);
            if (slot.Status != SlotStatus.EMPTY) {
                slot.ClearanceStartIndex = (ushort)startIndex;
                slot.ClearanceEndIndex = (ushort)endIndex;
            }
        }
    }

    private void AdvanceRingBuffer() {
        RingBufferSlot slot = ingressBuffer.GetSlot(ingressBuffer.Position);
        while (slot.Status == SlotStatus.VALID || slot.Status == SlotStatus.ADVANCE) {
            slot.Status = SlotStatus.EMPTY;
            ingressBuffer.Position = (ingressBuffer.Position + 1) % RingBufferReader.NUM_SLOTS;
            slot = ingressBuffer.GetSlot(ingressBuffer.Position);
        }
    }
}


public class RingBufferWriter {
    private RingBuffer egressBuffer;

    public RingBufferWriter(SharedMemory sharedMemory) {
        var egressBufferLoaded = sharedMemory.GetEgressBuffer();
        if (egressBufferLoaded is null) {
            throw new InvalidOperationException("Egress buffer is null.");
        }

        egressBuffer = egressBufferLoaded.Value;
    }

}
