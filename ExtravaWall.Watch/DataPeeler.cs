using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace ExtravaWall.Watch {
    public ref struct PeeledResponse {
        public ReadOnlySpan<byte> Peeled { get; set; }
        public ReadOnlySpan<byte> Remaining { get; set; }
    }

    public ref struct DataPeeler {
        private ReadOnlySpan<byte> Data { get; set; }
        public int Length => Data.Length;
        public DataPeeler(ReadOnlySpan<byte> data) {
            Data = data;
        }
        public ReadOnlySpan<byte> PeelBytes(int length) {
            var peeled = Data.Slice(0, length);
            Data = Data.Slice(length);
            return peeled;
        }

        public int PeelBytesToInt32() {
            var peeled = PeelBytes(sizeof(int));
            return BitConverter.ToInt32(peeled);
        }

        public ushort PeelBytesToUInt16() {
            var peeled = PeelBytes(sizeof(ushort));
            return BitConverter.ToUInt16(peeled);
        }

        public uint PeelBytesToUInt32() {
            var peeled = PeelBytes(sizeof(uint));
            return BitConverter.ToUInt32(peeled);
        }

        public ulong PeelBytesToUInt64() {
            var peeled = PeelBytes(sizeof(ulong));
            return BitConverter.ToUInt64(peeled);
        }

        public int PeelBytesToInt32Endian() {
            var peeled = PeelBytes(sizeof(int));
            var writeSpan = new Span<byte>(GC.AllocateUninitializedArray<byte>(peeled.Length));
            writeSpan.Reverse();
            return BitConverter.ToInt32(writeSpan);
        }

        public T PeelBytesToEnum<T>() where T : Enum {
            var peeled = PeelBytesToInt32();
            var enumType = typeof(T);
            return (T)Enum.ToObject(enumType, peeled);
        }
    }
}