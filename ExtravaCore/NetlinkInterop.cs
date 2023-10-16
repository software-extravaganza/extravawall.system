using System.Buffers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Text;

namespace ExtravaCore;

using System;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

public enum RoutingType : int {
    NONE = 0,
    PRE_ROUTING = 1,  // Signifies if the packet is in the pre-routing stage
    POST_ROUTING = 2  // Signifies if the packet is in the post-routing stage
}

public enum RoutingDecision : int {
    UNDECIDED = 0,    // Signifies that the packet has not been processed yet
    DROP = 1,         // Signifies that the packet should be dropped
    ACCEPT = 2,       // Signifies that the packet should be accepted
    MANIPULATE = 3    // Signifies that the packet has been manipulated
}

public enum EtherType : ushort {
    IPv4 = 0x0800,
    ARP = 0x0806,
    // Add other EtherType values as needed.
}

public enum IPProtocol : byte {
    ICMP = 1,
    TCP = 6,
    UDP = 17,
    // Add other protocol numbers as needed.
}

[Flags]
public enum TCPFlags : byte {
    URG = 0x20,
    ACK = 0x10,
    PSH = 0x08,
    RST = 0x04,
    SYN = 0x02,
    FIN = 0x01
}



// Ethernet header
public class EthernetHeader {
    public byte[] DestinationMac { get; set; }
    public byte[] SourceMac { get; set; }
    public EtherType Type { get; set; }

    public string DestinationMacString => ConvertMacBytesToString(DestinationMac);
    public string SourceMacString => ConvertMacBytesToString(SourceMac);

    private static string ConvertMacBytesToString(byte[] mac) {
        return string.Join(":", mac.Select(b => b.ToString("X2")));
    }
}

// IPv4 header
public class IPHeader {
    public byte VersionAndHeaderLength { get; set; }
    public byte TypeOfService { get; set; }
    public ushort TotalLength { get; set; }
    public ushort Identification { get; set; }
    public ushort FlagsAndOffset { get; set; }
    public byte TTL { get; set; }
    public IPProtocol Protocol { get; set; }
    public ushort HeaderChecksum { get; set; }
    public byte[] SourceAddress { get; set; }
    public byte[] DestinationAddress { get; set; }

    public List<IPOption> Options { get; set; }
    public string DestinationAddressString => IPAddressToString(DestinationAddress);
    public string SourceAddressString => IPAddressToString(SourceAddress);
    public static string IPAddressToString(byte[] address) {
        return string.Join(".", address);
    }
}

public class IPOption {
    public byte Type { get; set; } // Option Type
    public byte Length { get; set; } // Option Length, including the Type and Length fields
    public byte[] Data { get; set; } // Option-specific Data
}

// TCP header
public class TCPHeader {
    public ushort SourcePort { get; set; }
    public ushort DestinationPort { get; set; }
    public uint SequenceNumber { get; set; }
    public uint AcknowledgmentNumber { get; set; }
    public byte DataOffsetAndReserved { get; set; }
    public TCPFlags Flags { get; set; }
    public ushort Window { get; set; }
    public ushort Checksum { get; set; }
    public ushort UrgentPointer { get; set; }

    public List<TCPOption> Options { get; set; }
}

public class TCPOption {
    public byte Kind { get; set; }
    public byte Length { get; set; }
    public byte[] Data { get; set; }
}
public class UDPHeader {
    public ushort SourcePort { get; set; }
    public ushort DestinationPort { get; set; }
    public ushort Length { get; set; }
    public ushort Checksum { get; set; }
}



public class KernelClient {

