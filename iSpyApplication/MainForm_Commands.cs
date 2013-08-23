using System;
using System.Diagnostics;
using System.Linq;
using System.Timers;
using System.Windows.Forms;
using iSpyApplication.Controls;
using iSpyApplication.Joystick;

namespace iSpyApplication
{
    partial class MainForm
    {
        private JoystickDevice _jst;
        private readonly bool[] _buttonsLast = new bool[128];
        private bool _needstop, _sentdirection;

        private delegate void RunCheckJoystick();

        void TmrJoystickElapsed(object sender, ElapsedEventArgs e)
        {
            if (_shuttingDown)
                return;
            _tmrJoystick.Stop();
            Invoke(new RunCheckJoystick(CheckJoystick));
            _tmrJoystick.Start();
        }

        private void CheckJoystick()    {

            if (_jst != null)
            {
                _jst.UpdateStatus();

                CameraWindow cw=null;
                VolumeLevel vl=null;

                foreach (Control c in _pnlCameras.Controls)
                {
                    if (c.Focused)
                    {
                        cw = c as CameraWindow;
                        vl = c as VolumeLevel;
                        break;
                    }
                }

                for (int i = 0; i < _jst.Buttons.Length; i++)
                {

                    if (_jst.Buttons[i] != _buttonsLast[i] && _jst.Buttons[i])
                    {
                        int j = i + 1;
                        
                        if (j == Conf.Joystick.Listen)
                        {
                            if (cw != null)
                            {
                                if (cw.VolumeControl != null)
                                {
                                    cw.VolumeControl.Listening = !cw.VolumeControl.Listening;
                                }
                            }
                            if (vl!=null)
                            {
                                vl.Listening = !vl.Listening;
                            }
                        }

                        if (j == Conf.Joystick.Talk)
                        {
                            if (cw != null)
                            {
                                cw.Talking = !cw.Talking;
                                TalkTo(cw, cw.Talking);
                            }
                        }
                        
                        if (j == Conf.Joystick.Previous)
                        {
                            ProcessKey("previous_control");
                        }

                        if (j == Conf.Joystick.Next)
                        {
                            ProcessKey("next_control");
                        }

                        if (j == Conf.Joystick.Play)
                        {
                            ProcessKey("play");
                        }

                        if (j == Conf.Joystick.Stop)
                        {
                            ProcessKey("stop");
                        }

                        if (j == Conf.Joystick.Record)
                        {
                            ProcessKey("record");
                        }

                        if (j == Conf.Joystick.Snapshot)
                        {
                            if (cw!=null)
                                cw.SaveFrame();
                        }

                    }

                    _buttonsLast[i] = _jst.Buttons[i];

                }

                if (cw != null)
                {
                    _sentdirection = false;
                    int x = 0, y = 0;

                    double angle = -1000;

                    if (Conf.Joystick.XAxis < 0)
                    {
                        //dpad - handles x and y
                        int dpad = _jst.Dpads[(0 - Conf.Joystick.XAxis) - 1];
                        switch (dpad)
                        {
                            case 27000:
                                angle = 0;
                                break;
                            case 31500:
                                angle = Math.PI/4;
                                break;
                            case 0:
                                angle = Math.PI/2;
                                break;
                            case 4500:
                                angle = 3*Math.PI/4;
                                break;
                            case 9000:
                                angle = Math.PI;
                                break;
                            case 13500:
                                angle = -3*Math.PI/4;
                                break;
                            case 18000:
                                angle = -Math.PI/2;
                                break;
                            case 22500:
                                angle = -Math.PI/4;
                                break;
                        }
                    }
                    else
                    {
                        if (Conf.Joystick.XAxis > 0)
                        {
                            x = _jst.Axis[Conf.Joystick.XAxis - 1] - Conf.Joystick.CenterXAxis;
                        }

                        if (Conf.Joystick.YAxis > 0)
                        {
                            y = _jst.Axis[Conf.Joystick.YAxis - 1] - Conf.Joystick.CenterYAxis;
                        }

                        var d = Math.Sqrt((x*x) + (y*y));
                        if (d > 20)
                        {
                            angle = Math.Atan2(y, x);
                        }
                    }

                    if (angle > -1000)
                    {
                        if (Conf.Joystick.InvertYAxis)
                        {
                            angle = 0 - angle;
                        }
                        if (Conf.Joystick.InvertXAxis)
                        {
                            if (angle >= 0)
                                angle = Math.PI - angle;
                            else
                                angle = (0 - Math.PI) - angle;
                        }

                        cw.Calibrating = true;
                        cw.PTZ.SendPTZDirection(angle);
                        if (!cw.PTZ.DigitalPTZ)
                            _needstop = _sentdirection = true;
                    }

                    if (Conf.Joystick.ZAxis > 0)
                    {
                        var z = _jst.Axis[Conf.Joystick.ZAxis - 1] - Conf.Joystick.CenterZAxis;

                        if (Math.Abs(z) > 20)
                        {
                            if (Conf.Joystick.InvertZAxis)
                                z = 0-z;
                            cw.Calibrating = true;
                            cw.PTZ.SendPTZCommand(z > 0 ? Enums.PtzCommand.ZoomIn : Enums.PtzCommand.ZoomOut);

                            if (!cw.PTZ.DigitalZoom)
                                _needstop = _sentdirection = true;
                        }
                    }

                    if (!_sentdirection && _needstop)
                    {
                        cw.PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                        _needstop = false;
                    }
                }



            }
        }

