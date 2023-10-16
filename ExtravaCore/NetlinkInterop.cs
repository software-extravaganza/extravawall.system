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

public static class NetlinkInterop {
    public const int AF_NETLINK = 16;
    public const int SOCK_RAW = 3;
    public const int NETLINK_USER = 31;

    [StructLayout(LayoutKind.Sequential)]
    public struct sockaddr_nl {
        public ushort nl_family;
        public ushort nl_pad;
        public uint nl_pid;
        public uint nl_groups;
    }

    [DllImport("libc", SetLastError = true)]
    public static extern int socket(int domain, int type, int protocol);

    [DllImport("libc", SetLastError = true)]
    public static extern int bind(int sockfd, ref sockaddr_nl addr, int addrlen);

    [DllImport("libc", SetLastError = true)]
    public static extern int sendto(int sockfd, byte[] buf, int len, int flags, ref sockaddr_nl dest_addr, int addrlen);

    [DllImport("libc", SetLastError = true)]
    public static extern int recvfrom(int sockfd, byte[] buf, int len, int flags, ref sockaddr_nl src_addr, ref int addrlen);


    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int open(string pathname, int flags);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int close(int fd);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern IntPtr mmap(IntPtr addr, uint length, int prot, int flags, int fd, uint offset);

    [DllImport("libc.so.6", SetLastError = true)]
    public static extern int munmap(IntPtr addr, uint length);

    // public void UseSharedMemory() {
    //     int fd = open("/dev/mychardev", 0); // O_RDONLY
    //     if (fd < 0) {
    //         // Handle error
    //     }

    //     IntPtr sharedMemPtr = mmap(IntPtr.Zero, MEM_SIZE, PROT_READ, MAP_SHARED, fd, 0);
    //     if (sharedMemPtr == (IntPtr)(-1)) {
    //         // Handle error
    //     }

    //     // Use shared memory: For example, read first byte:
    //     byte firstByte = Marshal.ReadByte(sharedMemPtr);

    //     // Cleanup
    //     munmap(sharedMemPtr, MEM_SIZE);
    //     close(fd);
    // }
}




public class NetListener {
    private int sockfd;

    public void Begin() {
        sockfd = NetlinkInterop.socket(NetlinkInterop.AF_NETLINK, NetlinkInterop.SOCK_RAW, NetlinkInterop.NETLINK_USER);

        if (sockfd < 0) {
            Console.WriteLine("Failed to create socket.");
            return;
        }

        var addr = new NetlinkInterop.sockaddr_nl {
            nl_family = NetlinkInterop.AF_NETLINK,
            nl_pid = (uint)Process.GetCurrentProcess().Id,
            nl_groups = 0
        };

        if (NetlinkInterop.bind(sockfd, ref addr, Marshal.SizeOf(typeof(NetlinkInterop.sockaddr_nl))) != 0) {
            Console.WriteLine("Failed to bind socket.");
            return;
        }

        // Send registration message (if required by your kernel module)
        byte[] message = Encoding.ASCII.GetBytes("YOUR REGISTRATION MESSAGE");
        var kernelAddr = new NetlinkInterop.sockaddr_nl {
            nl_family = NetlinkInterop.AF_NETLINK,
            nl_pid = 0, // Destination PID = 0 for kernel
            nl_groups = 0
        };
        NetlinkInterop.sendto(sockfd, message, message.Length, 0, ref kernelAddr, Marshal.SizeOf(typeof(NetlinkInterop.sockaddr_nl)));

        // Listen for packets
        byte[] buffer = new byte[4096];
        while (true) {
            int addrLen = Marshal.SizeOf(typeof(NetlinkInterop.sockaddr_nl));
            var srcAddr = new NetlinkInterop.sockaddr_nl();
            int len = NetlinkInterop.recvfrom(sockfd, buffer, buffer.Length, 0, ref srcAddr, ref addrLen);
            if (len > 0) {
                Console.WriteLine(Encoding.ASCII.GetString(buffer, 0, len));
            }
        }
    }
}