    public static string PrintByteArray(byte[] bytes) {
        var sb = new StringBuilder("new byte[] { ");
        foreach (var b in bytes) {
            sb.Append(b + ", ");
        }
        sb.Append("}");
        return sb.ToString();
    }
    public static UDPHeader ParseUDPHeader(byte[] data) {
        if (data.Length < 8) // Minimum length for a UDP header
        {
            throw new ArgumentException("Data is too short to be a valid UDP header.");
        }

        return new UDPHeader {
            SourcePort = ConvertFromBytes<ushort>(data.Take(2).ToArray()),
            DestinationPort = ConvertFromBytes<ushort>(data.Skip(2).Take(2).ToArray()),
            Length = ConvertFromBytes<ushort>(data.Skip(4).Take(2).ToArray()),
            Checksum = ConvertFromBytes<ushort>(data.Skip(6).Take(2).ToArray())
        };
    }

    public static EthernetHeader ParseEthernetHeader(byte[] data) {
        return new EthernetHeader {
            DestinationMac = data.Skip(0).Take(6).ToArray(),
            SourceMac = data.Skip(6).Take(6).ToArray(),
            Type = (EtherType)ConvertFromBytes<ushort>(data.Skip(12).Take(2).ToArray())
        };
    }

    // Parsing logic for IP options
    public static List<IPOption> ParseIPOptions(byte[] data) {
        var options = new List<IPOption>();
        int index = 0;

        while (index < data.Length) {
            var optionType = data[index];
            if (optionType == 0) // End of Option List
                break;

            if (optionType == 1) // No Operation
            {
                options.Add(new IPOption { Type = optionType });
                index++;
                continue;
            }

            var optionLength = data[index + 1];
            var optionData = data.Skip(index + 2).Take(optionLength - 2).ToArray();

            options.Add(new IPOption { Type = optionType, Length = optionLength, Data = optionData });

            index += optionLength;
        }

        return options;
    }

    public static IPHeader ParseIPHeader(byte[] data) {
        if (data.Length < 20) {
            throw new ArgumentException("Data is too short to be a valid IP header.");
        }

        byte version = (byte)(data[0] >> 4);
        if (version != 4) // We only handle IPv4 here
        {
            throw new ArgumentException("Only IPv4 is supported.");
        }

        var ipHeader = new IPHeader {
            VersionAndHeaderLength = data[0],
            TypeOfService = data[1],
            TotalLength = ConvertFromBytes<ushort>(data.Skip(2).Take(2).ToArray()),
            Identification = ConvertFromBytes<ushort>(data.Skip(4).Take(2).ToArray()),
            FlagsAndOffset = ConvertFromBytes<ushort>(data.Skip(6).Take(2).ToArray()),
            TTL = data[8],
            Protocol = (IPProtocol)data[9],
            HeaderChecksum = ConvertFromBytes<ushort>(data.Skip(10).Take(2).ToArray()),
            SourceAddress = data.Skip(12).Take(4).ToArray(),
            DestinationAddress = data.Skip(16).Take(4).ToArray()
        };

        // Parse IP options if they exist.
        int ihl = ipHeader.VersionAndHeaderLength & 0x0F;  // Grabbing last 4 bits.
        if (ihl > 5) {
            ipHeader.Options = ParseIPOptions(data.Skip(20).Take((ihl - 5) * 4).ToArray());
        }

        return ipHeader;
    }

    public static TCPHeader ParseTCPHeader(byte[] data) {
        int tcpHeaderLength = (data[12] >> 4) * 4; // Extract header length from data offset field
        return new TCPHeader {
            SourcePort = ConvertFromBytes<ushort>(data.Skip(0).Take(2).ToArray()),
            DestinationPort = ConvertFromBytes<ushort>(data.Skip(2).Take(2).ToArray()),
            SequenceNumber = ConvertFromBytes<uint>(data.Skip(4).Take(4).ToArray()),
            AcknowledgmentNumber = ConvertFromBytes<uint>(data.Skip(8).Take(4).ToArray()),
            DataOffsetAndReserved = data[12],
            Flags = (TCPFlags)data[13],
            Window = ConvertFromBytes<ushort>(data.Skip(14).Take(2).ToArray()),
            Checksum = ConvertFromBytes<ushort>(data.Skip(16).Take(2).ToArray()),
            UrgentPointer = ConvertFromBytes<ushort>(data.Skip(18).Take(2).ToArray()),
            Options = ParseTCPOptions(data, tcpHeaderLength)
        };
    }

