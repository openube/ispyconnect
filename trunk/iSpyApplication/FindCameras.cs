using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using iSpyApplication.Video;

namespace iSpyApplication
{
    public partial class FindCameras : Form
    {
        private static DataTable _dt;
        private DataRow _drSelected;
        private bool _exiting;

        public int VideoSourceType;
        public int Ptzid = -1;
        public int Ptzentryid = 0;
        private const int MaxThreads = 10;
        public string FinalUrl = "";
        public string Username = "";
        public string Channel = "";
        public string Password = "";
        public string AudioModel = "";
        public string Flags = "";
        public string Cookies = "";
        public static List<String> DnsEntries = new List<string>();

        public FindCameras()
        {
            InitializeComponent();
            RenderResources();
        }

        private class UISync
        {
            private static ISynchronizeInvoke _sync;

            public static void Init(ISynchronizeInvoke sync)
            {
                _sync = sync;
            }

            public static void Execute(Action action)
            {
                try { _sync.BeginInvoke(action, null); }
                catch { }
            }
        }

        private void FindCameras_Load(object sender, EventArgs e)
        {
            llblDownloadVLC.Text = LocRm.GetString("DownloadVLC") + " v" + VlcHelper.VMin + " or greater";
            llblDownloadVLC.Visible = !VlcHelper.VlcInstalled;
            btnBack.Enabled = false;
            RenderResources();
            ddlHost.Items.Add(LocRm.GetString("AllAdaptors"));
            foreach (var ip in MainForm.AddressListIPv4)
            {
                string subnet = ip.ToString();
                subnet = subnet.Substring(0, subnet.LastIndexOf(".") + 1) + "x";
                if (!ddlHost.Items.Contains(subnet))
                    ddlHost.Items.Add(subnet);
            }
            ddlHost.SelectedIndex = 0;

            txtPorts.Text = MainForm.IPPORTS;


            if (MainForm.IPTABLE != null)
            {
                _dt = MainForm.IPTABLE.Copy();
                dataGridView1.DataSource = _dt;
                dataGridView1.Invalidate();
            }

            txtUsername.Text = MainForm.IPUN;
            txtPassword.Text = MainForm.IPPASS;
            txtIPAddress.Text = MainForm.IPADDR;
            txtChannel.Text = MainForm.IPCHANNEL;
            numPort.Value = MainForm.IPPORT;

            UISync.Init(this);
            LoadSources();
            ShowPanel(pnlConfig);

            
            
        }

        void ShowPanel(Control p)
        {
            pnlConfig.Dock = DockStyle.None;
            pnlConfig.Visible = false;
            pnlFindNetwork.Dock = DockStyle.None;
            pnlFindNetwork.Visible = false;
            pnlConnect.Dock = DockStyle.None;
            pnlConnect.Visible = false;

            p.Dock = DockStyle.Fill;
            p.Visible = true;

            if (p.Name == "pnlConfig")
            {
                btnBack.Enabled = false;
            }
            else
            {
                btnBack.Enabled = true;
            }
        }

        private void RenderResources()
        {
            Text = LocRm.GetString("ConnectCamera");
            button1.Text = LocRm.GetString("ScanLocalNetwork");
            label4.Text = LocRm.GetString("IPAddress");
            label2.Text = LocRm.GetString("Username");
            label3.Text = LocRm.GetString("Password");
            label1.Text = LocRm.GetString("CameraModel");
            label6.Text = LocRm.GetString("Port");
            label5.Text = LocRm.GetString("ScanInstructions");
            btnBack.Text = LocRm.GetString("Back");
            btnNext.Text = LocRm.GetString("Next");
            label8.Text = LocRm.GetString("Adaptor");

            linkLabel1.Text = LocRm.GetString("GetLatestList");
        }

