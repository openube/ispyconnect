using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Controls;

namespace iSpyApplication
{
    public partial class PTZTool : Form
    {
        private bool _loaded;
        private CameraWindow _cameraControl;
        private bool _mousedown;
        private Point _location = Point.Empty;

        public CameraWindow CameraControl
        {
            get { return _cameraControl; }
            set { 
                _cameraControl = value;
                _loaded = false;
                ddlExtended.Items.Clear();
                if (_cameraControl == null)
                {
                    ddlExtended.Items.Add(new ListItem(LocRm.GetString("ClickCamera"),""));
                    ddlExtended.SelectedIndex = 0;
                }

                pnlController.Enabled = false;
                if (value != null)
                {
                    if (CameraControl.Camobject.ptz > -1)
                    {
                        ddlExtended.Items.Add(new ListItem(LocRm.GetString("SelectCommand"), ""));
                        PTZSettings2Camera ptz = MainForm.PTZs.Single(p => p.id == CameraControl.Camobject.ptz);
                        if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command!=null)
                        {
                            foreach (var extcmd in ptz.ExtendedCommands.Command)
                            {
                                ddlExtended.Items.Add(new ListItem(extcmd.Name, extcmd.Value));
                            }
                        }
                        pnlController.Enabled = true;
                    }
                    else
                    {
                        switch (CameraControl.Camobject.ptz)    
                        {
                            case -1:
                                ddlExtended.Items.Add(new ListItem(LocRm.GetString("DigitalPTZonly"), ""));
                                pnlController.Enabled = true;
                                break;
                            case -2:
                                ddlExtended.Items.Add(new ListItem("IAM-Control", ""));
                                pnlController.Enabled = true;
                                break;
                            case -3:
                            case -4:
                                //Pelco extended
                                ddlExtended.Items.Add(new ListItem(LocRm.GetString("SelectCommand"), ""));
                                foreach(string cmd in PTZController.PelcoCommands)
                                {
                                    ddlExtended.Items.Add(new ListItem(cmd, cmd));
                                }
                                pnlController.Enabled = true;
                                break;
                            case -5:
                                //ONVIF
                                ddlExtended.Items.Add(new ListItem(LocRm.GetString("SelectCommand"), ""));
                                foreach(string cmd in CameraControl.PTZ.ONVIFPresets)
                                {
                                    ddlExtended.Items.Add(new ListItem(cmd, cmd));
                                }
                                pnlController.Enabled = true;
                                break;
                        }
                    }
                    Text = "PTZ: "+CameraControl.Camobject.name;
                    
                    ddlExtended.SelectedIndex = 0;
                }
                _loaded = true;
            }

        }

        public PTZTool()
        {
            InitializeComponent();
        }

        private void pnlPTZ_MouseDown(object sender, MouseEventArgs e)
        {
            if (_cameraControl == null)
                return;
            _mousedown = true;
            _location = e.Location;            
            tmrRepeater.Start();
            ProcessPtzInput(_location);
        }

        private void SendPtzCommand(string cmd, bool wait)
        {
            if (cmd == "")
            {
                MessageBox.Show(LocRm.GetString("CommandNotSupported"));
                return;
            }
            try
            {
                CameraControl.PTZ.SendPTZCommand(cmd, wait);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocRm.GetString("Validate_Camera_PTZIPOnly"), LocRm.GetString("Error"));
            }
        }

        private void ProcessPtzInput(Point p)
        {
            if (CameraControl.Camera == null)
                return;

            var comm = Enums.PtzCommand.Center;
            if (p.X < 60 && p.Y > 60 && p.Y < 106)
            {
                comm = Enums.PtzCommand.Left;
            }
            if (p.X < 60 && p.Y < 60)
            {
                comm = Enums.PtzCommand.Upleft;
            }
            if (p.X > 60 && p.X < 104 && p.Y < 60)
            {
                comm = Enums.PtzCommand.Up;
            }
            if (p.X > 104 && p.X < 164 && p.Y < 60)
            {
                comm = Enums.PtzCommand.UpRight;
            }
            if (p.X > 104 && p.X < 170 && p.Y > 60 && p.Y < 104)
            {
                comm = Enums.PtzCommand.Right;
            }
            if (p.X > 104 && p.X < 170 && p.Y > 104)
            {
                comm = Enums.PtzCommand.DownRight;
            }
            if (p.X > 60 && p.X < 104 && p.Y > 104)
            {
                comm = Enums.PtzCommand.Down;
            }
            if (p.X < 60 && p.Y > 104)
            {
                comm = Enums.PtzCommand.DownLeft;
            }

            if (p.X > 170 && p.Y < 45)
            {
                comm = Enums.PtzCommand.ZoomIn;
            }
            if (p.X > 170 && p.Y > 45 && p.Y < 90)
            {
                comm = Enums.PtzCommand.ZoomOut;
            }

            CameraControl.PTZ.SendPTZCommand(comm);


        }

        

        private void pnlPTZ_MouseUp(object sender, MouseEventArgs e)
        {
            _mousedown = false;
            tmrRepeater.Stop();
            if (CameraControl == null)
                return;

            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(p => p.id == CameraControl.Camobject.ptz);
            if (ptz != null && !String.IsNullOrEmpty(ptz.Commands.Stop))
                SendPtzCommand(ptz.Commands.Stop, true);
            
        }

        private void ddlExtended_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_loaded && CameraControl!=null)
            {
                if (ddlExtended.SelectedIndex > 0)
                {
                    var li = ((ListItem) ddlExtended.SelectedItem);
                    SendPtzCommand(li.Value, true);
                    ddlExtended.SelectedIndex = 0;
                }
            }
        }

        private struct ListItem
        {
            private readonly string _name;
            internal readonly string Value;

            public ListItem(string name, string value)
            {
                _name = name;
                Value = value;
            }

            public override string ToString()
            {
                return _name;
            }
        }

        private void PTZTool_Load(object sender, EventArgs e)
        {

        }

        private void pnlPTZ_MouseMove(object sender, MouseEventArgs e)
        {
            _location = e.Location;
        }

        private void tmrRepeater_Tick(object sender, EventArgs e)
        {
            if (_mousedown)
                ProcessPtzInput(_location);
        }

        private void PTZTool_FormClosing(object sender, FormClosingEventArgs e)
        {
            

        }
    }
}