    public static List<TCPOption> ParseTCPOptions(byte[] data, int dataOffset) {
        var options = new List<TCPOption>();
        int index = dataOffset;

        while (index < data.Length) {
            var kind = data[index];
            if (kind == 0 || kind == 1) // End or NOP
            {
                options.Add(new TCPOption { Kind = kind });
                index++;
                continue;
            }

            var optionLength = data[index + 1];
            var optionData = data.Skip(index + 2).Take(optionLength - 2).ToArray();

            options.Add(new TCPOption { Kind = kind, Length = optionLength, Data = optionData });
            index += optionLength;
        }

        return options;
    }

    public static byte[] ExtractPayload(byte[] data, int headerLength) {
        return data.Skip(headerLength).ToArray();
    }

    private static T ConvertFromBytes<T>(byte[] data) {
        if (typeof(T) == typeof(ushort)) {
            ushort value = BitConverter.ToUInt16(data, 0);
            return BitConverter.IsLittleEndian ? (T)(object)value : (T)(object)BitConverter.ToUInt16(data.Reverse().ToArray(), 0);
        } else if (typeof(T) == typeof(uint)) {
            uint value = BitConverter.ToUInt32(data, 0);
            return BitConverter.IsLittleEndian ? (T)(object)value : (T)(object)BitConverter.ToUInt32(data.Reverse().ToArray(), 0);
        }
        throw new ArgumentException("Unsupported type.");
    }

