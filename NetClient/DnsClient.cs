using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetClient
{
    public class DnsClient
    {
        public static System.Net.IPHostEntry GetDnsEntry(IPAddress dnsServer, string hostname)
        {
            Socket sk = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            sk.ReceiveTimeout = 3000;

            byte[] transID = BitConverter.GetBytes((Int16)new Random().Next(0,65536));

            string hostAsc = ASCIIEncoding.ASCII.GetString(
                ASCIIEncoding.ASCII.GetBytes(hostname));

            int namelen = hostAsc.Length;

            byte[] dnsQuery = new byte[18 + namelen];

            CopyBuffer(transID, dnsQuery, 0, 0, transID.Length);

            //0... .... .... ....   Query type
            //.000 0... .... ....   Opcode - is a query
            //.... ..0. .... ....   Response is not truncated
            //.... ...1 .... ....   Recursion is desired
            //.... .... .0.. ....   Z (reserved)
            //.... .... ...0 ....   Non authenticated data is unacceptable
            byte[] flags = { 1, 0 };

            CopyBuffer(flags, dnsQuery, 0, 2, flags.Length);

            byte[] questions = { 0, 1 };

            CopyBuffer(questions, dnsQuery, 0, 4, questions.Length);

            byte[] rr_s = { 0, 0, 0, 0, 0, 0 };

            CopyBuffer(rr_s, dnsQuery, 0, 6, rr_s.Length);

            int tldq_pos = hostAsc.LastIndexOf('.');

            dnsQuery[12] = (byte)hostAsc.Substring(0, tldq_pos).Length;

            byte[] name = ASCIIEncoding.ASCII.GetBytes(hostname);

            CopyBuffer(name, dnsQuery, 0, 13, name.Length);

            dnsQuery[13 + tldq_pos] = 0x03;
            
            byte[] endData = {0,0,1,0,1};

            CopyBuffer(endData, dnsQuery, 0, 13 + name.Length, endData.Length);

            sk.Connect(dnsServer, 53);
            sk.Send(dnsQuery);

            byte[] buffer = new byte[512];
            
            sk.Receive(buffer);

            byte[] ak = ExtractBuffer(buffer, 0x6, 2);
            

            ushort responses = BitConverter.ToUInt16(ak, 0);
            responses = ReverseBytes(responses);

            int answersStartOffset = dnsQuery.Length;
            IPAddress[] answers = ParseAnswerSection(buffer, answersStartOffset, responses);

            IPHostEntry iphs = new IPHostEntry();
            iphs.AddressList = answers;

            return iphs;

        }

        static IPAddress[] ParseAnswerSection(byte[] buffer, int AnswerStartOffset, int numberOfAnswers)
        {
            IPAddress[] res = new IPAddress[numberOfAnswers];

            for (int i = 0; i < numberOfAnswers; i++)
            {
                byte blocklen = ExtractBuffer(buffer, AnswerStartOffset + 1, 1)[0];
                byte[] ip = ExtractBuffer(buffer, AnswerStartOffset + blocklen, 4);
                res[i] = new IPAddress(ip);
                AnswerStartOffset += blocklen + 4;
            }

            return res;

        }

        public static UInt16 ReverseBytes(UInt16 value)
        {
            return (UInt16)((value & 0xFFU) << 8 | (value & 0xFF00U) >> 8);
        }

        private static void CopyBuffer(byte[] src, byte[] dst, int srcStartIndex, int dstStartIndex, int length)
        {
            for (int i = 0; i < length; i++)
            {
                dst[dstStartIndex + i] = src[srcStartIndex + i];
            }
        }

        private static byte[] ExtractBuffer(byte[] src, int start, int length)
        {
            byte[] buffer = new byte[length];
            int j = 0;
            for (int i = start; i < start + length; i++)
            {
                buffer[j] = src[i];
                j++;
            }

            return buffer;
        }
    }
}