public class KernelInterceptor {
    const int MAX_PAYLOAD = 1500; // arbitrary max payload size

    [DllImport("libnetlink.so", CharSet = CharSet.Ansi)]
    public static extern int connect_to_kernel();

    [DllImport("libnetlink.so", CharSet = CharSet.Ansi)]
    public static extern void close_connection();

    [DllImport("libnetlink.so", CharSet = CharSet.Ansi)]
    public static extern int receive_data_from_kernel(StringBuilder buffer, int length);


    [DllImport("libnetlink.so", CallingConvention = CallingConvention.Cdecl)]
    public static extern int send_data(byte[] message, int length);

    public void Start() {
        StringBuilder packetData = new StringBuilder(MAX_PAYLOAD);
        var connectionStatus = connect_to_kernel();
        if (connectionStatus != 0) {
            throw new Exception($"Failed to connect to kernel module (error code: {connectionStatus})");
        }

        byte[] buffer = new byte[MAX_PAYLOAD];

        while (true) {
            int len = receive_data_from_kernel(packetData, MAX_PAYLOAD);
            if (len == -1) {
                Console.WriteLine("Error receiving message");
                break;
            }

            // Handle the packet here. 
            // You can modify the `buffer` array as required.

            // If you wish to send it back (maybe after modification):
            if (send_data(buffer, len) == -1) {
                Console.WriteLine("Error sending message");
            }

            // If you want to drop the packet, just continue the loop without sending it back.
            Console.Write(".");
        }

        close_connection();
    }
}

public class XdpWrapper {
    const string LIB_PATH = "libxdpwrapper.so";

    [StructLayout(LayoutKind.Sequential)]
    public struct Packet {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1500)]
        public byte[] data;
        public UIntPtr len;
    }

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr init_socket(string ifname);

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern int get_packet(out Packet pkt);

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern void send_decision(int decision);

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern void cleanup();

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr list_interfaces();

    public static string? InitializeSocket(string interfaceName) {
        IntPtr resultPtr = init_socket(interfaceName);
        if (resultPtr == IntPtr.Zero) {
            return null; // Success
        }
        return Marshal.PtrToStringAnsi(resultPtr);
    }
}

public class NetListener2 {
    private CancellationTokenSource cts = new CancellationTokenSource();
    public NetListener2() {
    }

    public void Stop() {
        cts.Cancel();
    }

    public void Start() {
        string interfaces = Marshal.PtrToStringAnsi(XdpWrapper.list_interfaces());
        Console.WriteLine($"Available interfaces: {interfaces}");

        if (!interfaces.Contains("ens18")) {
            Console.WriteLine($"Interface ens18 not found among available interfaces.");
            return;
        }

        string? errorMessage;
        if ((errorMessage = XdpWrapper.InitializeSocket("ens18")) != null) {
            Console.WriteLine($"Failed to initialize AF_XDP socket. {errorMessage}");
            return;
        }

        while (!cts.Token.IsCancellationRequested)  // Just a simple loop for demonstration purposes
        {
            var pkt = new XdpWrapper.Packet();
            if (XdpWrapper.get_packet(out pkt) == 0) {
                Console.WriteLine($"Received packet of length: {pkt.len}");

                // Analyze the packet using pkt.data
                // ...

                // Make a decision based on analysis
                int decision = 1;  // Example: 1 = Forward, 0 = Drop
                XdpWrapper.send_decision(decision);
            }
        }

        XdpWrapper.cleanup();
    }
}

public class NFQueueInterop {
    const string LIB_PATH = "libnfqueuehandler.so";

    [DllImport(LIB_PATH, CallingConvention = CallingConvention.Cdecl)]
    public static extern void start_nfqueue();
}


