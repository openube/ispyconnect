using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Timer = System.Timers.Timer;

namespace iSpyMonitor
{
    public partial class Monitor : Form
    {
        internal DataTable dt = new DataTable("Activity");
        private Timer pollTimer;

        public Monitor()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var dc = new DataColumn("Time", typeof(DateTime));
            var dc2 = new DataColumn("Event", typeof(String));
            var dc3 = new DataColumn("Data", typeof(String));

            dt.Columns.Add(dc);
            dt.Columns.Add(dc2);
            dt.Columns.Add(dc3);
            dt.AcceptChanges();

            var dr = dt.NewRow();
            dr["Time"] = DateTime.Now;
            dr["Event"] = "STARTED";
            dr["Data"] = Program.ProgramName;

            dt.Rows.Add(dr);

            dataGridView1.DataSource = dt;
            dataGridView1.Invalidate();

            WindowState = FormWindowState.Minimized;
            Hide();

            pollTimer = new Timer(1000);
            pollTimer.Elapsed += tmrPoll_Tick;
            pollTimer.AutoReset = true;
            pollTimer.SynchronizingObject = this;
            pollTimer.Start();

        }

        private void tmrPoll_Tick(object sender, EventArgs e)
        {
            pollTimer.Stop();
            try
            {
                var w = Process.GetProcessesByName(Program.ProgramName);
                if (w.Length == 0)
                {
                    if (File.Exists(Program.AppDataPath + "exit.txt") &&
                        File.ReadAllText(Program.AppDataPath + "exit.txt") == "OK")
                    {
                        reallyclose = true;
                        Close();
                        return;
                    }

                    //app has crashed and terminated
                    var dr = dt.NewRow();
                    dr["Time"] = DateTime.Now;
                    dr["Event"] = "RESTART";
                    dr["Data"] = "";
                    dt.Rows.Add(dr);
                    dataGridView1.Invalidate();

                    var si = new ProcessStartInfo(Program.AppPath + Program.ProgramName+".exe", "");
                    Process.Start(si);

                }
                else
                {
                    var p = w[0];
                    var b = p.Responding;
                    var c = 0;
                    while (!b && c < 180)
                    {
                        b = p.Responding;
                        Thread.Sleep(1000);
                        c++;
                    }

                    if (!b)
                    {
                        //app has hung (3 minutes non responsive)
                        p.Kill();
                        var dr = dt.NewRow();
                        dr["Time"] = DateTime.Now;
                        dr["Event"] = "KILL (UNRESPONSIVE)";
                        dr["Data"] = "";
                        dt.Rows.Add(dr);
                        dataGridView1.Invalidate();
                    }

                }
            } catch {}

            pollTimer.Start();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
        }

        private void niMonitor_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            
        }

        private void niMonitor_DoubleClick(object sender, EventArgs e)
        {
            Activate();
            Visible = true;
            if (WindowState == FormWindowState.Minimized)
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
            TopMost = true;
            TopMost = false;//need to force a switch to move above other forms
            BringToFront();
            Focus();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            reallyclose = true;
            Close();
        }

        private bool reallyclose = false;
        private void Monitor_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason != CloseReason.WindowsShutDown)
            {
                if (!reallyclose)
                {
                    e.Cancel = true;
                    this.WindowState = FormWindowState.Minimized;
                }
            }
        }
    }
}