    public static async Task StartAsync() {
        const int intSize = sizeof(int);
        while (true) {
            try {
                using FileStream fsRead = new FileStream("/dev/extrava_to_user", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using FileStream fsAck = new FileStream("/dev/extrava_from_user", FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                //using MemoryStream readMem = new MemoryStream();
                while (true) {
                    Console.WriteLine("Waiting for data...");
                    //fsRead.CopyTo(readMem);
                    //Span<byte> readStream = new Span<byte>(readMem.GetBuffer());

                    //Span<byte> headerSpan = stackalloc byte[intSize + intSize];
                    byte[] headerData = new byte[intSize * 3]; // Assuming 64-bit kernel
                    Task<int> headerReadTask = fsRead.ReadAsync(headerData, 0, headerData.Length);
                    //Task headerDelayTask = Task.Delay(1000);
                    //fsRead.Read(headerData, 0, headerData.Length);
                    Task headerCompletedTask = await Task.WhenAny(headerReadTask); //, headerDelayTask);

                    if (headerCompletedTask != headerReadTask) {
                        fsRead.Close();
                        // The timeout elapsed before the read operation completed.
                        throw new TimeoutException("Header read operation timed out.");
                    }

                    (int version, int dataLength, RoutingType routingType) ProcessHeader() {
                        Console.WriteLine("Processing Header");
                        Span<byte> headerSpan = headerData.AsSpan().Slice(0, headerData.Length);
                        if (BitConverter.IsLittleEndian) {
                            headerSpan.Reverse();
                        }

                        int dataLength = BitConverter.ToInt32(headerSpan.Slice(0, intSize));
                        int version = BitConverter.ToInt32(headerSpan.Slice(intSize, intSize));
                        RoutingType routingType = (RoutingType)BitConverter.ToInt32(headerSpan.Slice(intSize * 2, intSize));
                        return (version, dataLength, routingType);
                    }

                    (int version, int dataLength, RoutingType routingType) = ProcessHeader();
                    byte[] packetData = new byte[dataLength];
                    Task<int> dataReadTask = fsRead.ReadAsync(packetData, 0, packetData.Length);
                    /// Task dataDelayTask = Task.Delay(1000);
                    //fsRead.Read(headerData, 0, headerData.Length);
                    Task dataCompletedTask = await Task.WhenAny(dataReadTask); //, dataDelayTask);
                    if (dataCompletedTask != dataReadTask) {
                        fsRead.Close();
                        // The timeout elapsed before the read operation completed.
                        throw new TimeoutException("Data read operation timed out.");
                    }

                    // if (BitConverter.IsLittleEndian) {
                    //     packetData = packetData.Reverse().ToArray();
                    // }

                    var routingDecision = RoutingDecision.UNDECIDED;
                    //EthernetHeader ethernetHeader = ParseEthernetHeader(packetData);
                    //if (ethernetHeader.Type == EtherType.IPv4) {
                    IPHeader ipHeader = ParseIPHeader(packetData); //.Skip(14).ToArray());
                    if (ipHeader.Protocol == IPProtocol.TCP) {
                        TCPHeader tcpHeader = ParseTCPHeader(packetData.Skip(14 + (ipHeader.VersionAndHeaderLength & 0xF) * 4).ToArray());
                        // Continue with further processing.
                    } else if (ipHeader.Protocol == IPProtocol.ICMP) {
                        if ((ipHeader.DestinationAddressString == "1.1.1.2" && routingType == RoutingType.POST_ROUTING) || routingType == RoutingType.PRE_ROUTING) {
                            routingDecision = RoutingDecision.ACCEPT;
                        } else {
                            routingDecision = RoutingDecision.DROP;
                        }
                    }
                    //}
                    //fsRead.Read(packetData, 0, dataLength);

                    void SendResponse(RoutingDecision decision) {
                        Console.WriteLine("Processing Data");
                        // Inspect the packetData as needed

                        //Convert bytes to string
                        // string str = Encoding.UTF8.GetString(packetData);
                        // Console.WriteLine(str);
                        // // Send back a directive
                        byte[] responseHeader = new byte[intSize * 3];
                        //BitConverter.GetBytes(pktId).CopyTo(directiveData, 0);
                        //BitConverter.GetBytes(1).CopyTo(directiveData, sizeof(long)); // For example, "1" for ACCEPT
                        var responseVersionBytes = BitConverter.GetBytes(1);
                        var responseDataBytes = BitConverter.GetBytes(1);
                        var decisionBytes = BitConverter.GetBytes((int)decision);
                        Console.WriteLine($"Sending response: {PrintByteArray(responseVersionBytes.ToArray())} {PrintByteArray(responseDataBytes.ToArray())} {PrintByteArray(decisionBytes.ToArray())}");
                        // if (BitConverter.IsLittleEndian) {
                        //     responseVersionBytes = responseVersionBytes.Reverse().ToArray();
                        //     responseDataBytes = responseDataBytes.Reverse().ToArray();
                        //     decisionBytes = decisionBytes.Reverse().ToArray();
                        // }
                        // Console.WriteLine($"Alternate response: {PrintByteArray(responseVersionBytes.Reverse().ToArray())} {PrintByteArray(responseDataBytes.Reverse().ToArray())} {PrintByteArray(decisionBytes.Reverse().ToArray())}");

                        responseVersionBytes.CopyTo(responseHeader, 0);
                        responseDataBytes.CopyTo(responseHeader, intSize);
                        decisionBytes.CopyTo(responseHeader, intSize * 2);
                        fsAck.Write(responseHeader, 0, responseHeader.Length);
                        fsAck.Flush();
                        Console.WriteLine("Response sent.");
                        //Console.WriteLine(responseHeader);

                        // byte[] responseData = new byte[intSize];

                        // // if (BitConverter.IsLittleEndian) {
                        // //     decisionBytes = decisionBytes.Reverse().ToArray();
                        // // }


                        // fsAck.Write(responseData, 0, responseData.Length);
                        // fsAck.Flush();
                    }

                    SendResponse(routingDecision);

                }

            } catch (IOException ex) {
                if (ex.Message.ToLower().Contains("unknown error 512")) {
                    Console.WriteLine("Can't access device file due to privilages.");
                    Thread.Sleep(1000);
                    continue;
                }
                Thread.Sleep(1000);
            } catch (Exception ex) {
                Console.WriteLine("Error: " + ex.Message);
                Thread.Sleep(1000);
            }
        }

    }

}