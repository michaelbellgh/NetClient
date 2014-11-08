using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetClient
{
    public class MinecraftClient
    {
         
        const int ANNOUNCE_MULTICAST_PORT = 4445;
        public static void AnnounceWorld(string motd, int port)
        {
            byte[] ANNOUNCE_MULTICAST_IP = { 0xE0, 0x0, 0x2, 0x3C };
            byte[] motd_bytes = ASCIIEncoding.ASCII.GetBytes("[MOTD]" + motd + "[/MOTD]");
            byte[] ip_bytes = ASCIIEncoding.ASCII.GetBytes("[AD]" + port + "[/AD]");

            Socket sk = new Socket(SocketType.Dgram, ProtocolType.Udp);
            byte[] buffer = new byte[motd_bytes.Length + ip_bytes.Length];
            motd_bytes.CopyTo(buffer, 0);
            ip_bytes.CopyTo(buffer, motd_bytes.Length);

            sk.SendTo(buffer, new IPEndPoint(new IPAddress(ANNOUNCE_MULTICAST_IP), ANNOUNCE_MULTICAST_PORT));
        }


        public static byte[] ReadUntilTerminatorChar(byte[] buffer, int start, int max, byte terminator, ref int TerminatorIndex)
        {
            byte[] buf = new byte[max];

            if (max < start) return null;

            for (int i = start; i < max; i++)
            {
                byte cur = buffer[i];
                if (cur == terminator)
                {
                    TerminatorIndex = i;
                    Array.Resize(ref buf, i - start);
                    Array.Copy(buffer, start, buf, 0, i - start);
                    return buf;
                }
            }
            Array.Copy(buffer, start, buf, 0, max - start);
            TerminatorIndex = -1;
            return buf;
        }

        public class ServerInfo
        {
            private IPAddress _addr;

            int _max;

            bool _isup;

            public bool IsUp
            {
                get { return _isup; }
                set { _isup = value; }
            }

            public int Max
            {
                get { return _max; }
                set { _max = value; }
            }
            int _online;

            public int CurrentlyOnline
            {
                get { return _online; }
                set { _online = value; }
            }
            private int _port;

            public int Port
            {
                get { return _port; }
                set { _port = value; }
            }
            private string _motd;

            public string Motd
            {
                get { return _motd; }
                set { _motd = value; }
            }


            public IPAddress Host
            {
                get { return _addr; }
            }

            public ServerInfo(IPAddress host, int port)
            {
                

                TcpClient tc = new TcpClient();
                try
                {
                    tc.Connect(host, port);
                }
                catch (Exception)
                {
                    _isup = false;
                    return;
                    
                }
                _isup = true;
                
                tc.GetStream().WriteByte(0xFE);

                byte response = (byte)tc.GetStream().ReadByte();
                if (response != 0xFF) { return; } //Not a mc server

                tc.GetStream().ReadByte();
                tc.GetStream().ReadByte(); //seek past empty bytes

                byte[] buffer = new byte[48];
                int responseLen = tc.GetStream().Read(buffer, 0, 48);


                Array.Resize<byte>(ref buffer, responseLen);

                

                string[] splits = ByteSplit(buffer,167);

                _motd = splits [0];
                _max = int.Parse(splits[2]);
                _online = int.Parse(splits[1]);
                _addr = host;
                _port = port;


            }

            public static string[] ByteSplit(byte[] input, byte split)
            {
                return Encoding.BigEndianUnicode.GetString(input).Split(new char[] { (char)split });
            }

 

 

        }
    }
}