public class NetListener3 {
    private CancellationTokenSource cts = new CancellationTokenSource();
    public NetListener3() {
    }

    public void Stop() {
        cts.Cancel();
    }

    public void Start() {
        NFQueueInterop.start_nfqueue();
    }
}


public class NetListener4 {


    const string LibNetfilterQueue = "libnetfilter_queue.so";
    const int BUFSIZE = 4096;

    [DllImport(LibNetfilterQueue)]
    public static extern IntPtr nfq_open();

    [DllImport(LibNetfilterQueue)]
    public static extern int nfq_close(IntPtr h);

    [DllImport(LibNetfilterQueue)]
    public static extern IntPtr nfq_bind_pf(IntPtr h, ushort pf);

    [DllImport(LibNetfilterQueue)]
    public static extern IntPtr nfq_create_queue(IntPtr h, ushort num, nfq_callback cb, IntPtr data);

    [DllImport(LibNetfilterQueue)]
    public static extern int nfq_set_verdict(IntPtr qh, uint id, uint verdict, uint data_len, IntPtr buf);

    [DllImport(LibNetfilterQueue)]
    public static extern int nfq_fd(IntPtr h);

    [DllImport(LibNetfilterQueue)]
    public static extern int nfq_handle_packet(IntPtr h, IntPtr buf, int len);

    // For the recv function
    [DllImport("libc.so.6")]
    public static extern int recv(int sockfd, IntPtr buf, int len, int flags);

    [DllImport("libc.so.6")]
    public static extern IntPtr strerror(int errnum);

    [DllImport("libc.so.6")]
    private static extern IntPtr __errno_location();

    // The delegate type for the callback
    public delegate int nfq_callback(IntPtr qh, IntPtr nfmsg, IntPtr nfad, IntPtr data);

    const int F_SETFL = 4;
    const int O_NONBLOCK = 04000;

    [DllImport("libc.so.6", SetLastError = true)]
    private static extern int fcntl(int fd, int cmd, int arg);


    [StructLayout(LayoutKind.Sequential)]
    public struct nfqnl_msg_packet_hdr {
        public ushort hw_protocol;
        public byte hook;
        public byte _pad;
        public uint packet_id;
    }

    [DllImport(LibNetfilterQueue)]
    public static extern IntPtr nfq_get_msg_packet_hdr(IntPtr nfad);


    [StructLayout(LayoutKind.Sequential)]
    public struct timeval {
        public long tv_sec;
        public long tv_usec;
    }

    [DllImport("libc.so.6", SetLastError = true)]
    private static extern int select(int nfds, ref fd_set readfds, IntPtr writefds, IntPtr exceptfds, ref timeval timeout);

    private const int FD_SETSIZE = 1024;

