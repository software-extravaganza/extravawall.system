using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net.Sockets;

namespace ExtravaCore {

    public static class NetlinkInterop {
        public const int AF_NETLINK = 16; // Protocol family number for Netlink

        [StructLayout(LayoutKind.Sequential)]
        public struct sockaddr_nl {
            public ushort nl_family; // AF_NETLINK
            public ushort nl_pad;    // Zero
            public uint nl_pid;      // Port ID
            public uint nl_groups;   // Multicast groups mask
        }

        // Define other necessary structures...

        [DllImport("libc", SetLastError = true)]
        public extern static int socket(int domain, int type, int protocol);

        [DllImport("libc", SetLastError = true)]
        public extern static int bind(IntPtr sockfd, ref sockaddr_nl addr, int addrlen);

        [DllImport("libc", SetLastError = true)]
        public static extern int recvfrom(int sockfd, byte[] buf, int len, int flags, IntPtr src_addr, IntPtr addrlen);


    }

    public class NetListener {
        const int SOCK_RAW = 3; // Socket type for raw sockets
        const int NETLINK_USERSOCK = 2; // Netlink protocol number for user sockets
        public void Begin() {
            int sockfd = NetlinkInterop.socket(NetlinkInterop.AF_NETLINK, SOCK_RAW, NETLINK_USERSOCK);

            if (sockfd < 0) {
                // Handle error
            }

            var addr = new NetlinkInterop.sockaddr_nl {
                nl_family = NetlinkInterop.AF_NETLINK,
                nl_pid = (uint)Process.GetCurrentProcess().Id,
                nl_groups = 0
            };

            if (NetlinkInterop.bind(sockfd, ref addr, Marshal.SizeOf(typeof(NetlinkInterop.sockaddr_nl))) != 0) {
                // Handle error
            }

            Listen(sockfd);
        }

        private void Listen(int sockfd) {
            while (true) { // keep listening until you decide to stop
                byte[] buffer = new byte[4096]; // 4K buffer, adjust as needed

                // Receive data from the socket
                int len = NetlinkInterop.recvfrom(sockfd, buffer, buffer.Length, 0, IntPtr.Zero, IntPtr.Zero);

                if (len > 0) {
                    // Print received data (for simplicity, as a hex dump)
                    Console.WriteLine(BitConverter.ToString(buffer, 0, len));

                    // TODO: Further processing of received data if needed
                } else if (len == 0) {
                    // Socket was gracefully closed or no more data
                    break;
                } else {
                    // Handle potential errors
                    Console.WriteLine("Error receiving data from socket.");
                    break;
                }
            }
        }
    }

}