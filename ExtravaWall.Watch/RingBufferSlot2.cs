
using System;
using System.Runtime.InteropServices;
using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Text;
using System.Buffers.Binary;

namespace ExtravaWall.Watch;
public static class SpanExtensions {
    public static Span<T> ReverseAndReturn<T>(this Span<T> span) where T : struct {
        for (int i = 0; i < span.Length / 2; i++) {
            T temp = span[i];
            span[i] = span[span.Length - i - 1];
            span[span.Length - i - 1] = temp;
        }

        return span;
    }
}
public static class NativeMethods2 {
    // Constants for mmap

    private const string LibName = "ringbuffer_client.so";
    const int _SC_PAGESIZE = 30;
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

    [DllImport("libc.so.6")]
    private static extern long sysconf(int name);

    // int get_size_for_slot_header_status() {
    //     return SLOT_HEADER_STATUS_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_status();


    // int get_size_for_slot_header_total_data_size() {
    //     return SLOT_HEADER_TOTAL_DATA_SIZE_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_total_data_size();

    // int get_size_for_slot_header_current_data_size() {
    //     return SLOT_HEADER_CURRENT_DATA_SIZE_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_current_data_size();

    // int get_size_for_slot_header_sequence_number() {
    //     return SLOT_HEADER_SEQUENCE_NUMBER_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_sequence_number();

    // int get_size_for_slot_header_clearance_start_index() {
    //     return SLOT_HEADER_CLEARANCE_START_INDEX_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_clearance_start_index();

    // int get_size_for_slot_header_clearance_end_index() {
    //     return SLOT_HEADER_CLEARANCE_END_INDEX_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header_clearance_end_index();

    // int get_size_for_slot_header() {
    //     return SLOT_HEADER_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_header();

    // int get_size_for_slot_data() {
    //     return SLOT_DATA_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot_data();

    // int get_size_for_slot() {
    //     return SLOT_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_slot();

    // int get_size_for_ring_buffer_header_status() {
    //     return RING_BUFFER_HEADER_STATUS_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_ring_buffer_header_status();

    // int get_size_for_ring_buffer_header_position() {
    //     return RING_BUFFER_HEADER__POSITION_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_ring_buffer_header_position();

    // int get_size_for_ring_buffer_header() {
    //     return RING_BUFFER_HEADER_SIZE;
    // }
    [DllImport(LibName)]
    public static extern int get_size_for_ring_buffer_header();

    // int get_size_for_ring_buffer_data() {
    //     return RING_BUFFER_DATA_SIZE;
    // }
    [DllImport(LibName)]
    public static extern uint get_size_for_ring_buffer_data();

    // int get_size_for_ring_buffer() {
    //     return RING_BUFFER_SIZE;
    // }
    [DllImport(LibName)]
    public static extern uint get_size_for_ring_buffer();

    // int get_size_for_duplex_ring_buffer() {
    //     return DUPLEX_RING_BUFFER_SIZE;
    // }
    [DllImport(LibName)]
    public static extern uint get_size_for_duplex_ring_buffer();

    // int get_size_for_duplex_ring_buffer_aligned() {
    //     return DUPLEX_RING_BUFFER_ALIGNED_SIZE;
    // }
    [DllImport(LibName)]
    public static extern uint get_size_for_duplex_ring_buffer_aligned();

    [DllImport(LibName)]
    public static extern ushort get_number_of_slots();

    [DllImport(LibName)]
    public static extern ushort get_size_for_slot_header_id();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_status();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_id();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_total_data_size();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_current_data_size();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_sequence_number();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_clearance_start_index();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_header_clearance_end_index();

    [DllImport(LibName)]
    public static extern int get_offset_for_slot_data();


    public static uint GetPageSize() {
        return (uint)sysconf(_SC_PAGESIZE);
    }

    public static uint PageAlign(uint size) {
        uint pageSize = GetPageSize();
        return (size + (pageSize - 1)) & ~(pageSize - 1);
    }
}


public unsafe class SharedMemory2 : IDisposable {
    private readonly IntPtr sharedMemoryPtr;
    private uint sharedMemorySize;
    private ulong UserRingBufferSentCount;
    public readonly byte* DuplexBuffer;
    private Logger logger;

    public int SLOT_HEADER_STATUS_SIZE { get; }
    public int SLOT_HEADER_TOTAL_DATA_SIZE_SIZE { get; }
    public int SLOT_HEADER_CURRENT_DATA_SIZE_SIZE { get; }
    public int SLOT_HEADER_SEQUENCE_NUMBER_SIZE { get; }
    public int SLOT_HEADER_CLEARANCE_START_INDEX_SIZE { get; }
    public int SLOT_HEADER_CLEARANCE_END_INDEX_SIZE { get; }
    public int SLOT_HEADER_SIZE { get; }
    public int SLOT_DATA_SIZE { get; }
    public int SLOT_SIZE { get; }
    public int RING_BUFFER_HEADER_STATUS_SIZE { get; }
    public int RING_BUFFER_HEADER__POSITION_SIZE { get; }
    public int RING_BUFFER_HEADER_SIZE { get; }
    public uint RING_BUFFER_DATA_SIZE { get; }
    public uint RING_BUFFER_SIZE { get; }
    public uint DUPLEX_RING_BUFFER_SIZE { get; }
    public uint DUPLEX_RING_BUFFER_ALIGNED_SIZE { get; }

    public int SLOT_HEADER_STATUS_OFFSET { get; }
    public int SLOT_HEADER_ID_OFFSET { get; }
    public int SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET { get; }
    public int SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET { get; }
    public int SLOT_HEADER_SEQUENCE_NUMBER_OFFSET { get; }
    public int SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET { get; }
    public int SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET { get; }
    public int SLOT_DATA_OFFSET { get; }

    public ushort NUM_SLOTS { get; }

    public ushort SLOT_HEADER_ID_SIZE { get; }

    unsafe void ReadFromBuffer(byte* dest, long offset, long length) {
        Thread.MemoryBarrier();
        if (DuplexBuffer != null && dest != null) {
            for (int i = 0; i < length; i++) {
                dest[i] = DuplexBuffer[offset + i];
            }
        }
    }

    unsafe void WriteToBufferUnsafe(byte* src, long offset, long length) {
        if (DuplexBuffer == null) {
            logger.Log("RingBuf: DuplexBuffer is NULL");
            return;
        }

        if (src == null) {
            logger.Log("RingBuf: src is NULL");
            return;
        }

        if (offset + length > DUPLEX_RING_BUFFER_ALIGNED_SIZE) {
            logger.Log("RingBuf: offset+length > DUPLEX_RING_BUFFER_ALIGNED_SIZE");
            return;
        }

        for (long i = 0; i < length; i++) {
            DuplexBuffer[offset + i] = src[i];
        }
        Thread.MemoryBarrier();
    }

    void WriteToBuffer(byte[] src, long offset) {
        if (DuplexBuffer == null) {
            logger.Log("RingBuf: DuplexBuffer is NULL");
            return;
        }

        if (src == null) {
            logger.Log("RingBuf: src is NULL");
            return;
        }

        if (offset + src.Length > DUPLEX_RING_BUFFER_ALIGNED_SIZE) {
            logger.Log("RingBuf: offset+length > DUPLEX_RING_BUFFER_ALIGNED_SIZE");
            return;
        }

        for (long i = 0; i < src.Length; i++) {
            DuplexBuffer[offset + i] = src[i];
        }
        Thread.MemoryBarrier();
    }

    unsafe RingBufferHeader ReadRingBufferHeader(uint offset, bool system) {
        RingBufferHeader header = new RingBufferHeader();
        Span<byte> buffer = stackalloc byte[RING_BUFFER_HEADER_SIZE]; // Buffer to hold read data

        fixed (byte* bufferPtr = buffer) {
            ReadFromBuffer(bufferPtr, offset, 5);

            header.Status = (RingBufferStatus)buffer[0];
            var headerPosition = buffer.Slice(1, RING_BUFFER_HEADER__POSITION_SIZE);
            //if (!system) {
            //headerPosition.Reverse();
            //}
            header.Position = BitConverter.ToUInt32(headerPosition);
        }

        return header;
    }

    unsafe void WriteRingBufferHeader(uint offset, RingBufferHeader header, bool system) {
        ProcessUInt32AsBytes(header.Position, false, span => {
            Span<byte> buffer = stackalloc byte[5]; // Buffer to hold data
            buffer[0] = (byte)header.Status;
            var headerPosition = buffer.Slice(1, RING_BUFFER_HEADER__POSITION_SIZE);
            //if (!system) {
            //span.Reverse();
            //}
            span.CopyTo(headerPosition);

            ByteSpanAsPointer(buffer, bufferPtr => {
                WriteToBufferUnsafe(bufferPtr, offset, 5);
            });
        });
    }

    unsafe RingBufferStatus ReadRingBufferStatus(uint offset) {
        RingBufferStatus status = RingBufferStatus.Inactive;
        ReadFromBuffer((byte*)&status, offset, RING_BUFFER_HEADER_STATUS_SIZE);
        return status;
    }

    unsafe RingBufferStatus ReadSystemRingBufferStatus() {
        return ReadRingBufferStatus(0);
    }

    unsafe RingBufferStatus ReadUserRingBufferStatus() {
        return ReadRingBufferStatus(RING_BUFFER_SIZE);
    }

    unsafe void WriteSystemRingBufferStatus(RingBufferStatus status) {
        WriteToBufferUnsafe((byte*)&status, 0, RING_BUFFER_HEADER_STATUS_SIZE);
    }

    unsafe void WriteUserRingBufferStatus(RingBufferStatus status) {
        WriteToBufferUnsafe((byte*)&status, RING_BUFFER_SIZE, RING_BUFFER_HEADER_STATUS_SIZE);
    }

    public SlotStatus ReadSystemRingBufferSlotStatus(int slotIndex) {
        return ReadRingBufferSlotStatus(0, slotIndex);
    }

    public SlotStatus ReadUserRingBufferSlotStatus(int slotIndex) {
        return ReadRingBufferSlotStatus(RING_BUFFER_SIZE, slotIndex);
    }

    public void WriteSystemRingBufferSlotStatus(uint slotIndex, SlotStatus status) {
        WriteRingBufferSlotStatus(0, slotIndex, status, true);
    }

    public void WriteUserRingBufferSlotStatus(uint slotIndex, SlotStatus status) {
        WriteRingBufferSlotStatus(RING_BUFFER_SIZE, slotIndex, status, false);
    }

    public int ReadRingBufferPosition(uint offset, bool system) {
        int position = 0;
        byte* positionBytes = (byte*)&position;
        ReadFromBuffer(positionBytes, offset + (uint)RING_BUFFER_HEADER_STATUS_SIZE, (uint)RING_BUFFER_HEADER__POSITION_SIZE);
        //ReverseBytesForPointer(positionBytes, RING_BUFFER_HEADER__POSITION_SIZE, system);
        return position;
    }

    public bool IsLittleEndian(bool reading) {
        if (reading) {
            return BitConverter.IsLittleEndian;
        } else {
            return BitConverter.IsLittleEndian;
        }
    }
    private void ReverseBytesForPointer(byte* positionBytes, int size, bool system) {
        // if (system) {
        //     return;
        // }

        var tempBytes = new byte[size];
        for (int i = 0; i < size; i++) {
            tempBytes[i] = positionBytes[size - i - 1];
        }

        //tempBytes.Reverse();
        for (int i = 0; i < size; i++) {
            positionBytes[i] = tempBytes[i];
        }
    }
    private void ReverseBytesForPointerIfNeeded(byte* positionBytes, int size, bool reading, bool system) {
        if (IsLittleEndian(reading)) { // Adjust this according to your buffer's endianness
            ReverseBytesForPointer(positionBytes, size, system);
        }
    }

    public int ReadSystemRingBufferPosition() {
        return ReadRingBufferPosition(0, true);
    }

    public int ReadUserRingBufferPosition() {
        return ReadRingBufferPosition(RING_BUFFER_SIZE, false);
    }

    public void ByteSpanAsPointer(Span<byte> span, BytePointerAction action) {
        fixed (byte* bytePtr = span) {
            action(bytePtr);
        }
    }

    public delegate void ByteAction(Span<byte> span);
    public delegate void ByteWithBufferAction(Span<byte> buffer, Span<byte> span);
    public void ProcessUInt32AsBytes(uint value, bool reading, ByteAction action) {
        Span<byte> uintBytes = stackalloc byte[4];
        if (IsLittleEndian(reading)) {
            BinaryPrimitives.WriteUInt32LittleEndian(uintBytes, value);
        } else {
            BinaryPrimitives.WriteUInt32BigEndian(uintBytes, value);
        }

        action(uintBytes);
    }


    public void ProcessUInt64AsBytesWithBuffer(Span<byte> buffer, ulong value, bool reading, ByteWithBufferAction action) {
        Span<byte> uintBytes = stackalloc byte[8];
        if (IsLittleEndian(reading)) {
            BinaryPrimitives.WriteUInt64LittleEndian(uintBytes, value);
        } else {
            BinaryPrimitives.WriteUInt64BigEndian(uintBytes, value);
        }

        action(buffer, uintBytes);
    }

    public void ProcessUInt32AsBytesWithBuffer(Span<byte> buffer, uint value, bool reading, ByteWithBufferAction action) {
        Span<byte> uintBytes = stackalloc byte[4];
        if (IsLittleEndian(reading)) {
            BinaryPrimitives.WriteUInt32LittleEndian(uintBytes, value);
        } else {
            BinaryPrimitives.WriteUInt32BigEndian(uintBytes, value);
        }

        action(buffer, uintBytes);
    }

    public void ProcessUShortAsBytesWithBuffer(Span<byte> buffer, ushort value, bool reading, ByteWithBufferAction action) {
        Span<byte> uintBytes = stackalloc byte[2];
        if (IsLittleEndian(reading)) {
            BinaryPrimitives.WriteUInt16LittleEndian(uintBytes, value);
        } else {
            BinaryPrimitives.WriteUInt16BigEndian(uintBytes, value);
        }

        action(buffer, uintBytes);
    }

    public delegate void BytePointerAction(byte* pointer);
    public delegate void BytePointerWithBufferAction(Span<byte> buffer, byte* pointer);
    public void ProcessUInt32AsBytePointer(uint value, bool reading, BytePointerAction action) {
        ProcessUInt32AsBytes(value, reading, uintBytes => {
            fixed (byte* bytePtr = uintBytes) {
                action(bytePtr);
            }
        });
    }
    public void WriteRingBufferPosition(uint offset, uint position, bool system) {
        ProcessUInt32AsBytePointer(position, false, (byte* pointer) => {
            //ReverseBytesForPointer(pointer, (int)RING_BUFFER_HEADER__POSITION_SIZE, system);
            WriteToBufferUnsafe(pointer, offset + (uint)RING_BUFFER_HEADER_STATUS_SIZE, (uint)RING_BUFFER_HEADER__POSITION_SIZE);
        });
    }

    public void WriteSystemRingBufferPosition(int position) {
        WriteRingBufferPosition(0, (uint)position, true);
    }

    public void WriteUserRingBufferPosition(int position) {
        WriteRingBufferPosition(RING_BUFFER_SIZE, (uint)position, false);
    }

