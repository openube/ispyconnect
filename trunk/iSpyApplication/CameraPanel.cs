using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using iSpyApplication.Controls;
using iSpyApplication.Properties;
using iSpyApplication.Video;

namespace iSpyApplication
{
    public partial class CameraPanel : DockContent
    {
        private static ViewController _vc;
        public static bool NeedsRedraw;
        private static List<LayoutItem> SavedLayout = new List<LayoutItem>();
        public static string NL = Environment.NewLine;

        public event EventHandler FileListUpdated;
        public event NotificationHandler ControlNotification;

        #region Nested type: AddObjectExternalDelegate

        private delegate void AddObjectExternalDelegate(int sourceIndex, string url, int width, int height, string name);

        #endregion

        #region Nested type: CameraCommandDelegate

        private delegate void CameraCommandDelegate(CameraWindow target);

        #endregion

        

        #region Nested type: MicrophoneCommandDelegate

        private delegate void MicrophoneCommandDelegate(VolumeLevel target);

        #endregion

        public CameraPanel()
        {
            InitializeComponent();
            _pnlCameras.BackColor = MainForm.Conf.MainColor.ToColor();
        }

        private void layoutPanel1_Scroll(object sender, ScrollEventArgs e)
        {
            if (_vc != null)
                _vc.Redraw();
        }

        public void LayoutObjects(int w, int h)
        {
            _pnlCameras.HorizontalScroll.Value = 0;
            _pnlCameras.VerticalScroll.Value = 0;
            _pnlCameras.Refresh();
            int num = _pnlCameras.Controls.Count;
            if (num == 0)
                return;
            // Get data.
            var rectslist = new List<Rectangle>();

            foreach (Control c in _pnlCameras.Controls)
            {
                bool skip = false;
                if (c is CameraWindow || c is VolumeLevel || c is FloorPlanControl)
                {
                    var p = (PictureBox)c;
                    if (w > 0)
                    {
                        p.Width = w;
                        p.Height = h;
                    }
                    if (w == -1)
                    {
                        if (c is CameraWindow)
                        {
                            var cw = ((CameraWindow)c);
                            if (cw.Camera != null && !cw.LastFrameNull)
                            {
                                p.Width = cw.LastFrame.Width + 2;
                                p.Height = cw.LastFrame.Height + 32;
                            }
                        }
                        else
                        {
                            p.Width = c.Width;
                            p.Height = c.Height;
                        }
                    }
                    int nh = p.Height;
                    if (c is CameraWindow)
                    {
                        if (((CameraWindow)c).VolumeControl != null)
                            nh += 40;
                    }
                    if (c is VolumeLevel)
                    {
                        if (((VolumeLevel)c).Paired)
                            skip = true;
                    }
                    if (!skip)
                    {
                        rectslist.Add(new Rectangle(0, 0, p.Width, nh));
                    }
                }
            }
            // Arrange the rectangles.
            Rectangle[] rects = rectslist.ToArray();
            int binWidth = _pnlCameras.Width;
            var proc = new C2BPProcessor();
            proc.SubAlgFillOneColumn(binWidth, rects);
            rectslist = rects.ToList();
            bool assigned = true;
            var indexesassigned = new List<int>();
            while (assigned)
            {
                assigned = false;
                foreach (Rectangle r in rectslist)
                {
                    for (int i = 0; i < _pnlCameras.Controls.Count; i++)
                    {
                        Control c = _pnlCameras.Controls[i];
                        if (c is CameraWindow || c is VolumeLevel || c is FloorPlanControl)
                        {
                            bool skip = false;
                            int hoffset = 0;
                            if (!indexesassigned.Contains(i))
                            {
                                if (c is CameraWindow)
                                {
                                    var cw = ((CameraWindow)c);
                                    if (cw.VolumeControl != null)
                                        hoffset = 40;
                                }
                                if (c is VolumeLevel)
                                {
                                    if (((VolumeLevel)c).Paired)
                                        skip = true;
                                }
                                if (!skip && c.Width == r.Width && c.Height + hoffset == r.Height)
                                {
                                    PositionPanel((PictureBox)c, new Point(r.X, r.Y), r.Width, r.Height - hoffset);
                                    rectslist.Remove(r);
                                    assigned = true;
                                    indexesassigned.Add(i);
                                    break;
                                }
                            }
                        }
                    }
                    if (assigned)
                        break;
                }
            }
            NeedsRedraw = true;
        }

        private static void PositionPanel(PictureBox p, Point xy, int w, int h)
        {
            p.Width = w;
            p.Height = h;
            p.Location = new Point(xy.X, xy.Y);
        }

