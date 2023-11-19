
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

public static class EndiannessHelper {
    public static T ReverseBytes<T>(T value) where T : unmanaged {
        Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));
        bytes.Reverse();
        return value;
    }
}
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RingBufferSlot {
    public ushort ClearanceStartIndex;  // Matches unsigned short in C
    public ushort ClearanceEndIndex;    // Matches unsigned short in C
    public ushort CurrentDataSize;      // Matches unsigned short in C
    public uint TotalDataSize;          // Matches unsigned int in C
    public byte SequenceNumber;         // Matches unsigned char in C
    [MarshalAs(UnmanagedType.U1)]
    public SlotStatus Status;           // Matches SlotStatus (enum) in C

    private fixed byte Data[RingBufferConstants.MAX_PAYLOAD_SIZE]; // Matches unsigned char[MAX_PAYLOAD_SIZE] in C

    public Span<byte> DataSpan {
        get {
            fixed (byte* pData = Data) {
                return new Span<byte>(pData, RingBufferConstants.MAX_PAYLOAD_SIZE);
            }
        }
    }

    public uint ReversedTotalDataSize => EndiannessHelper.ReverseBytes(TotalDataSize);
    public ushort ReversedCurrentDataSize => EndiannessHelper.ReverseBytes(CurrentDataSize);
    public ushort ReversedClearanceEndIndex => EndiannessHelper.ReverseBytes(ClearanceEndIndex);
    public ushort ReversedClearanceStartIndex => EndiannessHelper.ReverseBytes(ClearanceStartIndex);

    public static uint GetSize() {
        // Size of all fields plus the size of the fixed-size Data array
        return sizeof(ushort) * 3  // for 3 ushort fields
            + sizeof(uint)         // for TotalDataSize
            + sizeof(byte)         // for SequenceNumber
            + sizeof(SlotStatus)   // for Status (it's a byte-sized enum)
            + RingBufferConstants.MAX_PAYLOAD_SIZE; // size of the fixed byte array
    }

    public static uint GetTotalSize() {
        return GetSize();
    }
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RingBuffer {
    public uint Position;               // Matches unsigned int in C
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = RingBufferConstants.NUM_SLOTS)]
    public RingBufferSlot* Slots;      // Matches RingBufferSlot[NUM_SLOTS] in C

    public static uint GetSize() {
        return sizeof(uint) + (uint)IntPtr.Size; // Size of uint + size of pointer
    }

    public static uint GetTotalSize() {
        // Size of RingBuffer itself plus all of its RingBufferSlot instances
        return GetSize() + RingBufferSlot.GetTotalSize() * RingBufferConstants.NUM_SLOTS;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct DuplexRingBuffer {
    public RingBuffer* SystemBuffer;
    public RingBuffer* UserBuffer;

    public static uint GetSize() {
        return (uint)IntPtr.Size * 2U; // Two pointers
    }

    public static uint GetTotalSize() {
        // The total size of DuplexRingBuffer includes itself and both RingBuffers
        return GetSize() + 2 * RingBuffer.GetTotalSize();
    }
}

public enum SlotStatus : byte {
    EMPTY = 0,
    VALID = 1,
    ADVANCE_START = 2,
    ADVANCE = 3,
    ADVANCE_END = 4,
    PREPPING = 5
}

public enum RingBufferStatus : byte {
    Inactive = 0,
    Active = 1,
    Full = 2,
    Terminating = 3
}

public class RingBufferConstants {
    public const int NUM_SLOTS = 2048;
    public const int MAX_PAYLOAD_SIZE = 61440;

    // Other reader methods...
}

public unsafe struct DataBuffer {
    public byte* Data;
    public uint Size;
}


[StructLayout(LayoutKind.Sequential, Pack = 1)]
public unsafe struct RingBufferSlotHeader {
    public ushort ClearanceStartIndex;
    public ushort ClearanceEndIndex;
    public ushort CurrentDataSize;
    public uint TotalDataSize;
    public byte SequenceNumber;
    [MarshalAs(UnmanagedType.U1)]
    public SlotStatus Status;

    public ulong Id;

    // private fixed byte Data[RingBufferConstants.MAX_PAYLOAD_SIZE];

    // public Span<byte> DataSpan {
    //     get {
    //         fixed (byte* pData = Data) {
    //             return new Span<byte>(pData, RingBufferConstants.MAX_PAYLOAD_SIZE);
    //         }
    //     }
    // }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct RingBufferHeader {
    public RingBufferStatus Status;
    public uint Position;
}

// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct RingBuffer2 {
//     public uint Position;
//     [MarshalAs(UnmanagedType.ByValArray, SizeConst = RingBufferConstants.NUM_SLOTS)]
//     public RingBufferSlot2[] Slots;
// }

// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct DuplexRingBuffer2 {
//     public readonly RingBuffer2 SystemBuffer;
//     public readonly RingBuffer2 UserBuffer;
// }