using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
namespace NetClient
{
    public class NtpClient
    {
        public static DateTime GetNetworkTime(string hostAddress, int receiveTimeout)
        {

            DateTime localDt = GetUtcTime(hostAddress, receiveTimeout).ToLocalTime();
           
            if (TimeZoneInfo.Local.IsDaylightSavingTime(localDt))
            {
                localDt = localDt.AddHours(1);
                
            }

            return localDt;
        }


        public static DateTime GetUtcTime(string hostAddress, int timeout)
        {
            IPAddress ipa = Dns.GetHostAddresses(hostAddress)[0];

            byte[] ntpData = new byte[48];

            ntpData[0] = 0x1B; //Set Leap indicator to 0, Version 3 = 3 (ipv4), Mode = 3 (client)
            IPEndPoint ipe = new IPEndPoint(ipa, 123);

            System.Net.Sockets.Socket sock = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork,
                System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);

            sock.Connect(ipe);

            sock.ReceiveTimeout = timeout;

            sock.Send(ntpData);
            sock.Receive(ntpData);
            sock.Close();

            byte serverReplyTime = 40;

            ulong intPart = BitConverter.ToUInt32(ntpData, serverReplyTime);

            ulong fractPract = BitConverter.ToUInt32(ntpData, serverReplyTime + 4);

            intPart = SwapEndianness(intPart);
            fractPract = SwapEndianness(fractPract);

            var milliseconds = (intPart * 1000) + ((fractPract * 1000) / 0x100000000L);

            DateTime networkDateTime = (new DateTime(1900, 1, 1, 0, 0, 0).AddMilliseconds((long)milliseconds));
            return networkDateTime;
        }

        static uint SwapEndianness(ulong x)
        {
            return (uint)(((x & 0x000000ff) << 24) +
                           ((x & 0x0000ff00) << 8) +
                           ((x & 0x00ff0000) >> 8) +
                           ((x & 0xff000000) >> 24));
        }
    }
}
