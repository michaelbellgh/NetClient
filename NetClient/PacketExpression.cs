using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetClient
{
    public class PacketExpression
    {
        string _fieldName, _notes;

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }
        int _length;

        public int Length
        {
            get { return _length; }
            set { _length = value; }
        }
        byte[] _defaultData;

        public byte[] DefaultData
        {
            get { return _defaultData; }
            set { _defaultData = value; }
        }
        byte[] _data;

        public byte[] Data
        {
            get { return _data; }
            set { _data = value; }
        }


        public PacketExpression(string FieldName, string Notes, int Length, byte[] Data, byte[] DefaultData)
        {
            _fieldName = FieldName;
            _length = Length;
            _defaultData = DefaultData;
            _data = Data;
        }

        public PacketExpression(string FieldName,byte[] Data)
        {
            _fieldName = FieldName;
            _length = Data.Length;
            _defaultData = Data;
            _data = Data;
        }

    }
}