    [StructLayout(LayoutKind.Sequential)]
    public struct fd_set {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FD_SETSIZE / 8 / sizeof(ulong))]
        public ulong[] fds_bits;
    }

    private static void FD_ZERO(ref fd_set set) {
        for (int i = 0; i < set.fds_bits.Length; i++)
            set.fds_bits[i] = 0;
    }

    private static void FD_SET(int fd, ref fd_set set) {
        set.fds_bits[fd / (8 * sizeof(ulong))] |= (1UL << (fd % (8 * sizeof(ulong))));
    }
    private static int GetErrno() {
        return Marshal.ReadInt32(__errno_location());
    }
    private static string? GetLastErrorMessage() {
        return Marshal.PtrToStringAnsi(strerror(GetErrno()));
    }
    static int PacketHandler(IntPtr qh, IntPtr nfmsg, IntPtr nfad, IntPtr data) {
        // For demonstration, just print the packet ID and then allow it
        IntPtr hdr_ptr = nfq_get_msg_packet_hdr(nfad);
        if (hdr_ptr == IntPtr.Zero) {
            Console.WriteLine("Error retrieving packet header.");
            return -1;
        }

        nfqnl_msg_packet_hdr header = Marshal.PtrToStructure<nfqnl_msg_packet_hdr>(hdr_ptr);
        uint id = header.packet_id;
        Console.WriteLine($"Received packet with ID: {id}");
        nfq_set_verdict(qh, id, 1, 0, IntPtr.Zero); // 1 = NF_ACCEPT
        return 0;
    }


    public static void Start() {
        IntPtr handle = nfq_open();
        if (handle == IntPtr.Zero) {
            Console.WriteLine($"Error opening NFQueue: {GetLastErrorMessage()}");
            return;
        }

        if (nfq_bind_pf(handle, 2) < 0) // 2 = AF_INET for IPv4
        {
            Console.WriteLine($"Error binding to AF_INET: {GetLastErrorMessage()}");
            nfq_close(handle);
            return;
        }

        nfq_callback cb = PacketHandler;
        if (nfq_create_queue(handle, 1, cb, IntPtr.Zero) == IntPtr.Zero) {
            Console.WriteLine($"Error creating queue: {GetLastErrorMessage()}");
            nfq_close(handle);
            return;
        }

        // Simple event loop to process packets
        int fd = nfq_fd(handle);
        fcntl(fd, F_SETFL, O_NONBLOCK);
        IntPtr buf = Marshal.AllocHGlobal(BUFSIZE);

        fd_set readSet = new fd_set { fds_bits = new ulong[FD_SETSIZE / 8 / sizeof(ulong)] };
        timeval timeout;

        timeout.tv_sec = 5;  // Set timeout to 5 seconds for demonstration. Adjust as needed.
        timeout.tv_usec = 0;

        FD_ZERO(ref readSet);
        FD_SET(fd, ref readSet);
        while (true) {
            int selectResult = select(fd + 1, ref readSet, IntPtr.Zero, IntPtr.Zero, ref timeout);
            if (selectResult > 0) {
                Console.WriteLine("Data available for reading.");
                int received = recv(fd, buf, BUFSIZE, 0);
                nfq_handle_packet(handle, buf, received);
            } else if (selectResult == 0) {
                Console.WriteLine("select() timed out.");
            } else {
                Console.WriteLine($"Error in select(): {GetLastErrorMessage()}");
            }
        }
        // try {
        //     while (true) {
        //         int received = recv(fd, buf, BUFSIZE, 0);
        //         if (received < 0) {
        //             Console.WriteLine($"Error receiving packet: {GetLastErrorMessage()}");
        //             break;
        //         }
        //         nfq_handle_packet(handle, buf, received);
        //     }
        // } finally {
        //     Marshal.FreeHGlobal(buf);
        // }

        nfq_close(handle);
    }
}



public class NfqnlTest {
    const string LibName = "libnetfilter_queue.so";

    // Structure and enum declarations would go here...

    // P/Invoke declarations
    [DllImport(LibName)]
    public static extern IntPtr nfq_open();

    [DllImport(LibName)]
    public static extern int nfq_unbind_pf(IntPtr handle, uint pf);

    [DllImport(LibName)]
    public static extern int nfq_bind_pf(IntPtr handle, uint pf);

    [DllImport(LibName)]
    public static extern IntPtr nfq_create_queue(IntPtr handle, ushort num,
        NfqCallback callback, IntPtr data);

    [DllImport(LibName)]
    public static extern int nfq_set_mode(IntPtr qh, uint mode, uint range);

    [DllImport(LibName)]
    public static extern int nfq_fd(IntPtr handle);

    [DllImport(LibName)]
    public static extern int nfq_handle_packet(IntPtr handle, byte[] buf, int len);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr nfq_get_msg_packet_hdr(IntPtr nfad);

    [DllImport("libc.so.6")]
    public static extern ushort ntohs(ushort netshort);

    [DllImport("libc.so.6")]
    public static extern uint ntohl(uint netlong);

