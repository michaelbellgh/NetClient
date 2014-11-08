using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetClient
{
    public class PacketStore
    {
        public Dictionary<string, Packet> _packets;

        public PacketStore()
        {
            _packets = new Dictionary<string, Packet>();

            Packet NTP = new Packet("NTP");
            NTP.AddField(new PacketExpression("sntp-header", "Contains leap year bit,client and mode bit", 1, null, new byte[] { 0x1B }));

            _packets.Add(NTP.Protocol, NTP);

            Packet SimpleDNS = new Packet("DNS");
            SimpleDNS.AddField("transaction-id", new byte[] { 0x01, 0x01 });
            SimpleDNS.AddField("flags", new byte[] { 0x01, 0x00 });
            SimpleDNS.AddField("questions", new byte[] { 0x00, 0x01 });
            SimpleDNS.AddField("rr-fields", new byte[] { 0, 0, 0, 0, 0, 0 });
            SimpleDNS.AddField("hostname-length", new byte[] { 0x6 });
            SimpleDNS.AddField("hostname", new byte[] { 0x67, 0x6f, 0x6f, 0x67, 0x6C, 0x65 });
            SimpleDNS.AddField("tqld-sep", new byte[] { 0x03 });
            SimpleDNS.AddField("tqld", new byte[] { 0x63, 0x6f, 0x6d });
            SimpleDNS.AddField("!stringend", new byte[] { 0x00 });
            SimpleDNS.AddField("query-type", new byte[] { 0, 1 });
            SimpleDNS.AddField("query-class", new byte[] { 0, 1 });

            _packets.Add(SimpleDNS.Protocol, SimpleDNS);

            Packet DHCP_Request = new Packet("DHCPREQ");
            DHCP_Request.AddField("message-type", (ushort)1); //Boot request
            DHCP_Request.AddField("hardware-type", (ushort)1); //Ethernet
            DHCP_Request.AddField("hardware-address-length", (ushort)6);
            DHCP_Request.AddField("transaction-id", (int)new Random().Next(0,int.MaxValue));
            DHCP_Request.AddField("seconds-elapsed", (ushort)0);
            DHCP_Request.AddField("bootp-flags", (ushort)0); //Unicast
            DHCP_Request.AddField("client-ip-address", new byte[4]);
            DHCP_Request.AddField("your-ip-address", new byte[4] );
            DHCP_Request.AddField("next-ip-address", new byte[4] );
            DHCP_Request.AddField("relay-ip-address", new byte[4] );
            DHCP_Request.AddField("client-mac-address", new byte[4]);
            DHCP_Request.AddField("client-mac-padding", new byte[16]);
            DHCP_Request.AddField("server-host-name", new byte[64]);
            DHCP_Request.AddField("boot-file-name", new byte[128]);
            DHCP_Request.AddField("magic-cookie", new byte[] {0x63,0x82,0x53,0x63});
            DHCP_Request.AddField("dhcp-message-type-53", new byte[] { });
        }

        public Packet this[string Protocol]
        {
            get
            {
                return _packets[Protocol];
            }
            set
            {
                _packets[Protocol] = value;
            }
        }

        public bool SendPacket(Socket sk,Packet p)
        {
            sk.Send(p.MakeBytePacket());
            return true;
        }
    }
}
