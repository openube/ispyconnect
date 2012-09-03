using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class PTZTool : Form
    {
        private bool _loaded;
        private CameraWindow _cameraControl;
        private bool _mousedown = false;
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
                    ddlExtended.Items.Add(new ListItem("Click on a Camera", ""));
                    ddlExtended.SelectedIndex = 0;
                }

                pnlController.Enabled = false;
                if (value != null)
                {
                    if (CameraControl.Camobject.ptz > -1)
                    {
                        ddlExtended.Items.Add(new ListItem("Select Command", ""));
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
                        ddlExtended.Items.Add(new ListItem("Digital PTZ only", ""));
                        pnlController.Enabled = true;
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
                    LocRm.GetString("Validate_Camera_PTZIPOnly") + Environment.NewLine + Environment.NewLine +
                    ex.Message, LocRm.GetString("Error"));
            }
        }

        private void ProcessPtzInput(Point p)
        {
            if (CameraControl.Camera == null)
                return;
            bool d = MainForm.PTZs.SingleOrDefault(q => q.id == CameraControl.Camobject.ptz) == null;
            Enums.PtzCommand comm = Enums.PtzCommand.Center;
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
                if (!d)
                {
                    PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(q => q.id == CameraControl.Camobject.ptz);
                    if (ptz == null || String.IsNullOrEmpty(ptz.Commands.ZoomIn))
                        d = true;
                }
            
            }
            if (p.X > 170 && p.Y > 45 && p.Y < 90)
            {
                comm = Enums.PtzCommand.ZoomOut;
                if (!d)
                {
                    PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(q => q.id == CameraControl.Camobject.ptz);
                    if (ptz == null || String.IsNullOrEmpty(ptz.Commands.ZoomIn)) //use zoomin just in case zoomout is defined and zoomin isn't
                        d = true;
                }
            }

            if (d)
            {
                Rectangle r = CameraControl.Camera.ViewRectangle;
                if (r != Rectangle.Empty)
                {
                    if (comm==Enums.PtzCommand.ZoomOut || comm==Enums.PtzCommand.ZoomIn)
                        CameraControl.Camera.ZPoint = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
                    double angle = 0;
                    bool isangle = true;
                    switch (comm)
                    {
                        case Enums.PtzCommand.Left:
                            angle=0;
                            break;
                        case Enums.PtzCommand.Upleft:
                            angle = Math.PI/4;
                            break;
                        case Enums.PtzCommand.Up:
                            angle= Math.PI / 2;
                            break;
                        case Enums.PtzCommand.UpRight:
                            angle = 3 * Math.PI / 4;
                            break;
                        case Enums.PtzCommand.Right:
                            angle = Math.PI;
                            break;
                        case Enums.PtzCommand.DownRight:
                            angle = -3 * Math.PI / 4;
                            break;
                        case Enums.PtzCommand.Down:
                            angle = -Math.PI / 2;
                            break;
                        case Enums.PtzCommand.DownLeft:
                            angle = -Math.PI / 4;
                            break;
                        case Enums.PtzCommand.ZoomIn:
                            isangle = false;
                            CameraControl.Camera.ZFactor += 0.2f;
                            break;
                        case Enums.PtzCommand.ZoomOut:
                            isangle = false;
                            CameraControl.Camera.ZFactor -= 0.2f;
                            if (CameraControl.Camera.ZFactor < 1)
                                CameraControl.Camera.ZFactor = 1;
                            break;
                        case Enums.PtzCommand.Center:
                            isangle = false;
                            CameraControl.Camera.ZFactor = 1;
                            break;
                        
                    }
                    if (isangle)
                    {
                        CameraControl.Camera.ZPoint.X -= Convert.ToInt32(15 * Math.Cos(angle));
                        CameraControl.Camera.ZPoint.Y -= Convert.ToInt32(15 * Math.Sin(angle));
                    }

                }
            }
            else
            {
                CameraControl.PTZ.SendPTZCommand(comm, false);    
            }
            
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
