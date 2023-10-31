#include "type_converters.h"

const char* hookToString(unsigned int hooknum) {
    switch (hooknum) {
        case NF_INET_PRE_ROUTING:
            return "PRE_ROUTING";
        case NF_INET_LOCAL_IN:
            return "LOCAL_IN";
        case NF_INET_FORWARD:
            return "FORWARD";
        case NF_INET_LOCAL_OUT:
            return "LOCAL_OUT";
        case NF_INET_POST_ROUTING:
            return "POST_ROUTING";
        default:
            return "UNKNOWN_HOOK";
    }
}

const char* routeTypeToString(unsigned int route_type) {
    switch (route_type) {
        case PRE_ROUTING:
            return "PRE_ROUTING";
        case POST_ROUTING:
            return "POST_ROUTING";
        case LOCAL_ROUTING:
            return "LOCAL_ROUTING";
        default:
            return "NONE_ROUTING";
    }
}

const char* ipProtocolToString(unsigned int proto) {
    switch (proto) {
        case 0: return "HOPOPT (IPv6 Hop-by-Hop Option)";
        case 1: return "ICMP (Internet Control Message)";
        case 2: return "IGMP (Internet Group Management)";
        case 3: return "GGP (Gateway-to-Gateway)";
        case 4: return "IPv4 (IPv4 encapsulation)";
        case 5: return "ST (Stream)";
        case 6: return "TCP (Transmission Control)";
        case 7: return "CBT";
        case 8: return "EGP (Exterior Gateway Protocol)";
        case 9: return "IGP (any private interior gateway)";
        case 10: return "BBN-RCC-MON (BBN RCC Monitoring)";
        case 11: return "NVP-II (Network Voice Protocol)";
        case 12: return "PUP (PUP)";
        case 13: return "ARGUS (deprecated)";
        case 14: return "EMCON (EMCON)";
        case 15: return "XNET (Cross Net Debugger)";
        case 16: return "CHAOS (Chaos)";
        case 17: return "UDP (User Datagram)";
        case 18: return "MUX (Multiplexing)";
        case 19: return "DCN-MEAS (DCN Measurement Subsystems)";
        case 20: return "HMP (Host Monitoring)";
        case 21: return "PRM (Packet Radio Measurement)";
        case 22: return "XNS-IDP (XEROX NS IDP)";
        case 23: return "TRUNK-1";
        case 24: return "TRUNK-2";
        case 25: return "LEAF-1";
        case 26: return "LEAF-2";
        case 27: return "RDP (Reliable Data Protocol)";
        case 28: return "IRTP (Internet Reliable Transaction)";
        case 29: return "ISO-TP4 (ISO Transport Protocol Class 4)";
        case 30: return "NETBLT (Bulk Data Transfer Protocol)";
        case 31: return "MFE-NSP (MFE Network Services Protocol)";
        case 32: return "MERIT-INP (MERIT Internodal Protocol)";
        case 33: return "DCCP (Datagram Congestion Control Protocol)";
        case 34: return "3PC (Third Party Connect Protocol)";
        case 35: return "IDPR (Inter-Domain Policy Routing Protocol)";
        case 36: return "XTP";
        case 37: return "DDP (Datagram Delivery Protocol)";
        case 38: return "IDPR-CMTP (IDPR Control Message Transport Proto)";
        case 39: return "TP++ (TP++ Transport Protocol)";
        case 40: return "IL (IL Transport Protocol)";
        case 41: return "IPv6 (IPv6 encapsulation)";
        case 42: return "SDRP (Source Demand Routing Protocol)";
        case 43: return "IPv6-Route (Routing Header for IPv6)";
        case 44: return "IPv6-Frag (Fragment Header for IPv6)";
        case 45: return "IDRP (Inter-Domain Routing Protocol)";
        case 46: return "RSVP (Reservation Protocol)";
        case 47: return "GRE (Generic Routing Encapsulation)";
        case 48: return "DSR (Dynamic Source Routing Protocol)";
        case 49: return "BNA";
        case 50: return "ESP (Encap Security Payload)";
        case 51: return "AH (Authentication Header)";
        case 52: return "I-NLSP (Integrated Net Layer Security TUBA)";
        case 53: return "SWIPE (deprecated)";
        case 54: return "NARP (NBMA Address Resolution Protocol)";
        case 55: return "MOBILE (IP Mobility)";
        case 56: return "TLSP (Transport Layer Security Protocol using Kryptonet key management)";
        case 57: return "SKIP";
        case 58: return "IPv6-ICMP (ICMP for IPv6)";
        case 59: return "IPv6-NoNxt (No Next Header for IPv6)";
        case 60: return "IPv6-Opts (Destination Options for IPv6)";
        case 61: return "any host internal protocol";
        case 62: return "CFTP";
        case 63: return "any local network";
        case 64: return "SAT-EXPAK (SATNET and Backroom EXPAK)";
        case 65: return "KRYPTOLAN";
        case 66: return "RVD (MIT Remote Virtual Disk Protocol)";
        case 67: return "IPPC (Internet Pluribus Packet Core)";
        case 68: return "any distributed file system";
        case 69: return "SAT-MON (SATNET Monitoring)";
        case 70: return "VISA (VISA Protocol)";
        case 71: return "IPCV (Internet Packet Core Utility)";
        case 72: return "CPNX (Computer Protocol Network Executive)";
        case 73: return "CPHB (Computer Protocol Heart Beat)";
        case 74: return "WSN (Wang Span Network)";
        case 75: return "PVP (Packet Video Protocol)";
        case 76: return "BR-SAT-MON (Backroom SATNET Monitoring)";
        case 77: return "SUN-ND (SUN ND PROTOCOL-Temporary)";
        case 78: return "WB-MON (WIDEBAND Monitoring)";
        case 79: return "WB-EXPAK (WIDEBAND EXPAK)";
        case 80: return "ISO-IP (ISO Internet Protocol)";
        case 81: return "VMTP (VMTP)";
        case 82: return "SECURE-VMTP (SECURE-VMTP)";
        case 83: return "VINES";
        case 84: return "TTP (Transaction Transport Protocol)";
        case 85: return "IPTM (Internet Protocol Traffic Manager)";
        case 86: return "NSFNET-IGP (NSFNET-IGP)";
        case 87: return "DGP (Dissimilar Gateway Protocol)";
        case 88: return "TCF";
        case 89: return "EIGRP (EIGRP)";
        case 90: return "OSPF (OSPF IGP)";
        case 91: return "Sprite-RPC (Sprite RPC Protocol)";
        case 92: return "LARP (Locus Address Resolution Protocol)";
        case 93: return "MTP (Multicast Transport Protocol)";
        case 94: return "AX.25";
        case 95: return "OS";
        case 96: return "MICP (Mobile Internetworking Control Pro.)";
        case 97: return "SCC-SP (Semaphore Communications Sec. Pro.)";
        case 98: return "ETHERIP (Ethernet-within-IP Encapsulation)";
        case 99: return "ENCAP";
        case 100: return "any private encryption scheme";
        case 101: return "GMTP";
        case 102: return "IFMP (Ipsilon Flow Management Protocol)";
        case 103: return "PNNI (PNNI over IP)";
        case 104: return "PIM (Protocol Independent Multicast)";
        case 105: return "ARIS";
        case 106: return "SCPS (SCPS)";
        case 107: return "QNX";
        case 108: return "A/N";
        case 109: return "IPComp (IP Payload Compression Protocol)";
        case 110: return "SNP (Sitara Networks Protocol)";
        case 111: return "Compaq-Peer (Compaq Peer Protocol)";
        case 112: return "IPX-in-IP (IPX in IP)";
        case 113: return "VRRP (Virtual Router Redundancy Protocol)";
        case 114: return "PGM (PGM Reliable Transport Protocol)";
        case 115: return "any 0-hop protocol";
        case 116: return "L2TP (Layer Two Tunneling Protocol)";
        case 117: return "DDX (D-II Data Exchange (DDX))";
        case 118: return "IATP (Interactive Agent Transfer Protocol)";
        case 119: return "STP (Schedule Transfer Protocol)";
        case 120: return "SRP (SpectraLink Radio Protocol)";
        case 121: return "UTI (Universal Transport Interface Protocol)";
        case 122: return "SMP (Simple Message Protocol)";
        case 123: return "SM (Simple Multicast Protocol)";
        case 124: return "PTP (Performance Transparency Protocol)";
        case 125: return "ISIS over IPv4";
        case 126: return "FIRE";
        case 127: return "CRTP (Combat Radio Transport Protocol)";
        case 128: return "CRUDP (Combat Radio User Datagram)";
        case 129: return "SSCOPMCE";
        case 130: return "IPLT";
        case 131: return "SPS (Secure Packet Shield)";
        case 132: return "PIPE (Private IP Encapsulation within IP)";
        case 133: return "SCTP (Stream Control Transmission Protocol)";
        case 134: return "FC (Fibre Channel)";
        case 135: return "RSVP-E2E-IGNORE";
        case 136: return "Mobility Header";
        case 137: return "UDPLite";
        case 138: return "MPLS-in-IP";
        case 139: return "manet (MANET Protocols)";
        case 140: return "Shim6 (Shim6 Protocol)";
        case 141: return "WESP (Wrapped Encapsulating Security Payload)";
        case 142: return "ROHC (Robust Header Compression)";
        case 143: return "Ethernet";
        case 253: return "Use for experimentation and testing";
        case 254: return "Use for experimentation and testing";
        case 255: return "Reserved";
        default: return "UNKNOWN_PROTOCOL";
    }
}