        private void PortScannerManager(string host)
        {
            var ports = new List<int>();

            foreach (string s in txtPorts.Text.Split(','))
            {
                int p;
                if (int.TryParse(s, out p))
                {
                    if (p < 65535 && p > 0)
                        ports.Add(p);
                }
            }
            pbScanner.Value = 0;

            var manualEvents = new ManualResetEvent[MaxThreads];
            int j = 0;
            for (int k = 0; k < MaxThreads; k++)
            {
                manualEvents[k] = new ManualResetEvent(true);
            }

            var ipranges = new List<string>();
            if (host == LocRm.GetString("AllAdaptors"))
            {
                foreach (string s in ddlHost.Items)
                {
                    if (s != LocRm.GetString("AllAdaptors"))
                        ipranges.Add(s);
                }
            }
            else
            {
                ipranges.Add(host);
            }

            UISync.Execute(() => pbScanner.Maximum = ipranges.Count * 254);
            MainForm.LogMessageToFile("Scanning LAN");
            j = 0;
            foreach (string IP in DnsEntries)
            {
                string ip = IP;
                int k = j;
                var scanner = new Thread(p => PortScanner(ports, ip, manualEvents[k]));
                scanner.Start();

                j = WaitHandle.WaitAny(manualEvents);
                UISync.Execute(() => pbScanner.PerformStep());
                if (_exiting)
                    break;
            }

            if (!_exiting)
            {
                j = 0;
                foreach (string shost in ipranges)
                {
                    for (int i = 0; i < 255; i++)
                    {

                        string ip = shost.Replace("x", i.ToString());
                        if (!DnsEntries.Contains(ip))
                        {
                            int k = j;
                            manualEvents[k].Reset();
                            var scanner = new Thread(p => PortScanner(ports, ip, manualEvents[k]));
                            scanner.Start();

                            j = WaitHandle.WaitAny(manualEvents);
                            UISync.Execute(() => pbScanner.PerformStep());
                        }
                        if (_exiting)
                            break;
                    }
                    if (_exiting)
                        break;
                }
            }

            if (j > 0)
                WaitHandle.WaitAll(manualEvents);


            //populate MAC addresses
            try
            {
                var arpStream = ExecuteCommandLine("arp", "-a");
                // Consume first three lines
                for (int i = 0; i < 3; i++)
                {
                    arpStream.ReadLine();
                }
                // Read entries
                while (!arpStream.EndOfStream)
                {
                    var line = arpStream.ReadLine();
                    if (line != null)
                    {
                        line = line.Trim();
                        while (line.Contains("  "))
                        {
                            line = line.Replace("  ", " ");
                        }
                        var parts = line.Trim().Split(' ');

                        if (parts.Length == 3)
                        {
                            for (int i = 0; i < _dt.Rows.Count; i++)
                            {
                                DataRow dr = _dt.Rows[i];
                                string ip = parts[0];
                                if (ip == dr["IP Address"].ToString().Split(':')[0])
                                {
                                    dr["MAC Address"] = parts[1];
                                }
                            }
                        }
                    }
                }
                _dt.AcceptChanges();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            UISync.Execute(() => dataGridView1.Refresh());
            UISync.Execute(() => button1.Enabled = true);
            UISync.Execute(() => pbScanner.Value = 0);
        }


        public static StreamReader ExecuteCommandLine(String file, String arguments = "")
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            startInfo.FileName = file;
            startInfo.Arguments = arguments;

            Process process = Process.Start(startInfo);

            return process.StandardOutput;
        }


