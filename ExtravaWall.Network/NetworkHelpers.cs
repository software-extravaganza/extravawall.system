using System.Net;

namespace ExtravaWall.Network;

public class NetworkHelpers {
    public static IPAddress ConvertUintToIpAddress(uint ip) {
        byte[] bytes = BitConverter.GetBytes(ip);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        
        return new IPAddress(bytes);
    }
    
}