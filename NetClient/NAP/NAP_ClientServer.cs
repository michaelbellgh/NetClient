using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetClient.NAP
{
    public class NAP_ClientServer : IDisposable
    {
        //Packet format
        /*   0  1  2  3  4  5     6    7   8    9    A    B    C    D
        *    4e 41 50 00 00 (xx) (xx) (yy) (zz) (zz) (zz) (zz) (ll) (ll) (pp) (pp) [xx = packet type, yy = number of records, zz = packet size, ll = reserved ff]
         * (xx) (xx) - Pointer to next record
         * key - The service key, terminated by \0
         * name - the friendly name, terminated by \0
         * descriptions - the friendly description, terminated by \0. If null, use \0
         * suggested app - the suggested app, termed  by \0
         * host ip = the 4 octet, ipv4 address that hosts the service
         * port - the host service port
         * f0f0 - end of record
         * fefe - end of all records   
         * 
         */

        //Private fields
        UdpClient _udp;
        public readonly IPAddress multicast_ip = IPAddress.Parse("224.0.0.10");
        public const int MULTICAST_PORT = 4733;

        private bool _cont = true;


        #region Packet operations


        private PacketType GetPacketType(ref  byte[] payload)
        {
            int num = Get16BitIntFromBuffer(payload, 5);
            PacketType pt = (PacketType)num;
            return pt;
        }

        public byte[] MakeRecordsPacket(Int16 packet_type)
        {
            byte[] header = new byte[16];
            insert_bytes(ref header, new byte[] { 0x4e, 0x41, 0x50, 0x0, 0x0 }, 0, 0, 5);
            insert_bytes(ref header, BitConverter.GetBytes(packet_type), 0, 5, 2);
            insert_bytes(ref header, BitConverter.GetBytes((Int16)_records.Count), 0, 7, 1);
            //Do packet size here
            insert_bytes(ref header, new byte[] { 0xff, 0xff }, 0, 12, 2);
            insert_bytes(ref header, BitConverter.GetBytes(16), 0, 14, 2);

            List<byte[]> records = new List<byte[]>();

            int pos = 16;

            for (int i = 0; i < _records.Count; i++)
            {
                int posl = 0;

                int len = 2 + _records[i].Key.Length + 1 + _records[i].Name.Length + 1 +
                    _records[i].Description.Length + 1 + _records[i].Suggested_app.Length + 1 +
                    4 + 2 + 2;

                byte[] r = new byte[len];

                if (i + 1 != _records.Count)
                {
                    byte[] pointer = BitConverter.GetBytes((UInt16)(len + header.Length));
                    insert_bytes(ref r, pointer, 0, posl, 2);
                }
                else
                {
                    insert_bytes(ref r, new byte[] { 0x0, 0x0 }, 0, posl, 2);
                }

                posl += 2;


                byte[] string_fields = ASCIIEncoding.ASCII.GetBytes(_records[i].Key + "\0" + _records[i].Name + "\0" + _records[i].Description + "\0" +
                    _records[i].Suggested_app + "\0");
                insert_bytes(ref r, string_fields, 0, 2, string_fields.Length);

                posl += string_fields.Length;

                insert_bytes(ref r, _records[i].Host.GetAddressBytes(), 0, 2 + string_fields.Length, 4);

                posl += 4;

                insert_bytes(ref r, BitConverter.GetBytes((Int16)_records[i].Port), 0, 2 + string_fields.Length + 4, 2);

                posl += 2;

                insert_bytes(ref r, new byte[] { 0xf0, 0xf0 }, 0, 2 + string_fields.Length + 4 + 2, 2);
                records.Add(r);

                pos = pos + posl;


            }

            int total_len = 16 + 2;

            for (int i2 = 0; i2 < records.Count; i2++)
            {
                total_len += records[i2].Length;
            }

            byte[] final = new byte[total_len];
            insert_bytes(ref final, header, 0, 0, header.Length);


            int pos2 = 16;
            foreach (byte[] b in records)
            {
                insert_bytes(ref final, b, 0, pos2, b.Length);
                pos2 += b.Length;
            }

            insert_bytes(ref final, new byte[] { 0xfe, 0xfe }, 0, pos2, 2);
            insert_bytes(ref final, BitConverter.GetBytes(final.Length), 0, 8, 4); //Packet size header

            return final;

        }

        public List<NAP_Service> GetRecordsFromPacket(byte[] payload)
        {
            int packet_type = Get16BitIntFromBuffer(payload, 0x5);
            int packet_size = Get32BitIntFromBuffer(payload, 0x8);
            int record_count = Get8BitIntFromBuffer(payload, 7);

            List<NAP_Service> recs = new List<NAP_Service>();

            byte[] tmparray = new byte[2];
            insert_bytes(ref tmparray, payload, 14, 0, 2);
            Array.Reverse(tmparray);
            int a = BitConverter.ToInt16(tmparray, 0);


            int pos = Get16BitIntFromBuffer(payload, 14);

            for (int i = 0; i < record_count; i++)
            {
                NAP_Service ns = new NAP_Service("", "", 1);

                Int16 pointer_to_next = Get16BitIntFromBuffer(payload, pos);
                pos += 2;

                int trm = 0;

                byte[] keybuf = ReadUntilTerminatorChar(payload, pos, 255, 0x00, ref trm);
                ns.Key = ASCIIEncoding.ASCII.GetString(keybuf);

                pos += trm - pos;

                byte[] namebuf = ReadUntilTerminatorChar(payload, pos, 255, 0x00, ref trm);
                ns.Name = ASCIIEncoding.ASCII.GetString(namebuf);

                pos += trm - pos;

                byte[] descbuf = ReadUntilTerminatorChar(payload, pos, 255, 0x00, ref trm);
                ns.Description = ASCIIEncoding.ASCII.GetString(descbuf);


                pos += trm - pos;

                byte[] suggesbuf = ReadUntilTerminatorChar(payload, pos, 255, 0x0, ref trm);
                ns.Suggested_app = ASCIIEncoding.ASCII.GetString(suggesbuf);

                pos += trm - pos;

                byte[] ip = new byte[4];
                CopyBuffer(payload, ip, pos, 0, 4);
                ns.Host = new IPAddress(ip);
                pos += 4;

                int port = BitConverter.ToInt16(payload, pos);
                ns.Port = port;

                recs.Add(ns);
                pos = pointer_to_next;

            }
            return recs;
        }

        //Packet format
        /*   0  1  2  3  4  5     6    7   8    9    A    B    C    D
        *    4e 41 50 00 00 (xx) (xx) (yy) (zz) (zz) (zz) (zz) (ll) (ll) (pp) (pp) [xx = packet type, yy = number of records, zz = packet size, ll = reserved ff]
         * (xx) (xx) - Pointer to next record
         * key - The service key, terminated by \0
         * name - the friendly name, terminated by \0
         * descriptions - the friendly description, terminated by \0. If null, use \0
         * suggested app - the suggested app, termed  by \0
         * host ip = the 4 octet, ipv4 address that hosts the service
         * port - the host service port
         * f0f0 - end of record
         * fefe - end of all records
         */

        public const int RECORD_NON_STRING_LEN = 10;

        private byte[] MakeSimpleHeader(PacketType packetType)
        {
            byte[] buffer = new byte[16];
            insert_bytes(ref buffer, new byte[] { 0x4e, 0x41, 0x50, 0x00, 0x00 }, 0, 0, 5);
            insert_bytes(ref buffer, BitConverter.GetBytes((UInt16)packetType), 0, 5, 2);
            insert_bytes(ref buffer, new byte[] { 0xff, 0xff }, 0, 0xC, 2);

            return buffer;
        }

        #endregion

        #region ByteTools
        public static byte[] ReadUntilTerminatorChar(byte[] buffer, int start, int max, byte terminator, ref int TerminatorIndex, bool passTerm = true)
        {
            byte[] buf = new byte[max];

            if (max < start) return null;

            for (int i = start; i < max; i++)
            {
                byte cur = buffer[i];
                if (cur == terminator)
                {
                    TerminatorIndex = i + (passTerm ? 1 : 0);
                    Array.Resize(ref buf, i - start);
                    Array.Copy(buffer, start, buf, 0, i - start);
                    return buf;
                }
            }
            Array.Copy(buffer, start, buf, 0, max - start);
            TerminatorIndex = -1;
            return buf;
        }

        private void insert_bytes(ref byte[] dst, byte[] src, int srcIndex, int dstIndex, int len)
        {
            CopyBuffer(src, dst, srcIndex, dstIndex, len);
            return;

            int dsti = dstIndex;
            for (int i = srcIndex; i < len; i++)
            {
                dst[dsti] = src[i];
                dsti++;
            }


        }

        private void CopyBuffer(byte[] src, byte[] dst, int srcStartIndex, int dstStartIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                dst[dstStartIndex + i] = src[srcStartIndex + i];
            }
        }

        int Get8BitIntFromBuffer(byte[] src, int startIndex)
        {
            return (int)src[startIndex];

        }

        Int16 Get16BitIntFromBuffer(byte[] src, int startIndex)
        {
            byte[] data = new byte[2];
            CopyBuffer(src, data, startIndex, 0, 2);
            return BitConverter.ToInt16(src, startIndex);

        }

        Int32 Get32BitIntFromBuffer(byte[] src, int startIndex)
        {
            byte[] data = new byte[4];
            CopyBuffer(src, data, startIndex, 0, 4);
            return BitConverter.ToInt32(data, 0);
        }
        #endregion


        #region Network operations


        byte[] MakeRecordBytes(NAP_Service ns)
        {
            string finalstring = ns.Key + '\0' + ns.Name + '\0' + ns.Description + '\0' + ns.Suggested_app + '\0';
            byte[] final = new byte[ASCIIEncoding.ASCII.GetByteCount(finalstring) + RECORD_NON_STRING_LEN];

            insert_bytes(ref final, ASCIIEncoding.ASCII.GetBytes(finalstring), 0, 2, ASCIIEncoding.ASCII.GetByteCount(finalstring));
            byte[] ip_bytes = ns.Host.GetAddressBytes();
            int pos = 2 + ASCIIEncoding.ASCII.GetByteCount(finalstring);
            insert_bytes(ref final, ip_bytes, 0, pos, 4);

            byte[] portnum = BitConverter.GetBytes((Int16)ns.Port);
            insert_bytes(ref final, portnum, 0, pos + 4, 2);

            insert_bytes(ref final, new byte[] { 0xf0, 0xf0 }, 0, pos + 6, 2);

            return final;
        }


        List<NAP_Service> _records = new List<NAP_Service>();

        public List<NAP_Service> Records
        {
            get { return _records; }
        }

        public void JoinMulticastGroup(IPAddress multicastIP)
        {
            _udp.JoinMulticastGroup(multicastIP);
        }

        public void SendRecordsTo(IPEndPoint ipe)
        {
            byte[] packet = MakeRecordsPacket(16);
            _udp.Send(packet, packet.Length, ipe);
        }

        public void SendRecordsToGroup()
        {
            byte[] packet = MakeRecordsPacket(16);
            _udp.Send(packet, packet.Length, new IPEndPoint(multicast_ip, MULTICAST_PORT));
        }

        public void RequestRecordsUpdateGroup()
        {
            byte[] header = MakeSimpleHeader(PacketType.RequestForRecordsGroup);
            _udp.Send(header, header.Length, new IPEndPoint(multicast_ip, MULTICAST_PORT));
        }



        public byte[] MakeDeletedRecordsUpdate(IEnumerable<NAP_Service> services)
        {
            byte[] header = MakeSimpleHeader(PacketType.DeleteRecords);
            List<byte[]> recs = new List<byte[]>();
            long len = 16;
            int pos = header.Length;
            foreach (NAP_Service n in services)
            {
                byte[] rec = MakeRecordBytes(n);
                pos += rec.Length;
                len+= rec.Length;
                insert_bytes(ref rec, BitConverter.GetBytes((Int16)pos), 0, 0, 2);
                recs.Add(rec);
            }
            len += 2;

            byte[] final = new byte[len];
            insert_bytes(ref final, header, 0, 0, header.Length);
            pos=16;
            foreach (byte[] n in recs)
            {
                insert_bytes(ref final, n, 0, pos, n.Length);
                pos += n.Length;
            }
            insert_bytes(ref final, new byte[] { 0xfe, 0xfe }, 0, pos, 2);
            return final; 
        }

        public void RequestRecordsUpdateUnicast(IPEndPoint host)
        {
            byte[] header = MakeSimpleHeader(PacketType.RequestForRecordsGroup);
            _udp.Send(header, header.Length, host);
        }

        public void SendDeleteUpdateMulticast(IEnumerable<NAP_Service> services)
        {
            byte[] packet = MakeDeletedRecordsUpdate(services);
            _udp.Send(packet, packet.Length, new IPEndPoint(multicast_ip, MULTICAST_PORT));
        }

        private void _startListening()
        {
            if (_udp == null) _udp = new UdpClient(AddressFamily.InterNetwork);
            if (!_udp.Client.IsBound) _udp.Client.Bind(new IPEndPoint(IPAddress.Any, MULTICAST_PORT));

            System.Threading.Thread th = new System.Threading.Thread(new System.Threading.ThreadStart(_listenThread));
            th.Start();

        }



        public bool DoesServiceExistInGroup(string key, out List<NAP_Service> services)
        {

            _need_udp = true;
            _reset.WaitOne(1000);
            RequestRecordsUpdateGroup();
            Thread.Sleep(200);
            while (_udp.Available == 0)
            {
                Thread.Sleep(10);

            }

            IPEndPoint ipe = null;
            byte[] buffer = new byte[_udp.Available];
            buffer = _udp.Receive(ref ipe);
            List<NAP_Service> newrecs = new List<NAP_Service>();

            PacketType pt = GetPacketType(ref buffer);
            if (pt == PacketType.RequestForRecordsGroup) SendRecordsToGroup();
            while (pt != PacketType.RecordMessage)
            {

                if (_udp.Available > 0)
                {
                    buffer = _udp.Receive(ref ipe);
                    pt = GetPacketType(ref buffer);

                }

            }

            if (GetPacketType(ref buffer) == PacketType.RecordMessage)
            {
                List<NAP_Service> recs = GetRecordsFromPacket(buffer);
                foreach (NAP_Service n in recs)
                {
                    if ((n.Key.ToLower() == key.ToLower()))
                    {
                        newrecs.Add(n);
                    }
                }

                if (newrecs.Count > 0) { services = newrecs; return true; }
            }
            _reset.Set();
            services = null;
            return false;



        }


        private bool _need_udp = false;
        private AutoResetEvent _reset = new AutoResetEvent(false);
        private void _listenThread()
        {


            while (_udp.Client.IsBound && _cont == true)
            {
                if (_need_udp)
                {
                    _reset.Set();
                    _reset.WaitOne(1000);
                }
                if (_udp.Available > 0)
                {
                    IPEndPoint ipe = null;
                    byte[] buffer = new byte[_udp.Available];
                    buffer = _udp.Receive(ref ipe);

                    _actionPacket(buffer, ipe);
                }

                Thread.Sleep(0);
            }
        }

        private void _actionPacket(byte[] payload, IPEndPoint host)
        {
            PacketType pt = GetPacketType(ref payload);

            switch (pt)
            {
                case PacketType.RecordMessage:
                    {
                        List<NAP_Service> records = GetRecordsFromPacket(payload);
                        if (RecordsRecieved != null)
                        {
                            bool add = RecordsRecieved.Invoke(records, host);
                            if (add)
                            {

                                AddServices(records);
                            }
                        }

                        break;
                    }
                case PacketType.RequestForRecordsGroup:
                    {
                        SendRecordsToGroup();
                        break;
                    }
                case PacketType.RequestForRecordsUnicast:
                    {
                        SendRecordsTo(host);
                        break;
                    }
                case PacketType.DeleteRecords:
                    {
                        List<NAP_Service> recs = GetRecordsFromPacket(payload);
                        
                        break;
                    }
                default:
                    break;
            }
        }

        #endregion


        #region Enums and Data types

        enum PacketType
        {
            RecordMessage = 16,
            RequestForRecordsGroup = 24,
            RequestForRecordsUnicast = 32,
            DeleteRecords = 64
        }


        public delegate bool RecordsRecievedDelegate(List<NAP_Service> records, IPEndPoint remoteHost);

        public event RecordsRecievedDelegate RecordsRecieved;



        #endregion

        public void AddService(NAP_Service service)
        {
            if (!ServiceExists(service,_records))
            {
                _records.Add(service);
            }
        }



        public void AddServices(IEnumerable<NAP_Service> services)
        {
            foreach (NAP_Service n in services)
            {

                if (!ServiceExists(n,_records))
                {
                    _records.Add(n);
                }
            }
        }

        public void RemoveServices(IEnumerable<NAP_Service> services)
        {
            foreach (NAP_Service item in services)
            {
                if(ServiceExists(item,_records))
                {
                    _records.Remove(item);
                }
            }
        }

        public static bool ServiceExists(NAP_Service srv,List<NAP_Service> records)
        {
            foreach (NAP_Service n in records)
            {
                if (srv.Host.Equals(n.Host) && srv.Port == n.Port)
                {
                    return true;
                }
            }
            return false;
        }

        public NAP_ClientServer()
        {
            _udp = new UdpClient(MULTICAST_PORT);
            JoinMulticastGroup(multicast_ip);
            _startListening();


        }




        public void Dispose()
        {
            _cont = false;
        }
    }

    public class NAP_Service
    {
        string _key, _name, _description, _suggested_app;

        public string Key
        {
            get { return _key; }
            set { _key = value; }
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

        public string Suggested_app
        {
            get { return _suggested_app; }
            set { _suggested_app = value; }
        }
        int _port;

        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
        IPAddress _host;

        public IPAddress Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return new IPEndPoint(_host, _port); }
        }

        public NAP_Service(string key, string name, int port)
        {
            _key = key;
            _name = name;
            _host = GetLocalIPv4Address();
            _port = port;
            _description = "";
            _suggested_app = "";
        }
        public NAP_Service(string key, string name, IPAddress host, int port)
        {
            _key = key;
            _name = name;
            _host = host;
            _port = port;
            _description = "";
            _suggested_app = "";
        }

        public NAP_Service(string key, string name, IPAddress host, int port, string description, string suggested_app)
        {
            _key = key;
            _name = name;
            _description = description;
            _suggested_app = suggested_app;
            _port = port;
            _host = host;
        }

        #region filter overloads

        public static List<NAP_Service> FilterRecords(List<NAP_Service> records, string key)
        {
            List<NAP_Service> newlist = new List<NAP_Service>();
            if (records.Count > 0)
            {
                foreach (NAP_Service ns in records)
                {
                    if (ns.Key == key && !NAP_ClientServer.ServiceExists(ns, newlist)) newlist.Add(ns);
                }

            }
            return newlist;
        }

        public static List<NAP_Service> FilterRecords(List<NAP_Service> records, IPAddress ip)
        {
            List<NAP_Service> newlist = new List<NAP_Service>();
            if (records.Count > 0)
            {
                foreach (NAP_Service ns in records)
                {
                    if (ns.Host == ip && !NAP_ClientServer.ServiceExists(ns, newlist)) newlist.Add(ns);
                }

            }
            return newlist;
        }

        public static List<NAP_Service> FilterRecords(List<NAP_Service> records, IPAddress ip, string key)
        {
            List<NAP_Service> newlist = new List<NAP_Service>();
            if (records.Count > 0)
            {
                foreach (NAP_Service ns in records)
                {
                    if (ns.Host.Equals(ip) && ns.Key == key && !NAP_ClientServer.ServiceExists(ns, newlist))
                    {
                        newlist.Add(ns);
                    }
                }

            }
            return newlist;
        }

        #endregion

        public static IPAddress GetLocalIPv4Address()
        {
            IPHostEntry host;
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;

                }
            }
            return null;
        }
    }
}
