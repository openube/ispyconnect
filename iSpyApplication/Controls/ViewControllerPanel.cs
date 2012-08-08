using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyApplication
{
    class ViewControllerPanel : PictureBox
    {
        public LayoutPanel LayoutTarget;
        private double _xRat = 0, _yRat = 0;
        private int _hScrollBound = 0, _vScrollBound = 0;
        private Point _startPoint = Point.Empty;
        private int _hscrolloffset = 0;
        private int _vscrolloffset = 0;

        public ViewControllerPanel(LayoutPanel layoutTarget)
        {
            LayoutTarget = layoutTarget;
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            UpdateStyles();


        }

        
        protected override void OnPaint(PaintEventArgs pe)
        {
            var gLayout = pe.Graphics;

            var rects = new List<AlertRectangle>();
            double xMax = 0, yMax = 0;

            int xOff = LayoutTarget.HorizontalScroll.Value;
            int yOff = LayoutTarget.VerticalScroll.Value;
            
            foreach (Control c in LayoutTarget.Controls)
            {
                bool alert = false;
                if (c is CameraWindow)
                {
                    alert = ((CameraWindow) c).Alerted;
                }
                else
                {
                    if (c is VolumeLevel)
                    {
                        alert = ((VolumeLevel) c).Alerted;
                    }
                }
                


                rects.Add(new AlertRectangle(new Rectangle(c.Location.X + xOff, c.Location.Y + yOff, c.Width, c.Height), alert, LayoutTarget.Controls.GetChildIndex(c)));
                if (c.Height + c.Location.Y + yOff > yMax)
                    yMax = c.Height + c.Location.Y + yOff;
                if (c.Width + c.Location.X + xOff > xMax)
                    xMax = c.Width + c.Location.X + xOff;
            }
            rects = rects.OrderByDescending(p => p.ChildIndex).ToList();
            

            if (xMax > 0 && yMax > 0)
            {
                var gridBrush = new SolidBrush(Color.DarkGray);
                var gridPen = new Pen(Color.White, 1);
                var alertPen = new Pen(Color.Red,2);
                var clientPen = new Pen(Color.Orange, 2);
                var rc = LayoutTarget.ClientRectangle;
                rc.X = xOff;
                rc.Y = yOff;

                gLayout.FillRectangle(gridBrush, 0, 0, Width, Height);
                if (xMax < rc.Width + rc.X)
                    xMax = rc.Width + rc.X;
                if (yMax < rc.Height + rc.Y)
                    yMax = rc.Height + rc.Y;

                _hScrollBound = Convert.ToInt32(xMax - rc.Width);
                
                _vScrollBound = Convert.ToInt32(yMax - rc.Height);
                if (LayoutTarget.VerticalScroll.Visible && LayoutTarget.HorizontalScroll.Visible)
                {
                    _hScrollBound += SystemInformation.VerticalScrollBarWidth;
                    _vScrollBound += SystemInformation.HorizontalScrollBarHeight;
                }

                _xRat = Convert.ToDouble(Width) / xMax;
                _yRat = Convert.ToDouble(Height) / yMax;

                foreach (var r in rects)
                {
                    if (!r.Alert)
                        gLayout.DrawRectangle(gridPen, (float)_xRat * r.Rect.X, (float)_yRat * r.Rect.Y, (float)_xRat * r.Rect.Width,
                                          (float)_yRat * r.Rect.Height);
                    else
                    {
                        gLayout.DrawRectangle(alertPen, (float)_xRat * r.Rect.X, (float)_yRat * r.Rect.Y, (float)_xRat * r.Rect.Width,
                                          (float)_yRat * r.Rect.Height);
                    
                    }

                }

                gLayout.DrawRectangle(clientPen, (float)_xRat * rc.X, (float)_yRat * rc.Y, (float)_xRat * rc.Width,
                                      (float)_yRat * rc.Height);

                gridBrush.Dispose();
                gridPen.Dispose();
                alertPen.Dispose();
                clientPen.Dispose();
            }

            base.OnPaint(pe);
        }


        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (LayoutTarget != null)
                if (LayoutTarget.TopLevelControl != null) ((MainForm)LayoutTarget.TopLevelControl).ShowIfUnlocked();
            base.OnMouseDoubleClick(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && (LayoutTarget.VerticalScroll.Visible ||LayoutTarget.HorizontalScroll.Visible))
            {
                var dx = e.Location.X - _startPoint.X;
                var dy = e.Location.Y - _startPoint.Y;
                var mx = dx/_xRat;
                var my = dy/_yRat;

                var hScroll = _hscrolloffset + mx;
                var vScroll = _vscrolloffset + my;

                if (hScroll > _hScrollBound) hScroll = _hScrollBound;
                if (vScroll > _vScrollBound) vScroll = _vScrollBound;

                if (hScroll < 0) hScroll = 0;
                if (vScroll < 0) vScroll = 0;
                

                LayoutTarget.HorizontalScroll.Value = Convert.ToInt32(hScroll);
                LayoutTarget.VerticalScroll.Value = Convert.ToInt32(vScroll);
                Invalidate();
            }
           
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            _startPoint = e.Location;
            _hscrolloffset = LayoutTarget.HorizontalScroll.Value;
            _vscrolloffset = LayoutTarget.VerticalScroll.Value;
            base.OnMouseDown(e);
        }


        private class AlertRectangle
        {
            public Rectangle Rect;
            public bool Alert;
            public int ChildIndex;

            public AlertRectangle(Rectangle rect, bool alert, int childIndex)
            {
                Rect = rect;
                Alert = alert;
                ChildIndex = childIndex;
            }
        }
    }
}
