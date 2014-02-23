using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using AForge.Imaging.Filters;
using PictureBox = AForge.Controls.PictureBox;

namespace iSpyApplication.Controls
{
    public sealed partial class FloorPlanControl : PictureBox
    {
        #region Public

        public bool NeedSizeUpdate;
        public bool ResizeParent;
        public objectsFloorplan Fpobject;
        public double LastAlertTimestamp;
        private Point _mouseLoc = Point.Empty;
        public double LastRefreshTimestamp;
        public int LastOid;
        public int LastOtid;
        public bool IsAlert;
        public Rectangle RestoreRect = Rectangle.Empty;
        private readonly ToolTip _toolTipFp;
        private int _ttind = -1;
        private readonly object _lockobject = new object();

        private readonly SolidBrush _alertBrush = new SolidBrush(Color.FromArgb(200, 255, 0, 0));
        private readonly SolidBrush _noalertBrush = new SolidBrush(Color.FromArgb(200, 75, 172, 21));
        private readonly SolidBrush _offlineBrush = new SolidBrush(Color.FromArgb(200, 0, 0, 0));

        private readonly SolidBrush _alertBrushScanner = new SolidBrush(Color.FromArgb(50, 255, 0, 0));
        private readonly SolidBrush _noalertBrushScanner = new SolidBrush(Color.FromArgb(50, 75, 172, 21));

        private readonly SolidBrush _drawBrush = new SolidBrush(Color.FromArgb(255, 255, 255, 255));
        private readonly SolidBrush _sbTs = new SolidBrush(Color.FromArgb(128, 0, 0, 0));

        private readonly Font _drawFont = new Font(FontFamily.GenericSansSerif, 9);



        public Bitmap ImgPlan
        {
            
            get
            {
                if (_imgplan == null)
                    return null;
                
                return _imgplan;

            }
            set
            {
                lock (_lockobject)
                {
                    if (_imgview != null)
                        _imgview.Dispose();
                    if (_imgplan != null)
                        _imgplan.Dispose();
                    _imgplan = value;
                    if (_imgplan!=null)
                        _imgview = (Bitmap)_imgplan.Clone();
                }
            }
        }


        public Bitmap ImgView
        {
             get
             {
                 return _imgview;
             }  
        }

        public MainForm Owner;
        public bool NeedsRefresh = true, RefreshImage = true;


        #endregion
        private DateTime _mouseMove = DateTime.MinValue;
        private Bitmap _imgplan, _imgview;
        private const int ButtonOffset = 4, ButtonCount = 2;

        private static int ButtonWidth
        {
            get { return MainForm.ButtonWidth; }
        }
        private static int ButtonPanelWidth
        {
            get
            {
                return ((ButtonWidth + ButtonOffset) * ButtonCount + ButtonOffset);
            }
        }
        private static int ButtonPanelHeight
        {
            get { return (ButtonWidth + ButtonOffset * 2); }
        }

        #region SizingControls

        public void UpdatePosition()
        {
            Monitor.Enter(this);

            if (Parent != null && ImgPlan != null)
            {
                int width = ImgPlan.Width;
                int height = ImgPlan.Height;

                SuspendLayout();
                Size = new Size(width + 2, height + 26);
                ResumeLayout();
                NeedSizeUpdate = false;
            }
            Monitor.Exit(this);
        }

        private MousePos GetMousePos(Point location)
        {
            var result = MousePos.NoWhere;
            int rightSize = Padding.Right;
            int bottomSize = Padding.Bottom;
            var testRect = new Rectangle(Width - rightSize, 0, Width - rightSize, Height - bottomSize);
            if (testRect.Contains(location)) result = MousePos.Right;
            testRect = new Rectangle(0, Height - bottomSize, Width - rightSize, Height);
            if (testRect.Contains(location)) result = MousePos.Bottom;
            testRect = new Rectangle(Width - rightSize, Height - bottomSize, Width, Height);
            if (testRect.Contains(location)) result = MousePos.BottomRight;
            return result;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Invalidate();
            }
            _toolTipFp.RemoveAll();
            _toolTipFp.Dispose();