    public delegate int NfqCallback(IntPtr qh, IntPtr nfmsg, IntPtr nfad, IntPtr data);

    public static void Main() {
        IntPtr h = nfq_open();
        if (h == IntPtr.Zero) {
            Console.WriteLine("Error during nfq_open()");
            Environment.Exit(1);
        }

        if (nfq_unbind_pf(h, (uint)AddressFamily.InterNetwork) < 0) {
            Console.WriteLine("Error during nfq_unbind_pf()");
            Environment.Exit(1);
        }

        if (nfq_bind_pf(h, (uint)AddressFamily.InterNetwork) < 0) {
            Console.WriteLine("Error during nfq_bind_pf()");
            Environment.Exit(1);
        }

        IntPtr qh = nfq_create_queue(h, 0, Callback, IntPtr.Zero);
        if (qh == IntPtr.Zero) {
            Console.WriteLine("Error during nfq_create_queue()");
            Environment.Exit(1);
        }

        if (nfq_set_mode(qh, 2 /* NFQNL_COPY_PACKET */, 0xffff) < 0) {
            Console.WriteLine("Can't set packet_copy mode");
            Environment.Exit(1);
        }

        int fd = nfq_fd(h);
        byte[] buffer = new byte[4096];

        try {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);

            var localIP = IPAddress.Parse("10.1.250.226"); // replace with your local IP

            socket.Bind(new IPEndPoint(localIP, fd));
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.HeaderIncluded, true);

            while (true) {
                int received = socket.Receive(buffer);
                if (received > 0) {
                    nfq_handle_packet(h, buffer, received);
                }
            }
        } catch (Exception ex) {
            Console.WriteLine(ex);
        }
    }

    public static int Callback(IntPtr qh, IntPtr nfmsg, IntPtr nfad, IntPtr data) {
        var ph = nfq_get_msg_packet_hdr(nfad);
        if (ph != IntPtr.Zero) {
            var pktHdr = Marshal.PtrToStructure<nfqnl_msg_packet_hdr>(ph);
            uint id = ntohl(pktHdr.packet_id);
            Console.WriteLine($"hw_protocol=0x{ntohs(pktHdr.hw_protocol):X4} hook={pktHdr.hook} id={id}");
        }


        return 0; // or whatever verdict you decide
    }

}

[StructLayout(LayoutKind.Sequential)]
public struct nfqnl_msg_packet_hdr {
    public ushort hw_protocol;
    public byte hook;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public byte[] pad;
    public uint packet_id;
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
                    byte[] headerData = new byte[intSize * 2]; // Assuming 64-bit kernel
                    Task<int> headerReadTask = fsRead.ReadAsync(headerData, 0, headerData.Length);
                    //Task headerDelayTask = Task.Delay(1000);
                    //fsRead.Read(headerData, 0, headerData.Length);
                    Task headerCompletedTask = await Task.WhenAny(headerReadTask); //, headerDelayTask);

                    if (headerCompletedTask != headerReadTask) {
                        fsRead.Close();
                        // The timeout elapsed before the read operation completed.
                        throw new TimeoutException("Header read operation timed out.");
                    }

                    (int version, int dataLength) ProcessHeader() {
                        Console.WriteLine("Processing Header");
                        Span<byte> headerSpan = headerData.AsSpan().Slice(0, headerData.Length);
                        if (BitConverter.IsLittleEndian) {
                            headerSpan.Reverse();
                        }

                        int dataLength = BitConverter.ToInt32(headerSpan.Slice(0, intSize));
                        int version = BitConverter.ToInt32(headerSpan.Slice(intSize, intSize));
                        return (version, dataLength);
                    }

                    (int version, int dataLength) = ProcessHeader();
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
                    //fsRead.Read(packetData, 0, dataLength);

                    void ProcessData() {
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
                        var decisionBytes = BitConverter.GetBytes(2);
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

                    ProcessData();

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