using NetClient.NAP;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SpeedTestApp
{
    public partial class frmMain : Form
    {
        NetClient.SpeedTest _st = new NetClient.SpeedTest();
        NetClient.NAP.NAP_ClientServer _ncs = new NetClient.NAP.NAP_ClientServer();
        System.Threading.Thread _threadST;

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnStartTest_Click(object sender, EventArgs e)
        {
            if(cmbServers.Text != "")
            {
                IPAddress ip = IPAddress.Parse(cmbServers.Text);
                List<NAP_Service> rList = NAP_Service.FilterRecords(_ncs.Records, ip,"spdtest");

                if(rList.Count == 1)
                {
                    Int64 res = _st.TestDownloadSpeed(rList[0].RemoteEndPoint);
                    lblStatus.Text = Math.Round((double)res, 2).ToString() + " Mbits";
                }

            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            _ncs.RecordsRecieved += _ncs_RecordsRecieved;
            lblLocalIPv4.Text = NAP_Service.GetLocalIPv4Address().ToString();
            _ncs.RequestRecordsUpdateGroup();
        }

        bool _ncs_RecordsRecieved(List<NetClient.NAP.NAP_Service> records, System.Net.IPEndPoint remoteHost)
        {
            if (this.InvokeRequired)
            {
                bool add = (bool)Invoke(new NAP_ClientServer.RecordsRecievedDelegate(_ncs_RecordsRecieved)
                    , new object[] { records, remoteHost });
                return add;
            }
            else
            {
                List<NAP_Service> rList = NAP_Service.FilterRecords(records, "spdtest");
                foreach (NAP_Service item in rList)
                {
                    if (!cmbServers.Items.Contains(item.Host.ToString()))
                    {
                        cmbServers.Items.Add(item.Host.ToString());
                    }
                }
                if (cmbServers.Items.Count > 0)
                {
                    cmbServers.SelectedIndex = 0;
                }
                return true;
            }

            return false;
        }

        private void btnStartServer_Click(object sender, EventArgs e)
        {
            _threadST = new System.Threading.Thread(new System.Threading.ThreadStart(_st.RecieveDownloadSpeedTest));
            _threadST.Start();
            lblDaemonStatus.Text = "running!";
            lblDaemonStatus.ForeColor = Color.Green;
            _ncs.AddService(new NAP_Service("spdtest", "LAN Speed Test Service", 25000));
            _ncs.SendRecordsToGroup();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _st.StopListening();
            _ncs.Dispose();
        }
    }
}