            _alertBrush.Dispose();
            _noalertBrush.Dispose();
            _offlineBrush.Dispose();

            _alertBrushScanner.Dispose();
            _noalertBrushScanner.Dispose();
            _drawBrush.Dispose();
            _sbTs.Dispose();
            _drawFont.Dispose();
            
            base.Dispose(disposing);
        }


        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Select();
            IntPtr hwnd = Handle;
            if ((ResizeParent) && (Parent != null) && (Parent.IsHandleCreated))
            {
                hwnd = Parent.Handle;
            }
            
            if (e.Button == MouseButtons.Left)
            {
                MousePos mousePos = GetMousePos(e.Location);
                if (mousePos== MousePos.NoWhere)
                {
                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width / 2 - ButtonPanelWidth / 2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                            e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //settings
                                if (TopLevelControl != null)
                                    ((MainForm)TopLevelControl).EditFloorplan(Fpobject);
                            }
                            else
                            {
                                string url = MainForm.Webserver + "/watch_new.aspx";// "?tab=2";
                                if (WsWrapper.WebsiteLive && MainForm.Conf.ServicesEnabled)
                                {
                                    MainForm.OpenUrl(url);
                                }
                                else if (TopLevelControl != null) ((MainForm)TopLevelControl).Connect(url, false);
                            }
                        }
                    }
                }
                if (MainForm.Conf.LockLayout) return;
                switch (mousePos)
                {
                    case MousePos.Right:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeE, IntPtr.Zero);
                        }
                        break;
                    case MousePos.Bottom:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeS, IntPtr.Zero);
                        }
                        break;
                    case MousePos.BottomRight:
                        {
                            NativeCalls.ReleaseCapture(hwnd);
                            NativeCalls.SendMessage(hwnd, NativeCalls.WmSyscommand, NativeCalls.ScDragsizeSe,
                                                    IntPtr.Zero);
                        }
                        break;
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseLoc.X == e.X && _mouseLoc.Y == e.Y)
                return;
            _mouseMove = Helper.Now;
            MousePos mousePos = GetMousePos(e.Location);
            switch (mousePos)
            {
                case MousePos.Right:
                    Cursor = Cursors.SizeWE;
                    break;
                case MousePos.Bottom:
                    Cursor = Cursors.SizeNS;
                    break;
                case MousePos.BottomRight:
                    Cursor = Cursors.SizeNWSE;
                    break;
                default:
                    Cursor = Cursors.Hand;

                    if (MainForm.Conf.ShowOverlayControls)
                    {
                        int leftpoint = Width / 2 - ButtonPanelWidth / 2;
                        int ypoint = Height - 24 - ButtonPanelHeight;
                        var toolTipLocation = new Point(e.Location.X, ypoint + ButtonPanelHeight + 1);
                        if (e.Location.X > leftpoint && e.Location.X < leftpoint + ButtonPanelWidth &&
                            e.Location.Y > ypoint && e.Location.Y < ypoint + ButtonPanelHeight)
                        {
                            int x = e.Location.X - leftpoint;
                            if (x < ButtonWidth + ButtonOffset)
                            {
                                //power
                                if (_ttind != 0)
                                {
                                    _toolTipFp.Show(LocRm.GetString("Edit"), this, toolTipLocation, 1000);
                                    _ttind = 0;
                                }
                            }
                            else
                            {
                                if (x < (ButtonWidth + ButtonOffset) * 2)
                                {
                                    //record
                                    if (_ttind != 1)
                                    {
                                        _toolTipFp.Show(LocRm.GetString("MediaoverTheWeb"), this, toolTipLocation, 1000);
                                        _ttind = 1;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _toolTipFp.Hide(this);
                            _ttind = -1;
                        }
                    }
                    break;
            }
            base.OnMouseMove(e);
        }

        protected override void OnResize(EventArgs eventargs)
        {
            if ((ModifierKeys & Keys.Shift) == Keys.Shift)
            {
                double ar = Convert.ToDouble(MinimumSize.Width)/Convert.ToDouble(MinimumSize.Height);
                if (ImgPlan != null)
                    ar = Convert.ToDouble(ImgPlan.Width)/Convert.ToDouble(ImgPlan.Height);
                Width = Convert.ToInt32(ar*Height);
            }

            base.OnResize(eventargs);
            if (Width < MinimumSize.Width) Width = MinimumSize.Width;
            if (Height < MinimumSize.Height) Height = MinimumSize.Height;
            _minimised = Size.Equals(MinimumSize);

        }

        private bool _minimised;

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            Cursor = Cursors.Default;
            _mouseMove = DateTime.MinValue;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            Cursor = Cursors.Hand;
            Invalidate();
        }

        #region Nested type: MousePos

        private enum MousePos
        {
            NoWhere,
            Right,
            Bottom,
            BottomRight
        }

        #endregion

        
        #endregion

        public FloorPlanControl(objectsFloorplan ofp, MainForm owner)
        {
            Owner = owner;
            InitializeComponent();

            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;
            BackColor = MainForm.BackgroundColor;
            Fpobject = ofp;
            MouseClick += FloorPlanControlClick;

            _toolTipFp = new ToolTip { AutomaticDelay = 500, AutoPopDelay = 1500 };
        }

        private void FloorPlanControlClick(object sender, MouseEventArgs e)
        {
            var local = new Point(e.X, e.Y);
            double xRat = Convert.ToDouble(Width)/ImageWidth;
            double yRat = Convert.ToDouble(Height)/ImageHeight;
            double hittargetw = 22*xRat;
            double hittargeth = 22*yRat;

            double wrat = Convert.ToDouble(ImageWidth) / 533d;
            double hrat = Convert.ToDouble(ImageHeight) / 400d;


            bool changeHighlight = true;

            if (Highlighted)
            {
                foreach (objectsFloorplanObjectsEntry fpoe in Fpobject.objects.@object)
                {
                    if (((fpoe.x*wrat) - hittargetw)*xRat <= local.X && ((fpoe.x*wrat) + hittargetw)*xRat > local.X &&
                        ((fpoe.y*hrat) - hittargeth)*yRat <= local.Y && ((fpoe.y*hrat) + hittargeth)*yRat > local.Y)
                    {
                        switch (fpoe.type)
                        {
                            case "camera":
                                CameraWindow cw = Owner.GetCameraWindow(fpoe.id);
                                if (cw != null)
                                {
                                    //cw.Location = new Point(Location.X + e.X, Location.Y + e.Y);
                                    cw.BringToFront();
                                    cw.Focus();
                                }

                                changeHighlight = false;
                                break;
                            case "microphone":
                                VolumeLevel vl = Owner.GetVolumeLevel(fpoe.id);
                                if (vl != null)
                                {
                                    //vl.Location = new Point(Location.X + e.X, Location.Y + e.Y);
                                    vl.BringToFront();
                                    vl.Focus();
                                }

                                changeHighlight = false;
                                break;
                        }
                        break;
                    }                   
                }
            }

            if (changeHighlight)
            {
                bool hl = Highlighted;
                Owner.ClearHighlights();

                Highlighted = !hl;
            }
            if (Highlighted)
            {
                foreach (objectsFloorplanObjectsEntry fpoe in Fpobject.objects.@object)
                {
                    switch (fpoe.type)
                    {
                        case "camera":
                            CameraWindow cw = Owner.GetCameraWindow(fpoe.id);
                            if (cw!=null)
                                cw.Highlighted = true;
                            break;
                        case "microphone":
                            VolumeLevel vl = Owner.GetVolumeLevel(fpoe.id);
                            if (vl!=null)
                                vl.Highlighted = true;

                            break;
                    }
                }
            }

            Owner.Invalidate(true);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        public bool Highlighted;

        public Color BorderColor
        {
            get
            {
                if (Highlighted)
                    return MainForm.FloorPlanHighlightColor;

                if (Focused)
                    return MainForm.BorderHighlightColor;

                return MainForm.BorderDefaultColor;

            }
        }

        public int BorderWidth
        {
            get
            {
                return Highlighted ? 2 : 1;
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Graphics gPlan = pe.Graphics;

            Rectangle rc = ClientRectangle;

            var grabPoints = new[]
                                    {
                                        new Point(rc.Width - 15, rc.Height), new Point(rc.Width, rc.Height - 15),
                                        new Point(rc.Width, rc.Height)
                                    };
            int textpos = rc.Height - 20;

            var grabBrush = new SolidBrush(BorderColor);
            var borderPen = new Pen(grabBrush, BorderWidth);
            
            
            try
            {
                


                if (_imgview != null)
                {
                    if (!_minimised)
                        gPlan.DrawImage(_imgview, rc.X + 1, rc.Y + 1, rc.Width - 2, rc.Height - 26);

                    gPlan.CompositingMode = CompositingMode.SourceOver;
                    gPlan.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                    gPlan.DrawString(LocRm.GetString("FloorPlan") + ": " + Fpobject.name, _drawFont,
                                _drawBrush,
                                new PointF(5, textpos));
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }

            if (_mouseMove > Helper.Now.AddSeconds(-3) && MainForm.Conf.ShowOverlayControls)
            {
                int leftpoint = Width/2 - ButtonPanelWidth/2;
                int ypoint = Height - 24 - ButtonPanelHeight;

                gPlan.FillRectangle(_sbTs, leftpoint, ypoint, ButtonPanelWidth, ButtonPanelHeight);

                gPlan.DrawString("E", MainForm.Iconfont, MainForm.IconBrush, leftpoint + ButtonOffset,
                                 ypoint + ButtonOffset);
                gPlan.DrawString("C", MainForm.Iconfont, MainForm.IconBrush, leftpoint + (ButtonOffset*2) + ButtonWidth,
                                 ypoint + ButtonOffset);

            }

            gPlan.DrawRectangle(borderPen, 0, 0, rc.Width - 1, rc.Height - 1);
            gPlan.FillPolygon(grabBrush, grabPoints);
            grabBrush.Dispose();  
            borderPen.Dispose();

            base.OnPaint(pe);
        }

        public int ImageWidth
        {
            get
            {
                if (_imgview != null)
                    return _imgview.Width;
                return 533;
            }
        }

        public int ImageHeight
        {
            get
            {
                if (_imgview != null)
                    return _imgview.Height;
                return 400;
            }
        }

        public void Tick()
        {
            if (NeedSizeUpdate)
            {
                UpdatePosition();
            }

            if (NeedsRefresh)
            {
                bool alert = false;
                lock (this)
                {
                    if (RefreshImage || (_imgplan == null && !String.IsNullOrEmpty(Fpobject.image)))
                    {
                        if (_imgplan!=null)
                        {
                            try
                            {
                                _imgplan.Dispose();
                            } catch
                            {
                            }
                            _imgplan = null;
                        }
                        if (_imgview != null)
                        {
                            try
                            {
                                _imgview.Dispose();
                            }
                            catch
                            {
                            }
                            _imgview = null;
                        }
                        var img = (Bitmap)Image.FromFile(Fpobject.image);
                        if (!Fpobject.originalsize)
                        {
                            var rf = new ResizeBilinear(533, 400);
                            _imgplan = rf.Apply((Bitmap)img);
                            _imgview = (Bitmap)_imgplan.Clone();
                        }
                        else
                        {
                            _imgplan = img;
                            _imgview = (Bitmap)_imgplan.Clone();
                        }
                        RefreshImage = false;
                    }
                    if (_imgplan == null)
                        return;

                    
                    

                    Graphics gLf = Graphics.FromImage(_imgview);
                    gLf.DrawImage(_imgplan, 0, 0,_imgplan.Width,_imgplan.Height);

                    bool itemRemoved = false;
                    double wrat = Convert.ToDouble(ImageWidth) / 533d;
                    double hrat = Convert.ToDouble(ImageHeight) / 400d;

                    foreach (objectsFloorplanObjectsEntry fpoe in Fpobject.objects.@object)
                    {
                        var p = new Point(fpoe.x, fpoe.y);
                        if (Fpobject.originalsize)
                        {
                            p.X = Convert.ToInt32(p.X*wrat);
                            p.Y = Convert.ToInt32(p.Y*hrat);
                        }
                        if (fpoe.fov == 0)
                            fpoe.fov = 135;
                        if (fpoe.radius == 0)
                            fpoe.radius = 80;
                        switch (fpoe.type)
                        {
                            case "camera":
                                {
                                    var cw = Owner.GetCameraWindow(fpoe.id);
                                    if (cw != null)
                                    {
                                        double drad = (fpoe.angle - 180) * Math.PI / 180;
                                        var points = new[]
                                            {
                                                new Point(p.X + 11+Convert.ToInt32(20*Math.Cos(drad)), p.Y + 11 + Convert.ToInt32((20* Math.Sin(drad)))),
                                                new Point(p.X + 11+Convert.ToInt32(20*Math.Cos(drad+(135*Math.PI/180))), p.Y + 11 + Convert.ToInt32((20* Math.Sin(drad+(135*Math.PI/180))))),
                                                new Point(p.X + 11+Convert.ToInt32(10*Math.Cos(drad+(180*Math.PI/180))), p.Y + 11 + Convert.ToInt32((10* Math.Sin(drad+(180*Math.PI/180))))),
                                                new Point(p.X + 11+Convert.ToInt32(20*Math.Cos(drad-(135*Math.PI/180))), p.Y + 11 + Convert.ToInt32((20* Math.Sin(drad-(135*Math.PI/180)))))
                                            };
                                        if (cw.Camobject.settings.active && !cw.VideoSourceErrorState)
                                        {
                                            int offset = (fpoe.radius / 2) - 11;
                                            if (cw.Alerted)
                                            {
                                                gLf.FillPolygon(_alertBrush, points);

                                                gLf.FillPie(_alertBrushScanner, p.X - offset, p.Y - offset, fpoe.radius, fpoe.radius,
                                                            (float)(fpoe.angle - 180 - (fpoe.fov / 2)), fpoe.fov);
                                                alert = true;
                                            }
                                            else
                                            {
                                                gLf.FillPolygon(_noalertBrush, points);
                                                gLf.FillPie(_noalertBrushScanner, p.X - offset, p.Y - offset, fpoe.radius, fpoe.radius,
                                                            (float)(fpoe.angle - 180 - (fpoe.fov / 2)), fpoe.fov);
                                            }
                                        }
                                        else
                                        {
                                            gLf.FillPolygon(_offlineBrush, points);
                                        }

                                    }
                                    else
                                    {
                                        fpoe.id = -2;
                                        itemRemoved = true;
                                    }
                                }
                                break;
                            case "microphone":
                                {
                                    var vw = Owner.GetVolumeLevel(fpoe.id);
                                    if (vw != null)
                                    {
                                        if (vw.Micobject.settings.active && !vw.AudioSourceErrorState)
                                        {
                                            if (vw.Alerted)
                                            {
                                                gLf.FillEllipse(_alertBrush, p.X - 20, p.Y - 20, 40, 40);
                                                alert = true;
                                            }
                                            else
                                            {
                                                gLf.FillEllipse(_noalertBrush, p.X - 15, p.Y - 15, 30, 30);
                                            }
                                        }
                                        else
                                        {
                                            gLf.FillEllipse(_offlineBrush, p.X - 15, p.Y - 15, 30, 30);
                                        }
                                    }
                                    else
                                    {
                                        fpoe.id = -2;
                                        itemRemoved = true;
                                    }
                                }
                                break;
                        }
                    }

                    if (itemRemoved)
                        Fpobject.objects.@object = Fpobject.objects.@object.Where(fpoe => fpoe.id > -2).ToArray();
                    

                    gLf.Dispose();
                }
                Invalidate();
                LastRefreshTimestamp = Helper.Now.UnixTicks();
                NeedsRefresh = false;
                IsAlert = alert;
            }
        }

        #region Nested type: ThreadSafeCommand

        public class ThreadSafeCommand : EventArgs
        {
            public string Command;
            // Constructor
            public ThreadSafeCommand(string command)
            {
                Command = command;
            }
        }

        #endregion
    }
}