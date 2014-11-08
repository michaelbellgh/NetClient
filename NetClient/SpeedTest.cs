using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NetClient
{
    public class SpeedTest
    {

        bool _contTest = true;


        public void StopListening()
        {
            _contTest = false;
        }

        public void RecieveDownloadSpeedTest()
        {
            long bytes_rec = 0;
            UdpClient uc = new UdpClient(25000);
            byte[] buffer = new byte[uc.Client.ReceiveBufferSize];
            IPEndPoint ipe = null;

            while(_contTest)
            {
                if(uc.Available > 0)
                {
                    byte[] buf = uc.Receive(ref ipe);
                    if(buf!= null && buf.Length == 2)
                    {
                        if(buf[0] == 0x34 && buf[1] == 0xFF)
                        {
                            uc.Send(new byte[] { 0x35, 0xFE }, 2, ipe);
                            Timer t = new Timer();
                            t.Elapsed += t_Elapsed;
                            t.Interval = 20000;
                            t.Start();

                            while (_contTest)
                            {
                                if (uc.Available > 0)
                                {
                                    buffer = uc.Receive(ref ipe);
                                    bytes_rec += buffer.Length;
                                }
                            }
                            uc.Send(BitConverter.GetBytes((Int64)bytes_rec), 8, ipe);
                        }
                    }
                }
            }

            

            uc.Close();

            //return bytes_rec / 1000000;
        }

        /// <summary>
        /// Gets the speed of a remote client (This -> Daemon)
        /// </summary>
        /// <param name="ipe"></param>
        /// <returns>Returns the speed, over 20 seconds, in mbits</returns>
        public Int64 TestDownloadSpeed(IPEndPoint ipe)
        {
            UdpClient u = new UdpClient(25001);
            byte[] buffer = new byte[1458];
            byte[] signal = { 0x34, 0xFF }; //Request response
            u.Send(signal, 2, ipe);
            System.Threading.Thread.Sleep(100);
            
            byte[] sigrep = u.Receive(ref ipe);

            if (sigrep != null && sigrep.Length == 2)
            {
                if (sigrep[0] == 0x35 && sigrep[1] == 0xFE)
                {
                    while (u.Available == 0)
                    {
                        u.Send(buffer, buffer.Length, ipe);
                    }
                    byte[] bytesrecbuf = u.Receive(ref ipe);
                    Int64 bytesrec = BitConverter.ToInt64(bytesrecbuf, 0);
                    return (long)((bytesrec / 20) / Math.Pow(10, 6) * 8);
                }
            }
            return 0;


        }

        void t_Elapsed(object sender, ElapsedEventArgs e)
        {
            _contTest = false;
        }

        private bool IsByteArrayEqual(byte[] array1,byte[] array2,int start, int len)
        {
            for (int i = start; i < len + start; i++)
            {
                if (array1[i] == array2[i]) continue;
                return false;
            }
            return true;
        }
    }

   
}
