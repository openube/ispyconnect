using System;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public partial class ActionEditor : UserControl
    {
        public objectsCameraAlertevents Aec;
        public objectsMicrophoneAlertevents Aem;
        public event EventHandler LoginRequested;

        public string[] Actions =
            {
                "Exe|Execute File",
                "URL|Call URL",
                "NM|Network Message",
                "S|Play Sound",
                "SW|Show Window",
                "B|Beep",
                "M|Maximise",
                "TA|Trigger Alert On...",
                "E|Send Email [SUBSCRIBER]",
                "SMS|Send SMS [SUBSCRIBER]",
                "TM|Send Twitter Message [SUBSCRIBER]"
            };

        public ActionEditor()
        {
            InitializeComponent();
        }
        
        public void Init(objectsCameraAlertevents ae)
        {
            Aec = ae;
            Init();

        }
        public void Init(objectsMicrophoneAlertevents ae)
        {
            Aem = ae;
            Init();

        }

        private void Init() {
            ddlAction.Items.Clear();
            ddlAction.Items.Add(new ListItem {Name = "Select Action", Restricted = false, Value = ""});
            foreach (string s in Actions)
            {
                var oc = s.Split('|');
                var li = new ListItem();
                var restricted = false;

                if (MainForm.Conf.Subscribed)
                    oc[1] = oc[1].Replace("[SUBSCRIBER]", "");
                else
                {
                    if (oc[1].IndexOf("[SUBSCRIBER]", StringComparison.Ordinal) != -1)
                    {
                        oc[1] = oc[1].Replace("[SUBSCRIBER]", "(Subscribers Only)");
                        restricted = true;
                    }
                }
                li.Name = oc[1];
                li.Value = oc[0];
                li.Restricted = restricted;
                ddlAction.Items.Add(li);
            }
            ddlAction.SelectedIndex = 0;
            flpActions.VerticalScroll.Visible = true;
            flpActions.HorizontalScroll.Visible = false;
            RenderEventList();
        }

        void RenderEventList()
        {
            flpActions.Controls.Clear();
            int vertScrollWidth = SystemInformation.VerticalScrollBarWidth;

            var w = flpActions.Width - 2;
            
            
            if (Aec != null)
            {
                if (Aec.entries.Length*AlertEventRow.Height>=flpActions.Height)
                w = flpActions.Width - vertScrollWidth-2;
                foreach (var e in Aec.entries)
                {
                    var c = new AlertEventRow(e) {Width = w};
                    c.AlertEntryDelete += CAlertEntryDelete;
                    c.AlertEntryEdit += CAlertEntryEdit;
                    c.MouseOver += CMouseOver;
                    flpActions.Controls.Add(c);
                    flpActions.SetFlowBreak(c,true);
                }
            }
            if (Aem != null)
            {
                if (Aem.entries.Length * AlertEventRow.Height >= flpActions.Height)
                    w = flpActions.Width - vertScrollWidth - 2;

                foreach (var e in Aem.entries)
                {
                    var c = new AlertEventRow(e) { Width = w};
                    c.AlertEntryDelete += CAlertEntryDelete;
                    c.AlertEntryEdit += CAlertEntryEdit;
                    c.MouseOver += CMouseOver;
                    flpActions.Controls.Add(c);
                    flpActions.SetFlowBreak(c, true);
                }
            }
            
            flpActions.PerformLayout();
            flpActions.HorizontalScroll.Visible = flpActions.HorizontalScroll.Enabled = false;
            
        }

        void CMouseOver(object sender, EventArgs e)
        {
            foreach(var c in flpActions.Controls)
            {
                var o = (AlertEventRow) c;
                if (o!=sender)
                {
                    o.RevertBackground();

                }
            }
        }

        void CAlertEntryEdit(object sender, EventArgs e)
        {
            string t = "";
            string param1Val = "";
            string param2Val = "";
            string param3Val = "";
            string param4Val = "";

            if (Aec != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeC;
                t = oe.type;
                param1Val = oe.param1;
                param2Val = oe.param2;
                param3Val = oe.param3;
                param4Val = oe.param4;
            }
            if (Aem != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeM;
                t = oe.type;
                param1Val = oe.param1;
                param2Val = oe.param2;
                param3Val = oe.param3;
                param4Val = oe.param4;
            }

            bool cancel;
            var config = GetConfig(param2Val, param3Val, param4Val, param1Val, t, out cancel);

            if (cancel)
                return;

            if (Aec != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeC;

                if (config.Length > 0)
                {
                    oe.param1 = config[0];
                }
                if (config.Length > 1)
                {
                    oe.param2 = config[1];
                }
                if (config.Length > 2)
                {
                    oe.param3 = config[2];
                }
                if (config.Length > 3)
                {
                    oe.param4 = config[3];
                }
            }

            if (Aem != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeM;

                if (config.Length > 0)
                {
                    oe.param1 = config[0];
                }
                if (config.Length > 1)
                {
                    oe.param2 = config[1];
                }
                if (config.Length > 2)
                {
                    oe.param3 = config[2];
                }
                if (config.Length > 3)
                {
                    oe.param4 = config[3];
                }
            }
            RenderEventList();
        }

        private string[] GetConfig(string param2Val, string param3Val, string param4Val, string param1Val, string t,
                                   out bool cancel)
        {
            cancel = false;
            var config = new string[] {};
            switch (t)
            {
                case "Exe":
                    config = GetParamConfig(GetName(t), out cancel, "File|FBD:*.*", param1Val, "Arguments", param2Val);
                    break;
                case "URL":
                    if (param1Val == "")
                        param1Val = "http://";
                    if (param2Val == "")
                        param2Val = "True";
                    config = GetParamConfig(GetName(t), out cancel, "URL", param1Val, "POST Grab|Checkbox:True", param2Val);
                    break;
                case "NM":
                    if (param3Val=="")
                       param3Val = "1010";
                    config = GetParamConfig(GetName(t), out cancel, "Type|DDL:TCP,UDP", param1Val, "IP Address",
                                            param2Val, "Port|Numeric:0,65535", param3Val, "Message", param4Val);
                    break;
                case "S":
                    config = GetParamConfig("Sound", out cancel, "File|FBD:*.wav", param1Val);
                    break;
                case "SW":
                case "B":
                case "M":
                    config = new [] {"","","",""};
                    break;
                case "TA":
                    config = GetParamConfig(GetName(t), out cancel, "Object|Object", param1Val);
                    break;
                case "E":
                    if (param2Val == "")
                        param2Val = "True";
                    config = GetParamConfig(GetName(t), out cancel, "Email Address", param1Val, "Include Grab|Checkbox:True", param2Val);
                    break;
                case "SMS":
                    config = GetParamConfig(GetName(t), out cancel, "SMS Number|SMS", param1Val);
                    break;
                case "TM":
                    config = GetParamConfig(GetName(t), out cancel, "|Link:Authorise Twitter", MainForm.Webserver + "/account.aspx?task=twitter-auth");
                    break;
            }
            
            return config;
        }

        private string GetName(string type)
        {
            var n = "";
            foreach(var s in Actions)
            {
                if (s.StartsWith(type+"|"))
                {
                    n = s.Substring(type.Length + 1).Replace("[SUBSCRIBER]","");
                }
            }
            return n;
        }

        void CAlertEntryDelete(object sender, EventArgs e)
        {
            if (Aec != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeC;
                var l = Aec.entries.ToList();
                l.Remove(oe);
                Aec.entries = l.ToArray();
            }
            if (Aem != null)
            {
                var oe = ((AlertEventRow)sender).OcaeeM;
                var l = Aem.entries.ToList();
                l.Remove(oe);
                Aem.entries = l.ToArray();
            }

            RenderEventList();
        }


        private struct ListItem
        {
            public string Name, Value;
            public bool Restricted;

            public override string ToString()
            {
                return Name;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (ddlAction.SelectedIndex < 1)
            {
                MessageBox.Show(this, "Select an action to add");
                return;
            }
            var oa = (ListItem) ddlAction.SelectedItem;
            
            if (!MainForm.Conf.Subscribed && oa.Restricted)
            {
                if (LoginRequested!=null)
                    LoginRequested(this, EventArgs.Empty);
                return;
            }
            bool cancel;
            string[] config = GetConfig("", "", "", "", oa.Value, out cancel);
            if (cancel)
                return;

            if (Aec != null)
            {
                var ocaee = new objectsCameraAlerteventsEntry
                                {
                                    type = oa.Value,
                                    param1 = config[0],
                                    param2 = config[1],
                                    param3 = config[2],
                                    param4 = config[3]
                                };

                var l = Aec.entries.ToList();
                l.Add(ocaee);
                Aec.entries = l.ToArray();
            }

            if (Aem != null)
            {
                var ocaee = new objectsMicrophoneAlerteventsEntry { type = oa.Value,
                    param1 = config[0],
                    param2 = config[1],
                    param3 = config[2],
                    param4 = config[3]
                                };

                var l = Aem.entries.ToList();
                l.Add(ocaee);
                Aem.entries = l.ToArray();
            }

            RenderEventList();
        }

        private string[] GetParamConfig(string typeName, out bool cancel, 
            string param1 = "", string param1Value = "",
            string param2 = "", string param2Value = "",
            string param3 = "", string param3Value = "",
            string param4 = "", string param4Value = "")
        {
            cancel = false;
            var pc = new ParamConfig {TypeName = typeName, 
                Param1 = param1, Param1Value = param1Value,
                Param2 = param2, Param2Value = param2Value,
                Param3 = param3, Param3Value = param3Value,
                Param4 = param4, Param4Value = param4Value};
            if (pc.ShowDialog(this)!=DialogResult.OK)
            {
                cancel = true;
            }
            var cfg = new [] { pc.Param1Value, pc.Param2Value, pc.Param3Value, pc.Param4Value };
            pc.Dispose();
            return cfg;
        }
    }
}