    public RingBufferSlotHeader ReadRingBufferSlotHeader(uint offset, int slotIndex, bool isSharedMemoryBigEndian) {
        RingBufferSlotHeader header = new RingBufferSlotHeader();
        Span<byte> buffer = stackalloc byte[SLOT_HEADER_SIZE]; // Buffer to hold read data

        fixed (byte* bufferPtr = buffer) {
            uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (uint)slotIndex * (uint)SLOT_SIZE;
            ReadFromBuffer(bufferPtr, baseOffset, SLOT_HEADER_SIZE);

            header.Status = (SlotStatus)buffer[SLOT_HEADER_STATUS_OFFSET];
            header.Id = ReadUInt64(buffer.Slice(SLOT_HEADER_ID_OFFSET, SLOT_HEADER_ID_SIZE), isSharedMemoryBigEndian);
            header.TotalDataSize = ReadUInt32(buffer.Slice(SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE), isSharedMemoryBigEndian);
            header.CurrentDataSize = ReadUInt16(buffer.Slice(SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE), isSharedMemoryBigEndian);
            header.SequenceNumber = buffer[SLOT_HEADER_SEQUENCE_NUMBER_OFFSET];
            header.ClearanceStartIndex = ReadUInt16(buffer.Slice(SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE), isSharedMemoryBigEndian);
            header.ClearanceEndIndex = ReadUInt16(buffer.Slice(SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE), isSharedMemoryBigEndian);
        }

        return header;
    }

    private static ulong ReadUInt64(Span<byte> data, bool isSharedMemoryBigEndian) {
        if (isSharedMemoryBigEndian == BitConverter.IsLittleEndian) {
            data.Reverse();
        }
        return BitConverter.ToUInt64(data);
    }

    private static uint ReadUInt32(Span<byte> data, bool isSharedMemoryBigEndian) {
        if (isSharedMemoryBigEndian == BitConverter.IsLittleEndian) {
            data.Reverse();
        }
        return BitConverter.ToUInt32(data);
    }

    private static ushort ReadUInt16(Span<byte> data, bool isSharedMemoryBigEndian) {
        if (isSharedMemoryBigEndian == BitConverter.IsLittleEndian) {
            data.Reverse();
        }
        return BitConverter.ToUInt16(data);
    }

    public RingBufferSlotHeader ReadSystemRingBufferSlotHeader(int slotIndex) {
        return ReadRingBufferSlotHeader(0, slotIndex, false);
    }

    public RingBufferSlotHeader ReadUserRingBufferSlotHeader(int slotIndex) {
        return ReadRingBufferSlotHeader(RING_BUFFER_SIZE, slotIndex, true);
    }



    public void WriteRingBufferSlotHeader(uint offset, int slotIndex, RingBufferSlotHeader slot_header, bool system) {
        Span<byte> buffer = stackalloc byte[SLOT_HEADER_SIZE]; // Buffer to hold data

        // ProcessUInt64AsBytesWithBuffer(buffer, slot_header.Id, false, (b, span) => {
        //     logger.Log("Header ID bytes: " + BitConverter.ToString(span.ToArray()).Replace("-", " "));
        //     span.Slice(0, SLOT_HEADER_ID_SIZE).CopyTo(b.Slice(SLOT_HEADER_ID_OFFSET, SLOT_HEADER_ID_SIZE));
        // });

        ProcessUInt32AsBytesWithBuffer(buffer, slot_header.TotalDataSize, false, (b, span) => {
            logger.Log("Total data size bytes: " + BitConverter.ToString(span.ToArray()).Replace("-", " "));
            span.Slice(0, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE).CopyTo(b.Slice(SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET, SLOT_HEADER_TOTAL_DATA_SIZE_SIZE));
        });

        ProcessUShortAsBytesWithBuffer(buffer, slot_header.CurrentDataSize, false, (b, span) => {
            logger.Log("Current data size bytes: " + BitConverter.ToString(span.ToArray()).Replace("-", " "));
            span.Slice(0, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE).CopyTo(b.Slice(SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET, SLOT_HEADER_CURRENT_DATA_SIZE_SIZE));
        });

        buffer[15] = slot_header.SequenceNumber;
        ProcessUShortAsBytesWithBuffer(buffer, slot_header.ClearanceStartIndex, false, (b, span) => {
            logger.Log("Clearance start index bytes: " + BitConverter.ToString(span.ToArray()).Replace("-", " "));
            span.Slice(0, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE).CopyTo(b.Slice(SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_START_INDEX_SIZE));
        });

        ProcessUShortAsBytesWithBuffer(buffer, slot_header.ClearanceEndIndex, false, (b, span) => {
            logger.Log("Clearance end index bytes: " + BitConverter.ToString(span.ToArray()).Replace("-", " "));
            span.Slice(0, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE).CopyTo(b.Slice(SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET, SLOT_HEADER_CLEARANCE_END_INDEX_SIZE));
        });

        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + ((uint)slotIndex * (uint)SLOT_SIZE);
        WriteToBuffer(buffer.ToArray(), baseOffset);

        WriteRingBufferSlotStatus(offset, (uint)slotIndex, slot_header.Status, system);
    }

    public void WriteSystemRingBufferSlotHeader(int slotIndex, RingBufferSlotHeader slot_header) {
        WriteRingBufferSlotHeader(0, slotIndex, slot_header, true);
    }

    public void WriteUserRingBufferSlotHeader(int slotIndex, RingBufferSlotHeader slot_header) {
        WriteRingBufferSlotHeader(RING_BUFFER_SIZE, slotIndex, slot_header, false);
    }

    public SlotStatus ReadRingBufferSlotStatus(uint offset, int slotIndex) {
        SlotStatus status = SlotStatus.EMPTY;
        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (uint)slotIndex * (uint)SLOT_SIZE;
        ReadFromBuffer((byte*)&status, baseOffset, SLOT_HEADER_STATUS_SIZE);
        return status;
    }

    public ulong ReadRingBufferSlotId(uint offset, int slotIndex, bool system) {
        ulong id = 0;
        byte* idBytes = (byte*)&id;
        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (uint)slotIndex * (uint)SLOT_SIZE + (uint)SLOT_HEADER_STATUS_SIZE;
        ReadFromBuffer(idBytes, baseOffset, (uint)RING_BUFFER_HEADER__POSITION_SIZE);
        //ReverseBytesForPointer(idBytes, SLOT_HEADER_ID_SIZE, system);
        return id;
    }

    public ulong ReadSystemRingBufferSlotId(int slotIndex) {
        return ReadRingBufferSlotId(0, slotIndex, true);
    }

    public ulong ReadUserRingBufferSlotId(int slotIndex) {
        return ReadRingBufferSlotId(RING_BUFFER_SIZE, slotIndex, false);
    }

    public void WriteRingBufferSlotStatus(uint offset, uint slotIndex, SlotStatus status, bool system) {
        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (slotIndex * (uint)SLOT_SIZE);
        var oldSlotStatus = ReadRingBufferSlotStatus(offset, (int)slotIndex);
        if (oldSlotStatus != status) {
            if (status == SlotStatus.VALID && !system) {
                UserRingBufferSentCount++;
                logger.Log("Slot ID " + UserRingBufferSentCount);
                WriteRingBufferSlotId(offset, slotIndex, UserRingBufferSentCount, false);
                //WriteRingBufferPosition(offset, ++slotIndex, false);
            } else if (status == SlotStatus.EMPTY && !system) {
                WriteRingBufferSlotId(offset, slotIndex, 0, false);
                //WriteRingBufferPosition(offset, ++slotIndex, false);
            }

            WriteToBufferUnsafe((byte*)&status, baseOffset, SLOT_HEADER_STATUS_SIZE);
        }
    }

    public void WriteRingBufferSlotId(uint offset, uint slotIndex, ulong id, bool system) {
        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (slotIndex * (uint)SLOT_SIZE) + (uint)SLOT_HEADER_STATUS_SIZE;
        byte* idBytes = (byte*)&id;
        //ReverseBytesForPointer(idBytes, SLOT_HEADER_ID_SIZE, system);
        WriteToBufferUnsafe((byte*)&id, baseOffset, SLOT_HEADER_ID_SIZE);
    }

    public void WriteSystemRingBufferSlotId(uint slotIndex, ulong id) {
        WriteRingBufferSlotId(0, slotIndex, id, true);
    }

    public void WriteUserRingBufferSlotId(uint slotIndex, ulong id) {
        WriteRingBufferSlotId(RING_BUFFER_SIZE, slotIndex, id, false);
    }

    public byte* ReadRingBufferSlotData(uint offset, int slotIndex, ushort dataSize) {
        byte* data = (byte*)Marshal.AllocHGlobal(dataSize);
        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + ((uint)slotIndex * (uint)SLOT_SIZE) + (uint)SLOT_HEADER_SIZE;
        ReadFromBuffer(data, baseOffset, dataSize);
        return data;
    }

    public byte* ReadSystemRingBufferSlotData(int slotIndex, ushort dataSize) {
        return ReadRingBufferSlotData(0, slotIndex, dataSize);
    }

    public byte* ReadUserRingBufferSlotData(int slotIndex, ushort dataSize) {
        return ReadRingBufferSlotData(RING_BUFFER_SIZE, slotIndex, dataSize);
    }

    public void WriteRingBufferSlotData(uint offset, uint slotIndex, Span<byte> data, ushort dataSize) {
        byte* dataPtr = (byte*)Marshal.AllocHGlobal(dataSize);
        // if (IsLittleEndian(false)) {
        //     data.Reverse();
        //     CopyData(data, dataPtr, 0, dataSize);
        // } else {
        CopyData(data, dataPtr, 0, dataSize);
        //}

        uint baseOffset = offset + (uint)RING_BUFFER_HEADER_SIZE + (slotIndex * (uint)SLOT_SIZE) + (uint)SLOT_HEADER_SIZE;
        WriteToBufferUnsafe(dataPtr, baseOffset, dataSize);
    }

    public void WriteSystemRingBufferSlotData(uint slotIndex, Span<byte> data, ushort dataSize) {
        WriteRingBufferSlotData(0, slotIndex, data, dataSize);
    }

    public void WriteUserRingBufferSlotData(uint slotIndex, Span<byte> data, ushort dataSize) {
        WriteRingBufferSlotData(RING_BUFFER_SIZE, slotIndex, data, dataSize);
    }

    public unsafe void CopyData(byte* source, byte* destination, int destinationOffset, int bytesToCopy) {
        // Pointer arithmetic is used to calculate the offset in the destination
        byte* destinationPtr = destination + destinationOffset;

        // Copying the data from source to the destination at the given offset
        Buffer.MemoryCopy(source, destinationPtr, bytesToCopy, bytesToCopy);
    }

    public unsafe void CopyData(byte* source, byte[] destination, int destinationOffset, int bytesToCopy) {
        // Ensure not copying beyond the bounds of the destination array
        if (destinationOffset + bytesToCopy > destination.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesToCopy), "Attempting to write beyond the bounds of the destination array.");

