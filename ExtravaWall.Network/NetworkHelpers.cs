using System.Net;
using System.Net.NetworkInformation;
using NetworkManager.DBus;
using Tmds.DBus;

namespace ExtravaWall.Network;

public static class NetworkHelpers {
    public static (ulong tx, ulong rx) GetBytesSentReceived() {
        ulong txBytes = 0;
        ulong rxBytes = 0;
        try {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var networkInterface in networkInterfaces) {
                //Get the properties
                txBytes += (ulong)networkInterface.GetIPv4Statistics().BytesSent;
                rxBytes += (ulong)networkInterface.GetIPv4Statistics().BytesReceived;
            }
        } catch (Exception) {
            //todo: handle exception
        }

        return (txBytes, rxBytes);
    }

    public static IPAddress ConvertUintToIpAddress(uint ip) {
        byte[] bytes = BitConverter.GetBytes(ip);
        // if (BitConverter.IsLittleEndian) {
        //     Array.Reverse(bytes);
        // }

        return new IPAddress(bytes);
    }

    public static List<IPAddressInfo> ConvertUintToIp4Addresses(uint[][] dbusIpAddressInfoArray) {
        var addressNetworkPairs = new List<IPAddressInfo>();
        foreach (var dbusIpAddressInfo in dbusIpAddressInfoArray) {
            addressNetworkPairs.Add(new IPAddressInfo(dbusIpAddressInfo));
        }

        return addressNetworkPairs;
    }
}

public class IPAddressInfo {
    public IPAddress IPAddress { get; private init; }
    public int PrefixLength { get; private set; }


    public IPAddress GateWayIPAddress { get; private set; }

    public IPAddressInfo(uint[] dbusIpAddressInfo) {
        if (dbusIpAddressInfo == null) throw new ArgumentNullException(nameof(dbusIpAddressInfo));
        if (dbusIpAddressInfo.Length == 0) throw new ArgumentException("Value cannot be an empty collection.", nameof(dbusIpAddressInfo));
        if (dbusIpAddressInfo.Length < 3) throw new ArgumentException(string.Format(Resources.ArraySizeErrorMessage, dbusIpAddressInfo.Length, 3), nameof(dbusIpAddressInfo));
        IPAddress = NetworkHelpers.ConvertUintToIpAddress(dbusIpAddressInfo[0]);
        PrefixLength = (int)dbusIpAddressInfo[1];
        GateWayIPAddress = NetworkHelpers.ConvertUintToIpAddress(dbusIpAddressInfo[2]);
    }

    public override string ToString() {
        if (PrefixLength > 0) {
            return $"{IPAddress}/{PrefixLength}";
        }

        return $"{IPAddress}";
    }
}