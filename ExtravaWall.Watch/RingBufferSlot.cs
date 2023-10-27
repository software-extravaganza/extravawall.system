
using System;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace ExtravaWall.Watch;

public static class SharedMemoryManager {
    private const string LibName = "ringbuffer_client.so";

    [DllImport(LibName)]
    public static extern IntPtr open_shared_memory(string path);

    [DllImport(LibName)]
    public static extern RingBufferSlot read_slot(int index);

    [DllImport(LibName)]
    public static extern void write_slot(int index, RingBufferSlot slot);

    [DllImport(LibName)]
    public static extern void close_shared_memory();

    // Additional methods to wrap read/write operations...
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RingBufferSlot {
    public ushort ClearanceStartIndex;  // Matches unsigned short in C
    public ushort ClearanceEndIndex;    // Matches unsigned short in C
    public SlotStatus Status;           // Matches SlotStatus (enum) in C
    public ushort CurrentDataSize;      // Matches unsigned short in C
    public uint TotalDataSize;          // Matches unsigned int in C
    public byte SequenceNumber;         // Matches unsigned char in C

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = RingBufferConstants.MAX_PAYLOAD_SIZE)]
    public byte[] Data;                 // Matches unsigned char[MAX_PAYLOAD_SIZE] in C
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RingBuffer {
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = RingBufferConstants.NUM_SLOTS)]
    public RingBufferSlot[] Slots;      // Matches RingBufferSlot[NUM_SLOTS] in C
    public uint Position;               // Matches unsigned int in C
}

public enum SlotStatus : byte {
    EMPTY = 0,
    VALID = 1,
    ADVANCE = 2
}

public class RingBufferConstants {
    public const int NUM_SLOTS = 2048;
    public const int MAX_PAYLOAD_SIZE = 61440;

    // Other reader methods...
}