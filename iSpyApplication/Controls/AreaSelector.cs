using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace iSpyApplication
{
    public sealed partial class AreaSelector : Panel
    {
        public Point RectStart = Point.Empty;
        public Point RectStop = Point.Empty;
        private bool _bMouseDown;
        private List<Rectangle> _motionZonesRectangles = new List<Rectangle>();

        public Bitmap BmpBack;
        public Bitmap LastFrame
        {
            set
            {
                if (BmpBack != null)
                    BmpBack.Dispose();
                BmpBack = value;
                Invalidate();
            }
        }

        public void ClearRectangles()
        {
            _motionZonesRectangles = new List<Rectangle>();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int startX = Convert.ToInt32((e.X * 1.0) / (Width * 1.0) * 100);
            int startY = Convert.ToInt32((e.Y * 1.0) / (Height * 1.0) * 100);
            if (startX > 100)
                startX = 100;
            if (startY > 100)
                startY = 100;
            RectStop = new Point(startX, startY);
            RectStart = new Point(startX, startY);
            OnBoundsChanged();
            _bMouseDown = true;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            int endX = Convert.ToInt32((e.X * 1.0) / (Width * 1.0) * 100);
            int endY = Convert.ToInt32((e.Y * 1.0) / (Height * 1.0) * 100);
            if (endX > 100)
                endX = 100;
            if (endY > 100)
                endY = 100;
            RectStop = new Point(endX, endY);
            _bMouseDown = false;
            if (Math.Sqrt(Math.Pow(endX - RectStart.X, 2) + Math.Pow(endY - RectStart.Y, 2)) < 5)
            {
                RectStart = new Point(0, 0);
                RectStop = new Point(100, 100);
                OnBoundsChanged();
            }
            var start = new Point();
            var stop = new Point();
            
            start.X = RectStart.X;
            if (RectStop.X<RectStart.X)
                start.X = RectStop.X;
            start.Y = RectStart.Y;
            if (RectStop.Y<RectStart.Y)
                start.Y = RectStop.Y;

            stop.X = RectStop.X;
            if (RectStop.X<RectStart.X)
                stop.X = RectStart.X;
            stop.Y = RectStop.Y;
            if (RectStop.Y<RectStart.Y)
                stop.Y = RectStart.Y;

            var size = new Size(stop.X-start.X,stop.Y-start.Y);
            _motionZonesRectangles.Add(new Rectangle(start, size));
            RectStart = Point.Empty;
            RectStop = Point.Empty;
            OnBoundsChanged();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (_bMouseDown)
            {
                int endX = Convert.ToInt32((e.X * 1.0) / (Width * 1.0) * 100);
                int endY = Convert.ToInt32((e.Y * 1.0) / (Height * 1.0) * 100);
                if (endX > 100)
                    endX = 100;
                if (endY > 100)
                    endY = 100;

                RectStop = new Point(endX, endY);
                OnBoundsChanged();
            }
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _bMouseDown = false;
        }

        public AreaSelector()
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 3, 3);
            _motionZonesRectangles = new List<Rectangle>();
            BackgroundImageLayout = ImageLayout.Stretch;
        }
        public objectsCameraDetectorZone[] MotionZones
        {
            get
            {
                var ocdzs = new List<objectsCameraDetectorZone>();
                for (int index = 0; index < _motionZonesRectangles.Count; index++)
                {
                    Rectangle r = _motionZonesRectangles[index];
                    var ocdz = new objectsCameraDetectorZone
                                   {
                                       left = r.Left,
                                       top = r.Top,
                                       width = r.Width,
                                       height = r.Height
                                   };
                    ocdzs.Add(ocdz);
                }
                return ocdzs.ToArray();
            }
            set
            {
                _motionZonesRectangles = new List<Rectangle>();
                if (value == null) return;
                foreach (var r in value)
                    _motionZonesRectangles.Add(new Rectangle(r.left, r.top, r.width, r.height));
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            // lock
            Monitor.Enter(this);
            
            var g = pe.Graphics;
            var c = Color.FromArgb(128, 255,255,255);
            var h = new SolidBrush(c);
            var p = new Pen(Color.DarkGray);
            try
            {
                if (BmpBack!=null)
                    g.DrawImage(BmpBack, 0, 0, Width, Height);

                double wmulti = Convert.ToDouble(Width) / Convert.ToDouble(100);
                double hmulti = Convert.ToDouble(Height) / Convert.ToDouble(100);
                if (_motionZonesRectangles.Count > 0)
                {
                    foreach (var r in _motionZonesRectangles)
                    {
                        var rMod = new Rectangle(Convert.ToInt32(r.X * wmulti), Convert.ToInt32(r.Y * hmulti), Convert.ToInt32(r.Width * wmulti), Convert.ToInt32(r.Height * hmulti));
                        g.FillRectangle(h, rMod);
                        g.DrawRectangle(p, rMod);
                    }
                }
                var p1 = new Point(Convert.ToInt32(RectStart.X * wmulti), Convert.ToInt32(RectStart.Y * hmulti));
                var p2 = new Point(Convert.ToInt32(RectStop.X * wmulti), Convert.ToInt32(RectStop.Y * hmulti));

                var ps = new[] { p1, new Point(p1.X, p2.Y), p2, new Point(p2.X, p1.Y), p1 };
                g.FillPolygon(h, ps);
                g.DrawPolygon(p, ps);
                
            }
            catch
            {
            }
            p.Dispose();
            h.Dispose();

            Monitor.Exit(this);

            base.OnPaint(pe);
        }
        public event EventHandler BoundsChanged;

        private void OnBoundsChanged()
        {
            BoundsChanged(this, EventArgs.Empty);
        }
    }
}