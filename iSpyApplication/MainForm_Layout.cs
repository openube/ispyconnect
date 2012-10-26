using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using C2BP;
using iSpyApplication.Controls;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyApplication
{
    public partial class MainForm
    {
        private void LayoutObjects(int w, int h)
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
                            if (cw.Camera != null && cw.Camera.LastFrame != null)
                            {
                                p.Width = cw.Camera.LastFrame.Width + 2;
                                p.Height = cw.Camera.LastFrame.Height + 32;
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
                            if (!indexesassigned.Contains(i) && c is PictureBox)
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

        private void ResetLayout()
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
            resetLayoutToolStripMenuItem1.Enabled = mnuResetLayout.Enabled = true;
        }

        public void Maximise(object obj)
        {
            Maximise(obj, true);
        }

        public void Maximise(object obj, bool minimiseIfMaximised)
        {
            if (obj == null || Conf.LockLayout)
                return;
            if (obj.GetType() == typeof(CameraWindow))
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

                        double wFact = Convert.ToDouble(_pnlCameras.Width)/Convert.ToDouble(wh[0]);
                        double hFact = Convert.ToDouble(_pnlCameras.Height)/Convert.ToDouble(wh[1]);
                        if (cameraControl.VolumeControl != null)
                            hFact = Convert.ToDouble((_pnlCameras.Height - 40))/Convert.ToDouble(wh[1]);
                        if (hFact <= wFact)
                        {
                            cameraControl.Width = Convert.ToInt32(((Convert.ToDouble(_pnlCameras.Width)*hFact)/wFact));
                            cameraControl.Height = _pnlCameras.Height;
                        }
                        else
                        {
                            cameraControl.Width = _pnlCameras.Width;
                            cameraControl.Height = Convert.ToInt32((Convert.ToDouble(_pnlCameras.Width)*wFact)/hFact);
                        }
                        cameraControl.Location = new Point(((_pnlCameras.Width - cameraControl.Width)/2),
                                                           ((_pnlCameras.Height - cameraControl.Height)/2));
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
                catch(Exception ex)
                {
                    LogExceptionToFile(ex);
                }
            }

            if (obj.GetType() == typeof(VolumeLevel))
            {
                var vf = ((VolumeLevel)obj);
                vf.BringToFront();
                if (vf.Paired)
                {
                    CameraWindow cw = GetCameraWindow(Cameras.Single(p => p.settings.micpair == vf.Micobject.id).id);
                    if (vf.Width == _pnlCameras.Width)
                    {
                        if (minimiseIfMaximised)
                            Minimize(cw, false);
                    }
                    else
                        Maximise(cw);
                }
            }

            if (obj.GetType() == typeof(FloorPlanControl))
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

        private void MaxMin()
        {
            fullScreenToolStripMenuItem1.Checked = menuItem3.Checked = !fullScreenToolStripMenuItem1.Checked;
            if (fullScreenToolStripMenuItem1.Checked)
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.None;
                WinApi.SetWinFullScreen(Handle);
            }
            else
            {
                WindowState = FormWindowState.Maximized;
                FormBorderStyle = FormBorderStyle.Sizable;
            }
            Conf.Fullscreen = fullScreenToolStripMenuItem1.Checked;
        }

        private void LayoutOptimised()
        {
            double numberCameras = Cameras.Count;
            int dispHeight, dispWidth, dispArea, camArea, camX, camY, useX = 320, useY = 200, lastArea;
            dispArea = _pnlCameras.Width * _pnlCameras.Height;
            lastArea = dispArea;


            for (int y = 1; y <= numberCameras; y++)
            {
                camX = y;
                camY = (int)Math.Round((numberCameras / y) + 0.499999999, 0);

                dispWidth = _pnlCameras.Width / camX;
                dispHeight = dispWidth / 4 * 3;
                camArea = (int)numberCameras * (dispWidth * (dispHeight + 40));
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



        private void UnlockLayout()
        {
            Conf.LockLayout = menuItem22.Checked = false;
        }
    }
}