const char* dscpToString(u8 dscp) {
    switch (dscp) {
        case 0x00: return "CS0 (Default)";
        case 0x08: return "CS1";
        case 0x10: return "CS2";
        case 0x18: return "CS3";
        case 0x20: return "CS4";
        case 0x28: return "CS5";
        case 0x30: return "CS6";
        case 0x38: return "CS7";
        // Add other DSCP values if needed
        default: return "Unknown DSCP";
    }
}

const char* ecnToString(u8 ecn) {
    switch (ecn) {
        case 0x00: return "Not-ECT";
        case 0x01: return "ECT(1)";
        case 0x02: return "ECT(0)";
        case 0x03: return "CE";
        default: return "Unknown ECN";  // This shouldn't happen as ECN is 2 bits
    }
}

void intToBytes(s32 value, unsigned char bytes[sizeof(s32)]) {
    for (size_t i = 0; i < sizeof(s32); i++) {
        bytes[i] = (value >> (8 * (sizeof(s32) - 1 - i))) & 0xFF;
    }
}

s32 bytesToInt(uint8_t *bytes, size_t num_bytes) {
    s32 result = 0;
    if (num_bytes > sizeof(result)) {
        num_bytes = sizeof(result);
    }
    for (size_t i = 0; i < num_bytes; ++i) {
        result |= ((s32)bytes[i]) << (i * 8);
    }
    return result;
}

__u32 bytesToUint(uint8_t *bytes) {
    __u32 result = 0;
    for (size_t i = 0; i < sizeof(__u32); ++i) {
        result |= ((__u32)bytes[i]) << (i * 8);
    }
    return result;
}

/* 
 * Converts an IP address to a human-readable format and stores it in the provided buffer 
 * Ensures that the buffer size is at least IP_BUFFER_SIZE for safe snprintf operation 
 */
void ipToString(const unsigned int ip, char *buffer, size_t buf_len) {
    if (!buffer) {
        LOG_ERROR("Buffer provided is NULL.");
        return;
    }

    if (buf_len < IP_BUFFER_SIZE) {  
        LOG_ERROR("Buffer size is incorrect. Expected at least %d, got %zu.", IP_BUFFER_SIZE, buf_len);
        return;  // added this line
    }

    snprintf(buffer, buf_len, "%pI4", &ip);
    buffer[IP_BUFFER_SIZE - 1] = '\0';  // Ensure null termination (safety measure)
}