        // Copy data from unmanaged memory (pointed to by source) to managed array (destination)
        Marshal.Copy((IntPtr)source, destination, destinationOffset, bytesToCopy);
    }

    public unsafe void CopyData(byte[] source, byte* destination, int destinationOffset, int bytesToCopy) {
        // Ensure not copying beyond the bounds of the source array
        if (bytesToCopy > source.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesToCopy), "Attempting to copy more bytes than are available in the source array.");

        // Getting a pointer to the destination offset
        byte* destinationWithOffset = destination + destinationOffset;

        // Copy data from managed array (source) to unmanaged memory (destinationWithOffset)
        Marshal.Copy(source, 0, (IntPtr)destinationWithOffset, bytesToCopy);
    }

    public unsafe void CopyData(Span<byte> source, byte* destination, int destinationOffset, int bytesToCopy) {
        // Ensure not copying beyond the bounds of the source span
        if (bytesToCopy > source.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesToCopy), "Attempting to copy more bytes than are available in the source span.");

        // Pointer to the destination at the desired offset
        byte* destWithOffset = destination + destinationOffset;

        // Manually copy each byte
        for (int i = 0; i < bytesToCopy; i++) {
            destWithOffset[i] = source[i];
        }
    }
    public unsafe void CopyData(byte* source, Span<byte> destination, int bytesToCopy) {
        // Ensure not copying beyond the bounds of the destination span
        if (bytesToCopy > destination.Length)
            throw new ArgumentOutOfRangeException(nameof(bytesToCopy), "Attempting to write beyond the bounds of the destination span.");

        // Copy data from unmanaged memory (pointed to by source) to the span (destination)
        Span<byte> sourceSpan = new Span<byte>(source, bytesToCopy);
        sourceSpan.CopyTo(destination);
    }

    static IntPtr OffsetOf(IntPtr basePtr, long offset) {
        // Safely handle the offset depending on the platform
        if (IntPtr.Size == 8) {
            // 64-bit
            return new IntPtr(basePtr.ToInt64() + offset);
        } else if (IntPtr.Size == 4) {
            // 32-bit (check for overflow)
            return new IntPtr(checked(basePtr.ToInt32() + (int)offset));
        } else {
            throw new InvalidOperationException("Unsupported platform");
        }
    }
    static T* ToPointer<T>(IntPtr address) where T : unmanaged {
        return (T*)address.ToPointer();
    }

    public SharedMemory2(Logger logger, string devicePath) {
        this.logger = logger;
        SLOT_HEADER_STATUS_SIZE = NativeMethods2.get_size_for_slot_header_status();
        SLOT_HEADER_TOTAL_DATA_SIZE_SIZE = NativeMethods2.get_size_for_slot_header_total_data_size();
        SLOT_HEADER_CURRENT_DATA_SIZE_SIZE = NativeMethods2.get_size_for_slot_header_current_data_size();
        SLOT_HEADER_SEQUENCE_NUMBER_SIZE = NativeMethods2.get_size_for_slot_header_sequence_number();
        SLOT_HEADER_CLEARANCE_START_INDEX_SIZE = NativeMethods2.get_size_for_slot_header_clearance_start_index();
        SLOT_HEADER_CLEARANCE_END_INDEX_SIZE = NativeMethods2.get_size_for_slot_header_clearance_end_index();
        SLOT_HEADER_SIZE = NativeMethods2.get_size_for_slot_header();
        SLOT_DATA_SIZE = NativeMethods2.get_size_for_slot_data();
        SLOT_SIZE = NativeMethods2.get_size_for_slot();
        RING_BUFFER_HEADER_STATUS_SIZE = NativeMethods2.get_size_for_ring_buffer_header_status();
        RING_BUFFER_HEADER__POSITION_SIZE = NativeMethods2.get_size_for_ring_buffer_header_position();
        RING_BUFFER_HEADER_SIZE = NativeMethods2.get_size_for_ring_buffer_header();
        RING_BUFFER_DATA_SIZE = NativeMethods2.get_size_for_ring_buffer_data();
        RING_BUFFER_SIZE = NativeMethods2.get_size_for_ring_buffer();
        DUPLEX_RING_BUFFER_SIZE = NativeMethods2.get_size_for_duplex_ring_buffer();
        DUPLEX_RING_BUFFER_ALIGNED_SIZE = NativeMethods2.get_size_for_duplex_ring_buffer_aligned();
        NUM_SLOTS = NativeMethods2.get_number_of_slots();
        SLOT_HEADER_ID_SIZE = NativeMethods2.get_size_for_slot_header_id();
        SLOT_HEADER_STATUS_OFFSET = NativeMethods2.get_offset_for_slot_header_status();
        SLOT_HEADER_ID_OFFSET = NativeMethods2.get_offset_for_slot_header_id();
        SLOT_HEADER_TOTAL_DATA_SIZE_OFFSET = NativeMethods2.get_offset_for_slot_header_total_data_size();
        SLOT_HEADER_CURRENT_DATA_SIZE_OFFSET = NativeMethods2.get_offset_for_slot_header_current_data_size();
        SLOT_HEADER_SEQUENCE_NUMBER_OFFSET = NativeMethods2.get_offset_for_slot_header_sequence_number();
        SLOT_HEADER_CLEARANCE_START_INDEX_OFFSET = NativeMethods2.get_offset_for_slot_header_clearance_start_index();
        SLOT_HEADER_CLEARANCE_END_INDEX_OFFSET = NativeMethods2.get_offset_for_slot_header_clearance_end_index();
        SLOT_DATA_OFFSET = NativeMethods2.get_offset_for_slot_data();

        Console.WriteLine($"Size of DuplexRingBuffer: {DUPLEX_RING_BUFFER_SIZE}");
        Console.WriteLine($"Size of DuplexRingBuffer page aligned: {DUPLEX_RING_BUFFER_ALIGNED_SIZE}");
        Console.WriteLine($"Size of RingBuffer: {RING_BUFFER_SIZE}");
        //Console.WriteLine($"Size of RingBuffer page aligned: {GetSizeForRingBufferAligned()}");
        Console.WriteLine($"Size of RingBufferSlot: {SLOT_SIZE}");

        sharedMemorySize = DUPLEX_RING_BUFFER_ALIGNED_SIZE;

        // Open the device
        int fd = NativeMethods2.open(devicePath, NativeMethods2.O_RDWR);
        if (fd < 0) {
            throw new InvalidOperationException("Failed to open device.");
        }

        // // Map the shared memory
        sharedMemoryPtr = NativeMethods2.mmap(IntPtr.Zero, sharedMemorySize, NativeMethods2.PROT_READ | NativeMethods2.PROT_WRITE, NativeMethods2.MAP_SHARED, fd, 0);
        if (sharedMemoryPtr == (IntPtr)(-1)) {
            throw new InvalidOperationException("Failed to map shared memory.");
        }
        Console.WriteLine("Memory address of sharedMemoryPtr: " + sharedMemoryPtr.ToString("X"));

        // IntPtr systemBufferPtr = OffsetOf(sharedMemoryPtr, IntPtr.Size * 2);
        // IntPtr userBufferPtr = OffsetOf(systemBufferPtr, sizeof(uint) + IntPtr.Size);
        // IntPtr systemSlotsPtr = OffsetOf(userBufferPtr, sizeof(uint) + IntPtr.Size);
        // IntPtr userSlotsPtr = OffsetOf(systemSlotsPtr, RingBufferSlot.GetTotalSize() * RingBufferConstants.NUM_SLOTS);

        //DuplexBuffer = (DuplexRingBuffer2)Marshal.PtrToStructure(sharedMemoryPtr, typeof(DuplexRingBuffer2));
        DuplexBuffer = (byte*)sharedMemoryPtr.ToPointer();
        // DuplexBuffer->SystemBuffer = ToPointer<RingBuffer>(systemBufferPtr);
        // DuplexBuffer->UserBuffer = ToPointer<RingBuffer>(userBufferPtr);
        // DuplexBuffer->SystemBuffer->Slots = ToPointer<RingBufferSlot>(systemSlotsPtr);
        // DuplexBuffer->UserBuffer->Slots = ToPointer<RingBufferSlot>(userSlotsPtr);

        // EgressBuffer = DuplexBuffer.UserBuffer;
        // IngressBuffer = DuplexBuffer.SystemBuffer;
        // Close the device
        NativeMethods2.close(fd);
    }


    // public DuplexRingBuffer* GetDuplexBuffer() {
    //     if (sharedMemoryPtr == IntPtr.Zero) {
    //         throw new InvalidOperationException("Shared memory pointer is null.");
    //     }

    //     // logger.Log($"Size of DuplexRingBuffer: {Marshal.SizeOf(typeof(DuplexRingBuffer))}");
    //     // logger.Log($"Size of DuplexRingBuffer page aligned: {GetPageAlignedDuplexBufferSize()}");
    //     // logger.Log($"Size of RingBuffer: {Marshal.SizeOf(typeof(RingBuffer))}");
    //     // logger.Log($"Size of RingBuffer page aligned: {GetPageAlignedBufferSize()}");
    //     // logger.Log($"Size of RingBufferSlot: {Marshal.SizeOf(typeof(RingBufferSlot))}");
    //     // logger.Log("Memory address of sharedMemoryPtr: " + sharedMemoryPtr.ToString("X"));

    //     try {
    //         //return (DuplexRingBuffer)Marshal.PtrToStructure(sharedMemoryPtr, typeof(DuplexRingBuffer));

    //         return *(DuplexRingBuffer*)sharedMemoryPtr.ToPointer();

    //     } catch (Exception ex) {
    //         logger.Log($"Error accessing slot: {ex.Message}");
    //     }

    //     throw new InvalidOperationException("Failed to get duplex buffer.");
    // }
    // public ref DuplexRingBuffer? GetDuplexBuffer() {
    //     if (sharedMemoryPtr == IntPtr.Zero) {
    //         throw new InvalidOperationException("Shared memory pointer is null.");
    //     }


    //     try {
    //         return ref (DuplexRingBuffer?)Marshal.PtrToStructure(sharedMemoryPtr, typeof(DuplexRingBuffer));
    //     } catch (Exception ex) {
    //         logger.Log($"Error accessing slot: {ex.Message}");
    //     }


    //     return null;
    // }

    // public RingBuffer? GetIngressBuffer() {
    //     if (sharedMemoryPtr == IntPtr.Zero) {
    //         return null;
    //     }
    //     logger.Log($"Size of RingBuffer: {Marshal.SizeOf(typeof(RingBuffer))}");
    //     logger.Log($"Size of RingBufferSlot: {Marshal.SizeOf(typeof(RingBufferSlot))}");
    //     logger.Log("Memory address of sharedMemoryPtr: " + sharedMemoryPtr.ToString("X"));



    //     // RingBuffer* buffer = (RingBuffer*)sharedMemoryPtr.ToPointer();
    //     // if (buffer == null) {
    //     //     throw new InvalidOperationException("RingBuffer pointer is null.");
    //     // }
    //     // byte[] ringBuffer = *buffer + ringBufferSize;
    //     // return null;
    //     // Attempt to read a slot
    //     try {
    //         // RingBufferSlot firstSlot = buffer->Slots[0];
    //         if (sharedMemoryPtr == IntPtr.Zero) {
    //             return null;
    //         }
    //         return (RingBuffer?)Marshal.PtrToStructure(sharedMemoryPtr, typeof(RingBuffer));
    //         // Use firstSlot here...
    //     } catch (Exception ex) {
    //         logger.Log($"Error accessing slot: {ex.Message}");
    //     }


    //     return null;
    // }

    // public RingBuffer? GetEgressBuffer() {
    //     if (sharedMemoryPtr == IntPtr.Zero) {
    //         return null;
    //     }

    //     IntPtr egressPtr = IntPtr.Add(sharedMemoryPtr, (int)SharedMemory2.GetPageAlignedBufferSize());
    //     return (RingBuffer?)Marshal.PtrToStructure(egressPtr, typeof(RingBuffer));
    // }

    public void Dispose() {
        if (sharedMemoryPtr != IntPtr.Zero) {
            NativeMethods2.munmap(sharedMemoryPtr, sharedMemorySize);
            //sharedMemoryPtr = IntPtr.Zero;
        }
    }
}

// public class RingBufferConstants2 {
//     public const int NUM_SLOTS = 2048;
//     public const uint MAX_PAYLOAD_SIZE = 61440U;
//     public const uint RING_BUFFER_SLOT_HEADER_SIZE = 11U;
//     public const uint RING_BUFFER_SLOT_SIZE = MAX_PAYLOAD_SIZE + RING_BUFFER_SLOT_HEADER_SIZE;
//     public const uint RING_BUFFER_HEADER_SIZE = 4U;
//     public const uint RING_BUFFER_SIZE = RING_BUFFER_HEADER_SIZE + (RING_BUFFER_SLOT_SIZE * NUM_SLOTS);

// }

public unsafe class RingBufferReader {
    private readonly Logger logger;

    //public const int RING_BUFFER_SLOT_SIZE = sizeof(RingBufferSlot);

    private readonly SharedMemory2 sharedMemory;

    public RingBufferReader(Logger logger, SharedMemory2 sharedMemory) {
        this.logger = logger;
        this.sharedMemory = sharedMemory;
        UserBufferActiveFreeSlots = sharedMemory.NUM_SLOTS;
    }

