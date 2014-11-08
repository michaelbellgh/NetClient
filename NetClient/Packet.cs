using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetClient
{
    public class Packet
    {
        Dictionary<string, PacketExpression> _contents;
        string _protocol;

        public string Protocol
        {
            get { return _protocol; }
            set { _protocol = value; }
        }

        public Packet(string name)
        {
            _protocol = name;
            _contents = new Dictionary<string, PacketExpression>();
        }

        public PacketExpression this[string FieldName]
        {
            get
            {
                return _contents[FieldName];
            }
            set
            {
                _contents[FieldName] = value;
            }
        }

        public void AddField(PacketExpression field)
        {
            _contents.Add(field.FieldName, field);
        }

        public void AddField(string Name,byte[] Data)
        {
            _contents.Add(Name,new PacketExpression(Name,Data));
        }
        public void AddField(string Name, ushort Num)
        {
            _contents.Add(Name, new PacketExpression(Name, BitConverter.GetBytes(Num)));
        }

        public void AddField(string Name, int Num)
        {
            _contents.Add(Name, new PacketExpression(Name, BitConverter.GetBytes(Num)));
        }

        public byte[] MakeBytePacket()
        {
            byte[] pkt = new byte[get_total_bytes()];
            int pos = 0;

            foreach (PacketExpression p in _contents.Values)
            {

                Array.Copy(p.Data, 0, pkt, pos, p.Data.Length);
                pos += p.Data.Length;
            }

            return pkt;
        }

        int get_total_bytes()
        {
            int res = 0;
            foreach (PacketExpression p in _contents.Values)
            {
                res += p.Data.Length;
            }

            return res;
        }
    }
}
