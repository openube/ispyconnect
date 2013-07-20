using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public partial class MediaPanel : FlowLayoutPanel
    {
        public bool Loading = false;
        public Point selectStart = Point.Empty;
        public Point selectEnd = Point.Empty;

        public MediaPanel()
        {
            InitializeComponent();
            KeyDown += MediaPanel_KeyDown;
            DoubleBuffered = true;
        }

        void MediaPanel_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode==Keys.Delete)
            {
                var topLevelControl = (MainForm) TopLevelControl;
                if (topLevelControl != null) topLevelControl.DeleteSelectedMedia();
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            var g = pe.Graphics;
            if (Loading)
            {
                var txt = LocRm.GetString("Loading");
                var s = g.MeasureString(txt, MainForm.Drawfont);
                g.DrawString(txt, MainForm.Drawfont, MainForm.OverlayBrush, Convert.ToInt32(Width / 2) - s.Width / 2, Convert.ToInt32(Height / 2) - s.Height / 2);
            }

            if (selectStart != Point.Empty && selectEnd != Point.Empty)
            {
                var b = new SolidBrush(Color.White);
                var p = new Pen(b, 1) { DashStyle = DashStyle.Dash };
                g.DrawLine(p, selectStart.X, selectStart.Y, selectStart.X, selectEnd.Y);
                g.DrawLine(p, selectStart.X, selectEnd.Y, selectEnd.X, selectEnd.Y);
                g.DrawLine(p, selectEnd.X, selectEnd.Y, selectEnd.X, selectStart.Y);
                g.DrawLine(p, selectEnd.X, selectStart.Y, selectStart.X, selectStart.Y);

                b.Dispose();
                p.Dispose();
            }
            
            

        }

        private void MediaPanel_MouseDown(object sender, MouseEventArgs e)
        {
            selectStart = e.Location;
        }

        private void MediaPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (Math.Sqrt(Math.Pow(selectStart.X-selectEnd.X,2)+Math.Pow(selectStart.Y-selectEnd.Y,2))>5)
            {
                var r = NormRect(selectStart, selectEnd);
                foreach(PreviewBox pb in Controls)
                {
                    if (pb.Location.X < r.X+r.Width && pb.Location.X+pb.Width > r.X &&
                        pb.Location.Y < r.Y+r.Height && pb.Location.Y+pb.Height > r.Y)
                    {
                        pb.Selected = true;
                        pb.Invalidate();
                    }
    
                }

            }
            selectStart = Point.Empty;
            Invalidate();
        }

        internal Rectangle NormRect(Point p1, Point p2)
        {
            int x = p1.X, y = p1.Y, w, h;
            w = Math.Abs(p1.X - p2.X);
            h = Math.Abs(p1.Y - p2.Y);

            if (p2.X < p1.X)
                x = p2.X;
            if (p2.Y < p1.Y)
                y = p2.Y;
            return new Rectangle(x, y, w, h);
        }

        private void MediaPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectStart != Point.Empty)
            {
                selectEnd = e.Location;
                Invalidate();
            }
        }
    }
}
