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
    POST_ROUTING = 2, // Signifies if the packet is in the post-routing stage
    LOCAL_ROUTING = 3 // Signifies if the packet is in the local-routing stage
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
    HOPOPT = 0,
    ICMP = 1,
    IGMP = 2,
    GGP = 3,
    IPv4 = 4,
    ST = 5,
    TCP = 6,
    CBT = 7,
    EGP = 8,
    IGP = 9,
    BBN_RCC_MON = 10,
    NVP_II = 11,
    PUP = 12,
    ARGUS = 13,
    EMCON = 14,
    XNET = 15,
    CHAOS = 16,
    UDP = 17,
    MUX = 18,
    DCN_MEAS = 19,
    HMP = 20,
    PRM = 21,
    XNS_IDP = 22,
    TRUNK_1 = 23,
    TRUNK_2 = 24,
    LEAF_1 = 25,
    LEAF_2 = 26,
    RDP = 27,
    IRTP = 28,
    ISO_TP4 = 29,
    NETBLT = 30,
    MFE_NSP = 31,
    MERIT_INP = 32,
    DCCP = 33,
    ThreePC = 34,
    IDPR = 35,
    XTP = 36,
    DDP = 37,
    IDPR_CMTP = 38,
    TP_PP = 39,
    IL = 40,
    IPv6 = 41,
    SDRP = 42,
    IPv6_Route = 43,
    IPv6_Frag = 44,
    IDRP = 45,
    RSVP = 46,
    GRE = 47,
    DSR = 48,
    BNA = 49,
    ESP = 50,
    AH = 51,
    I_NLSP = 52,
    SWIPE = 53,
    NARP = 54,
    MOBILE = 55,
    TLSP = 56,
    SKIP = 57,
    IPv6_ICMP = 58,
    IPv6_NoNxt = 59,
    IPv6_Opts = 60,
    AnyHostInternalProtocol = 61,
    CFTP = 62,
    AnyLocalNetwork = 63,
    SAT_EXPAK = 64,
    KRYPTOLAN = 65,
    RVD = 66,
    IPPC = 67,
    AnyDistributedFileSystem = 68,
    SAT_MON = 69,
    VISA = 70,
    IPCV = 71,
    CPNX = 72,
    CPHB = 73,
    WSN = 74,
    PVP = 75,
    BR_SAT_MON = 76,
    SUN_ND = 77,
    WB_MON = 78,
    WB_EXPAK = 79,
    ISO_IP = 80,
    VMTP = 81,
    SECURE_VMTP = 82,
    VINES = 83,
    TTP = 84,
    IPTM = 85,
    NSFNET_IGP = 86,
    DGP = 87,
    TCF = 88,
    EIGRP = 89,
    OSPF = 90,
    Sprite_RPC = 91,
    LARP = 92,
    MTP = 93,
    AX_25 = 94,
    OS = 95,
    MICP = 96,
    SCC_SP = 97,
    ETHERIP = 98,
    ENCAP = 99,
    AnyPrivateEncryptionScheme = 100,
    GMTP = 101,
    IFMP = 102,
    PNNI = 103,
    PIM = 104,
    ARIS = 105,
    SCPS = 106,
    QNX = 107,
    A_N = 108,
    IPComp = 109,
    SNP = 110,
    Compaq_Peer = 111,
    IPX_in_IP = 112,
    VRRP = 113,
    PGM = 114,
    Any0HopProtocol = 115,
    L2TP = 116,
    DDX = 117,
    IATP = 118,
    STP = 119,
    SRP = 120,
    UTI = 121,
    SMP = 122,
    SM = 123,
    PTP = 124,
    ISIS_over_IPv4 = 125,
    FIRE = 126,
    CRTP = 127,
    CRUDP = 128,
    SSCOPMCE = 129,
    IPLT = 130,
    SPS = 131,
    PIPE = 132,
    SCTP = 133,
    FC = 134,
    RSVP_E2E_IGNORE = 135,
    Mobility_Header = 136,
    UDPLite = 137,
    MPLS_in_IP = 138,
    manet = 139,
    Shim6 = 140,
    WESP = 141,
    ROHC = 142,
    Ethernet = 143,
    Experimentation1 = 253,
    Experimentation2 = 254,
    Reserved = 255
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

    public class CreationResult<T> {
        public T? Value { get; private set; }
        public bool Success { get; private set; }
        public string? Error { get; private set; }

        public CreationResult(T? value, bool success, string? error) {
            Value = value;
            Success = success;
            Error = error;
        }
    }

    public static CreationResult<IPHeader> ParseIPHeader(ReadOnlySpan<byte> data) {
        if (data.Length < 20) {
            return new CreationResult<IPHeader>(null, false, "Data is too short to be a valid IP header.");
        }

        byte version = (byte)(data[0] >> 4);
        if (version != 4) // We only handle IPv4 here
        {
            return new CreationResult<IPHeader>(null, false, "Only IPv4 is supported.");
        }

        var ipHeader = new IPHeader {
            VersionAndHeaderLength = data[0],
            TypeOfService = data[1],
            TotalLength = ConvertFromBytes<ushort>(data.Slice(2, 2).ToArray()),
            Identification = ConvertFromBytes<ushort>(data.Slice(4, 2).ToArray()),
            FlagsAndOffset = ConvertFromBytes<ushort>(data.Slice(6, 2).ToArray()),
            TTL = data[8],
            Protocol = (IPProtocol)data[9],
            HeaderChecksum = ConvertFromBytes<ushort>(data.Slice(10, 2).ToArray()),
            SourceAddress = data.Slice(12, 4).ToArray(),
            DestinationAddress = data.Slice(16, 4).ToArray()
        };

        // Parse IP options if they exist.
        int ihl = ipHeader.VersionAndHeaderLength & 0x0F;  // Grabbing last 4 bits.
        if (ihl > 5) {
            ipHeader.Options = ParseIPOptions(data.Slice(20, (ihl - 5) * 4).ToArray());
        }

        return new CreationResult<IPHeader>(ipHeader, true, null);
    }

    public static TCPHeader ParseTCPHeader(Span<byte> data) {
        if (data.Length < 20) {
            return null;
        }

        int tcpHeaderLength = (data[12] >> 4) * 4; // Extract header length from data offset field
        return new TCPHeader {
            SourcePort = ConvertFromBytes<ushort>(data.Slice(0, 2).ToArray()),
            DestinationPort = ConvertFromBytes<ushort>(data.Slice(2, 2).ToArray()),
            SequenceNumber = ConvertFromBytes<uint>(data.Slice(4, 4).ToArray()),
            AcknowledgmentNumber = ConvertFromBytes<uint>(data.Slice(8, 4).ToArray()),
            DataOffsetAndReserved = data[12],
            Flags = (TCPFlags)data[13],
            Window = ConvertFromBytes<ushort>(data.Slice(14, 2).ToArray()),
            Checksum = ConvertFromBytes<ushort>(data.Slice(16, 2).ToArray()),
            UrgentPointer = ConvertFromBytes<ushort>(data.Slice(18, 2).ToArray()),
            Options = ParseTCPOptions(data, tcpHeaderLength)
        };
    }

    public static List<TCPOption> ParseTCPOptions(Span<byte> data, int dataOffset) {
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
            if (index + 2 + optionLength - 2 >= data.Length || optionLength - 2 < 0) {
                return options;
            }

            var optionData = data.Slice(index + 2, optionLength - 2).ToArray();

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
        long packetsProcessed = 0;
        while (true) {
            try {
                using FileStream fsRead = new FileStream("/dev/extrava_to_process", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using FileStream fsAck = new FileStream("/dev/extrava_from_process", FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                //using MemoryStream readMem = new MemoryStream();
                while (true) {
                    //Console.WriteLine("Waiting for data...");

                    byte[] headerData = new byte[intSize * 4]; // Assuming 64-bit kernel
                    Task<int> headerReadTask = fsRead.ReadAsync(headerData, 0, headerData.Length);
                    Task headerCompletedTask = await Task.WhenAny(headerReadTask); //, headerDelayTask);

                    if (headerCompletedTask != headerReadTask) {
                        fsRead.Close();
                        // The timeout elapsed before the read operation completed.
                        throw new TimeoutException("Header read operation timed out.");
                    }

                    (bool reset, int version, int dataLength, RoutingType routingType) ProcessHeader() {
                        //Console.WriteLine("Processing Header");
                        Span<byte> rawSpan = headerData.AsSpan();
                        Span<byte> resetByte = rawSpan.Slice(0, intSize);
                        Span<byte> headerSpan = rawSpan.Slice(0, headerData.Length);
                        if (BitConverter.IsLittleEndian) {
                            headerSpan.Reverse();
                        }

                        int flags = BitConverter.ToInt32(headerSpan.Slice(intSize * 3, intSize));
                        int dataLength = BitConverter.ToInt32(headerSpan.Slice(0, intSize));
                        int version = BitConverter.ToInt32(headerSpan.Slice(intSize, intSize));
                        RoutingType routingType = (RoutingType)BitConverter.ToInt32(headerSpan.Slice(intSize * 2, intSize));

                        bool shouldResetRead = flags == 1;
                        return (shouldResetRead, version, dataLength, routingType);
                    }

                    (bool shouldResetRead, int version, int dataLength, RoutingType routingType) = ProcessHeader();
                    if (shouldResetRead) {
                        continue;
                    }

                    byte[] rawData = new byte[dataLength + intSize];
                    Task<int> dataReadTask = fsRead.ReadAsync(rawData, 0, rawData.Length);
                    Task dataCompletedTask = await Task.WhenAny(dataReadTask); //, dataDelayTask);
                    if (dataCompletedTask != dataReadTask) {
                        fsRead.Close();
                        // The timeout elapsed before the read operation completed.
                        throw new TimeoutException("Data read operation timed out.");
                    }

                    var rawSpan = rawData.AsMemory();
                    var flagsSpan = rawSpan.Slice(0, intSize);
                    var packetData = rawSpan.Slice(intSize).ToArray();
                    var flags = BitConverter.ToInt32(flagsSpan.Slice(0, intSize).ToArray());
                    bool shouldResetWrite = flags == 1;
                    if (shouldResetWrite) {
                        continue;
                    }

                    var routingDecision = RoutingDecision.ACCEPT;
                    // //EthernetHeader ethernetHeader = ParseEthernetHeader(packetData);
                    // //if (ethernetHeader.Type == EtherType.IPv4) {
                    // CreationResult<IPHeader> ipHeaderResult = ParseIPHeader(packetData); //.Skip(14).ToArray());
                    // if (!ipHeaderResult.Success || ipHeaderResult.Value is null) {
                    //     //Console.WriteLine(ipHeaderResult.Error);
                    //     continue;
                    // }

                    // IPHeader ipHeader = ipHeaderResult.Value;
                    // if (ipHeader.Protocol == IPProtocol.TCP) {
                    //     TCPHeader tcpHeader = ParseTCPHeader(packetData.Skip(14 + (ipHeader.VersionAndHeaderLength & 0xF) * 4).ToArray());
                    //     // Continue with further processing.
                    // } else if (ipHeader.Protocol == IPProtocol.ICMP) {
                    //     // var routeString = routingType switch {
                    //     //     RoutingType.NONE => "None",
                    //     //     RoutingType.PRE_ROUTING => "Pre-Routing",
                    //     //     RoutingType.POST_ROUTING => "Post-Routing",
                    //     //     RoutingType.LOCAL_ROUTING => "Local-Routing",
                    //     //     _ => "Unknown"
                    //     // };

                    //     // Console.WriteLine($"Ping! Source {ipHeader.SourceAddressString}, Destination {ipHeader.DestinationAddressString}, Routing Type {routeString}");
                    //     // if ((ipHeader.DestinationAddressString == "1.1.1.2" && routingType != RoutingType.PRE_ROUTING) || routingType == RoutingType.PRE_ROUTING) {
                    //     //     routingDecision = RoutingDecision.ACCEPT;
                    //     // } else {
                    //     //     routingDecision = RoutingDecision.DROP;
                    //     // }
                    // }
                    //}
                    //fsRead.Read(packetData, 0, dataLength);

                    void SendResponse(RoutingDecision decision) {
                        //Console.WriteLine("Processing Data");
                        // Inspect the packetData as needed

                        //Convert bytes to string
                        // string str = Encoding.UTF8.GetString(packetData);
                        // Console.WriteLine(str);
                        // // Send back a directive
                        byte[] responseHeader = new byte[intSize * 2];
                        //BitConverter.GetBytes(pktId).CopyTo(directiveData, 0);
                        //BitConverter.GetBytes(1).CopyTo(directiveData, sizeof(long)); // For example, "1" for ACCEPT
                        var responseVersionBytes = BitConverter.GetBytes(1);
                        var responseLengthBytes = BitConverter.GetBytes(intSize);
                        var decisionBytes = BitConverter.GetBytes((int)decision);
                        // if (BitConverter.IsLittleEndian) {
                        //     responseVersionBytes = responseVersionBytes.Reverse().ToArray();
                        //     responseDataBytes = responseDataBytes.Reverse().ToArray();
                        //     decisionBytes = decisionBytes.Reverse().ToArray();
                        // }
                        // Console.WriteLine($"Alternate response: {PrintByteArray(responseVersionBytes.Reverse().ToArray())} {PrintByteArray(responseDataBytes.Reverse().ToArray())} {PrintByteArray(decisionBytes.Reverse().ToArray())}");

                        responseVersionBytes.CopyTo(responseHeader, 0);
                        responseLengthBytes.CopyTo(responseHeader, intSize);
                        fsAck.Write(responseHeader, 0, responseHeader.Length);
                        fsAck.Flush();
                        //Console.WriteLine("Response header sent.");

                        byte[] responseData = new byte[intSize];
                        decisionBytes.CopyTo(responseData, 0);
                        fsAck.Write(responseData, 0, responseData.Length);
                        fsAck.Flush();
                        //Console.WriteLine("Response data sent.");
                        //Console.WriteLine(responseHeader);
                        packetsProcessed++;
                        //if (packetsProcessed % 1000 == 0) {
                        //Console.Write(".");
                        //}

                        if (packetsProcessed % 5000 == 0) {
                            Console.Write($"Processed {packetsProcessed} packets.");
                        }
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


    public static string IpProtocolToString(int proto) => proto switch {
        0 => "HOPOPT (IPv6 Hop-by-Hop Option)",
        1 => "ICMP (Internet Control Message)",
        2 => "IGMP (Internet Group Management)",
        3 => "GGP (Gateway-to-Gateway)",
        4 => "IPv4 (IPv4 encapsulation)",
        5 => "ST (Stream)",
        6 => "TCP (Transmission Control)",
        7 => "CBT",
        8 => "EGP (Exterior Gateway Protocol)",
        9 => "IGP (any private interior gateway)",
        10 => "BBN-RCC-MON (BBN RCC Monitoring)",
        11 => "NVP-II (Network Voice Protocol)",
        12 => "PUP (PUP)",
        13 => "ARGUS (deprecated)",
        14 => "EMCON (EMCON)",
        15 => "XNET (Cross Net Debugger)",
        16 => "CHAOS (Chaos)",
        17 => "UDP (User Datagram)",
        18 => "MUX (Multiplexing)",
        19 => "DCN-MEAS (DCN Measurement Subsystems)",
        20 => "HMP (Host Monitoring)",
        21 => "PRM (Packet Radio Measurement)",
        22 => "XNS-IDP (XEROX NS IDP)",
        23 => "TRUNK-1",
        24 => "TRUNK-2",
        25 => "LEAF-1",
        26 => "LEAF-2",
        27 => "RDP (Reliable Data Protocol)",
        28 => "IRTP (Internet Reliable Transaction)",
        29 => "ISO-TP4 (ISO Transport Protocol Class 4)",
        30 => "NETBLT (Bulk Data Transfer Protocol)",
        31 => "MFE-NSP (MFE Network Services Protocol)",
        32 => "MERIT-INP (MERIT Internodal Protocol)",
        33 => "DCCP (Datagram Congestion Control Protocol)",
        34 => "3PC (Third Party Connect Protocol)",
        35 => "IDPR (Inter-Domain Policy Routing Protocol)",
        36 => "XTP",
        37 => "DDP (Datagram Delivery Protocol)",
        38 => "IDPR-CMTP (IDPR Control Message Transport Proto)",
        39 => "TP++ (TP++ Transport Protocol)",
        40 => "IL (IL Transport Protocol)",
        41 => "IPv6 (IPv6 encapsulation)",
        42 => "SDRP (Source Demand Routing Protocol)",
        43 => "IPv6-Route (Routing Header for IPv6)",
        44 => "IPv6-Frag (Fragment Header for IPv6)",
        45 => "IDRP (Inter-Domain Routing Protocol)",
        46 => "RSVP (Reservation Protocol)",
        47 => "GRE (Generic Routing Encapsulation)",
        48 => "DSR (Dynamic Source Routing Protocol)",
        49 => "BNA",
        50 => "ESP (Encap Security Payload)",
        51 => "AH (Authentication Header)",
        52 => "I-NLSP (Integrated Net Layer Security TUBA)",
        53 => "SWIPE (deprecated)",
        54 => "NARP (NBMA Address Resolution Protocol)",
        55 => "MOBILE (IP Mobility)",
        56 => "TLSP (Transport Layer Security Protocol using Kryptonet key management)",
        57 => "SKIP",
        58 => "IPv6-ICMP (ICMP for IPv6)",
        59 => "IPv6-NoNxt (No Next Header for IPv6)",
        60 => "IPv6-Opts (Destination Options for IPv6)",
        61 => "any host internal protocol",
        62 => "CFTP",
        63 => "any local network",
        64 => "SAT-EXPAK (SATNET and Backroom EXPAK)",
        65 => "KRYPTOLAN",
        66 => "RVD (MIT Remote Virtual Disk Protocol)",
        67 => "IPPC (Internet Pluribus Packet Core)",
        68 => "any distributed file system",
        69 => "SAT-MON (SATNET Monitoring)",
        70 => "VISA (VISA Protocol)",
        71 => "IPCV (Internet Packet Core Utility)",
        72 => "CPNX (Computer Protocol Network Executive)",
        73 => "CPHB (Computer Protocol Heart Beat)",
        74 => "WSN (Wang Span Network)",
        75 => "PVP (Packet Video Protocol)",
        76 => "BR-SAT-MON (Backroom SATNET Monitoring)",
        77 => "SUN-ND (SUN ND PROTOCOL-Temporary)",
        78 => "WB-MON (WIDEBAND Monitoring)",
        79 => "WB-EXPAK (WIDEBAND EXPAK)",
        80 => "ISO-IP (ISO Internet Protocol)",
        81 => "VMTP (VMTP)",
        82 => "SECURE-VMTP (SECURE-VMTP)",
        83 => "VINES",
        84 => "TTP (Transaction Transport Protocol)",
        85 => "IPTM (Internet Protocol Traffic Manager)",
        86 => "NSFNET-IGP (NSFNET-IGP)",
        87 => "DGP (Dissimilar Gateway Protocol)",
        88 => "TCF",
        89 => "EIGRP (EIGRP)",
        90 => "OSPF (OSPF IGP)",
        91 => "Sprite-RPC (Sprite RPC Protocol)",
        92 => "LARP (Locus Address Resolution Protocol)",
        93 => "MTP (Multicast Transport Protocol)",
        94 => "AX.25",
        95 => "OS",
        96 => "MICP (Mobile Internetworking Control Pro.)",
        97 => "SCC-SP (Semaphore Communications Sec. Pro.)",
        98 => "ETHERIP (Ethernet-within-IP Encapsulation)",
        99 => "ENCAP",
        100 => "any private encryption scheme",
        101 => "GMTP",
        102 => "IFMP (Ipsilon Flow Management Protocol)",
        103 => "PNNI (PNNI over IP)",
        104 => "PIM (Protocol Independent Multicast)",
        105 => "ARIS",
        106 => "SCPS (SCPS)",
        107 => "QNX",
        108 => "A/N",
        109 => "IPComp (IP Payload Compression Protocol)",
        110 => "SNP (Sitara Networks Protocol)",
        111 => "Compaq-Peer (Compaq Peer Protocol)",
        112 => "IPX-in-IP (IPX in IP)",
        113 => "VRRP (Virtual Router Redundancy Protocol)",
        114 => "PGM (PGM Reliable Transport Protocol)",
        115 => "any 0-hop protocol",
        116 => "L2TP (Layer Two Tunneling Protocol)",
        117 => "DDX (D-II Data Exchange (DDX))",
        118 => "IATP (Interactive Agent Transfer Protocol)",
        119 => "STP (Schedule Transfer Protocol)",
        120 => "SRP (SpectraLink Radio Protocol)",
        121 => "UTI (Universal Transport Interface Protocol)",
        122 => "SMP (Simple Message Protocol)",
        123 => "SM (Simple Multicast Protocol)",
        124 => "PTP (Performance Transparency Protocol)",
        125 => "ISIS over IPv4",
        126 => "FIRE",
        127 => "CRTP (Combat Radio Transport Protocol)",
        128 => "CRUDP (Combat Radio User Datagram)",
        129 => "SSCOPMCE",
        130 => "IPLT",
        131 => "SPS (Secure Packet Shield)",
        132 => "PIPE (Private IP Encapsulation within IP)",
        133 => "SCTP (Stream Control Transmission Protocol)",
        134 => "FC (Fibre Channel)",
        135 => "RSVP-E2E-IGNORE",
        136 => "Mobility Header",
        137 => "UDPLite",
        138 => "MPLS-in-IP",
        139 => "manet (MANET Protocols)",
        140 => "Shim6 (Shim6 Protocol)",
        141 => "WESP (Wrapped Encapsulating Security Payload)",
        142 => "ROHC (Robust Header Compression)",
        143 => "Ethernet",
        253 => "Use for experimentation and testing",
        254 => "Use for experimentation and testing",
        255 => "Reserved",
        _ => "UNKNOWN_PROTOCOL"
    };


}