        public void SwitchObjects(bool scheduledOnly, bool on)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    if (on && !cameraControl.Camobject.settings.active)
                    {
                        if (!scheduledOnly || cameraControl.Camobject.schedule.active)
                            cameraControl.Enable();
                    }

                    if (!on && cameraControl.Camobject.settings.active)
                    {
                        if (!scheduledOnly || cameraControl.Camobject.schedule.active)
                            cameraControl.Disable();
                    }
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;

                    if (on && !volumeControl.Micobject.settings.active)
                    {
                        if (!scheduledOnly || volumeControl.Micobject.schedule.active)
                            volumeControl.Enable();
                    }

                    if (!on && volumeControl.Micobject.settings.active)
                    {
                        if (!scheduledOnly || volumeControl.Micobject.schedule.active)
                            volumeControl.Disable();
                    }
                }
            }
        }

        public void RecordOnDetect(bool on)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    cameraControl.Camobject.detector.recordondetect = on;
                    if (on && cameraControl.Camobject.detector.recordonalert)
                        cameraControl.Camobject.detector.recordonalert = false;
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;
                    volumeControl.Micobject.detector.recordondetect = on;
                    if (on && volumeControl.Micobject.detector.recordonalert)
                        volumeControl.Micobject.detector.recordonalert = false;
                }
            }
        }

        public void SnapshotAll()
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    if (cameraControl.Camobject.settings.active)
                        cameraControl.SaveFrame();
                }
            }
        }

        public void RecordOnAlert(bool on)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    cameraControl.Camobject.detector.recordonalert = on;
                    if (on && cameraControl.Camobject.detector.recordondetect)
                        cameraControl.Camobject.detector.recordondetect = false;
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;
                    volumeControl.Micobject.detector.recordonalert = on;
                    if (on && volumeControl.Micobject.detector.recordondetect)
                        volumeControl.Micobject.detector.recordondetect = false;
                }
            }
        }

        public void AlertsActive(bool on)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    cameraControl.Camobject.alerts.active = on;
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;
                    volumeControl.Micobject.alerts.active = on;
                }
            }
        }

        public void RecordAll(bool record)
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                if (c is CameraWindow)
                {
                    var cameraControl = (CameraWindow)c;
                    if (cameraControl.IsEnabled)
                        cameraControl.RecordSwitch(record);
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;
                    if (volumeControl.IsEnabled)
                        volumeControl.RecordSwitch(record);
                }
            }
        }

        private void ShowRemoteCommands()
        {
            var ma = new RemoteCommands { Owner = this };
            ma.ShowDialog(this);
            ma.Dispose();
            LoadCommands();
        }

        public static void InitRemoteCommands()
        {
            //copy over 
            _remotecommands.Clear();
            var cmd = new objectsCommand
            {
                command = "ispy ALLON",
                id = 0,
                name = "cmd_SwitchAllOn",
                smscommand = "ALL ON"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy ALLOFF",
                id = 1,
                name = "cmd_SwitchAllOff",
                smscommand = "ALL OFF"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy APPLYSCHEDULE",
                id = 2,
                name = "cmd_ApplySchedule",
                smscommand = "APPLY SCHEDULE"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy RECORDONDETECTON",
                id = 3,
                name = "cmd_RecordOnDetectAll",
                smscommand = "RECORDONDETECTON"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy RECORDONALERTON",
                id = 4,
                name = "cmd_RecordOnAlertAll",
                smscommand = "RECORDONALERTON"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy RECORDINGOFF",
                id = 5,
                name = "cmd_RecordOffAll",
                smscommand = "RECORDINGOFF"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy ALERTON",
                id = 6,
                name = "cmd_AlertsOnAll",
                smscommand = "ALERTSON"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy ALERTOFF",
                id = 7,
                name = "cmd_AlertsOffAll",
                smscommand = "ALERTSOFF"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy RECORD",
                id = 8,
                name = "cmd_RecordAll",
                smscommand = "RECORD"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy RECORDSTOP",
                id = 9,
                name = "cmd_RecordAllStop",
                smscommand = "RECORDSTOP"
            };
            _remotecommands.Add(cmd);

            cmd = new objectsCommand
            {
                command = "ispy SNAPSHOT",
                id = 10,
                name = "cmd_SnapshotAll",
                smscommand = "SNAPSHOT"
            };
            _remotecommands.Add(cmd);
        }

        private void RunCommand(int commandIndex)
        {
            objectsCommand oc = RemoteCommands.FirstOrDefault(p => p.id == commandIndex);

            if (oc != null)
            {
                RunCommand(oc.command);
            }
        }

        internal void RunCommand(string command)
        {
            try
            {
                if (command.ToLower().StartsWith("ispy ") || command.ToLower().StartsWith("ispy.exe "))
                {
                    string cmd2 = command.Substring(command.IndexOf(" ", StringComparison.Ordinal) + 1).ToLower().Trim();
                    if (cmd2.StartsWith("commands "))
                        cmd2 = cmd2.Substring(cmd2.IndexOf(" ", StringComparison.Ordinal) + 1).Trim();

                    string cmd = cmd2.Trim('"');
                    string[] commands = cmd.Split('|');
                    foreach (string command2 in commands)
                    {
                        if (command2 != "")
                        {
                            if (InvokeRequired)
                                Invoke(new ExternalCommandDelegate(ProcessCommandInternal), command2.Trim('"'));
                            else
                                ProcessCommandInternal(command2.Trim('"'));
                        }
                    }
                }
                else
                    Process.Start(command);
            }
            catch (Exception ex)
            {
                LogExceptionToFile(ex);
            }

        }

        public void ProcessKey(string keycommand)
        {
            bool focussed = false;

            switch (keycommand.ToLower())
            {
                case "channelup":
                case "nexttrack":
                case "next_control":
                    for (int i = 0; i < _pnlCameras.Controls.Count; i++)
                    {
                        Control c = _pnlCameras.Controls[i];
                        if (c.Focused)
                        {
                            i++;
                            if (i == _pnlCameras.Controls.Count)
                                i = 0;
                            _pnlCameras.Controls[i].Focus();
                            focussed = true;
                            break;
                        }
                    }
                    if (!focussed && _pnlCameras.Controls.Count > 0)
                    {
                        _pnlCameras.Controls[0].Focus();
                    }
                    break;
                case "channeldown":
                case "previoustrack":
                case "previous_control":
                    for (int i = 0; i < _pnlCameras.Controls.Count; i++)
                    {
                        Control c = _pnlCameras.Controls[i];
                        if (c.Focused)
                        {
                            i--;
                            if (i == -1)
                                i = _pnlCameras.Controls.Count - 1;
                            _pnlCameras.Controls[i].Focus();
                            focussed = true;
                            break;
                        }
                    }

                    if (!focussed && _pnlCameras.Controls.Count > 0)
                    {
                        _pnlCameras.Controls[0].Focus();
                    }
                    break;
                case "play":
                case "pause":
                    foreach (Control c in _pnlCameras.Controls)
                    {
                        if (c.Focused)
                        {
                            if (c is CameraWindow)
                            {
                                CameraWindow cw = (CameraWindow)c;
                                if (cw.Camobject.settings.active)
                                {
                                    Maximise(cw);
                                }
                                else
                                    cw.Enable();
                            }
                            if (c is VolumeLevel)
                            {
                                VolumeLevel vw = (VolumeLevel)c;
                                if (vw.Micobject.settings.active)
                                {
                                    Maximise(vw);
                                }
                                else
                                    vw.Enable();
                            }
                            break;
                        }
                    }
                    break;
                case "stop":
                    foreach (Control c in _pnlCameras.Controls)
                    {
                        if (c.Focused)
                        {
                            if (c is CameraWindow)
                            {
                                ((CameraWindow)c).Disable();
                            }
                            if (c is VolumeLevel)
                            {
                                ((VolumeLevel)c).Disable();
                            }
                            break;
                        }
                    }
                    break;
                case "record":
                    foreach (Control c in _pnlCameras.Controls)
                    {
                        if (c.Focused)
                        {
                            if (c is CameraWindow)
                            {
                                ((CameraWindow)c).RecordSwitch(!((CameraWindow)c).Recording);
                            }
                            if (c is VolumeLevel)
                            {
                                ((VolumeLevel)c).RecordSwitch(!((VolumeLevel)c).Recording);
                            }
                            break;
                        }
                    }
                    break;

                case "zoom":
                    foreach (Control c in _pnlCameras.Controls)
                    {

                        if (c.Focused)
                        {
                            if (c is CameraWindow || c is VolumeLevel || c is FloorPlanControl)
                            {
                                Maximise(c);
                                break;
                            }
                        }

                    }
                    break;
                case "standby":
                case "back":
                case "power":
                   Close();
                    break;

            }
        }
    }
}