        public CameraWindow GetCameraWindow(int cameraId)
        {
            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(CameraWindow)) continue;
                var cw = (CameraWindow)c;
                if (cw.Camobject.id == cameraId)
                    return cw;
            }
            return null;
        }

        public VolumeLevel GetVolumeLevel(int microphoneId)
        {
            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(VolumeLevel)) continue;
                var vw = (VolumeLevel)c;
                if (vw.Micobject.id == microphoneId)
                    return vw;
            }
            return null;
        }


        public void ResetLayout()
        {
            foreach (LayoutItem li in SavedLayout)
            {
                switch (li.ObjectTypeId)
                {
                    case 1:
                        VolumeLevel vl = GetMicrophone(li.ObjectId);
                        if (vl != null)
                        {
                            vl.Location = new Point(li.LayoutRectangle.X, li.LayoutRectangle.Y);
                            vl.Size = new Size(li.LayoutRectangle.Width, li.LayoutRectangle.Height);
                        }
                        break;
                    case 2:
                        CameraWindow cw = GetCameraWindow(li.ObjectId);
                        if (cw != null)
                        {
                            cw.Location = new Point(li.LayoutRectangle.X, li.LayoutRectangle.Y);
                            cw.Size = new Size(li.LayoutRectangle.Width, li.LayoutRectangle.Height);
                        }
                        break;
                    case 3:
                        FloorPlanControl fp = GetFloorPlan(li.ObjectId);
                        if (fp != null)
                        {
                            fp.Location = new Point(li.LayoutRectangle.X, li.LayoutRectangle.Y);
                            fp.Size = new Size(li.LayoutRectangle.Width, li.LayoutRectangle.Height);
                        }
                        break;
                }
            }
        }

        public VolumeLevel GetMicrophone(int microphoneId)
        {
            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(VolumeLevel)) continue;
                var vw = (VolumeLevel)c;
                if (vw.Micobject.id != microphoneId) continue;
                return vw;
            }
            return null;
        }

        public FloorPlanControl GetFloorPlan(int floorPlanId)
        {
            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(FloorPlanControl)) continue;
                var fp = (FloorPlanControl)c;
                if (fp.Fpobject.id != floorPlanId) continue;
                return fp;
            }
            return null;
        }

        public void SaveLayout()
        {
            //save layout
            SavedLayout.Clear();

            foreach (Control c in _pnlCameras.Controls)
            {
                var r = new Rectangle(c.Location.X, c.Location.Y, c.Width, c.Height);
                if (c is CameraWindow)
                {
                    SavedLayout.Add(new LayoutItem
                    {
                        LayoutRectangle = r,
                        ObjectId = ((CameraWindow)c).Camobject.id,
                        ObjectTypeId = 2
                    });
                }
                if (c is FloorPlanControl)
                {
                    SavedLayout.Add(new LayoutItem
                    {
                        LayoutRectangle = r,
                        ObjectId = ((FloorPlanControl)c).Fpobject.id,
                        ObjectTypeId = 3
                    });
                }
                if (c is VolumeLevel)
                {
                    SavedLayout.Add(new LayoutItem
                    {
                        LayoutRectangle = r,
                        ObjectId = ((VolumeLevel)c).Micobject.id,
                        ObjectTypeId = 1
                    });
                }

            }
        }

        public void Maximise(object obj)
        {
            Maximise(obj, true);
        }

        private delegate void MaximiseDelegate(object obj, bool minimiseIfMaximised);

        public void Maximise(object obj, bool minimiseIfMaximised)
        {
            if (obj == null)
                return;
            if (InvokeRequired)
            {
                BeginInvoke(new MaximiseDelegate(Maximise), obj, minimiseIfMaximised);
                return;
            }
            if (obj is CameraWindow)
            {

                var cameraControl = ((CameraWindow)obj);
                cameraControl.BringToFront();


                try
                {
                    //
                    // by Marco@BlueOceanLtd.asia / 1. May 2012
                    //
                    // maximise camera by keep it's aspect ratio and center to the main window
                    // cameraControl.RestoreRect is set to Empty if not maximised and can be checked if camera is maximised or normal view
                    // 
                    if (cameraControl.RestoreRect.IsEmpty)
                    {
                        var s = "320x240";
                        if (!String.IsNullOrEmpty(cameraControl.Camobject.resolution))
                            s = cameraControl.Camobject.resolution;
                        var wh = s.Split('x');

                        cameraControl.RestoreRect = new Rectangle(cameraControl.Location.X, cameraControl.Location.Y,
                                                                  cameraControl.Width, cameraControl.Height);

                        double wFact = Convert.ToDouble(_pnlCameras.Width) / Convert.ToDouble(wh[0]);
                        double hFact = Convert.ToDouble(_pnlCameras.Height) / Convert.ToDouble(wh[1]);
                        if (cameraControl.VolumeControl != null)
                            hFact = Convert.ToDouble((_pnlCameras.Height - 40)) / Convert.ToDouble(wh[1]);
                        if (hFact <= wFact)
                        {
                            cameraControl.Width = Convert.ToInt32(((Convert.ToDouble(_pnlCameras.Width) * hFact) / wFact));
                            cameraControl.Height = _pnlCameras.Height;
                        }
                        else
                        {
                            cameraControl.Width = _pnlCameras.Width;
                            cameraControl.Height = Convert.ToInt32((Convert.ToDouble(_pnlCameras.Height) * wFact) / hFact);
                        }
                        cameraControl.Location = new Point(((_pnlCameras.Width - cameraControl.Width) / 2),
                                                           ((_pnlCameras.Height - cameraControl.Height) / 2));
                        if (cameraControl.VolumeControl != null)
                            cameraControl.Height -= 40;
                    }
                    else
                    {
                        if (minimiseIfMaximised)
                            Minimize(obj, false);
                        cameraControl.RestoreRect = Rectangle.Empty;
                    }
                    //
                    // end
                    //
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
            }

            if (obj is VolumeLevel)
            {
                var vf = ((VolumeLevel)obj);
                vf.BringToFront();
                if (vf.Paired)
                {
                    CameraWindow cw = GetCameraWindow(MainForm.Cameras.Single(p => p.settings.micpair == vf.Micobject.id).id);
                    if (vf.Width == _pnlCameras.Width)
                    {
                        if (minimiseIfMaximised)
                            Minimize(cw, false);
                    }
                    else
                        Maximise(cw);
                }
            }

            if (obj is FloorPlanControl)
            {
                var fp = ((FloorPlanControl)obj);
                fp.BringToFront();

                if (fp.RestoreRect.IsEmpty)
                {
                    fp.RestoreRect = new Rectangle(fp.Location.X, fp.Location.Y,
                                                              fp.Width, fp.Height);
                    var wFact = Convert.ToDouble(_pnlCameras.Width) / fp.Width;
                    var hFact = Convert.ToDouble(_pnlCameras.Height) / fp.Height;

                    if (hFact <= wFact)
                    {
                        fp.Width = (int)(_pnlCameras.Width / wFact * hFact);
                        fp.Height = _pnlCameras.Height;
                    }
                    else
                    {
                        fp.Width = _pnlCameras.Width;
                        fp.Height = (int)(_pnlCameras.Height / hFact * wFact);
                    }
                    fp.Location = new Point(((_pnlCameras.Width - fp.Width) / 2), ((_pnlCameras.Height - fp.Height) / 2));
                }
                else
                {
                    if (minimiseIfMaximised)
                        Minimize(obj, false);
                    fp.RestoreRect = Rectangle.Empty;
                }
            }
        }

        internal void Minimize(object obj, bool tocontents)
        {
            if (obj == null)
                return;
            if (obj is CameraWindow)
            {
                var cw = (CameraWindow)obj;
                var r = cw.RestoreRect;
                if (r != Rectangle.Empty && !tocontents)
                {
                    cw.Location = r.Location;
                    cw.Height = r.Height;
                    cw.Width = r.Width;
                }
                else
                {
                    if (cw.Camera != null)
                    {
                        Bitmap bmp = cw.LastFrame;
                        if (bmp != null)
                        {
                            cw.Width = bmp.Width + 2;
                            cw.Height = bmp.Height + 26;
                            bmp.Dispose();

                        }
                    }
                    else
                    {
                        cw.Width = 322;
                        cw.Height = 266;
                    }
                }
                cw.Invalidate();
            }

            if (obj is VolumeLevel)
            {
                var cw = (VolumeLevel)obj;
                var r = cw.RestoreRect;
                if (r != Rectangle.Empty && !tocontents)
                {
                    cw.Location = r.Location;
                    cw.Height = r.Height;
                    cw.Width = r.Width;
                }
                else
                {
                    cw.Width = 160;
                    cw.Height = 40;
                }
                cw.Invalidate();
            }

            if (obj is FloorPlanControl)
            {
                var fp = (FloorPlanControl)obj;
                var r = fp.RestoreRect;
                if (r != Rectangle.Empty && !tocontents)
                {
                    fp.Location = r.Location;
                    fp.Height = r.Height;
                    fp.Width = r.Width;
                    fp.Invalidate();
                }
                else
                {
                    if (fp.ImgPlan != null)
                    {
                        fp.Width = fp.ImgPlan.Width + 2;
                        fp.Height = fp.ImgPlan.Height + 26;
                    }
                    else
                    {
                        fp.Width = 322;
                        fp.Height = 266;
                    }
                }
            }
        }

        private void VolumeControlDoubleClick(object sender, EventArgs e)
        {
            Maximise(sender);
        }

        private void FloorPlanDoubleClick(object sender, EventArgs e)
        {
            Maximise(sender);
        }

        

        public void LayoutOptimised()
        {
            double numberCameras = MainForm.Cameras.Count;
            int useX = 320, useY = 200;
            int dispArea = _pnlCameras.Width * _pnlCameras.Height;
            int lastArea = dispArea;


            for (int y = 1; y <= numberCameras; y++)
            {
                int camX = y;
                var camY = (int)Math.Round((numberCameras / y) + 0.499999999, 0);

                int dispWidth = _pnlCameras.Width / camX;
                int dispHeight = dispWidth / 4 * 3;
                int camArea = (int)numberCameras * (dispWidth * (dispHeight + 40));
                if (((dispArea - camArea) <= lastArea) && ((dispArea - camArea) > 0) && (((camY * (dispHeight + 40)) < _pnlCameras.Height)))
                {
                    useX = dispWidth;
                    useY = dispHeight;
                    lastArea = dispArea - camArea;
                }

                dispHeight = (_pnlCameras.Height - (camY * 40)) / camY;
                dispWidth = dispHeight * 4 / 3;
                camArea = (int)numberCameras * (dispWidth * (dispHeight + 40));
                if (((dispArea - camArea) <= lastArea) && ((dispArea - camArea) > 0) && (((camX * dispWidth) < _pnlCameras.Width)))
                {
                    useX = dispWidth;
                    useY = dispHeight;
                    lastArea = dispArea - camArea;
                }
            }
            LayoutObjects(useX, useY);
        }

        internal void ClearHighlights()
        {
            foreach (Control c in _pnlCameras.Controls)
            {
                var cameraWindow = c as CameraWindow;
                if (cameraWindow != null)
                {
                    cameraWindow.Highlighted = false;
                }
                else
                {
                    var floorPlanControl = c as FloorPlanControl;
                    if (floorPlanControl != null)
                    {
                        floorPlanControl.Highlighted = false;
                    }
                    else
                    {
                        var volumeLevel = c as VolumeLevel;
                        if (volumeLevel != null)
                        {
                            volumeLevel.Highlighted = false;
                        }
                    }
                }
                

            }
        }


        

        

        internal void RenderObjects()
        {

            for (int index = 0; index < MainForm.Cameras.Count; index++)
            {
                objectsCamera oc = MainForm.Cameras[index];
                DisplayCamera(oc);
            }

            for (int index = 0; index < MainForm.Microphones.Count; index++)
            {
                objectsMicrophone om = MainForm.Microphones[index];
                DisplayMicrophone(om);
            }

            for (int index = 0; index < MainForm.FloorPlans.Count; index++)
            {
                objectsFloorplan ofp = MainForm.FloorPlans[index];
                DisplayFloorPlan(ofp);
            }

            for (int index = 0; index < MainForm.Cameras.Count; index++)
            {
                objectsCamera oc = MainForm.Cameras[index];
                var cw = GetCameraWindow(oc.id);
                if (MainForm.Conf.AutoSchedule && oc.schedule.active && oc.schedule.entries.Any())
                {
                    oc.settings.active = false;
                    cw.ApplySchedule();
                }
                else
                {
                    try
                    {
                        if (oc.settings.active)
                            cw.Enable();
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                    }
                }
            }

            for (int index = 0; index < MainForm.Microphones.Count; index++)
            {
                objectsMicrophone om = MainForm.Microphones[index];
                var vl = GetVolumeLevel(om.id);
                if (MainForm.Conf.AutoSchedule && om.schedule.active && om.schedule.entries.Any())
                {
                    om.settings.active = false;
                    vl.ApplySchedule();
                }
                else
                {
                    if (om.settings.active)
                        vl.Enable();
                }
            }


            bool cam = false;
            if (_pnlCameras.Controls.Count > 0)
            {
                //prevents layering issues
                for (int index = 0; index < _pnlCameras.Controls.Count; index++)
                {
                    var c = _pnlCameras.Controls[index];
                    var cw = c as CameraWindow;
                    if (cw != null && cw.VolumeControl == null)
                    {
                        cam = true;
                        cw.BringToFront();
                    }
                }
                _pnlCameras.Controls[0].Focus();
            }
            //if (!cam)
                //flowPreview.Loading = false;

            MainForm.NeedsSync = true;
        }

        

        private void SetMicrophoneEvents(VolumeLevel vw)
        {
            vw.DoubleClick += VolumeControlDoubleClick;
            vw.MouseDown += VolumeControlMouseDown;
            vw.MouseUp += VolumeControlMouseUp;
            vw.MouseMove += VolumeControlMouseMove;
            vw.RemoteCommand += VolumeControlRemoteCommand;
            vw.Notification += ControlNotificationHandler;
            vw.FileListUpdated += FileListUpdatedHandler;
        }

        void ControlNotificationHandler(object sender, NotificationType type)
        {
            if (ControlNotification != null)
            {
                ControlNotification(sender, type);
            }
        }
        
        void FileListUpdatedHandler(object sender)
        {
            if (FileListUpdated!=null)
            {
                FileListUpdated(sender, EventArgs.Empty);
            }
        }

        private void SetFloorPlanEvents(FloorPlanControl fpc)
        {
            fpc.DoubleClick += FloorPlanDoubleClick;
            fpc.MouseDown += FloorPlanMouseDown;
            fpc.MouseUp += FloorPlanMouseUp;
            fpc.MouseMove += FloorPlanMouseMove;
        }

        private void DisplayMicrophone(objectsMicrophone mic)
        {
            var micControl = new VolumeLevel(mic);
            SetMicrophoneEvents(micControl);
            micControl.BackColor = MainForm.Conf.BackColor.ToColor();
            _pnlCameras.Controls.Add(micControl);
            micControl.Location = new Point(mic.x, mic.y);
            micControl.Size = new Size(mic.width, mic.height);
            micControl.BringToFront();
            micControl.Tag = GetControlIndex();

            try
            {
                string path = MainForm.Conf.MediaDirectory + "audio\\" + mic.directory + "\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            micControl.GetFiles();
        }

        internal void DisplayFloorPlan(objectsFloorplan ofp)
        {
            var fpControl = new FloorPlanControl(ofp, this);
            SetFloorPlanEvents(fpControl);
            fpControl.BackColor = MainForm.Conf.BackColor.ToColor();
            _pnlCameras.Controls.Add(fpControl);
            fpControl.Location = new Point(ofp.x, ofp.y);
            fpControl.Size = new Size(ofp.width, ofp.height);
            fpControl.BringToFront();
            fpControl.Tag = GetControlIndex();
        }

        internal void EditCamera(objectsCamera cr)
        {
            int cameraId = Convert.ToInt32(cr.id);
            CameraWindow cw = null;

            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(CameraWindow)) continue;
                var cameraControl = (CameraWindow)c;
                if (cameraControl.Camobject.id == cameraId)
                {
                    cw = cameraControl;
                    break;
                }
            }

            if (cw == null) return;
            TopMost = false;
            var ac = new AddCamera { CameraControl = cw };
            ac.ShowDialog(this);
            ac.Dispose();
            TopMost = MainForm.Conf.AlwaysOnTop;
        }

        internal void EditMicrophone(objectsMicrophone om)
        {
            VolumeLevel vlf = null;

            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(VolumeLevel)) continue;
                var vl = (VolumeLevel)c;
                if (vl.Micobject.id == om.id)
                {
                    vlf = vl;
                    break;
                }
            }

            if (vlf != null)
            {
                TopMost = false;
                var am = new AddMicrophone { VolumeLevel = vlf };
                am.ShowDialog(this);
                am.Dispose();
                TopMost = MainForm.Conf.AlwaysOnTop;
            }
        }

        internal void EditFloorplan(objectsFloorplan ofp)
        {
            FloorPlanControl fpc = null;

            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(FloorPlanControl)) continue;
                var fp = (FloorPlanControl)c;
                if (fp.Fpobject.id != ofp.id) continue;
                fpc = fp;
                break;
            }

            if (fpc != null)
            {
                var afp = new AddFloorPlan { Fpc = fpc, Owner = this };
                afp.ShowDialog(this);
                afp.Dispose();
                fpc.Invalidate();
            }
        }

        public CameraWindow GetCamera(int cameraId)
        {
            for (int index = 0; index < _pnlCameras.Controls.Count; index++)
            {
                Control c = _pnlCameras.Controls[index];
                if (c.GetType() != typeof(CameraWindow)) continue;
                var cw = (CameraWindow)c;
                if (cw.Camobject.id != cameraId) continue;
                return cw;
            }
            return null;
        }



        public void RemoveCamera(CameraWindow cameraControl, bool confirm)
        {
            if (confirm &&
                MessageBox.Show(LocRm.GetString("AreYouSure"), LocRm.GetString("Confirm"), MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            cameraControl.ShuttingDown = true;
            cameraControl.MouseDown -= CameraControlMouseDown;
            cameraControl.MouseUp -= CameraControlMouseUp;
            cameraControl.MouseMove -= CameraControlMouseMove;
            cameraControl.DoubleClick -= CameraControlDoubleClick;
            cameraControl.RemoteCommand -= CameraControlRemoteCommand;
            cameraControl.Notification -= ControlNotificationHandler;
            if (cameraControl.Recording)
                cameraControl.RecordSwitch(false);

            cameraControl.Disable();
            cameraControl.SaveFileList();

            if (cameraControl.VolumeControl != null)
                RemoveMicrophone(cameraControl.VolumeControl, false);

            if (InvokeRequired)
                Invoke(new CameraCommandDelegate(RemoveCameraPanel), cameraControl);
            else
                RemoveCameraPanel(cameraControl);
        }

        public event ObjectEventHandler ObjectRemoved;


        private void RemoveCameraPanel(CameraWindow cameraControl)
        {
            _pnlCameras.Controls.Remove(cameraControl);
            if (!MainForm._closing)
            {
                CameraWindow control = cameraControl;
                var oc = MainForm.Cameras.FirstOrDefault(p => p.id == control.Camobject.id);
                if (oc != null)
                {
                    if (ObjectRemoved != null)
                    {
                        ObjectRemoved(this, new ObjectEventArgs(2, oc.id));
                    }
                    
                    MainForm.Cameras.Remove(oc);
                }

                foreach (var ofp in MainForm.FloorPlans)
                    ofp.needsupdate = true;

                MainForm.NeedsSync = true;
                SetNewStartPosition();
            }
            Application.DoEvents();
            cameraControl.Dispose();
            if (!MainForm._closing)
            {
                //LoadPreviews();
            }
        }

        public void RemoveMicrophone(VolumeLevel volumeControl, bool confirm)
        {
            if (confirm &&
                MessageBox.Show(LocRm.GetString("AreYouSure"), LocRm.GetString("Confirm"), MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;
            volumeControl.ShuttingDown = true;
            volumeControl.MouseDown -= VolumeControlMouseDown;
            volumeControl.MouseUp -= VolumeControlMouseUp;
            volumeControl.MouseMove -= VolumeControlMouseMove;
            volumeControl.DoubleClick -= VolumeControlDoubleClick;
            volumeControl.RemoteCommand -= VolumeControlRemoteCommand;
            volumeControl.Notification -= ControlNotificationHandler;
            if (volumeControl.Recording)
                volumeControl.RecordSwitch(false);

            volumeControl.Disable();
            volumeControl.SaveFileList();

            if (InvokeRequired)
                Invoke(new MicrophoneCommandDelegate(RemoveMicrophonePanel), volumeControl);
            else
                RemoveMicrophonePanel(volumeControl);
        }

        private void RemoveMicrophonePanel(VolumeLevel volumeControl)
        {
            _pnlCameras.Controls.Remove(volumeControl);

            if (!MainForm._closing)
            {
                var control = volumeControl;
                var om = MainForm.Microphones.SingleOrDefault(p => p.id == control.Micobject.id);
                if (om != null)
                {
                    if (ObjectRemoved!=null)
                    {
                        ObjectRemoved(this,new ObjectEventArgs(1,om.id));
                    }
                    for (var index = 0; index < MainForm.Cameras.Count(p => p.settings.micpair == om.id); index++)
                    {
                        var oc = MainForm.Cameras.Where(p => p.settings.micpair == om.id).ToList()[index];
                        oc.settings.micpair = -1;
                    }
                    MainForm.Microphones.Remove(om);

                    foreach (var ofp in MainForm.FloorPlans)
                        ofp.needsupdate = true;
                }
                SetNewStartPosition();
                MainForm.NeedsSync = true;
            }
            Application.DoEvents();
            volumeControl.Dispose();
        }

        internal void RemoveFloorplan(FloorPlanControl fpc, bool confirm)
        {
            if (confirm &&
                MessageBox.Show(LocRm.GetString("AreYouSure"), LocRm.GetString("Confirm"), MessageBoxButtons.OKCancel,
                                MessageBoxIcon.Warning) == DialogResult.Cancel)
                return;

            if (fpc.Fpobject != null && fpc.Fpobject.objects != null && fpc.Fpobject.objects.@object != null)
            {
                foreach (var o in fpc.Fpobject.objects.@object)
                {
                    switch (o.type)
                    {
                        case "camera":
                            CameraWindow cw = GetCameraWindow(o.id);
                            if (cw != null)
                            {
                                //cw.Location = new Point(Location.X + e.X, Location.Y + e.Y);
                                cw.Highlighted = false;
                                cw.Invalidate();
                            }
                            break;
                        case "microphone":
                            VolumeLevel vl = GetMicrophone(o.id);
                            if (vl != null)
                            {
                                vl.Highlighted = false;
                                vl.Invalidate();
                            }
                            break;
                    }
                }
            }
            _pnlCameras.Controls.Remove(fpc);


            if (!MainForm._closing)
            {
                objectsFloorplan ofp = MainForm.FloorPlans.SingleOrDefault(p => p.id == fpc.Fpobject.id);
                if (ofp != null)
                    MainForm.FloorPlans.Remove(ofp);
                SetNewStartPosition();
                MainForm.NeedsSync = true;
            }
            fpc.Dispose();
        }


        internal void AddCamera(int videoSourceIndex, bool startWizard = false)
        {
            CameraWindow cw = NewCameraWindow(videoSourceIndex);
            TopMost = false;
            var ac = new AddCamera { CameraControl = cw, StartWizard = startWizard, IsNew = true };
            ac.ShowDialog(this);
            if (ac.DialogResult == DialogResult.OK)
            {
                MainForm.Conf.LockLayout = false;
                string path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\thumbs\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\grabs\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                SetNewStartPosition();
                if (cw.VolumeControl != null && !cw.VolumeControl.IsEnabled)
                    cw.VolumeControl.Enable();
                MainForm.NeedsSync = true;
            }
            else
            {
                int cid = cw.Camobject.id;
                cw.Disable();
                _pnlCameras.Controls.Remove(cw);
                cw.Dispose();
                MainForm.Cameras.RemoveAll(p => p.id == cid);
            }
            ac.Dispose();
            TopMost = MainForm.Conf.AlwaysOnTop;
        }

        private CameraWindow NewCameraWindow(int videoSourceIndex)
        {
            var oc = new objectsCamera
            {
                alerts = new objectsCameraAlerts(),
                detector = new objectsCameraDetector
                {
                    motionzones =
                        new objectsCameraDetectorZone
                        [0]
                },
                notifications = new objectsCameraNotifications(),
                recorder = new objectsCameraRecorder(),
                schedule = new objectsCameraSchedule { entries = new objectsCameraScheduleEntry[0] },
                settings = new objectsCameraSettings(),
                ftp = new objectsCameraFtp(),
                id = -1,
                directory = MainForm.RandomString(5),
                ptz = -1,
                x = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                y = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                name = LocRm.GetString("Camera") + " " + MainForm.NextCameraId,
                ptzschedule = new objectsCameraPtzschedule
                {
                    active = false,
                    entries = new objectsCameraPtzscheduleEntry[] { }
                }
            };
            oc.flipx = oc.flipy = false;
            oc.width = 320;
            oc.height = 240;
            oc.description = "";
            oc.resolution = "320x240";
            oc.newrecordingcount = 0;

            oc.alerts.active = true;
            oc.alerts.mode = "movement";
            oc.alerts.alertoptions = "false,false";
            oc.alerts.objectcountalert = 1;
            oc.alerts.minimuminterval = 180;
            oc.alerts.processmode = "continuous";
            oc.alerts.pluginconfig = "";
            oc.alerts.trigger = "";

            oc.notifications.sendemail = false;
            oc.notifications.sendsms = false;
            oc.notifications.sendmms = false;
            oc.notifications.emailgrabinterval = 0;

            oc.ftp.enabled = false;
            oc.ftp.port = 21;
            oc.ftp.mode = 0;
            oc.ftp.server = "ftp://";
            oc.ftp.interval = 10;
            oc.ftp.filename = "mylivecamerafeed.jpg";
            oc.ftp.localfilename = "{0:yyyy-MM-dd_HH-mm-ss_fff}.jpg";
            oc.ftp.ready = true;
            oc.ftp.text = "www.ispyconnect.com";
            oc.ftp.quality = 75;

            oc.schedule.active = false;

            oc.settings.active = false;
            oc.settings.deleteavi = true;
            oc.settings.ffmpeg = MainForm.Conf.FFMPEG_Camera;
            oc.settings.emailaddress = MainForm.EmailAddress;
            oc.settings.smsnumber = MainForm.MobileNumber;
            oc.settings.suppressnoise = true;
            oc.settings.login = "";
            oc.settings.password = "";
            oc.settings.useragent = "Mozilla/5.0";
            oc.settings.frameinterval = 10;
            oc.settings.sourceindex = videoSourceIndex;
            oc.settings.micpair = -1;
            oc.settings.frameinterval = 200;
            oc.settings.maxframerate = 10;
            oc.settings.maxframeraterecord = 10;
            oc.settings.ptzautotrack = false;
            oc.settings.framerate = 10;
            oc.settings.timestamplocation = 1;
            oc.settings.ptztimetohome = 100;
            oc.settings.ptzchannel = "0";
            oc.settings.timestampformatter = "FPS: {FPS} {0:G} ";
            oc.settings.timestampfontsize = 10;
            oc.settings.notifyondisconnect = false;
            oc.settings.ptzautohomedelay = 30;
            oc.settings.accessgroups = "";
            oc.settings.nobuffer = true;
            oc.settings.reconnectinterval = 0;
            oc.settings.timestampforecolor = "255,255,255";
            oc.settings.timestampbackcolor = "70,70,70";
            oc.settings.timestampfont = FontXmlConverter.ConvertToString(MainForm.Drawfont);
            oc.settings.timestampshowback = true;

            oc.settings.youtube = new objectsCameraSettingsYoutube
            {
                autoupload = false,
                category = MainForm.Conf.YouTubeDefaultCategory,
                tags = "iSpy, Motion Detection, Surveillance",
                @public = false
            };

            oc.settings.storagemanagement = new objectsCameraSettingsStoragemanagement
            {
                enabled = false,
                maxage = 72,
                maxsize = 1024

            };

            oc.alertevents = new objectsCameraAlertevents { entries = new objectsCameraAlerteventsEntry[] { } };

            oc.settings.desktopresizeheight = 480;
            oc.settings.desktopresizewidth = 640;
            oc.settings.resize = false;

            if (VlcHelper.VlcInstalled)
                oc.settings.vlcargs = "-I" + NL + "dummy" + NL + "--ignore-config";
            else
                oc.settings.vlcargs = "";

            oc.detector.recordondetect = true;
            oc.detector.keepobjectedges = false;
            oc.detector.processeveryframe = 1;
            oc.detector.nomovementintervalnew = oc.detector.nomovementinterval = 30;
            oc.detector.movementintervalnew = oc.detector.movementinterval = 1;

            oc.detector.calibrationdelay = 15;
            oc.detector.color = ColorTranslator.ToHtml(MainForm.Conf.TrackingColor.ToColor());
            oc.detector.type = "Two Frames";
            oc.detector.postprocessor = "None";
            oc.detector.minsensitivity = 20;
            oc.detector.maxsensitivity = 100;
            oc.detector.minwidth = 20;
            oc.detector.minheight = 20;
            oc.detector.highlight = true;

            oc.recorder.bufferseconds = 2;
            oc.recorder.inactiverecord = 8;
            oc.recorder.timelapse = 0;
            oc.recorder.timelapseframes = 0;
            oc.recorder.maxrecordtime = 900;
            oc.recorder.timelapsesave = 60;
            oc.recorder.quality = 8;
            oc.recorder.timelapseframerate = 5;
            oc.recorder.crf = true;

            oc.settings.audioport = 80;
            oc.settings.audiomodel = "None";
            oc.settings.audioip = "";

            var cameraControl = new CameraWindow(oc) { BackColor = MainForm.Conf.BackColor.ToColor() };
            _pnlCameras.Controls.Add(cameraControl);

            cameraControl.Location = new Point(oc.x, oc.y);
            cameraControl.Size = new Size(320, 240);
            cameraControl.AutoSize = true;
            cameraControl.BringToFront();
            SetCameraEvents(cameraControl);
            if (MainForm.Conf.AutoLayout)
                LayoutObjects(0, 0);

            cameraControl.Tag = GetControlIndex();

            return cameraControl;
        }

        public int AddMicrophone(int audioSourceIndex)
        {
            VolumeLevel vl = NewVolumeLevel(audioSourceIndex);
            TopMost = false;
            var am = new AddMicrophone { VolumeLevel = vl };
            am.ShowDialog(this);

            int micid = -1;

            if (am.DialogResult == DialogResult.OK)
            {
                MainForm.Conf.LockLayout = false;
                micid = am.VolumeLevel.Micobject.id = MainForm.NextMicrophoneId;
                MainForm.Microphones.Add(vl.Micobject);
                string path = MainForm.Conf.MediaDirectory + "audio\\" + vl.Micobject.directory + "\\";
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                SetNewStartPosition();
                MainForm.NeedsSync = true;
            }
            else
            {
                vl.Disable();
                _pnlCameras.Controls.Remove(vl);
                vl.Dispose();
            }
            am.Dispose();
            TopMost = MainForm.Conf.AlwaysOnTop;
            return micid;

        }

        private VolumeLevel NewVolumeLevel(int audioSourceIndex)
        {
            var om = new objectsMicrophone
            {
                alerts = new objectsMicrophoneAlerts(),
                detector = new objectsMicrophoneDetector(),
                notifications = new objectsMicrophoneNotifications(),
                recorder = new objectsMicrophoneRecorder(),
                schedule = new objectsMicrophoneSchedule
                {
                    entries
                        =
                        new objectsMicrophoneScheduleEntry
                        [
                        0
                        ]
                },
                settings = new objectsMicrophoneSettings(),
                id = -1,
                directory = MainForm.RandomString(5),
                x = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                y = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                width = 160,
                height = 40,
                description = "",
                newrecordingcount = 0,
                name = LocRm.GetString("Microphone") + " " + MainForm.NextMicrophoneId
            };

            om.settings.typeindex = audioSourceIndex;
            om.settings.deletewav = true;
            om.settings.ffmpeg = MainForm.Conf.FFMPEG_Microphone;
            om.settings.buffer = 4;
            om.settings.samples = 8000;
            om.settings.bits = 16;
            om.settings.gain = 100;
            om.settings.channels = 1;
            om.settings.decompress = true;
            om.settings.smsnumber = MainForm.MobileNumber;
            om.settings.emailaddress = MainForm.EmailAddress;
            om.settings.active = false;
            om.settings.notifyondisconnect = false;
            if (VlcHelper.VlcInstalled)
                om.settings.vlcargs = "-I" + NL + "dummy" + NL + "--ignore-config";
            else
                om.settings.vlcargs = "";

            om.settings.storagemanagement = new objectsMicrophoneSettingsStoragemanagement
            {
                enabled = false,
                maxage = 72,
                maxsize = 1024

            };

            om.detector.sensitivity = 60;
            om.detector.nosoundinterval = 30;
            om.detector.soundinterval = 0;
            om.detector.recordondetect = true;

            om.alerts.mode = "sound";
            om.alerts.minimuminterval = 180;
            om.alerts.executefile = "";
            om.alerts.active = true;
            om.alerts.alertoptions = "false,false";
            om.alerts.trigger = "";

            om.recorder.inactiverecord = 5;
            om.recorder.maxrecordtime = 900;

            om.notifications.sendemail = false;
            om.notifications.sendsms = false;

            om.schedule.active = false;
            om.alertevents = new objectsMicrophoneAlertevents { entries = new objectsMicrophoneAlerteventsEntry[] { } };

            var volumeControl = new VolumeLevel(om) { BackColor = MainForm.Conf.BackColor.ToColor() };
            _pnlCameras.Controls.Add(volumeControl);

            volumeControl.Location = new Point(om.x, om.y);
            volumeControl.Size = new Size(160, 40);
            volumeControl.BringToFront();
            SetMicrophoneEvents(volumeControl);

            if (MainForm.Conf.AutoLayout)
                LayoutObjects(0, 0);

            volumeControl.Tag = GetControlIndex();

            return volumeControl;
        }

        private int GetControlIndex()
        {
            int i = 0;
            while (true)
            {

                bool b = false;
                foreach (Control c in _pnlCameras.Controls)
                {
                    if (c.Tag is int)
                    {
                        if (((int)c.Tag) == i)
                        {
                            b = true;
                            break;
                        }
                    }
                }
                if (!b)
                {
                    return i;
                }
                i++;
            }

        }

        internal void AddFloorPlan()
        {
            var ofp = new objectsFloorplan
            {
                objects = new objectsFloorplanObjects { @object = new objectsFloorplanObjectsEntry[0] },
                id = -1,
                image = "",
                height = 480,
                width = 640,
                x = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                y = Convert.ToInt32(MainForm.Random.NextDouble() * 100),
                name = LocRm.GetString("FloorPlan") + " " + MainForm.NextFloorPlanId
            };

            var fpc = new FloorPlanControl(ofp, this) { BackColor = MainForm.Conf.BackColor.ToColor() };
            _pnlCameras.Controls.Add(fpc);

            fpc.Location = new Point(ofp.x, ofp.y);
            fpc.Size = new Size(320, 240);
            fpc.BringToFront();
            fpc.Tag = GetControlIndex();

            var afp = new AddFloorPlan { Fpc = fpc, Owner = this };
            afp.ShowDialog(this);
            if (afp.DialogResult == DialogResult.OK)
            {
                MainForm.Conf.LockLayout = false;
                afp.Fpc.Fpobject.id = MainForm.NextFloorPlanId;
                MainForm.FloorPlans.Add(ofp);
                SetFloorPlanEvents(fpc);
                SetNewStartPosition();
                fpc.Invalidate();
            }
            else
            {
                _pnlCameras.Controls.Remove(fpc);
                fpc.Dispose();
            }
            afp.Dispose();
        }

        private void SetCameraEvents(CameraWindow cameraControl)
        {
            cameraControl.MouseDown += CameraControlMouseDown;
            cameraControl.MouseWheel += CameraControlMouseWheel;
            cameraControl.MouseUp += CameraControlMouseUp;
            cameraControl.MouseMove += CameraControlMouseMove;
            cameraControl.DoubleClick += CameraControlDoubleClick;
            cameraControl.RemoteCommand += CameraControlRemoteCommand;
            cameraControl.Notification += ControlNotificationHandler;
            cameraControl.FileListUpdated += FileListUpdatedHandler;
        }

        private void AddCameraExternal(int sourceIndex, string url, int width, int height, string name)
        {
            CameraWindow cw = NewCameraWindow(sourceIndex);
            cw.Camobject.settings.desktopresizewidth = width;
            cw.Camobject.settings.desktopresizeheight = height;
            cw.Camobject.settings.resize = false;
            cw.Camobject.name = name;

            cw.Camobject.settings.videosourcestring = url;

            cw.Camobject.id = MainForm.NextCameraId;
            MainForm.Cameras.Add(cw.Camobject);
            string path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\thumbs\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            path = MainForm.Conf.MediaDirectory + "video\\" + cw.Camobject.directory + "\\grabs\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            cw.Camobject.settings.accessgroups = "";


            SetNewStartPosition();
            cw.Enable();
            cw.NeedSizeUpdate = true;
        }

        private void AddMicrophoneExternal(int sourceIndex, string url, int width, int height, string name)
        {
            VolumeLevel vl = NewVolumeLevel(sourceIndex);
            vl.Micobject.name = name;
            vl.Micobject.settings.sourcename = url;

            vl.Micobject.id = MainForm.NextMicrophoneId;
            MainForm.Microphones.Add(vl.Micobject);
            string path = MainForm.Conf.MediaDirectory + "audio\\" + vl.Micobject.directory + "\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            vl.Micobject.settings.accessgroups = "";
            SetNewStartPosition();
            vl.Enable();
        }

        internal VolumeLevel AddCameraMicrophone(int cameraid, string name)
        {
            if (cameraid == -1)
                cameraid = MainForm.NextCameraId;
            VolumeLevel vl = NewVolumeLevel(4);
            vl.Micobject.name = name;
            vl.Micobject.settings.sourcename = cameraid.ToString(CultureInfo.InvariantCulture);
            vl.Micobject.id = MainForm.NextMicrophoneId;
            MainForm.Microphones.Add(vl.Micobject);
            string path = MainForm.Conf.MediaDirectory + "audio\\" + vl.Micobject.directory + "\\";
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            vl.Micobject.settings.accessgroups = "";
            SetNewStartPosition();
            //vl.Enable();
            return vl;
        }

        #region CameraEvents

        private void CameraControlMouseMove(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow)sender;
            if (e.Button == MouseButtons.Left && !MainForm.Conf.LockLayout)
            {
                int newLeft = cameraControl.Left + (e.X - cameraControl.Camobject.x);
                int newTop = cameraControl.Top + (e.Y - cameraControl.Camobject.y);
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;
                if (newLeft + cameraControl.Width > 5 && newLeft < ClientRectangle.Width - 5)
                {
                    cameraControl.Left = newLeft;
                }
                if (newTop + cameraControl.Height > 5 && newTop < ClientRectangle.Height - 50)
                {
                    cameraControl.Top = newTop;
                }
            }

        }

        private void CameraControlMouseDown(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow)sender;
            cameraControl.Focus();

            switch (e.Button)
            {
                case MouseButtons.Left:
                    cameraControl.Camobject.x = e.X;
                    cameraControl.Camobject.y = e.Y;
                    cameraControl.BringToFront();
                    if (cameraControl.VolumeControl != null)
                        cameraControl.VolumeControl.BringToFront();
                    break;
                case MouseButtons.Right:
                    //ContextTarget = cameraControl;
                    //pluginCommandsToolStripMenuItem.Visible = false;
                    //_setInactiveToolStripMenuItem.Visible = false;
                    //_activateToolStripMenuItem.Visible = false;
                    //_recordNowToolStripMenuItem.Visible = false;
                    //_listenToolStripMenuItem.Visible = false;
                    //_applyScheduleToolStripMenuItem1.Visible = true;
                    //_resetRecordingCounterToolStripMenuItem.Visible = true;
                    //_resetRecordingCounterToolStripMenuItem.Text = LocRm.GetString("ResetRecordingCounter") + " (" +
                    //                                               cameraControl.Camobject.newrecordingcount + ")";
                    //pTZToolStripMenuItem.Visible = false;
                    //if (cameraControl.Camobject.settings.active)
                    //{
                    //    _setInactiveToolStripMenuItem.Visible = true;
                    //    _recordNowToolStripMenuItem.Visible = true;
                    //    _takePhotoToolStripMenuItem.Visible = true;
                    //    if (cameraControl.Camobject.ptz > -1)
                    //    {
                    //        pTZToolStripMenuItem.Visible = true;
                    //        while (pTZToolStripMenuItem.DropDownItems.Count > 1)
                    //            pTZToolStripMenuItem.DropDownItems.RemoveAt(1);

                    //        PTZSettings2Camera ptz = PTZs.SingleOrDefault(p => p.id == cameraControl.Camobject.ptz);
                    //        if (ptz != null)
                    //        {
                    //            if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command != null)
                    //            {
                    //                foreach (var extcmd in ptz.ExtendedCommands.Command)
                    //                {
                    //                    ToolStripItem tsi = new ToolStripMenuItem
                    //                    {
                    //                        Text = extcmd.Name,
                    //                        Tag =
                    //                            cameraControl.Camobject.id + "|" + extcmd.Value
                    //                    };
                    //                    tsi.Click += TsiClick;
                    //                    pTZToolStripMenuItem.DropDownItems.Add(tsi);
                    //                }
                    //            }
                    //        }
                    //    }

                    //    if (cameraControl.Camera != null && cameraControl.Camera.Plugin != null)
                    //    {
                    //        pluginCommandsToolStripMenuItem.Visible = true;

                    //        while (pluginCommandsToolStripMenuItem.DropDownItems.Count > 1)
                    //            pluginCommandsToolStripMenuItem.DropDownItems.RemoveAt(1);

                    //        var pc = cameraControl.PluginCommands;
                    //        if (pc != null)
                    //        {
                    //            foreach (var c in pc)
                    //            {
                    //                ToolStripItem tsi = new ToolStripMenuItem
                    //                {
                    //                    Text = c,
                    //                    Tag =
                    //                        cameraControl.Camobject.id + "|" + c
                    //                };
                    //                tsi.Click += PCClick;
                    //                pluginCommandsToolStripMenuItem.DropDownItems.Add(tsi);
                    //            }
                    //        }
                    //    }

                    //}
                    //else
                    //{
                    //    _activateToolStripMenuItem.Visible = true;
                    //    _recordNowToolStripMenuItem.Visible = false;
                    //    _takePhotoToolStripMenuItem.Visible = false;
                    //}
                    //_recordNowToolStripMenuItem.Text =
                    //    LocRm.GetString(cameraControl.Recording ? "StopRecording" : "StartRecording");
                    //ctxtMnu.Show(cameraControl, new Point(e.X, e.Y));
                    break;
                case MouseButtons.Middle:
                    cameraControl.PTZReference = new Point(cameraControl.Width / 2, cameraControl.Height / 2);
                    cameraControl.PTZNavigate = true;
                    break;
            }
        }

        private void TsiClick(object sender, EventArgs e)
        {
            string[] cfg = ((ToolStripMenuItem)sender).Tag.ToString().Split('|');
            int camid = Convert.ToInt32(cfg[0]);
            var cw = GetCameraWindow(camid);
            if (cw != null && cw.PTZ != null)
            {
                cw.Calibrating = true;
                cw.PTZ.SendPTZCommand(cfg[1]);
            }
        }

        private void PCClick(object sender, EventArgs e)
        {
            string[] cfg = ((ToolStripMenuItem)sender).Tag.ToString().Split('|');
            int camid = Convert.ToInt32(cfg[0]);
            var cw = GetCameraWindow(camid);
            if (cw != null && cw.PluginCommands != null)
            {
                cw.ExecutePluginCommand(cfg[1]);
            }
        }

        private static void CameraControlMouseWheel(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow)sender;

            cameraControl.PTZNavigate = false;
            if (cameraControl.PTZ != null)
            {
                cameraControl.Calibrating = true;
                cameraControl.PTZ.SendPTZCommand(e.Delta > 0 ? Enums.PtzCommand.ZoomIn : Enums.PtzCommand.ZoomOut, true);
                if (cameraControl.PTZ.IsContinuous)
                    cameraControl.PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                ((HandledMouseEventArgs)e).Handled = true;

            }
        }

        private static void CameraControlMouseUp(object sender, MouseEventArgs e)
        {
            var cameraControl = (CameraWindow)sender;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    cameraControl.Camobject.x = cameraControl.Left;
                    cameraControl.Camobject.y = cameraControl.Top;
                    break;
            }
        }

        private void CameraControlDoubleClick(object sender, EventArgs e)
        {
            Maximise(sender);
        }

        #endregion

        #region VolumeEvents

        private void VolumeControlMouseDown(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel)sender;
            switch (e.Button)
            {
                case MouseButtons.Left:
                    if (!volumeControl.Paired)
                    {
                        volumeControl.Micobject.x = e.X;
                        volumeControl.Micobject.y = e.Y;
                    }
                    volumeControl.BringToFront();
                    if (volumeControl.Paired)
                    {
                        CameraWindow cw =
                            GetCameraWindow(MainForm.Cameras.Single(p => p.settings.micpair == volumeControl.Micobject.id).id);
                        cw.BringToFront();
                    }
                    break;
                case MouseButtons.Right:
                    //ContextTarget = volumeControl;
                    //pluginCommandsToolStripMenuItem.Visible = false;
                    //_setInactiveToolStripMenuItem.Visible = false;
                    //_activateToolStripMenuItem.Visible = false;
                    //_listenToolStripMenuItem.Visible = true;
                    //_takePhotoToolStripMenuItem.Visible = false;
                    //_resetRecordingCounterToolStripMenuItem.Visible = true;
                    //_applyScheduleToolStripMenuItem1.Visible = true;
                    //pTZToolStripMenuItem.Visible = false;
                    //_resetRecordingCounterToolStripMenuItem.Text = LocRm.GetString("ResetRecordingCounter") + " (" +
                    //                                               volumeControl.Micobject.newrecordingcount + ")";
                    //if (volumeControl.Listening)
                    //{
                    //    _listenToolStripMenuItem.Text = LocRm.GetString("StopListening");
                    //    _listenToolStripMenuItem.Image = Resources.listenoff2;
                    //}
                    //else
                    //{
                    //    _listenToolStripMenuItem.Text = LocRm.GetString("Listen");
                    //    _listenToolStripMenuItem.Image = Resources.listen2;
                    //}
                    //_recordNowToolStripMenuItem.Visible = false;
                    //if (volumeControl.Micobject.settings.active)
                    //{
                    //    _setInactiveToolStripMenuItem.Visible = true;
                    //    _recordNowToolStripMenuItem.Visible = true;
                    //    _listenToolStripMenuItem.Enabled = true;
                    //}
                    //else
                    //{
                    //    _activateToolStripMenuItem.Visible = true;
                    //    _recordNowToolStripMenuItem.Visible = false;
                    //    _listenToolStripMenuItem.Enabled = false;
                    //}
                    //_recordNowToolStripMenuItem.Text =
                    //    LocRm.GetString(volumeControl.ForcedRecording ? "StopRecording" : "StartRecording");
                    //ctxtMnu.Show(volumeControl, new Point(e.X, e.Y));
                    break;
            }
            volumeControl.Focus();
        }

        private static void VolumeControlMouseUp(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel)sender;
            if (e.Button == MouseButtons.Left && !volumeControl.Paired)
            {
                volumeControl.Micobject.x = volumeControl.Left;
                volumeControl.Micobject.y = volumeControl.Top;
            }
        }


        private void VolumeControlMouseMove(object sender, MouseEventArgs e)
        {
            var volumeControl = (VolumeLevel)sender;
            if (e.Button == MouseButtons.Left && !volumeControl.Paired && !MainForm.Conf.LockLayout)
            {
                int newLeft = volumeControl.Left + (e.X - Convert.ToInt32(volumeControl.Micobject.x));
                int newTop = volumeControl.Top + (e.Y - Convert.ToInt32(volumeControl.Micobject.y));
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;
                if (newLeft + volumeControl.Width > 5 && newLeft < ClientRectangle.Width - 5)
                {
                    volumeControl.Left = newLeft;
                }
                if (newTop + volumeControl.Height > 5 && newTop < ClientRectangle.Height - 50)
                {
                    volumeControl.Top = newTop;
                }
            }

        }

        #endregion

        #region FloorPlanEvents

        private void FloorPlanMouseDown(object sender, MouseEventArgs e)
        {
            var fpc = (FloorPlanControl)sender;
            if (e.Button == MouseButtons.Left)
            {
                fpc.Fpobject.x = e.X;
                fpc.Fpobject.y = e.Y;
                fpc.BringToFront();
            }
            else
            {
                if (e.Button == MouseButtons.Right)
                {
                    //ContextTarget = fpc;
                    //pluginCommandsToolStripMenuItem.Visible = false;
                    //_setInactiveToolStripMenuItem.Visible = false;
                    //_listenToolStripMenuItem.Visible = false;
                    //_activateToolStripMenuItem.Visible = false;
                    //_resetRecordingCounterToolStripMenuItem.Visible = false;
                    //_recordNowToolStripMenuItem.Visible = false;
                    //_takePhotoToolStripMenuItem.Visible = false;
                    //_applyScheduleToolStripMenuItem1.Visible = false;
                    //pTZToolStripMenuItem.Visible = false;

                    //ctxtMnu.Show(fpc, new Point(e.X, e.Y));
                }
            }
            fpc.Focus();
        }

        private static void FloorPlanMouseUp(object sender, MouseEventArgs e)
        {
            var fpc = (FloorPlanControl)sender;
            if (e.Button == MouseButtons.Left)
            {
                fpc.Fpobject.x = fpc.Left;
                fpc.Fpobject.y = fpc.Top;
            }
        }

        private void FloorPlanMouseMove(object sender, MouseEventArgs e)
        {
            var fpc = (FloorPlanControl)sender;
            if (e.Button == MouseButtons.Left && !MainForm.Conf.LockLayout)
            {
                int newLeft = fpc.Left + (e.X - Convert.ToInt32(fpc.Fpobject.x));
                int newTop = fpc.Top + (e.Y - Convert.ToInt32(fpc.Fpobject.y));
                if (newLeft < 0) newLeft = 0;
                if (newTop < 0) newTop = 0;
                if (newLeft + fpc.Width > 5 && newLeft < ClientRectangle.Width - 5)
                {
                    fpc.Left = newLeft;
                }
                if (newTop + fpc.Height > 5 && newTop < ClientRectangle.Height - 50)
                {
                    fpc.Top = newTop;
                }
            }
        }

        #endregion

        #region RestoreSavedCameras

        private void DisplayCamera(objectsCamera cam)
        {
            var cameraControl = new CameraWindow(cam);
            SetCameraEvents(cameraControl);
            cameraControl.BackColor = MainForm.Conf.BackColor.ToColor();
            _pnlCameras.Controls.Add(cameraControl);
            cameraControl.Location = new Point(cam.x, cam.y);
            cameraControl.Size = new Size(cam.width, cam.height);
            cameraControl.BringToFront();
            cameraControl.Tag = GetControlIndex();

            string path = MainForm.Conf.MediaDirectory + "video\\" + cam.directory + "\\";
            try
            {
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                path = MainForm.Conf.MediaDirectory + "video\\" + cam.directory + "\\thumbs\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    //move existing thumbs into directory
                    var lfi =
                        Directory.GetFiles(MainForm.Conf.MediaDirectory + "video\\" + cam.directory + "\\", "*.jpg").ToList();
                    foreach (string file in lfi)
                    {
                        string destfile = file;
                        int i = destfile.LastIndexOf(@"\", StringComparison.Ordinal);
                        destfile = file.Substring(0, i) + @"\thumbs" + file.Substring(i);
                        File.Move(file, destfile);
                    }
                }
                path = MainForm.Conf.MediaDirectory + "video\\" + cam.directory + "\\grabs\\";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }

            cameraControl.GetFiles();
        }

        private void CameraControlRemoteCommand(object sender, ThreadSafeCommand e)
        {
            InvokeMethod i = DoInvoke;
            Invoke(i, new object[] { e.Command });
        }

        private delegate void InvokeMethod(string command);

        private void DoInvoke(string methodName)
        {
            if (methodName == "show")
            {
                Activate();
                Visible = true;
                if (WindowState == FormWindowState.Minimized)
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                }
                return;
            }
            if (methodName.StartsWith("bringtofrontcam"))
            {
                int camid = Convert.ToInt32(methodName.Split(',')[1]);
                foreach (Control c in _pnlCameras.Controls)
                {
                    if (c is CameraWindow)
                    {
                        var cameraControl = (CameraWindow)c;
                        if (cameraControl.Camobject.id == camid)
                        {
                            cameraControl.BringToFront();
                            break;
                        }
                    }
                }
                return;
            }
            if (methodName.StartsWith("bringtofrontmic"))
            {
                int micid = Convert.ToInt32(methodName.Split(',')[1]);
                foreach (Control c in _pnlCameras.Controls)
                {
                    if (c is VolumeLevel)
                    {
                        var vl = (VolumeLevel)c;
                        if (vl.Micobject.id == micid)
                        {
                            vl.BringToFront();
                            break;
                        }
                    }
                }
                return;
            }
        }

        public void AddObjectExternal(int objectTypidId, int sourceIndex, int width, int height, string name, string url)
        {
            if (!VlcHelper.VlcInstalled && sourceIndex == 5)
                return;
            switch (objectTypidId)
            {
                case 2:
                    if (MainForm.Cameras.FirstOrDefault(p => p.settings.videosourcestring == url) == null)
                    {
                        if (InvokeRequired)
                            Invoke(new AddObjectExternalDelegate(AddCameraExternal), sourceIndex, url, width, height,
                                   name);
                        else
                            AddCameraExternal(sourceIndex, url, width, height, name);
                    }
                    break;
                case 1:
                    if (MainForm.Microphones.FirstOrDefault(p => p.settings.sourcename == url) == null)
                    {
                        if (InvokeRequired)
                            Invoke(new AddObjectExternalDelegate(AddMicrophoneExternal), sourceIndex, url, width, height,
                                   name);
                        else
                            AddMicrophoneExternal(sourceIndex, url, width, height, name);
                    }
                    break;
            }
            MainForm.NeedsSync = true;
        }

        private void SetNewStartPosition()
        {
            if (MainForm.Conf.AutoLayout)
                LayoutObjects(0, 0);
        }

        private void VolumeControlRemoteCommand(object sender, VolumeLevel.ThreadSafeCommand e)
        {
            InvokeMethod i = DoInvoke;
            Invoke(i, new object[] { e.Command });
        }

        #endregion

        private void CameraPanel_Load(object sender, EventArgs e)
        {

        }
    }
}