        private void PortScanner(IEnumerable<int> ports, string ipaddress, ManualResetEvent mre)
        {
            bool _found = false;
            if (!DnsEntries.Contains(ipaddress))
            {
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                byte[] buffer = Encoding.ASCII.GetBytes(data);

                var netMon = new Ping();
                var options = new PingOptions(128, true);
                PingReply pr = netMon.Send(ipaddress, 3000, buffer, options);
                _found = pr != null && pr.Status == IPStatus.Success;
            }
            else
            {
                _found = true;
            }
            if (_found)
            {
                MainForm.LogMessageToFile("Ping response from " + ipaddress);
                string hostname = "Unknown";
                try
                {
                    var ipToDomainName = Dns.GetHostEntry(ipaddress);
                    hostname = ipToDomainName.HostName;
                }
                catch
                {
                }
                var nc = new NetworkCredential("user", "password");
                foreach (int iport in ports)
                {
                    try
                    {
                        string req = ipaddress + ":" + iport;
                        var request = (HttpWebRequest)WebRequest.Create("http://" + req);
                        request.Referer = "";
                        request.Timeout = 3000;
                        request.UserAgent = "Mozilla/5.0";
                        request.AllowAutoRedirect = false;

                        HttpWebResponse response = null;

                        try
                        {
                            response = (HttpWebResponse)request.GetResponse();
                        }
                        catch (WebException e)
                        {
                            response = (HttpWebResponse)e.Response;
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogMessageToFile("Web error from " + ipaddress + ":" + iport + " " + ex.Message);
                        }
                        if (response != null)
                        {
                            MainForm.LogMessageToFile("Web response from " + ipaddress + ":" + iport + " " +
                                                      response.StatusCode);
                            if (response.Headers != null)
                            {
                                string webserver = "yes";
                                foreach (string k in response.Headers.AllKeys)
                                {
                                    if (k.ToLower().Trim() == "server")
                                        webserver = response.Headers[k];
                                }
                                lock (_dt)
                                {
                                    DataRow dr = _dt.NewRow();
                                    dr[0] = ipaddress;
                                    dr[1] = iport;
                                    dr[2] = hostname;
                                    dr[3] = webserver;
                                    _dt.Rows.Add(dr);
                                    _dt.AcceptChanges();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogMessageToFile("Web error from " + ipaddress + ":" + iport + " " + ex.Message);

                    }
                }
                UISync.Execute(() => dataGridView1.Refresh());
            }
            mre.Set();
        }

        private void dataGridView1_RowHeaderMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ScanNetwork();

        }


        private void ScanNetwork()
        {
            button1.Enabled = false;
            _dt = new DataTable("Network");

            _dt.Columns.Add(new DataColumn("IP Address"));
            _dt.Columns.Add(new DataColumn("Port"));
            _dt.Columns.Add(new DataColumn("Device Name"));
            _dt.Columns.Add(new DataColumn("WebServer"));
            _dt.Columns.Add(new DataColumn("MAC Address"));
            _dt.AcceptChanges();
            dataGridView1.DataSource = _dt;
            string host = ddlHost.SelectedItem.ToString();

            var nb = new NetworkBrowser();

            DnsEntries.Clear();
            try
            {
                foreach (string s1 in nb.GetNetworkComputers())
                {
                    var ipEntry = Dns.GetHostEntry(s1.Trim('\\'));
                    var addr = ipEntry.AddressList.Where(p => p.AddressFamily == AddressFamily.InterNetwork);
                    foreach (var t in addr)
                    {
                        DnsEntries.Add(t.ToString().Trim());
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }

            var manager = new Thread(p => PortScannerManager(host)) { Name = "Port Scanner", IsBackground = false, Priority = ThreadPriority.Normal };
            manager.Start();
        }

        private void LoadSources()
        {
            if (MainForm.Sources == null)
                return;
            ddlMake.Items.Clear();
            foreach (var m in MainForm.Sources)
            {
                ddlMake.Items.Add(m.name);
            }
            ddlMake.Items.Insert(0, LocRm.GetString("PleaseSelect"));
            if (MainForm.IPTYPE != "")
            {
                try
                {
                    ddlMake.SelectedItem = MainForm.IPTYPE;
                }
                catch
                {
                    //may have been removed
                }
            }
            if (ddlMake.SelectedIndex == -1)
                ddlMake.SelectedIndex = 0;
        }

        private void LoadModels()
        {
            ddlModel.Items.Clear();
            if (MainForm.Sources == null || ddlMake.SelectedIndex<1)
                return;

            string make = ddlMake.SelectedItem.ToString();
            var m = MainForm.Sources.FirstOrDefault(p => p.name == make);

            string added = ",";
            ddlModel.Items.Add("Other");

            foreach(var u in m.url)
            {
                if (!String.IsNullOrEmpty(u.version) && added.IndexOf(","+u.version.ToUpper()+",") == -1)
                {
                    ddlModel.Items.Add(u.version);
                    added += u.version.ToUpper() + ",";
                }
            }
            if (ddlModel.SelectedIndex == -1)
                ddlModel.SelectedIndex = 0;

            if (MainForm.IPMODEL != "")
            {
                try
                {
                    ddlModel.SelectedItem = MainForm.IPMODEL;
                }
                catch
                {
                    //may have been removed
                }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void ddlMake_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadModels();
        }

        private void AddConnections()
        {
            pnlOptions.Controls.Clear();
            pnlOptions.AutoScroll = true;
            ShowPanel(pnlConnect);
            if (ddlMake.SelectedItem == null)
            {
                MessageBox.Show(this, "Select a camera make");
                ShowPanel(pnlConfig);
                return;
            }
            string make = ddlMake.SelectedItem.ToString();
            var bVlc = VlcHelper.VlcInstalled;
            var m = MainForm.Sources.FirstOrDefault(p => p.name == make);
            if (m == null)
            {
                MessageBox.Show(this, "There are no sources available for this camera.");
                ShowPanel(pnlConfig);
                return;
            }
            var c = false;
            string model = "";
            if (ddlModel.SelectedIndex>0)
                model = ddlModel.SelectedItem.ToString().ToUpper();

            var cand = m.url.ToList();
            if (model!="")
            {
                cand = cand.Where(p => String.IsNullOrEmpty(p.version) || p.version.ToUpper() == model).ToList();
            }
            cand = cand.OrderBy(p => p.Source).ToList();

            foreach (var u in cand)
            {
                string addr = GetAddr(u);



                string st = "";
                if (!String.IsNullOrEmpty(u.version))
                    st += u.version;
                else
                    st += "Other";
                st += ": " + u.Source + " " + addr.Replace("&","&&");


                var rb = new RadioButton { Text = st, AutoSize = true, Tag = u };
                if (u.Source == "FFMPEG")
                {
                    rb.Checked = true;
                    c = true;
                }
                if (u.Source == "VLC")
                {
                    if (!bVlc)
                        rb.Enabled = false;

                    pnlOptions.Controls.Add(rb);
                }
                else
                {
                    pnlOptions.Controls.Add(rb);
                }

            }

            if (!c)
            {
                const int i = 0;
                while (i < pnlOptions.Controls.Count)
                {
                    if (pnlOptions.Controls[i].Enabled)
                    {
                        ((RadioButton)pnlOptions.Controls[i]).Checked = true;
                        break;
                    }
                }
            }

        }


        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex > -1 && e.RowIndex < _dt.Rows.Count)
            {
                _drSelected = _dt.Rows[e.RowIndex];
                txtIPAddress.Text = _drSelected[0].ToString();
                numPort.Value = Convert.ToInt32(_drSelected[1]);
                if (_drSelected[2].ToString() == "iSpyServer")
                    ddlMake.SelectedItem = "iSpy Camera Server";
            }
        }

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void txtIPAddress_KeyUp(object sender, KeyEventArgs e)
        {
            btnBack.Enabled = ddlMake.SelectedIndex > -1 && txtIPAddress.Text.Trim() != "";
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var doc = new XmlDocument();
            try
            {
                doc.Load(MainForm.Website + "/getcontent.aspx?name=sources");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return;
            }
            doc.Save(Program.AppDataPath + @"XML\Sources.xml");
            MainForm.Sources = null;
            LoadSources();
            MessageBox.Show(LocRm.GetString("ResourcesUpdated"), LocRm.GetString("Note"));
        }

        private void ddlHost_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void FindCameras_FormClosing(object sender, FormClosingEventArgs e)
        {
            _exiting = true;
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (pnlConfig.Visible)
            {
                if (ddlMake.SelectedIndex == 0)
                {
                    MessageBox.Show(this, "Please choose a make");
                    return;
                }
                ShowPanel(pnlFindNetwork);
                return;
            }
            if (pnlFindNetwork.Visible)
            {
                string addr = txtIPAddress.Text.Trim();
                if (addr == "")
                {
                    MessageBox.Show(this, "Please select or enter an IP address");
                    return;
                }
                IPAddress ip;
                if (!IPAddress.TryParse(addr, out ip))
                {
                    MessageBox.Show(this, "Please enter an IP address only (excluding http://)");
                    return;
                }


                AddConnections();
                return;
            }
            if (pnlConnect.Visible)
            {
                if (ddlMake.SelectedIndex == 0)
                {
                    ShowPanel(pnlConfig);
                    return;
                }
                string make = ddlMake.SelectedItem.ToString();
                
                ManufacturersManufacturerUrl s = null;
                for (int j = 0; j < pnlOptions.Controls.Count; j++)
                {
                    if (pnlOptions.Controls[j] is RadioButton)
                    {
                        if (((RadioButton)pnlOptions.Controls[j]).Checked)
                        {
                            s = (ManufacturersManufacturerUrl) ((RadioButton) pnlOptions.Controls[j]).Tag;
                            break;
                        }
                    }
                }
                if (s == null) //should never happen
                    return;

                FinalUrl = GetAddr(s);

                switch (s.Source)
                {
                    case "JPEG":
                        VideoSourceType = 0;
                        break;
                    case "MJPEG":
                        VideoSourceType = 1;
                        break;
                    case "FFMPEG":
                        VideoSourceType = 2;
                        break;
                    case "VLC":
                        VideoSourceType = 5;
                        break;
                }

                Ptzid = -1;

                if (!s.@fixed)
                {
                    string n = make.ToLower();
                    bool quit = false;
                    foreach(var ptz in MainForm.PTZs)
                    {
                        int j = 0;
                        foreach(var m in ptz.Makes)
                        {
                            if (m.Name.ToLower() == n)
                            {
                                Ptzid = ptz.id;
                                Ptzentryid = j;
                                if (m.Model==s.version)
                                {
                                    Ptzid = ptz.id;
                                    Ptzentryid = j;
                                    quit = true;
                                    break;
                                }
                            }
                            j++;
                        }
                        if (quit)
                            break;
                    }
                }

                MainForm.IPUN = txtUsername.Text;
                MainForm.IPPASS = txtPassword.Text;
                MainForm.IPTYPE = ddlMake.SelectedItem.ToString();
                MainForm.IPADDR = txtIPAddress.Text;
                MainForm.IPPORTS = txtPorts.Text;
                MainForm.IPPORT = (int)numPort.Value;
                MainForm.IPCHANNEL = txtChannel.Text.Trim();
                AudioModel = s.AudioModel;

                if (_dt != null)
                    MainForm.IPTABLE = _dt.Copy();
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        private string GetAddr(ManufacturersManufacturerUrl s)
        {
            Username = txtUsername.Text.Trim();
            Password = txtPassword.Text.Trim();
            Channel = txtChannel.Text.Trim();

            string addr = txtIPAddress.Text.Trim();
            Flags = s.flags;
            Cookies = s.cookies;

            var nPort = (int)numPort.Value;

            if (s.Port == 0 && s.prefix == "rtsp://")
                s.Port = 554;

            if (s.Port > 0)
                nPort = s.Port;

            string connectUrl = s.prefix;

            if (!String.IsNullOrEmpty(Username))
            {
                connectUrl += Username;

                if (!String.IsNullOrEmpty(Password))
                    connectUrl += ":" + Password;
                connectUrl += "@";
                     
            }
            connectUrl += addr + ":" + nPort + "/";

            string url = s.url;
            url = url.Replace("[USERNAME]", Username).Replace("[PASSWORD]", Password);
            url = url.Replace("[CHANNEL]", txtChannel.Text.Trim());
            //defaults:
            url = url.Replace("[WIDTH]", "320");
            url = url.Replace("[HEIGHT]", "240");

            if (url.IndexOf("[AUTH]")!=-1)
            {
                string credentials = String.Format("{0}:{1}", Username, Password);
                byte[] bytes = Encoding.ASCII.GetBytes(credentials);
                url = url.Replace("[AUTH]", Convert.ToBase64String(bytes));
            }
                

                
            connectUrl += url;
            return connectUrl;
        }

        private void btnBack_Click(object sender, EventArgs e)
        {
            if (pnlFindNetwork.Visible)
            {
                ShowPanel(pnlConfig);

            }
            if (pnlConnect.Visible)
            {
                ShowPanel(pnlFindNetwork);
            }
        }

        private void llblDownloadVLC_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MainForm.OpenUrl("http://www.videolan.org/vlc/download-windows.html");
        }

        private void ddlModel_SelectedIndexChanged(object sender, EventArgs e)
        {

        }


    }
}
