using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using iSpyApplication.Controls;

namespace iSpyApplication
{
    partial class MainForm
    {
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
                    cameraControl.RecordSwitch(record);
                }
                if (c is VolumeLevel)
                {
                    var volumeControl = (VolumeLevel)c;
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
                    string cmd2 = command.Substring(command.IndexOf(" ") + 1).ToLower().Trim();
                    if (cmd2.StartsWith("commands "))
                        cmd2 = cmd2.Substring(cmd2.IndexOf(" ") + 1).Trim();

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