    public bool IsLittleEndian(bool reading) {
        return sharedMemory.IsLittleEndian(reading);
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
    public static byte[] GetBytes<T>(T str) {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    public static void PrintFieldBytes<T>(Logger logger, T structInstance, string fieldName, int fieldSize) {
        byte[] arr = GetBytes(structInstance);
        int offset = Marshal.OffsetOf<T>(fieldName).ToInt32();

        logger.Log($"Offset {offset} - Field '{fieldName}' - Hex data: {BitConverter.ToString(arr, offset, fieldSize).Replace("-", " ")}");
    }

    private ulong[] deBounceProcessedSlots = new ulong[10000];
    private int deBounceProcessedSlotsIndex = 0;
    private bool checkAndAddDebounceSlot(ulong id) {
        if (deBounceProcessedSlots.Contains(id)) {
            return false;
        }

        deBounceProcessedSlots[deBounceProcessedSlotsIndex] = id;
        deBounceProcessedSlotsIndex = (deBounceProcessedSlotsIndex + 1) % deBounceProcessedSlots.Length;
        return true;
    }

    int nextSystemReadPosition = 0;
    public ReadOnlySpan<byte> Read() {
        Span<byte> dataRead = new byte[0];
        int startIndex = nextSystemReadPosition;
        int endIndex = startIndex;
        int bytesWritten = 0;
        int? currentIndex = null;
        bool slotFound = false;
        List<int> userIndiciesToClear = new List<int>();
        while (!slotFound && currentIndex != startIndex) {
            if (currentIndex == null) {
                currentIndex = nextSystemReadPosition;
            }

            var header = sharedMemory.ReadSystemRingBufferSlotHeader(nextSystemReadPosition);
            if (header.Id == 0) {
                currentIndex = (currentIndex + 1) % sharedMemory.NUM_SLOTS;
                nextSystemReadPosition = currentIndex.Value;
                continue;
            }

            if (!checkAndAddDebounceSlot(header.Id)) {
                currentIndex = (currentIndex + 1) % sharedMemory.NUM_SLOTS;
                nextSystemReadPosition = currentIndex.Value;
                continue;
            }

            logger.Log($"Read slot #{nextSystemReadPosition} ID: {header.Id} with status {header.Status} and total size {header.TotalDataSize} size {header.CurrentDataSize} with clearance start {header.ClearanceStartIndex} and end {header.ClearanceEndIndex}");
            if (!slotFound) {
                dataRead = new byte[header.TotalDataSize];
            }

            if (header.ClearanceEndIndex > header.ClearanceStartIndex && header.ClearanceEndIndex > 0 && header.ClearanceEndIndex < sharedMemory.NUM_SLOTS) {
                for (uint clearSlotIndex = header.ClearanceStartIndex; clearSlotIndex < header.ClearanceEndIndex; clearSlotIndex++) {
                    sharedMemory.WriteUserRingBufferSlotStatus(clearSlotIndex, SlotStatus.EMPTY);
                    UserBufferSlotsClearedCounter++;
                    UserBufferActiveFreeSlots++;
                    UserBufferActiveUsedSlots--;
                }
            }

            if (header.Status == SlotStatus.VALID) {
                slotFound = true;
                var data = sharedMemory.ReadSystemRingBufferSlotData((int)currentIndex.Value, header.CurrentDataSize);
                sharedMemory.CopyData(data, dataRead.Slice(bytesWritten), header.CurrentDataSize);
                bytesWritten += header.CurrentDataSize;
                endIndex = sharedMemory.ReadSystemRingBufferPosition();
            } else if (header.Status == SlotStatus.ADVANCE) {
                var data = sharedMemory.ReadSystemRingBufferSlotData((int)currentIndex.Value, header.CurrentDataSize);
                sharedMemory.CopyData(data, dataRead.Slice(bytesWritten), header.CurrentDataSize);
                bytesWritten += header.CurrentDataSize;
                endIndex = sharedMemory.ReadSystemRingBufferPosition();
            }

            currentIndex = (currentIndex + 1) % sharedMemory.NUM_SLOTS;
            nextSystemReadPosition = currentIndex.Value;
        }

        if (slotFound) {
            systemIndiciesToClear.Enqueue(((uint)startIndex, (uint)endIndex));
        }

        foreach (var index in userIndiciesToClear.Distinct()) {
            sharedMemory.WriteUserRingBufferSlotStatus((uint)index, SlotStatus.EMPTY);
        }

        return dataRead;
    }


    private Queue<(uint Start, uint End)> systemIndiciesToClear = new Queue<(uint, uint)>();
    public void SendResponse(byte[] data) {
        WriteToSystemRingBuffer(data);
        // //Update the egress buffer's clearance indices to indicate which slots have been read
        // for (uint i = 0; i < RingBufferConstants2.NUM_SLOTS; i++) {
        //     sharedMemory.ReadUserRingBufferPosition();
        //     ref var slot = ref sharedMemory.EgressBuffer->Slots[i];
        //     if (slot.Status == SlotStatus.EMPTY) {
        //         slot.ClearanceStartIndex = (ushort)startIndex;
        //         slot.ClearanceEndIndex = (ushort)endIndex;
        //         Encoding.ASCII.GetBytes("Friend").CopyTo(slot.DataSpan);
        //         slot.Status = SlotStatus.VALID;
        //         sharedMemory.EgressBuffer->Position = i + 1;
        //         break;
        //     }
        // }
    }

    int FindContiguousEmptySlots(int required_slots) {
        int count = 0;
        int current_position = (int)sharedMemory.ReadUserRingBufferPosition();
        int start_position = current_position;

        do {
            if (sharedMemory.ReadUserRingBufferSlotStatus(current_position) == SlotStatus.EMPTY) {
                count++;
                if (count == required_slots) {
                    return start_position; // Found enough contiguous EMPTY Slots
                }
                current_position = (current_position + 1) % sharedMemory.NUM_SLOTS;
            } else {
                // Reset count and move to the next position
                count = 0;
                start_position = (start_position + 1) % sharedMemory.NUM_SLOTS;
                current_position = start_position;
            }
        } while (start_position != sharedMemory.ReadUserRingBufferPosition());

        return -1; // Not enough contiguous EMPTY Slots found
    }

    long UserBufferSlotsUsedCounter = 0;
    long UserBufferSlotsClearedCounter = 0;
    long UserBufferActiveUsedSlots = 0;
    long UserBufferActiveFreeSlots = 0;

    public IDictionary<int, DateTime> SlotWrittenTimes { get; } = new Dictionary<int, DateTime>();
    void WriteToSystemRingBuffer(Span<byte> data) {
        if (UserBufferSlotsUsedCounter % 10000 == 0) {
            printRingBufferCounters();
        }

        // for (var slotIndex = 0; slotIndex < slotWrittenTimes.Count; slotIndex++) {
        //     var slot = slotWrittenTimes.ElementAt(slotIndex);
        //     if (DateTime.Now - slot.Value > TimeSpan.FromMilliseconds(1000)) {
        //         sharedMemory.WriteUserRingBufferSlotStatus((uint)slot.Key, SlotStatus.EMPTY);
        //         slotWrittenTimes.Remove(slot.Key);
        //         logger.Log($"Clearing stale response in slot {slot.Key} due to timeout");
        //     }
        // }

        //logger.Log($"Writing {data.Length} bytes to user ring buffer");
        int remainingSize = data.Length;
        int bytesWritten = 0;
        ushort sequenceNumber = 0;
        int requiredSlots = (data.Length + sharedMemory.SLOT_DATA_SIZE - 1) / sharedMemory.SLOT_DATA_SIZE; // Calculate the number of Slots required
        RingBufferSlotHeader slotHeader = new RingBufferSlotHeader();
        // Find the first set of contiguous EMPTY Slots that match the required slot count
        int startPosition = FindContiguousEmptySlots(requiredSlots);
        if (startPosition == -1) {
            startPosition = 0;
        }

        sharedMemory.WriteUserRingBufferPosition((int)startPosition);

        while (remainingSize > 0) {
            ushort bytes_to_write = (ushort)Math.Min(remainingSize, sharedMemory.SLOT_DATA_SIZE);
            slotHeader.CurrentDataSize = bytes_to_write; //to_little_endian_16(bytes_to_write);
            slotHeader.TotalDataSize = (ushort)data.Length; //to_little_endian_32(size);
            if (systemIndiciesToClear.Count > 0) {
                var nextSetToClear = systemIndiciesToClear.Dequeue();
                slotHeader.ClearanceStartIndex = (ushort)nextSetToClear.Start;
                slotHeader.ClearanceEndIndex = (ushort)nextSetToClear.End;
            }

            if (data.Length > sharedMemory.SLOT_DATA_SIZE) {
                slotHeader.SequenceNumber = (byte)sequenceNumber++;
            } else {
                slotHeader.SequenceNumber = 0;
            }

            sharedMemory.WriteUserRingBufferSlotData((uint)startPosition, data.Slice(bytesWritten), (ushort)remainingSize);
            bytesWritten += bytes_to_write;
            remainingSize -= bytes_to_write;
            if (remainingSize > 0) {
                slotHeader.Status = SlotStatus.ADVANCE;
                sharedMemory.WriteUserRingBufferPosition((startPosition + 1) % sharedMemory.NUM_SLOTS);
            } else {
                slotHeader.Status = SlotStatus.VALID;
            }

            logger.Log($"Writing slot ({startPosition}) total data size: {slotHeader.TotalDataSize} current data size: {slotHeader.CurrentDataSize} sequence number: {slotHeader.SequenceNumber} status: {slotHeader.Status} clearance start: {slotHeader.ClearanceStartIndex} clearance end: {slotHeader.ClearanceEndIndex}");
            sharedMemory.WriteUserRingBufferSlotHeader(startPosition, slotHeader);

            if (SlotWrittenTimes.ContainsKey(startPosition)) {
                SlotWrittenTimes.Remove(startPosition);
            }

            SlotWrittenTimes.Add(startPosition, DateTime.Now);

            UserBufferSlotsUsedCounter++;
            UserBufferActiveFreeSlots--;
            UserBufferActiveUsedSlots++;
        }
    }

    private void printRingBufferCounters() {
        Console.WriteLine($"(Buffer stats) Total: {sharedMemory.NUM_SLOTS}; Actively Used: {UserBufferActiveUsedSlots}; Actively Free: {UserBufferActiveFreeSlots}; Total Used: {UserBufferSlotsUsedCounter}; Total Free: {UserBufferSlotsClearedCounter};");
    }


    // private void SendClearanceIndices(uint startIndex, uint endIndex) {

    //     //Update the egress buffer's clearance indices to indicate which slots have been read
    //     for (uint i = 0; i < RingBufferConstants2.NUM_SLOTS; i++) {
    //         ref var slot = ref sharedMemory.EgressBuffer->Slots[i];
    //         if (slot.Status == SlotStatus.EMPTY) {
    //             slot.ClearanceStartIndex = (ushort)startIndex;
    //             slot.ClearanceEndIndex = (ushort)endIndex;
    //             Encoding.ASCII.GetBytes("Friend").CopyTo(slot.DataSpan);
    //             slot.Status = SlotStatus.VALID;
    //             sharedMemory.EgressBuffer->Position = i + 1;
    //             break;
    //         }
    //     }
    // }

    // private void AdvanceRingBuffer(RingBufferSlotHeader header) {
    //     while (header.Status == SlotStatus.VALID || header.Status == SlotStatus.ADVANCE) {
    //         sharedMemory.EgressBuffer->Position = (sharedMemory.EgressBuffer->Position + 1) % RingBufferConstants2.NUM_SLOTS;
    //         slot = sharedMemory.EgressBuffer->Slots[sharedMemory.EgressBuffer->Position];
    //     }
    // }

    // private void AdvanceRingBuffer(ref RingBufferSlot slot) {
    //     while (slot.Status == SlotStatus.VALID || slot.Status == SlotStatus.ADVANCE) {
    //         sharedMemory.EgressBuffer->Position = (sharedMemory.EgressBuffer->Position + 1) % RingBufferConstants2.NUM_SLOTS;
    //         slot = sharedMemory.EgressBuffer->Slots[sharedMemory.EgressBuffer->Position];
    //     }
    // }

    //     private void SendClearanceIndices2(uint startIndex, uint endIndex) {
    //         //Update the egress buffer's clearance indices to indicate which slots have been read
    //         for (uint i = 0; i < RingBufferConstants2.NUM_SLOTS; i++) {
    //             ref var slot = ref sharedMemory.EgressBuffer.Slots[i];
    //             if (slot.Status == SlotStatus.EMPTY) {
    //                 slot.ClearanceStartIndex = (ushort)startIndex;
    //                 slot.ClearanceEndIndex = (ushort)endIndex;
    //                 Encoding.ASCII.GetBytes("Friend").CopyTo(slot.DataSpan);
    //                 slot.Status = SlotStatus.VALID;
    //                 sharedMemory.EgressBuffer.Position = i + 1;
    //                 break;
    //             }
    //         }
    //     }

    //     private void AdvanceRingBuffer2(RingBufferSlot2 slot) {
    //         while (slot.Status == SlotStatus.VALID || slot.Status == SlotStatus.ADVANCE) {
    //             sharedMemory.EgressBuffer.Position = (sharedMemory.EgressBuffer.Position + 1) % RingBufferConstants2.NUM_SLOTS;
    //             slot = sharedMemory.EgressBuffer.Slots[sharedMemory.EgressBuffer.Position];
    //         }
    //     }
}



// public class RingBufferWriter {
//     private RingBuffer egressBuffer;

//     public RingBufferWriter(SharedMemory2 sharedMemory) {
//         var egressBufferLoaded = sharedMemory.GetEgressBuffer();
//         if (egressBufferLoaded is null) {
//             throw new InvalidOperationException("Egress buffer is null.");
//         }

//         egressBuffer = egressBufferLoaded.Value;
//     }

// }

public class TestData {
    public const string TEST_PAYLOAD_10_PAST_SLOT = """
Lorem ipsum dolor sit amet, consectetur adipiscing elit. Curabitur faucibus ex erat, a tempus elit auctor id. Nullam commodo ex sed quam dictum dictum. Cras rhoncus faucibus lacus ac dictum. Sed sodales sem nec facilisis faucibus. Aenean velit purus, ornare placerat est non, elementum congue nulla. In eleifend id neque non sollicitudin. Duis sed nisi rutrum, interdum sem sit amet, ullamcorper risus. Maecenas fermentum ante in sapien euismod lacinia. Maecenas tortor purus, tincidunt porttitor mauris non, tincidunt blandit lectus. Duis eu mauris nibh.

Nulla dapibus mauris nec lectus imperdiet, eu placerat libero egestas.In erat nibh, elementum sed lorem id, commodo fringilla elit.Quisque in augue dictum, accumsan sem sed, facilisis nisi. Nullam commodo egestas lectus sit amet sagittis.Donec quis ipsum bibendum, dictum nibh et, pharetra lorem. Donec sed quam id dui blandit cursus sed ac sem. In volutpat cursus mauris eget cursus. Morbi iaculis nibh ac condimentum ullamcorper. Donec gravida congue urna vel interdum. Proin pharetra iaculis metus. Nullam sit amet sapien odio.Mauris condimentum mollis cursus. Morbi rhoncus arcu sit amet purus sagittis luctus. Donec vel urna tincidunt, pellentesque lectus vitae, pulvinar lorem. Ut quis enim diam. Ut feugiat porta libero, a fringilla tellus ultricies sed.

Quisque vitae laoreet metus, sed maximus sem.Vestibulum pretium pharetra nisl, sed pretium magna tincidunt in. Maecenas lorem sapien, tristique quis pellentesque vel, mollis laoreet libero.Nunc elementum purus ante, a mollis tellus pretium ullamcorper.In mi ex, blandit vitae molestie vel, maximus tristique turpis.Sed dapibus turpis sem, ut maximus mi tincidunt et.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Curabitur aliquam quis enim eu cursus.Integer elit lectus, tempus vitae felis at, venenatis vulputate purus.

Donec ut felis consequat, rhoncus felis a, ultrices purus. Vestibulum eget consequat turpis, in luctus ligula. Donec dictum id nibh sit amet mollis.Ut quis arcu nisi. Aenean ut ultrices enim, at dapibus mi.Ut malesuada tortor ut porta efficitur. Ut volutpat vestibulum mauris, at condimentum dolor accumsan eget.Mauris porta luctus pellentesque. Praesent non lectus ac neque gravida tristique.In lacinia a risus non aliquet. Aenean nunc eros, gravida quis nibh id, sagittis ultricies est.Mauris dictum ligula non ipsum cursus, non dictum justo pulvinar. Mauris quam ex, tempor ac tincidunt at, gravida porta magna.Ut sollicitudin iaculis turpis id imperdiet. Phasellus tempus lectus eu ullamcorper blandit. Sed non pellentesque arcu.

Ut congue, nibh at elementum dignissim, ipsum odio scelerisque ipsum, tempor accumsan massa est quis diam. Praesent commodo pellentesque auctor. Pellentesque fermentum dictum lacus laoreet rhoncus. Quisque in massa dui. Aliquam tortor libero, tincidunt vel est eget, faucibus porta turpis.Praesent aliquam pharetra lorem sed commodo. Sed et urna sed erat euismod lobortis vel eget enim. Nulla commodo odio at rhoncus consequat. Sed mauris risus, feugiat eu nulla quis, euismod egestas eros.Interdum et malesuada fames ac ante ipsum primis in faucibus.Quisque in justo dolor.

In egestas dui at velit iaculis, at egestas dolor vestibulum. Sed porttitor odio massa, ac bibendum orci porta vel.Mauris non consectetur ante, eu euismod metus.Duis sollicitudin hendrerit urna, sit amet pharetra sem convallis sit amet.Donec cursus cursus justo ac elementum. Suspendisse varius, urna vel tempus pellentesque, lectus quam mollis turpis, tincidunt porta ligula arcu pellentesque elit. Quisque tristique, ipsum vitae volutpat aliquet, est eros tempus tellus, ac ornare felis mauris non sem. Etiam convallis viverra massa, vel scelerisque est eleifend quis.Nulla quis nulla interdum, porttitor lorem vitae, molestie ipsum. Aenean nec odio eget est accumsan fringilla eu sed sem. Nullam nec porttitor tortor, sit amet blandit elit.

Nullam facilisis lacus ac orci semper, eu commodo tortor consectetur. Maecenas cursus vehicula viverra. Aliquam erat volutpat.Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Sed tincidunt nunc non nibh tristique, sit amet laoreet nunc mollis.Morbi purus urna, fringilla vel vehicula at, convallis at ligula.Phasellus quis neque mattis, sollicitudin enim non, cursus velit. Duis vitae est accumsan lorem posuere porta.

Ut felis magna, dignissim in ultricies eget, interdum gravida nisl.Suspendisse consectetur velit non vestibulum porttitor. Vivamus tincidunt est ipsum, eget feugiat augue pulvinar at.Phasellus rutrum posuere nunc, non pulvinar orci iaculis a.Phasellus in turpis in purus congue condimentum non vitae lacus. Ut at tempor sem. Aenean fringilla, nulla feugiat semper imperdiet, dui ligula pulvinar leo, congue ullamcorper orci urna vitae velit.

Sed dignissim lacus in lectus rutrum, ac euismod purus interdum. Nulla hendrerit ipsum nec scelerisque lobortis. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Aenean venenatis, arcu at congue molestie, urna arcu rutrum diam, pharetra blandit arcu dolor eu nunc. Nam non dictum arcu. Donec eu eros lorem. Sed pulvinar, dolor id iaculis iaculis, urna felis consequat sem, sed feugiat neque magna non augue. Nunc posuere imperdiet facilisis. In venenatis mauris eget erat accumsan maximus.Proin lobortis nulla et suscipit imperdiet. Duis tristique nibh quis elit commodo auctor.Pellentesque ullamcorper ultricies est aliquet blandit. Quisque luctus nunc nec ante consequat gravida.Duis porttitor, elit nec faucibus dignissim, metus diam laoreet libero, nec lobortis dolor ipsum eget erat. Suspendisse nec purus porttitor, venenatis elit ac, tempus purus. Sed ut pellentesque odio, eget eleifend leo.

In nec rhoncus erat, vel ullamcorper mi.Nunc consequat ipsum nec nunc dictum sodales.In scelerisque enim a felis mollis, eget egestas quam tempor. Mauris ut eleifend libero. Vivamus a turpis non felis semper malesuada a a nulla. Vestibulum urna dolor, consequat ut arcu venenatis, cursus sollicitudin lorem.In vitae ipsum vulputate felis fringilla tristique.Sed nunc nisl, volutpat eu vulputate vel, maximus in est.Ut feugiat ex sapien, nec gravida orci elementum at.In sit amet sodales ex.Aliquam ut efficitur lectus. Etiam lobortis neque eget arcu lacinia, nec pharetra felis commodo. Curabitur posuere, ligula at rutrum dictum, odio neque fermentum felis, id sodales odio lorem gravida purus.

Nam et lacus id dolor dictum feugiat.Quisque vitae interdum odio, eu elementum massa.Donec ante dolor, volutpat at tellus nec, congue dignissim metus.Etiam nec nibh sit amet odio gravida tristique at nec quam.Donec in mauris ipsum. Nunc ullamcorper tortor mauris, quis lacinia nunc varius et.Suspendisse volutpat metus erat, suscipit consequat elit pretium vitae.Suspendisse ante sapien, sodales sit amet metus eget, iaculis aliquet nisi.Morbi pulvinar neque erat, pretium rutrum mauris tempus sed.

Quisque euismod, orci nec aliquam tincidunt, leo mauris fermentum erat, quis tincidunt ex dolor nec metus. Mauris vitae dictum mi. Ut enim dui, tincidunt in urna semper, varius ultricies ipsum.Nulla eget nisl eu massa vehicula commodo.Nulla tincidunt et metus at malesuada. Aenean non lectus dolor. Vestibulum lobortis quam quam, ac rhoncus eros egestas vitae.Sed imperdiet lorem sit amet turpis rhoncus varius. Integer tincidunt auctor quam, ut consectetur erat vestibulum eget.Quisque vitae maximus diam. Quisque condimentum, ipsum eu varius lobortis, diam erat rutrum risus, vitae tempor lacus erat quis mi. Curabitur vestibulum tortor libero. Quisque pulvinar nulla dolor, sed malesuada felis tristique vel.Pellentesque venenatis mauris elit, tincidunt blandit lorem auctor et.Maecenas laoreet augue libero, et blandit ante viverra eget.Etiam eu dignissim libero, vitae sagittis lectus.

Proin posuere tellus felis, eget consequat ex euismod ut.Maecenas fermentum diam efficitur commodo condimentum. In hac habitasse platea dictumst.Nullam vel orci diam. Praesent gravida diam quis mi condimentum, vitae porttitor mauris convallis. Maecenas sit amet condimentum velit.Aliquam erat volutpat.

Nunc dictum vestibulum maximus. Nullam sed velit id nisl elementum malesuada.Curabitur vestibulum leo vitae neque mattis aliquam.Quisque id elementum velit. Aliquam id dolor lobortis, sagittis purus ac, ornare sapien. Donec magna arcu, gravida eu libero quis, facilisis tincidunt arcu.Morbi et pellentesque ante, ac facilisis orci.Duis ornare vestibulum urna eu vestibulum. Nulla in tincidunt tellus, ac ultricies orci.Pellentesque congue augue in varius mollis. Nulla sem turpis, cursus sed semper nec, pharetra ac nisi.Vestibulum vehicula dictum molestie. Suspendisse a dignissim augue. Fusce at posuere dolor.

Curabitur a auctor metus. Praesent eu felis tempus, bibendum elit elementum, bibendum nisi. Aliquam ut odio pellentesque, lacinia risus ac, fermentum lacus. Suspendisse iaculis nulla risus, vel ultrices lectus pretium ac.Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Vivamus sit amet rutrum magna.Cras quis dui viverra, varius turpis eu, fringilla magna.

Maecenas vitae ligula ut dui sollicitudin finibus.In hac habitasse platea dictumst.Suspendisse sagittis ex ac ex dignissim ultricies.Ut pretium, enim eu tristique rutrum, enim mi hendrerit dolor, in scelerisque justo lacus id neque.Aliquam maximus volutpat arcu non tincidunt. Phasellus in lorem in nulla viverra malesuada non quis lorem. Aliquam non pellentesque urna. Morbi euismod tincidunt nibh, ut accumsan purus aliquam quis.Quisque magna augue, mollis a mollis id, pharetra at est.

Nulla nec lacus ullamcorper, interdum sapien ultrices, viverra purus. Aenean ultricies ac nisl eu tristique. Donec eget nisl elementum, porta felis quis, commodo quam. In non lectus laoreet, pretium lorem quis, condimentum massa. Sed molestie mi ante, sed iaculis sapien sollicitudin vitae.Maecenas vitae rhoncus elit. Donec arcu magna, maximus in cursus eu, rutrum at dui.Fusce turpis mi, efficitur a elit id, ullamcorper imperdiet felis.Etiam rhoncus convallis dui congue blandit. Curabitur convallis ligula ex, quis eleifend eros commodo quis.Ut cursus elit eget ipsum scelerisque feugiat.

Sed a nulla quis mi tincidunt mattis.Duis pellentesque imperdiet ante vitae efficitur. Interdum et malesuada fames ac ante ipsum primis in faucibus.Praesent efficitur vitae orci fermentum dignissim. Proin eleifend vulputate ipsum vel iaculis. Fusce accumsan odio sed ligula placerat consequat.Nam fermentum ante in enim imperdiet, vitae iaculis dolor pellentesque. Donec aliquam mi lobortis vestibulum interdum. Donec eu mi nulla. Proin vel volutpat ante, et sodales dui.Pellentesque efficitur, dolor vitae porta maximus, dolor est dignissim velit, id tincidunt odio orci et nibh. Praesent eget purus tristique, auctor justo a, tempor erat. Donec a sem in tellus mattis consequat.Curabitur pharetra interdum nibh quis euismod.

Phasellus congue convallis rutrum. Mauris eleifend arcu tincidunt metus egestas commodo.Cras eu tempus dui. Aenean nibh leo, convallis non quam a, aliquet sagittis diam.In id arcu venenatis, accumsan dui at, tincidunt urna. Integer iaculis tincidunt ullamcorper. Donec pretium mattis nibh, vitae commodo elit aliquet ac.Nulla blandit semper orci at varius. Donec neque neque, consequat eget ligula quis, bibendum dictum nibh.Vestibulum malesuada, neque non rutrum luctus, dolor tortor lobortis est, ac elementum eros justo vitae mauris. Sed rutrum elementum ex, non congue sem luctus a.Donec libero dolor, finibus ut blandit aliquam, egestas ut tellus.Quisque et elementum lorem, sed fringilla nulla.Curabitur ex nisl, mattis ac leo id, pharetra aliquam urna.

Cras varius aliquet est sit amet ultricies.Morbi vehicula efficitur ipsum, eget blandit massa tempor eget.Donec viverra rutrum arcu, et hendrerit risus facilisis id.Aenean id blandit purus, vitae rhoncus urna.Pellentesque quis ornare ante. Proin laoreet enim gravida accumsan pellentesque. Aliquam ornare, elit in sodales fermentum, ligula nunc volutpat sapien, vel luctus risus ipsum ut metus. Etiam lorem purus, varius sit amet pulvinar non, vestibulum ut ligula.Proin sed maximus elit, ut efficitur dui.

Duis eu interdum est, vitae laoreet lorem.Sed suscipit neque vitae quam lacinia malesuada.Sed vitae egestas sem. Duis eget sapien ullamcorper, efficitur leo quis, molestie massa. Maecenas sem tortor, rhoncus in eros non, aliquam egestas nunc.Duis vitae rhoncus neque. Donec accumsan posuere orci quis tincidunt. Praesent pharetra elit at sem elementum varius.Praesent ut ante finibus, ullamcorper mi id, iaculis elit. Vestibulum dignissim luctus ultrices. Nam sed suscipit arcu, nec ultricies nulla.Donec suscipit, massa in dictum dignissim, neque nisi tristique nibh, at euismod risus lectus non mi. Sed eu rutrum ipsum.

Nunc vel lacinia risus. Pellentesque suscipit ullamcorper velit quis dictum. Donec at rutrum elit. Praesent a elit elit. In cursus est risus, a porta dolor varius sit amet. Mauris ullamcorper nunc quis bibendum vehicula. Quisque in gravida velit. Nulla sollicitudin cursus erat. Sed elementum velit id pulvinar aliquam.

Nulla mattis aliquam leo, sed vehicula purus varius eget.Suspendisse potenti. Duis dignissim magna mauris, a lacinia augue sagittis vel.Sed faucibus justo eros, ac malesuada massa aliquet nec.Curabitur faucibus viverra mauris, eu bibendum arcu tempor vel.Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Integer sodales lorem nec facilisis posuere. Nunc at magna a lectus venenatis convallis.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Aenean malesuada mauris at orci dictum, ut dictum lectus dictum.Nunc viverra elementum nulla, eget lobortis erat vehicula placerat.Fusce sit amet dignissim diam.

Mauris nec erat placerat, tristique risus ut, sodales nibh. Donec commodo at felis vel pellentesque. Donec in tellus purus. Praesent et ornare enim. Nullam condimentum congue diam, ultrices fermentum ex aliquam non.Pellentesque egestas nec turpis at mattis. Nam sollicitudin interdum velit cursus aliquet. Praesent sit amet commodo magna.Praesent et nulla ornare, auctor felis at, tincidunt erat. Fusce vitae egestas velit. In sit amet facilisis urna.Proin tempus ultricies enim in gravida.Nulla laoreet gravida dolor quis ullamcorper. Nulla pretium justo in nunc viverra porttitor.

Nullam sed lorem quis metus hendrerit maximus.Duis condimentum eros lacinia pellentesque condimentum. Praesent nisi ex, consectetur mollis aliquet ac, mattis sollicitudin erat.Donec eleifend justo in vehicula ultricies. Integer aliquet lectus vel metus fringilla consequat.Donec sagittis erat leo, vitae molestie quam malesuada fringilla.Donec tristique turpis nisl, ut accumsan dui lobortis quis.Fusce ex neque, suscipit ac quam sed, rhoncus rutrum lorem.Nam pellentesque tellus et purus tristique aliquet.

Proin a felis nec mi dictum gravida et quis risus. Maecenas tincidunt bibendum pharetra. Morbi eget ultricies orci, quis finibus leo.Aenean ultrices volutpat ipsum vel sodales. Pellentesque malesuada iaculis gravida. In accumsan sodales massa. Phasellus ut interdum lectus. Aenean augue lacus, consectetur eu eleifend iaculis, maximus et ligula.Sed nisi mi, placerat ac viverra sed, pulvinar vestibulum sem.Morbi tincidunt nibh id massa auctor fermentum.

Nunc id dolor auctor, porta diam ut, pharetra est. Nulla sed semper arcu. Nam varius fringilla sodales. Duis at velit eget magna vestibulum auctor quis sed velit. Pellentesque euismod tempus lectus elementum efficitur. Aliquam mauris arcu, semper in ipsum congue, porttitor sodales metus.Suspendisse ac nibh vitae lectus aliquam malesuada suscipit vitae erat. Nullam pulvinar ac nisl ut tempus.

Suspendisse scelerisque sollicitudin elit, eget mollis dolor suscipit ac.Vestibulum a cursus orci. Maecenas bibendum pulvinar semper. Vestibulum ultricies dui sit amet egestas faucibus.Vivamus efficitur hendrerit enim, et suscipit dui imperdiet eu.Mauris elementum aliquet velit, non posuere sem pulvinar nec.Donec et urna in nibh elementum blandit.Aliquam facilisis nulla tellus, in eleifend tortor malesuada ac. Nulla vitae elementum diam. Vestibulum eu condimentum tellus, nec ullamcorper urna.Nunc sagittis justo nunc, in consequat tellus imperdiet eget. Vivamus quis elementum eros, ut tincidunt mi.Quisque ut est maximus, lobortis mi ac, accumsan urna. Donec tristique neque non malesuada rhoncus. Duis elementum viverra ipsum, vel aliquam augue vehicula a.Vestibulum gravida nunc sit amet elit ultricies tempus.

In dictum augue nec mauris vestibulum tempus.Curabitur eros neque, vehicula vestibulum semper eget, imperdiet in elit.Phasellus at ligula feugiat quam convallis sollicitudin.In blandit orci a gravida auctor. Ut sed sapien ultricies, euismod ex vel, congue nulla. Proin porttitor mauris ut est tristique, ac congue dolor cursus. Aenean gravida sit amet mauris non molestie.Ut vehicula imperdiet ante ut tincidunt.

Vivamus ullamcorper velit quis ligula vehicula ullamcorper.Etiam non nisl ac diam pulvinar maximus.Aliquam quis tempor erat. Pellentesque lacinia et ex mollis pulvinar. Curabitur a libero ac sapien ornare egestas vitae in eros.Nullam varius, enim a aliquam iaculis, diam est vulputate odio, vitae pharetra mauris nibh non metus. Integer vitae tortor eu nibh luctus volutpat.Fusce a consectetur tellus.

Donec hendrerit elit turpis, vitae efficitur lacus sodales ut.Proin at posuere felis. Nunc at metus ac odio faucibus consectetur.Proin quis nunc ut nisl laoreet rutrum.Vivamus sed libero vel dui mollis euismod.Etiam et hendrerit nulla. Nunc viverra commodo pulvinar. Pellentesque ac ipsum viverra, commodo leo vitae, dapibus nisl. Aenean sed malesuada nibh. Maecenas turpis magna, faucibus et sem ultricies, blandit interdum justo.Duis lorem quam, fermentum eu maximus euismod, pulvinar nec libero.Praesent vitae lacinia justo.

Quisque bibendum fermentum ipsum, quis bibendum nisl pharetra eget.Duis ut enim dictum, accumsan massa id, egestas lorem. Proin egestas ipsum ut ante dignissim, in placerat nisl blandit.Aenean neque nibh, elementum non placerat vel, mattis vel leo.Cras vehicula libero nec nulla scelerisque pharetra.Integer et tristique velit, sed euismod justo.Ut sed elit libero. Proin lobortis ligula augue.

Donec congue turpis ac erat pretium, ut porta orci pharetra. Nulla porta quam velit. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Nam at enim id erat vestibulum sagittis.Sed ultricies, arcu id fringilla vestibulum, urna ante volutpat lacus, vitae volutpat enim nisl in elit.In efficitur ac turpis eu blandit. Aenean posuere sodales elit a mollis. Maecenas vel est sit amet odio vulputate malesuada. Nulla est dui, euismod id lectus eu, scelerisque bibendum nibh.Pellentesque sed est eu diam venenatis eleifend at in risus.Fusce faucibus ullamcorper posuere. Vestibulum ex turpis, sodales eu tincidunt eu, pretium sed justo.Sed quis leo enim. Proin vitae euismod dui, quis suscipit lacus.Proin ornare dolor a nisi viverra elementum.

Proin quis erat orci. Nam imperdiet sit amet nisl non mattis.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Phasellus vitae urna malesuada, mattis turpis id, pulvinar leo.Etiam non consequat orci, a ultrices ligula.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Nunc posuere dolor eget nisi sagittis rhoncus.Sed rutrum non elit nec pretium. Pellentesque ornare, arcu non dignissim sollicitudin, nulla nulla cursus nisi, in sagittis elit lacus at turpis.Nulla auctor elit imperdiet, elementum sem sit amet, accumsan urna. Nunc sed porta mauris, a convallis augue.

Aenean eget augue nunc. Proin ut rutrum nisi. Cras tincidunt nisl sit amet purus tempor pellentesque. Maecenas iaculis commodo dui vitae viverra. Quisque diam dui, aliquet nec condimentum a, ultricies at quam.Curabitur quis risus a magna porta hendrerit.Aliquam a nibh sit amet est commodo iaculis quis aliquet leo.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Nam velit urna, tincidunt a placerat quis, porta a metus.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Nulla aliquet, diam ut pretium pretium, nulla lacus egestas neque, et aliquam lectus nulla vitae justo. Cras in est in arcu maximus hendrerit in eget nibh. Phasellus iaculis ex est, id tempor elit commodo ut.Nam sed orci vel ex placerat mattis in eget est. Quisque convallis interdum molestie.

Nulla lacinia lectus eget libero malesuada, at congue leo molestie. Cras euismod sapien eu pellentesque tincidunt. Nullam et risus vel felis egestas ultrices quis id augue. Donec feugiat egestas vestibulum. Nullam at orci finibus, suscipit elit sed, consectetur sem. Pellentesque sed magna vel ipsum egestas tristique eu at est. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.Etiam ultricies augue nec finibus mollis. Etiam non ornare libero. Curabitur eleifend sit amet ipsum id facilisis.

Maecenas consectetur, nibh ac dapibus elementum, turpis enim porta est, sed cursus lacus elit eget nibh. Proin venenatis nisi vel auctor commodo. Cras vitae augue dignissim, placerat nulla aliquet, semper lorem. Mauris non dictum augue. Vivamus eget ante id nisi facilisis placerat ut id velit. Sed enim metus, finibus in fringilla at, vehicula sed nisi.Sed et tortor eu urna molestie suscipit a sit amet quam.Phasellus vel metus in nisl luctus imperdiet et ac turpis. Nullam ullamcorper nisl sed eros egestas, in sodales orci facilisis.Nunc nibh diam, fermentum eget nunc ut, facilisis scelerisque eros.Donec consectetur iaculis urna, ornare euismod velit consectetur quis.Donec fringilla turpis eu mi placerat, vel rhoncus velit malesuada. In hac habitasse platea dictumst.Morbi vulputate suscipit nunc eu dictum. Mauris aliquet leo a purus fringilla, sed varius sem mattis.

Pellentesque molestie dolor vel dolor maximus tristique.Cras turpis arcu, ultricies non mattis at, venenatis in neque.Maecenas id hendrerit mi. Morbi interdum arcu sed sollicitudin efficitur. Praesent blandit ipsum et varius bibendum. Mauris interdum feugiat feugiat. Integer eu pulvinar purus. Nullam massa neque, eleifend nec neque nec, sagittis blandit sapien.Donec id sem ac enim consequat eleifend sed vel massa. Pellentesque scelerisque quam nec odio ultricies feugiat.Phasellus ut fermentum risus. Suspendisse ornare urna ligula. Integer consequat, libero eu iaculis tempor, nisl dui luctus purus, ac sodales sapien massa non felis.

Morbi rhoncus convallis nibh, sit amet efficitur dolor vulputate non. Proin facilisis, tortor nec ultricies condimentum, risus ligula malesuada leo, sed molestie ante diam id magna. Proin consectetur lorem id ex facilisis laoreet.Nunc eget orci id nunc mattis fringilla.Mauris dignissim eu neque ac eleifend. Suspendisse elementum eget quam mattis tincidunt. Aliquam metus tortor, iaculis vitae dictum vitae, suscipit nec nisi.Phasellus non molestie justo. Ut risus massa, cursus vel ultrices ut, aliquam sed nisi.Aliquam non ex lobortis, rutrum augue at, accumsan tortor. Etiam vitae arcu molestie nisi rutrum mattis.Ut ornare ipsum mollis magna rhoncus, et laoreet leo interdum.

Nam ac lorem faucibus, porttitor enim dapibus, convallis nisl. Nam volutpat felis eu interdum vehicula. Phasellus rhoncus non sapien fermentum eleifend. Morbi at mi sed nulla pellentesque iaculis et facilisis ante. Nunc ex sem, ultricies quis orci a, dapibus lobortis quam.Donec pellentesque varius mi, at efficitur neque malesuada sed.Ut interdum viverra ligula vel ultrices. Integer eu felis euismod, malesuada velit et, tristique purus.

Sed id odio sit amet quam auctor vestibulum. Quisque ultricies placerat odio id finibus. Maecenas ac lectus tempus, elementum orci nec, congue elit. Morbi aliquet pharetra diam et bibendum. Suspendisse in mi dictum, blandit elit at, luctus orci. Nulla rutrum lorem diam, nec iaculis orci blandit non.Nulla eget iaculis nisl. Etiam consectetur neque id sagittis blandit. Curabitur ultrices eu magna quis aliquet. Suspendisse et ipsum lacus. Mauris eget nisi urna. Sed vitae magna vitae erat ultricies vestibulum.Praesent lectus leo, ullamcorper et enim non, ullamcorper aliquam ipsum.Morbi bibendum, libero eu suscipit rutrum, urna nulla dapibus nisi, eu tempus augue arcu porta urna.

Sed ex lectus, blandit quis commodo ac, aliquet non neque.Aliquam interdum lacinia libero, sit amet mollis quam varius feugiat. Nulla at felis interdum, egestas justo id, dapibus lacus. Ut semper pharetra leo at mattis. Curabitur pellentesque luctus congue. Pellentesque elit augue, mattis sit amet arcu convallis, auctor vestibulum lorem.Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Nulla ac commodo est, et auctor nisi.Morbi rhoncus lacinia sodales. In sed pharetra augue. Nulla mollis non tortor aliquet scelerisque. Nullam quam purus, accumsan eget neque eget, gravida rhoncus urna.Nunc commodo odio sem, sed tincidunt ipsum tempus commodo.Pellentesque at rutrum magna. Ut et justo ornare, varius diam imperdiet, porta nisi. Curabitur a magna vel arcu tincidunt efficitur nec vel arcu.

Suspendisse ac felis massa. Praesent facilisis, neque ac finibus vulputate, lorem lorem auctor nibh, eget bibendum massa risus non turpis. Sed in tincidunt libero. Nullam iaculis sed metus nec eleifend. Morbi ut odio vitae turpis aliquet congue nec eget leo. Duis est tellus, blandit luctus quam in, euismod pellentesque eros.Morbi et dolor orci. Mauris consectetur sagittis velit, at consequat ante fringilla ac.Nulla sed diam et nulla tristique vulputate.Aenean lacinia eu magna vitae vehicula.

Quisque dolor purus, dignissim a hendrerit a, condimentum vitae est.Vivamus quis lectus facilisis metus fringilla interdum ac eu ligula. Aliquam mattis, nibh id scelerisque sodales, lorem augue accumsan justo, vitae pretium dolor risus convallis quam. Morbi lectus erat, pellentesque at enim eget, volutpat efficitur dui.Pellentesque eget turpis non risus mattis accumsan.Donec mi neque, vestibulum at imperdiet quis, consectetur mollis elit.Nam lobortis neque nunc, sed tempor eros tincidunt semper.Etiam aliquet nibh id magna aliquam tempor.Integer ultricies nulla a mauris efficitur vulputate.Proin interdum malesuada neque quis hendrerit.

Fusce vehicula ipsum justo, vel dapibus velit aliquet sit amet. Nunc suscipit ante sit amet tortor blandit sollicitudin. Vestibulum tristique tortor a nibh sagittis vulputate.Curabitur sodales vulputate nisi nec malesuada. Duis ornare sem ornare magna tincidunt euismod.Nam id dapibus tortor. Lorem ipsum dolor sit amet, consectetur adipiscing elit.Aenean et tincidunt nibh, eu viverra velit.

Maecenas luctus tellus eu dui mollis lobortis.Nulla at laoreet turpis. Aliquam hendrerit ex luctus nulla malesuada, non tincidunt est posuere. Nunc pulvinar egestas mauris, non auctor mi sollicitudin ac.Vestibulum odio turpis, dictum a eros nec, vehicula lacinia turpis.Curabitur sed tempus lectus. Nam quis massa at tellus lacinia mollis.Morbi vitae velit id nibh dignissim dictum.Cras suscipit consectetur semper. Nullam hendrerit fermentum justo ac aliquam. Mauris sed mi lectus. Nullam laoreet egestas ligula. Quisque tincidunt vestibulum quam, a lacinia elit euismod in.

Duis porta quam eu tristique aliquam. Sed eget sollicitudin velit. Integer vitae pharetra mi. Donec euismod aliquet efficitur. Suspendisse mattis sollicitudin elit at cursus. Nulla scelerisque mollis ullamcorper. Nulla non justo nisi. Maecenas nec libero augue. Fusce feugiat quam dignissim dignissim efficitur. Aliquam finibus pretium urna eget finibus. Etiam et arcu tincidunt, vulputate lacus a, consectetur mauris. Etiam ut sapien convallis, egestas urna sed, dapibus massa. Praesent sagittis mauris a porttitor aliquet. Morbi sed dictum diam.

Cras sollicitudin suscipit pretium. Pellentesque viverra arcu dui, a interdum turpis sollicitudin ac.Fusce volutpat orci ac nunc rhoncus, semper elementum augue sollicitudin. Praesent dolor felis, pharetra ut tincidunt ac, rhoncus sit amet ex. Vestibulum vulputate id diam at iaculis. Duis ac leo porttitor, consectetur quam vel, efficitur tellus. Cras euismod tristique justo, tincidunt vestibulum lectus cursus in. In viverra imperdiet dolor. Nunc quis mauris id est semper condimentum ut sit amet leo.Phasellus id pulvinar augue, vel elementum nisl.Mauris sed volutpat lorem.

Suspendisse in interdum ligula. Proin non diam congue, tempor nulla at, consectetur tellus. Etiam congue, mi eu finibus auctor, eros diam hendrerit tellus, vel molestie ante dolor sed ipsum. Lorem ipsum dolor sit amet, consectetur adipiscing elit.Nulla vel scelerisque nisl. Integer rhoncus egestas mollis. Curabitur aliquam, lorem sed consectetur consectetur, ligula nunc sodales ante, id lacinia dui lorem ut purus. Integer rhoncus turpis vitae suscipit eleifend. Nullam id nunc ac odio sodales mollis.Proin eu sagittis neque, nec rutrum ligula.

Ut urna arcu, dapibus eget lacinia vitae, hendrerit non nunc.Vivamus euismod velit nec sapien ultrices tempus.Aliquam non felis in ipsum semper cursus eget eget nisl. Vivamus et elit eget mauris tempus facilisis in id orci. Integer rutrum pulvinar neque, eget malesuada tellus placerat in. Aenean semper orci non placerat cursus. Nam viverra pharetra nulla vel suscipit. Cras et condimentum mauris. Ut nec tortor velit. Donec eu magna ullamcorper, tempor leo placerat, interdum quam. Nullam condimentum enim bibendum mollis aliquet. Suspendisse finibus semper dapibus. Sed et mauris consequat, commodo leo quis, molestie mauris. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Sed et semper justo.Mauris elementum felis ac felis tincidunt fringilla.

Duis ac auctor magna, at iaculis lectus.Donec sed ipsum ut tortor cursus tincidunt vitae vitae massa. Interdum et malesuada fames ac ante ipsum primis in faucibus.Nulla viverra lacus et posuere maximus. Quisque ullamcorper, enim eu blandit vehicula, odio quam ornare elit, vehicula auctor erat nisl id felis. Curabitur vulputate tempor erat ac accumsan. Quisque in turpis id sem suscipit viverra.Suspendisse id libero arcu. Nam semper ante nec lorem commodo, sed hendrerit augue mollis. Integer suscipit pellentesque vestibulum. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Suspendisse tellus mi, pretium a blandit id, efficitur eget arcu.

Cras interdum leo tempus justo iaculis aliquet. Proin vel mattis dui. In a laoreet lacus. Praesent lectus nulla, condimentum non lorem eu, commodo mollis urna.Nam eget urna non metus porta faucibus.Lorem ipsum dolor sit amet, consectetur adipiscing elit.Sed luctus, felis nec condimentum blandit, turpis urna vehicula est, eu imperdiet metus elit eget diam. In hac habitasse platea dictumst.Aenean fermentum quam sed elit aliquam fringilla.Nunc placerat quis dui id faucibus. Maecenas dignissim urna sed purus auctor elementum.Integer fringilla leo augue, sit amet cursus sem sodales eget. Aliquam at velit mattis, vehicula lorem vel, pretium purus. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Vivamus et volutpat leo.

Nullam justo diam, faucibus a elementum non, varius a massa.Aliquam suscipit tincidunt magna eu egestas. Sed dictum in justo ut rutrum.Nam feugiat volutpat purus ac varius. Praesent posuere ligula quis dui porta ultrices.Curabitur vel volutpat ligula, quis condimentum leo.Donec mollis, erat vel efficitur porta, magna felis semper velit, interdum laoreet ipsum mauris euismod enim. Integer rutrum fermentum placerat. Etiam pretium, sapien vel imperdiet consequat, nulla odio egestas purus, nec imperdiet dolor ipsum ut orci. Donec laoreet molestie nisl id condimentum. Mauris posuere at orci nec viverra. Donec sapien felis, gravida et arcu non, fermentum ullamcorper sapien.Vestibulum eget elementum mi, quis posuere quam.Suspendisse congue ligula interdum nulla fermentum condimentum.Aliquam fermentum mollis dui. Nulla arcu lacus, sagittis eget mollis nec, rhoncus ut urna.

Praesent in eros nec nunc semper malesuada.Nulla fermentum egestas lacus, eu efficitur justo finibus ut.Phasellus dapibus nec ex ac posuere. Sed ligula augue, euismod et scelerisque gravida, aliquam sit amet ex. Fusce sodales magna vel pellentesque auctor. Quisque in neque tellus. Vestibulum nec risus dui. Morbi efficitur leo eget velit sodales, sit amet suscipit justo mollis.Praesent pretium vitae augue eget maximus. Donec ac orci in sapien consequat aliquet.Sed nec suscipit libero. Vivamus cursus mollis leo ut malesuada. Ut vitae tellus neque. Duis lobortis, metus quis consectetur gravida, felis arcu maximus sem, vitae ornare nisi tortor eu orci. Praesent diam tortor, maximus vel aliquet non, dignissim a massa.

Nullam sit amet risus eu augue condimentum condimentum id tristique mauris.Curabitur dapibus dolor sed purus cursus elementum.In quis lacinia massa, nec hendrerit ex.Aliquam mollis ultrices metus at consequat. Duis id diam faucibus, tempor urna vel, fermentum augue. Nunc sed leo lectus. Suspendisse at varius metus, ac porta nulla.Praesent porttitor a libero sed pretium. Nulla facilisi. Nullam malesuada, purus in blandit rutrum, ex turpis varius mauris, non tempor felis lacus in orci.Vestibulum non volutpat sapien, sit amet blandit justo. Curabitur sed ullamcorper felis, nec consequat mauris.Etiam at felis venenatis, semper velit in, ultricies neque.

Quisque volutpat quam eu nulla semper, in molestie risus accumsan.Duis id purus sed velit facilisis suscipit.Pellentesque eros est, pellentesque in pulvinar a, ultricies in leo.Nullam faucibus egestas quam, eu convallis ante condimentum sed.Donec varius, lorem non sagittis condimentum, odio nibh faucibus massa, eget facilisis eros risus ut odio. Quisque sapien leo, lacinia finibus gravida non, elementum quis ipsum.Quisque condimentum tristique dapibus. Vestibulum aliquet, sapien sit amet ultrices gravida, arcu orci blandit nibh, non sodales dui mauris eu purus.

Donec finibus arcu purus, vitae vehicula justo molestie ullamcorper.In eu arcu ex. Maecenas malesuada nulla eu orci dignissim, a congue magna aliquam. Vivamus quis sodales magna, at aliquet nisl.Morbi fermentum fringilla mi aliquet consequat. In rhoncus eu odio a egestas. In nec sodales quam. Mauris aliquet, tortor ut tempor luctus, nunc ipsum convallis dui, nec finibus lectus nisl nec tortor. Quisque vel iaculis tortor. Suspendisse dignissim magna scelerisque imperdiet feugiat.

Maecenas in odio efficitur, posuere nunc vel, sagittis velit. Sed ac nisi enim. Praesent mattis lorem lectus, a feugiat eros tempor a.Sed at semper nisl. Nullam commodo lectus non est pharetra consectetur.Ut eu dapibus sem. Aenean sagittis arcu posuere nunc laoreet congue.Nam convallis enim purus, in efficitur arcu porta sit amet.In nec aliquam lacus, sed mattis justo.

Suspendisse cursus gravida molestie. Etiam vestibulum neque ipsum, sed tristique mauris fringilla eget.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Praesent luctus nulla ac pretium laoreet.Praesent volutpat feugiat purus in vulputate.Aliquam non justo enim. Praesent sit amet eros fringilla, viverra arcu et, mattis felis. Morbi sollicitudin volutpat erat, vitae sagittis neque mollis et.Duis sit amet accumsan eros.Nam nec ipsum quis velit vestibulum vehicula.Sed sit amet ante lorem.

Nulla varius arcu non mollis dignissim. Fusce sodales nec ante ac venenatis. Nam id efficitur sem. Cras vitae massa laoreet, viverra purus eget, malesuada lorem. Sed eleifend est nibh, ut posuere arcu molestie auctor.Nunc lacinia congue fermentum. Suspendisse placerat sit amet enim ac dignissim.Sed congue felis tempor dolor congue rhoncus.Nulla fringilla quis libero tincidunt fringilla. Maecenas ipsum eros, commodo at ipsum in, dictum suscipit arcu.Nulla sem nunc, pulvinar eu egestas et, tempus sed quam.Pellentesque erat quam, maximus convallis nulla ac, condimentum placerat sem.Curabitur tempus interdum imperdiet. Fusce sed urna sed massa ultrices porttitor sed ac urna.

Quisque euismod metus a vulputate iaculis. Vivamus luctus tristique neque in consequat.Nunc diam ex, tincidunt vel semper sed, tempor id orci.In bibendum elementum tempor. Curabitur tellus est, luctus ut luctus eget, varius efficitur elit.Donec tincidunt, magna at mattis egestas, lacus sapien commodo neque, a aliquet turpis nisi sit amet ipsum.Sed maximus felis vel lorem lacinia, non semper lectus dignissim. Sed tempus tempor justo, vitae elementum elit interdum pharetra.Etiam nibh nibh, hendrerit nec fermentum id, tristique sed lacus.Aliquam erat volutpat.Donec placerat lorem sed accumsan pulvinar. Fusce vitae diam at felis ullamcorper suscipit.

Aliquam erat volutpat.Nullam interdum, arcu eu scelerisque aliquam, dolor enim pulvinar nisi, sit amet consequat dolor ipsum in tortor.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Fusce id enim at ipsum scelerisque finibus a nec risus. Quisque vitae ligula lacus. Quisque fermentum in est ut aliquam.Aliquam suscipit magna felis, faucibus molestie libero dignissim vitae.Quisque vel velit laoreet est varius laoreet.Mauris sed est lacinia, porta dui quis, vulputate eros. Ut egestas turpis nibh, vel pharetra felis egestas sed.Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Donec scelerisque, ex eu dapibus tempor, dui diam congue lacus, in tempus nulla ipsum nec mi.

Phasellus viverra lacus leo, in mollis nulla hendrerit non. Morbi mattis rutrum erat, at bibendum odio tincidunt elementum.Mauris eget mauris molestie, facilisis diam eget, semper lectus. Cras elementum semper tincidunt. Aliquam erat volutpat.Fusce porta sapien id dolor consectetur, non interdum est blandit. Quisque in tempus tortor. Cras vel convallis sem. Duis facilisis id odio euismod aliquet. Pellentesque habitant morbi tristique senectus et netus et malesuada fames ac turpis egestas.

Donec eget libero maximus, aliquet sapien ac, tristique lorem. Aenean tincidunt augue eros, non iaculis dui venenatis quis.Quisque viverra lacus quis erat gravida congue.Curabitur sit amet nibh vitae dolor finibus tincidunt. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Maecenas laoreet lacus vel dolor tristique, quis tempor dui sagittis. Nullam ex augue, interdum in felis auctor, semper semper ligula.Cras mollis lorem sed urna malesuada ullamcorper.

Curabitur accumsan sem et nulla cursus, egestas ultricies augue eleifend. Curabitur consequat justo at sem faucibus elementum.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Maecenas tincidunt bibendum lorem, a pretium mauris convallis nec.Mauris pellentesque nunc ac mauris lacinia euismod.Praesent justo leo, sollicitudin et libero nec, bibendum mattis ipsum.Cras eu dui viverra, fringilla nisl ut, ornare nulla. Sed molestie dictum ipsum, in blandit nisi venenatis sit amet.Quisque vestibulum accumsan enim, id varius diam facilisis vel.Praesent tincidunt purus nec massa venenatis dignissim.Phasellus luctus ligula tellus, posuere accumsan metus lobortis id.Sed mollis tristique augue in pharetra.Mauris eu laoreet sem. Phasellus tempor tellus eu nulla accumsan ullamcorper.

Fusce vulputate risus sed pulvinar lacinia. Morbi tempor faucibus condimentum. Duis non urna convallis, semper magna non, ultricies lacus. In hac habitasse platea dictumst.Aliquam ultrices lectus eros. Donec sed gravida magna. Curabitur non augue a nisi scelerisque luctus id a ante. Curabitur tristique velit in odio fermentum, nec suscipit turpis vulputate. Aenean pulvinar, odio sit amet posuere dictum, nisi ligula consectetur nibh, eget malesuada urna felis nec ligula. Nullam posuere turpis in mi luctus scelerisque.Suspendisse lobortis rhoncus justo, at tincidunt nibh pulvinar in.

Ut rhoncus dignissim consequat. Aenean sollicitudin dolor nec quam euismod tristique.Etiam a lectus ut neque fringilla consequat.Donec et lectus ut lorem imperdiet consectetur at ut turpis. Fusce non erat ac leo interdum ultrices ac ac metus. Integer augue tellus, vulputate eget dignissim sed, venenatis egestas turpis.Quisque ornare id dui vel posuere. Quisque ac ullamcorper sapien, interdum porttitor nisl.Nullam quis sem tellus. Nullam at dolor quis eros dictum semper id vel velit. Quisque accumsan, ante eget dapibus consequat, ligula nunc accumsan quam, eget condimentum felis magna quis elit. Etiam blandit at dui ac finibus. Aliquam a lectus sed metus mattis mattis.Praesent iaculis vitae diam vitae malesuada. Maecenas varius et urna at tincidunt. Phasellus sagittis eros id lacus interdum molestie.

Nunc commodo lectus vitae urna convallis, vel cursus dolor vestibulum. Vivamus eget erat sapien. Integer non sem vitae elit maximus malesuada vitae non metus. Quisque imperdiet urna et diam maximus commodo.Duis bibendum, quam vitae blandit mattis, arcu dui lobortis tellus, et placerat arcu lorem in augue.Nulla sed ultrices enim. Aenean id augue quis nulla mollis condimentum.

Aenean maximus et sem quis egestas. Suspendisse potenti. Praesent varius nec metus vitae suscipit. Nam ac augue nec diam tristique fringilla tincidunt non mauris. Morbi volutpat egestas lorem. Nunc eget ex magna. Phasellus tempor lectus ut pretium lacinia. Nullam sed convallis tortor. Cras risus nisi, cursus et lectus sit amet, congue volutpat enim.Nulla lacinia rutrum massa, ut sodales diam dictum ut.

Fusce in semper eros, eu efficitur nulla.Mauris at mauris et magna rutrum pretium ac eu nibh. Nullam a feugiat tellus. Vivamus et aliquam orci. Maecenas quis lorem non nisl feugiat convallis.Pellentesque tincidunt dictum magna in elementum.Nam egestas arcu in metus convallis hendrerit.Nulla malesuada rhoncus sagittis. Integer in dapibus tellus. Proin ac dapibus dui, maximus aliquam enim.Nam finibus pellentesque mi non vestibulum. Aliquam et libero cursus ante mattis tempor eu sed arcu. Donec auctor, mi at luctus volutpat, nibh nunc pulvinar lorem, at luctus turpis nibh in velit.Morbi et dui aliquam, sodales libero non, auctor ipsum. Aliquam nibh augue, sagittis eget dui eget, placerat venenatis ligula.

Nulla tristique porttitor odio, eu ultricies arcu ultrices id.Phasellus sit amet mauris accumsan, pharetra leo sit amet, dignissim metus. Nullam vel accumsan eros, a placerat enim.Proin nunc mauris, ullamcorper dignissim odio id, eleifend venenatis sem.Cras sagittis arcu vel tristique commodo. Suspendisse vestibulum efficitur interdum. Aenean convallis, diam at euismod congue, mi massa malesuada libero, nec tristique dui mi sed ligula. Nullam non sodales enim. Fusce nec libero non est sodales tempus.Donec volutpat dui non sapien fermentum, sed scelerisque augue ullamcorper. Mauris semper eros sit amet fermentum dapibus.Integer sollicitudin, lorem vel ornare egestas, tortor elit sagittis justo, id pulvinar tellus ligula at erat.

Ut mattis id quam vel vestibulum. Quisque placerat sed massa non dictum. Aenean imperdiet finibus gravida. Praesent enim sapien, accumsan eu fringilla ac, finibus quis nunc.Ut in ex semper, fringilla libero sed, rhoncus quam. Integer consequat, mi sit amet pulvinar euismod, ipsum tellus faucibus dolor, eget varius leo arcu non libero. Suspendisse laoreet, mauris vitae pretium elementum, nibh lectus feugiat nunc, at convallis enim nisl nec augue. Mauris scelerisque efficitur tristique. Nulla tempus interdum tortor, sed facilisis leo rutrum luctus.Nunc nec nibh volutpat, pulvinar elit ut, ultrices sapien. Pellentesque semper tristique efficitur. In et augue quis metus egestas facilisis eu a felis. Praesent iaculis dolor eu fermentum lobortis.

Proin tincidunt ultricies dui vitae luctus. Proin sapien tortor, mattis at dictum nec, tempus id urna.Vestibulum sed magna diam. Nam congue orci et leo tincidunt, at malesuada orci iaculis. Quisque luctus lacinia augue, vel vehicula orci feugiat quis.Fusce laoreet ornare nulla, sed pharetra lectus porta nec.Nunc eu sapien elit.

Pellentesque cursus a massa quis auctor. Nam dignissim, lectus ac finibus tristique, urna nibh porta purus, bibendum rhoncus elit turpis ut libero. Nullam ac velit elementum, volutpat nunc bibendum, maximus arcu. Cras ut placerat justo, non dictum odio.Ut quis aliquam elit. Curabitur a pellentesque arcu, non sagittis nibh.Curabitur vel lorem quam. Suspendisse feugiat odio in erat gravida varius.Nulla vel urna sit amet sapien dignissim sagittis. Aenean quis dignissim ipsum. Ut tempor tristique libero sed blandit. Aenean tempor ac leo euismod hendrerit. Donec mauris ante, sollicitudin ac sem non, euismod ornare quam.Pellentesque ac felis id mi blandit ullamcorper eu quis diam. Lorem ipsum dolor sit amet, consectetur adipiscing elit.Sed ut orci vel odio aliquet feugiat.

Praesent non sagittis metus. Morbi tristique tortor ac enim faucibus lacinia.Aliquam cursus varius purus eu pellentesque. Fusce mollis aliquet dolor, vel auctor dolor hendrerit sed.Maecenas vehicula libero ut rutrum molestie. Cras odio metus, iaculis ut erat ac, imperdiet interdum turpis.Maecenas sed tincidunt nisl. Aenean gravida purus non magna mollis consequat.In at feugiat metus, a ultrices tortor.

Quisque tristique mi ligula, ac sollicitudin tellus feugiat nec.Aliquam sit amet laoreet mauris.Curabitur semper dolor at ligula mollis laoreet.Morbi tempus condimentum accumsan. Praesent in odio vel felis malesuada interdum quis eleifend dolor. Donec vel velit et massa porta lacinia et at risus. Mauris nec nunc porta, egestas risus id, ullamcorper odio. Cras id nisi eget felis dignissim posuere.Vivamus est sapien, mollis ac nulla eu, laoreet elementum dolor.

Nulla facilisi. Fusce dapibus nibh eros, eu vehicula turpis tincidunt ut.Aliquam hendrerit metus vitae ante dapibus sodales.Integer ac ultrices velit. Praesent rutrum, purus eleifend rutrum blandit, quam odio posuere elit, sit amet dapibus tortor metus eu tortor.Nam dolor risus, semper vitae mollis eu, aliquet nec libero.Pellentesque convallis mi ut scelerisque mattis. Nulla nisl est, consequat mattis congue a, tempor vel lectus.Fusce elementum vehicula nibh, sed facilisis risus dapibus vitae.Nulla sem nisi, rhoncus id eros et, ultricies scelerisque orci.Mauris non quam nibh. Integer lacinia lectus sit amet massa fermentum, id volutpat urna rhoncus. Sed condimentum ipsum ut nisi elementum placerat.

Nullam consectetur ex tempus tristique tincidunt. Pellentesque et molestie sapien. Curabitur ex lacus, convallis at nisi non, rhoncus sollicitudin urna.Etiam sed molestie diam, at auctor lectus.Quisque sagittis vulputate orci, at aliquam leo ultricies vitae.Morbi felis elit, dictum quis volutpat ac, ullamcorper in ligula.Ut a eros vitae felis viverra gravida.Pellentesque porttitor venenatis mauris, ac consequat tortor sodales interdum.Mauris eget sapien pulvinar, feugiat felis in, tincidunt neque. Morbi eu semper magna. Etiam non urna at neque interdum placerat a eu est. Aenean in mollis ante.

Pellentesque aliquam nibh sit amet quam dapibus, vitae porttitor risus cursus. Suspendisse posuere non quam in mollis.Cras ac egestas neque. Nulla congue diam in nibh consequat, sed placerat elit maximus. Suspendisse vel rutrum leo. Pellentesque in sodales quam, a commodo urna.Phasellus tincidunt venenatis justo vitae tincidunt. Quisque sed erat non est maximus sagittis.Nam ac molestie mauris, in efficitur metus.

Phasellus semper magna sed velit malesuada tincidunt eu in dui.Maecenas non lobortis ligula. Donec condimentum libero ac nulla sollicitudin mattis.Phasellus vel dapibus lacus. Mauris euismod nibh eget feugiat eleifend. Vestibulum euismod fermentum nisi, a ultricies arcu molestie non.Integer ornare metus ac dolor euismod posuere.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Mauris molestie justo sit amet varius sollicitudin.Aliquam ultricies nulla et tortor elementum rutrum.Praesent aliquam risus id convallis bibendum. Sed a lobortis arcu, interdum laoreet magna.Aliquam faucibus urna massa, sit amet posuere erat rutrum commodo. Mauris imperdiet sem in justo dictum, eu pretium nunc suscipit.

Maecenas posuere, magna a dapibus mattis, lectus lectus euismod magna, vel tincidunt dolor lacus nec metus. Proin varius eros ut nunc pharetra, et semper velit venenatis. Etiam sapien tortor, eleifend quis purus a, blandit commodo mauris.Cras nec ex nec justo sagittis interdum.Aenean fermentum neque nec ligula dignissim, nec dictum lectus cursus. Duis venenatis, lacus eu egestas interdum, est risus vehicula mi, et cursus sem eros id odio. Ut sit amet ante sollicitudin, tempor magna vitae, placerat magna. Donec aliquam tortor at sem tristique finibus.Nullam dolor mauris, placerat et lorem id, tristique vestibulum dui.Aenean vehicula hendrerit ligula. Maecenas tempor est ac pulvinar rutrum. Nam semper diam quis nulla blandit, ut tempus nibh scelerisque. Nullam ultricies quam non sollicitudin malesuada. Praesent sit amet posuere ligula, at ultricies nibh.

Suspendisse elementum malesuada risus, sit amet luctus lacus fringilla nec. Duis in fringilla diam, vitae convallis turpis.Nulla maximus sollicitudin nisi in lacinia.Morbi dui sapien, interdum a eros tincidunt, imperdiet vehicula massa.Vestibulum quis nisi vitae arcu iaculis scelerisque vel eget erat. Integer sodales sed purus quis tristique. Nam sit amet viverra augue.

Fusce ut ipsum non velit fringilla ultricies ut at arcu. Duis ut semper odio, et mattis neque.Integer auctor nulla sed commodo placerat. Sed imperdiet turpis vel pulvinar vulputate. Nullam at nibh et felis mollis imperdiet ut non ipsum. Nunc id consequat est, non interdum ante.Proin ut tempor dui, egestas dignissim dolor.Quisque ultricies commodo tincidunt. Mauris tincidunt nisi ac malesuada iaculis.

Nunc tincidunt ac risus sed commodo. Pellentesque vitae molestie metus. Duis ultricies dui eu velit convallis, vel gravida diam ornare. Sed sit amet libero id erat ultricies blandit ut eu lacus.Etiam vitae blandit felis, vel rutrum nulla.Aliquam convallis quam ut magna facilisis mollis.Vivamus sodales blandit lorem id rutrum. Fusce vel hendrerit odio. Cras facilisis ut risus eget porta. Fusce ut auctor risus, interdum malesuada ante.Fusce congue enim vitae diam mattis condimentum.Mauris dictum erat fringilla mauris posuere luctus.Phasellus tincidunt enim ac nulla volutpat, luctus ullamcorper magna pretium. Aenean ut tincidunt arcu. Cras feugiat iaculis velit nec dictum.

Aliquam tincidunt sem pharetra odio auctor ultrices.Morbi eu eros nunc. Pellentesque non auctor justo. Curabitur fringilla rhoncus metus sed interdum. Sed sed tincidunt nisl, vel gravida nulla.Curabitur vulputate mi massa, sed elementum elit vulputate ac.Donec fringilla suscipit sem, scelerisque tincidunt nunc laoreet vitae.Nullam vel nunc tellus. Mauris vitae blandit mi. Donec sodales dignissim molestie. Aenean a facilisis quam, sit amet ultricies nisi.

Proin vulputate, sapien et vestibulum euismod, velit urna sagittis dui, id semper dui eros nec ipsum. Vestibulum vel eros massa. Ut eget sapien non dui tristique dictum.Curabitur eleifend, massa et sodales lobortis, nibh elit viverra nunc, eget euismod orci mauris in est.Donec rhoncus vestibulum nisl, nec porta risus euismod non.Proin ultricies urna velit, eu luctus sem placerat ac.Ut ut finibus augue. Vivamus a dolor vestibulum justo auctor aliquam.In nec velit porttitor, ullamcorper leo in, finibus risus. Suspendisse dolor magna, consectetur interdum ultrices eu, scelerisque in mi.

Morbi eu ultricies sapien. Sed mattis egestas lobortis. Donec maximus ligula non turpis lacinia tempor.Duis semper dui massa, tincidunt tincidunt ante posuere ac.Cras lobortis tristique vestibulum. Pellentesque a lectus ut justo eleifend rhoncus.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Quisque tincidunt sodales eros, et laoreet diam hendrerit volutpat.In quis ornare tortor.Proin mattis justo nunc, iaculis condimentum felis vulputate a.Vestibulum at leo aliquet, tincidunt urna sit amet, condimentum nibh. Nullam vitae sapien lacus. Curabitur nec feugiat ipsum. Proin suscipit, erat nec volutpat viverra, leo justo volutpat mi, id posuere eros nisi id erat. Lorem ipsum dolor sit amet, consectetur adipiscing elit.

Donec rhoncus massa ut orci tincidunt, in porttitor leo aliquet.Maecenas bibendum, nisi nec blandit suscipit, ex est fringilla tortor, id commodo tellus nibh vel erat. Duis at ullamcorper ante. Praesent viverra sodales massa, a pharetra justo.Morbi scelerisque faucibus velit eu vulputate. Fusce eu auctor magna. Phasellus arcu ex, egestas vel justo ac, venenatis faucibus mi.Cras non arcu et purus accumsan egestas sit amet vitae ante.

Duis lectus est, rhoncus at faucibus volutpat, lobortis vulputate felis.Aliquam consequat fermentum tempor. Cras blandit interdum eleifend. Nullam varius ante tortor, in maximus libero porta eget. Sed dolor risus, dignissim non sollicitudin in, volutpat sed nisl.Sed eget massa tempus, venenatis erat quis, mattis quam. Fusce fringilla nunc eget sapien sodales, eu sagittis ex semper. Etiam id nisl nulla. Ut eu scelerisque leo, eu fermentum nulla.Phasellus tincidunt, tellus vitae vehicula lobortis, massa urna faucibus dui, quis tempor massa justo ac tellus. Donec faucibus iaculis commodo. Nam id vehicula velit. In est turpis, sollicitudin in lorem vitae, pulvinar sodales velit.Duis metus ipsum, aliquam non luctus in, condimentum non nisl.Vestibulum viverra lorem sed nisl vestibulum placerat.

Pellentesque at erat mi. Nam elit libero, tincidunt vel volutpat quis, euismod et nulla.Phasellus at ex sit amet metus condimentum faucibus. Aliquam vitae rhoncus massa, a mollis dolor.Cras cursus dolor in ante porta feugiat.In hac habitasse platea dictumst.Phasellus sagittis fringilla mi eget pretium. Aenean est purus, blandit vitae facilisis vel, hendrerit eu odio.Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia curae; Nunc sit amet turpis mollis, varius mauris ac, gravida metus.Pellentesque vitae est sed elit malesuada consectetur eget in lectus.Etiam ligula nunc, aliquam id mi eget, sodales aliquam nisi.Nullam convallis orci eu eros tempor blandit.

Phasellus elementum interdum sapien, a facilisis ipsum varius eget.Nunc scelerisque arcu magna, at tempor eros placerat eu.Aliquam facilisis laoreet gravida. Aenean eu eros non elit bibendum blandit nec sit amet enim.Nam ut dui nec dui dignissim sollicitudin id a lacus. Fusce interdum dignissim magna, vel maximus urna consequat tristique.Nam vel faucibus enim. Vestibulum vestibulum maximus ligula, non sodales lectus pulvinar vel.Proin accumsan condimentum cursus. Donec finibus tristique nunc non placerat.

Fusce ultricies purus eget erat suscipit, quis venenatis mi consectetur. Cras viverra velit sed semper interdum. Interdum et malesuada fames ac ante ipsum primis in faucibus.Mauris aliquet et lectus nec porta. Donec ac lectus facilisis, scelerisque justo sit amet, venenatis erat. Nunc id risus vel diam laoreet blandit.Integer nec nisi ac eros dignissim venenatis eget et felis. Vivamus euismod tincidunt sollicitudin. Nam mauris augue, tincidunt ac diam non, eleifend dignissim justo.

Morbi interdum consequat feugiat. Ut semper nisi justo, in congue quam tempor quis. Nunc faucibus ornare mauris, eget bibendum ex commodo sed.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Vivamus sed odio quis dui pretium semper.Fusce auctor, nunc in luctus interdum, nisl neque fermentum nisi, viverra efficitur nisl metus vel elit. Fusce at blandit justo. Duis in lacus elit. Pellentesque at elit velit. Proin interdum odio vel lacus congue, a vehicula massa imperdiet. Aenean molestie leo a sapien egestas, vel viverra dui fermentum. Fusce sollicitudin odio erat, nec tempor tortor congue ac.Maecenas aliquam velit et nibh dignissim, at auctor augue faucibus.

Praesent turpis metus, ultrices ac semper nec, hendrerit eget velit.Sed vehicula, mi sollicitudin pretium tempor, nibh nisl viverra tortor, in cursus leo ex at libero.Fusce condimentum sit amet nisl vel molestie.Cras placerat dui sed rhoncus pharetra. Orci varius natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus.Vestibulum vitae sodales felis. Ut laoreet facilisis ante, ut malesuada eros tincidunt ac.

Phasellus augue sem, rutrum et arcu sed, dignissim volutpat arcu.Etiam volutpat ut neque quis tempor. Pellentesque cursus dapibus arcu, eu imperdiet neque feugiat vel.Fusce sollicitudin mauris arcu, vel aliquam ante elementum quis.Maecenas vestibulum volutpat lacus sed aliquet. Vivamus vel finibus ipsum. Sed nec diam non enim euismod accumsan et ac dui. Lorem ipsum dolor sit amet, consectetur adipiscing elit.Suspendisse pretium interdum enim.

Pellentesque ultricies purus nec turpis imperdiet vestibulum.In ante diam, tempus non dui ut, laoreet consectetur lectus.Praesent convallis dictum lorem fringilla elementum. Phasellus vitae dapibus leo. Nam mi massa, pharetra eget orci a, lacinia hendrerit erat.Vivamus in facilisis turpis. Morbi purus elit, porttitor sit amet leo non, vehicula convallis justo.Integer tristique interdum velit sit amet aliquet.Pellentesque eget quam sit amet neque iaculis semper. Curabitur at aliquet sem, ac blandit augue.

Aliquam erat volutpat.Vestibulum condimentum eros id dignissim hendrerit. Pellentesque rhoncus magna sapien, nec varius felis blandit accumsan.Vestibulum fermentum pulvinar sapien, sit amet condimentum odio efficitur finibus. Integer euismod eros vehicula maximus hendrerit. Nunc pharetra metus ac felis finibus mollis.Mauris suscipit quam ligula, id venenatis nisi auctor sit amet. Proin in velit vel est sagittis pellentesque.Cras malesuada nisl in odio tincidunt, sit amet imperdiet metus vulputate.Suspendisse ligula orci, vestibulum ut libero eget, convallis viverra odio.Aliquam vitae aliquet lectus. Nam aliquam enim in ex bibendum auctor.Vivamus maximus ligula consectetur mauris eleifend luctus.

Sed nec sapien sagittis diam vestibulum tristique efficitur sed sapien. Sed libero tellus, scelerisque ac viverra in, semper ut lacus.Proin id sem pellentesque quam volutpat venenatis.Donec laoreet tellus ac ornare laoreet. In vitae blandit nulla. Nam nec mauris a lorem vulputate viverra.Duis ultrices vulputate leo, molestie sollicitudin lectus.Phasellus ut turpis a dui mattis congue.Cras consectetur nisi velit, at convallis risus laoreet vitae.Phasellus dapibus diam neque, vitae sagittis orci tristique quis.Nam mattis turpis vitae tortor auctor iaculis.Nam pulvinar erat in erat rutrum pretium.Maecenas enim sapien, tempor eu bibendum et, pharetra fermentum felis.

Phasellus consectetur velit mauris, et tempor risus mollis in. Donec ullamcorper iaculis elit, sit amet viverra ante convallis luctus. In ornare odio a dui iaculis aliquet.Ut ullamcorper at nisi sit amet vestibulum.Praesent enim risus, egestas aliquet mattis eu, efficitur bibendum odio.Sed hendrerit ligula dui, a egestas ante pharetra ut.Nulla posuere libero ac pulvinar congue. Donec dapibus diam at quam porttitor, ac lacinia nunc ullamcorper.

Pellentesque eu sollicitudin augue. Sed ut tortor urna. Maecenas consequat enim quis ligula condimentum pharetra.Nam dolor quam, mattis sit amet egestas vel, blandit vel justo.Proin interdum lorem eget elit egestas, et auctor nibh tempus. Cras lobortis lorem et nisi posuere ullamcorper.Nulla sagittis metus eu mollis convallis. Ut sagittis ligula ipsum, dignissim elementum eros euismod at.Interdum et malesuada fames ac ante ipsum primis in faucibus.Praesent laoreet bibendum nulla eu consectetur. Vestibulum velit metus, blandit et maximus non, bibendum sed quam.Sed placerat quam ac lacinia finibus. Donec rutrum pharetra commodo.

Cras interdum ac metus vel volutpat. Lorem ipsum dolor sit amet, consectetur adipiscing elit.Pellentesque eu faucibus nisl. Praesent libero justo, ultrices nec felis id, elementum dapibus orci.Phasellus nec faucibus mi, sit amet elementum dolor. Nam dictum, ex a hendrerit mollis, lacus tellus placerat odio, ac congue velit purus ac est. Integer tempus nibh tincidunt feugiat dignissim.

Nunc placerat sollicitudin magna, vitae hendrerit erat mollis porta.In aliquam blandit elit, vel tempus risus.Donec bibendum, turpis nec tempus gravida, massa urna mollis eros, a finibus est lectus vel quam. Praesent sed porta turpis. Morbi pulvinar neque augue, non feugiat turpis faucibus a.Fusce a tempor tortor. Sed non tempor nisl, ac elementum purus.Aliquam vel nibh blandit, malesuada metus ut, sollicitudin mi. Praesent arcu metus, vestibulum a nisi ac, egestas iaculis lorem.Vivamus vel turpis consectetur quam accumsan.

Curabitur accumsan sem et nulla cursus, egestas ultricies augue eleifend. Curabitur consequat justo at sem faucibus elementum.Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos.Maecenas tincidunt bibendum lorem, a pretium mauris convallis nec.Mauris pellentesque nunc ac mauris lacinia euismod.Praesent justo leo, sollicitudin et libero nec, bibendum mattis ipsum.Cras eu dui viverra, fringilla nisl ut, ornare nulla. Sed molestie dictum ipsum, in blandit nisi venenatis sit amet.Quisque vestibulum accumsan enim, id varius diam facilisis vel.Praesent tincidunt purus nec massa venenatis dignissim.Phasellus luctus ligula tellus, posuere accumsan metus lobortis id.Sed mollis tristique augue in pharetra.Mauris eu laoreet sem. Phasellus tempor tellus eu nulla accumsan ullamcorper.
""";

}