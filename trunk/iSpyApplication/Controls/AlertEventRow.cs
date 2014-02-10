using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public sealed partial class AlertEventRow : UserControl
    {
        public new static int Height = 31;
        public objectsCameraAlerteventsEntry OcaeeC;
        public objectsMicrophoneAlerteventsEntry OcaeeM;

        public event EventHandler AlertEntryDelete;
        public event EventHandler AlertEntryEdit;
        public event EventHandler MouseOver;


        public AlertEventRow(objectsCameraAlerteventsEntry ocaee)
        {
            OcaeeC = ocaee;
            InitializeComponent();
            lblSummary.Text = GetSummary(OcaeeC.type, OcaeeC.param1, OcaeeC.param2, OcaeeC.param3, OcaeeC.param4);
            BackColor = DefaultBackColor;
        }

        public AlertEventRow(objectsMicrophoneAlerteventsEntry ocaee)
        {
            OcaeeM = ocaee;
            InitializeComponent();
            lblSummary.Text = GetSummary(OcaeeM.type, OcaeeM.param1, OcaeeM.param2, OcaeeM.param3, OcaeeM.param4);
            BackColor = DefaultBackColor;
        }

        private string GetSummary(string type, string param1, string param2, string param3, string param4)
        {
            string t= "Unknown";
            switch (type)
            {
                case "Exe":
                    t = "Execute: " + param1;
                    break;
                case "URL":
                    t = "URL: " + param1;
                    if (Convert.ToBoolean(param2))
                        t += " (POST grab)";
                    break;
                case "NM":
                    t = param1 + " " + param2 + ":" + param3 + " (" + param4 + ")";
                    break;
                case "S":
                    t = "Sound: " + param1;
                    break;
                case "SW":
                    t = "Show Window";
                    pbEdit.Visible = false;
                    break;
                case "B":
                    t = "Beep Speaker";
                    pbEdit.Visible = false;
                    break;
                case "M":
                    t = "Maximise";
                    pbEdit.Visible = false;
                    break;
                case "TA":
                    {
                        string[] op = param1.Split(',');
                        string n = "[removed]";
                        int id = Convert.ToInt32(op[1]);
                        switch (op[0])
                        {
                            case "1":
                                var om = MainForm.Microphones.FirstOrDefault(p => p.id == id);
                                if (om != null)
                                    n = om.name;
                                break;
                            case "2":
                                var oc = MainForm.Cameras.FirstOrDefault(p => p.id == id);
                                if (oc != null)
                                    n = oc.name;
                                break;
                        }
                        t = "Trigger Alert on " + n;
                    }
                    break;
                case "SOO":
                    {
                        string[] op = param1.Split(',');
                        string n = "[removed]";
                        int id = Convert.ToInt32(op[1]);
                        switch (op[0])
                        {
                            case "1":
                                var om = MainForm.Microphones.FirstOrDefault(p => p.id == id);
                                if (om != null)
                                    n = om.name;
                                break;
                            case "2":
                                var oc = MainForm.Cameras.FirstOrDefault(p => p.id == id);
                                if (oc != null)
                                    n = oc.name;
                                break;
                        }
                        t = "Switch on " + n;
                    }
                    break;
                case "E":
                    t = "Send Email: " + param1;
                    if (param2!="" && Convert.ToBoolean(param2))
                        t += " (include grab)";
                    break;
                case "SMS":
                    t = "Send SMS: " + param1;
                    break;
                case "TM":
                    t = "Tweet (Direct Message)";
                    pbEdit.Visible = false;
                    break;
            }
            //if (t.Length > 50)
            //    t = t.Substring(0, 47) + "...";
            return t;
        }

        

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (AlertEntryEdit != null)
                AlertEntryEdit(this, EventArgs.Empty);
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            if (AlertEntryDelete != null)
                AlertEntryDelete(this, EventArgs.Empty);
        }

        private void tableLayoutPanel1_MouseEnter(object sender, EventArgs e)
        {
            tableLayoutPanel1.BackColor = System.Drawing.Color.FromArgb(255, 221, 221, 221);
            if (MouseOver != null)
                MouseOver(this, EventArgs.Empty);
        }

        public void RevertBackground()
        {
            tableLayoutPanel1.BackColor = DefaultBackColor;
            Invalidate();
        }

    }
}
