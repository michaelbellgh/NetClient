using NetClient;
using NetClient.NAP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NetClientTest
{
    class Program
    {
        static void Main(string[] args)
        {

            SpeedTest st = new SpeedTest();
            new ThreadStart(st.RecieveDownloadSpeedTest).BeginInvoke(new AsyncCallback(test),null);
            st.TestDownloadSpeed(new IPEndPoint(IPAddress.Parse("192.168.1.10"), 25000));
           
            Console.Read();
        }

      
        static void test(IAsyncResult ia)
        {
            
        }

        public static bool evt(List<NAP_Service> recs,IPEndPoint ipe)
        {

            Console.WriteLine("From " + ipe.ToString());

            foreach (NAP_Service s in recs)
            {
                Console.WriteLine(s.Key + " - " + s.Name + "\n" + s.Description + "\n" + s.Suggested_app + "\n" + s.Host.ToString() + ":" + s.Port.ToString());
            }

            return true;
        }

        
    }
}
