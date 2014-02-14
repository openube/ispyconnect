using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using PictureBox = AForge.Controls.PictureBox;
using Timer = System.Timers.Timer;

namespace iSpyApplication.Controls
{
    /// <summary>
    /// Summary description for CameraWindow.
    /// </summary>
    public sealed class GridView : PictureBox
    {
        #region Private

        public Font Drawfont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular, GraphicsUnit.Pixel);
        public Font Iconfont = new Font(FontFamily.GenericSansSerif, 15, FontStyle.Bold, GraphicsUnit.Pixel);
        public Brush IconBrush = new SolidBrush(Color.White);
        public Brush IconBrushActive = new SolidBrush(Color.Red);
        public Brush OverlayBrush = new SolidBrush(Color.White);
        public Brush RecordBrush = new SolidBrush(Color.Red);


        internal MainForm MainClass;

        private const int Itempadding = 5;
        private int _maxItems = 36;
        private List<GridViewConfig> _controls;
        private int _itemwidth;
        private int _itemheight;
        private readonly Timer _tmrRefresh;
        public configurationGrid Cg;

        private int _cols = 1, _rows = 1;
        private GridViewConfig _maximised;

        private readonly object _objlock = new object();

        private int _overControlIndex = -1;

        private readonly Pen _pline = new Pen(Color.Gray, 2);
        private readonly Pen _vline = new Pen(Color.Green, 2);
        private readonly Pen _pAlert = new Pen(Color.Red, 2);
        private readonly SolidBrush _bOverlay = new SolidBrush(Color.FromArgb(100, Color.Black));

        #endregion


        public GridView(ref configurationGrid cg)
        {
            Cg = cg;
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;
            BackColor = MainForm.Conf.BackColor.ToColor();

            Init();
            
            

            _tmrRefresh = new Timer(1000d/cg.Framerate);
            _tmrRefresh.Elapsed += TmrRefreshElapsed;
            _tmrRefresh.Start();


        }

        public void Init()
        {
            _cols = Cg.Columns;
            _rows = Cg.Rows;
            
            _maxItems = _cols*_rows;
            if (Cg.ModeIndex > 0)
                _maxItems = 36;

            if (_controls!=null)
                _controls.Clear();
            _controls = new List<GridViewConfig>(_maxItems);
            
            for (int i = 0; i < _maxItems; i++)
                _controls.Add(null);           
            Text = Cg.name;

            switch (Cg.ModeIndex)
            {
                case 0:
                    AddItems();
                    break;
                case 1:
                case 2:
                    var tmrUpdateList = new Timer(1000);
                    tmrUpdateList.Elapsed += TmrUpdateLayoutElapsed;
                    tmrUpdateList.Start();
                    break;
            }
        }

        void AddItems()
        {
            if (Cg.GridItem == null)
                Cg.GridItem = new configurationGridGridItem[] { };

            foreach (var o in Cg.GridItem)
            {
                if (o.Item != null)
                {
                    if (o.GridIndex < _controls.Count)
                    {
                        var li = new List<GridItem>();
                        foreach (var c in o.Item)
                        {
                            li.Add(new GridItem("", c.ObjectID, c.TypeID));
                        }

                        if (li.Count == 0)
                            _controls[o.GridIndex] = null;
                        else
                        {
                            _controls[o.GridIndex] = new GridViewConfig(li, o.CycleDelay);
                        }
                    }
                }
            }
        }

        void TmrUpdateLayoutElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            lock(_objlock)
            {
                int i = 0;
                int del = 10;
                if (!String.IsNullOrEmpty(Cg.ModeConfig))
                    del = Convert.ToInt32(Cg.ModeConfig.Split(',')[0]);

                for(int k=0;k<_controls.Count;k++)
                {
                    var c = _controls[k];
                    if (c != null)
                    {
                        if (!c.Hold)
                            _controls[k] = null;
                    }
                }

                foreach(var cam in MainForm.Cameras)
                {
                    var ctrl = MainClass.GetCameraWindow(cam.id);

                    bool add = (Cg.ModeIndex == 1 && ctrl.LastMovementDetected > DateTime.Now.AddSeconds(0 - del)) || (Cg.ModeIndex == 2 && ctrl.LastAlerted > DateTime.Now.AddSeconds(0 - del));

                    if (add)
                    {
                        for (int k = 0; k < _controls.Count; k++)
                        {
                            var c = _controls[k];
                            if (c != null && c.ObjectIDs.Any(o => o.ObjectID == cam.id && o.TypeID == 2))
                            {
                                add = false;
                            }
                        }
                        if (add)
                        {
                            _controls[i] = new GridViewConfig(new List<GridItem> {new GridItem("", cam.id, 2)}, 1000);
                            i++;
                            if (i == _maxItems)
                                break;
                        }
                    }
                }
                if (i < _maxItems)
                {
                    foreach (var mic in MainForm.Microphones)
                    {
                        var ctrl = MainClass.GetVolumeLevel(mic.id);
                        //only want to display mics without associated camera controls
                        if (ctrl.CameraControl == null)
                        {

                            bool add = (Cg.ModeIndex == 1 && ctrl.SoundLastDetected > DateTime.Now.AddSeconds(0 - del)) ||
                                       (Cg.ModeIndex == 2 && ctrl.LastAlerted > DateTime.Now.AddSeconds(0 - del));
                            if (add)
                            {
                                for (int k = 0; k < _controls.Count; k++)
                                {
                                    var c = _controls[k];
                                    if (c!=null && c.ObjectIDs.Any(o => o.ObjectID == mic.id && o.TypeID == 1))
                                    {
                                        add = false;
                                    }
                                }
                                if (add)
                                {
                                    _controls[i] = new GridViewConfig(new List<GridItem> {new GridItem("", mic.id, 1)},
                                        1000);
                                    i++;
                                    if (i == _maxItems)
                                        break;
                                }
                            }
                        }
                    }
                }

                if (i == 0 && !String.IsNullOrEmpty(Cg.ModeConfig))
                {
                    //add default camera
                    string[] cfg = Cg.ModeConfig.Split(',');
                    if (cfg.Length > 1)
                    {
                        if (cfg[1] != "")
                        {
                            _controls[i] = new GridViewConfig(new List<GridItem> {new GridItem("", Convert.ToInt32(cfg[1]), 2)}, 1000);
                            i++;
                        }
                    }

                }
                
                if (i == 0)
                {
                    _cols = 1;
                    _rows = 1;
                }
                else
                {
                    _cols = (int) Math.Sqrt(i);
                    _rows = (int) Math.Ceiling(i/(float) _cols);
                }
            }

        }

        void TmrRefreshElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Invalidate();

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tmrRefresh.Stop();
                _tmrRefresh.Close();

                Invalidate();

            }

            _pAlert.Dispose();
            _pline.Dispose();
            _bOverlay.Dispose();
            _vline.Dispose();

            Drawfont.Dispose();
            Iconfont.Dispose();
            IconBrush.Dispose();
            IconBrushActive.Dispose();
            OverlayBrush.Dispose();
            RecordBrush.Dispose();
           
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (Cg.ModeIndex != 0)
            {
                lock (_objlock)
                {
                    DoPaint(pe);
                }
            }
            else
            {
                DoPaint(pe);
            }

            base.OnPaint(pe);
        }

        private void DoPaint(PaintEventArgs pe)
        {
            Graphics gGrid = pe.Graphics;
            
            try
            {
                int cols = _cols;
                int rows = _rows;

                if (_maximised != null)
                {
                    cols = 1;
                    rows = 1;

                }
                Rectangle rc = ClientRectangle;
                _itemwidth = (rc.Width - cols * Itempadding) / cols;
                _itemheight = (rc.Height - rows * Itempadding) / rows;

                //draw lines
                for (var i = 0; i < cols; i++)
                {
                    var x = (i * (_itemwidth + Itempadding) - Itempadding / 2);
                    gGrid.DrawLine(_pline, x, 0, x, rc.Height);
                }
                for (var i = 0; i < rows; i++)
                {
                    var y = (i * (_itemheight + Itempadding) - Itempadding / 2);

                    gGrid.DrawLine(_pline, 0, y, rc.Width, y);
                }

                var ind = 0;
                var j = 0;
                var k = 0;
                for (var i = 0; i < cols * rows; i++)
                {
                    var x = j * (_itemwidth + Itempadding);
                    var y = k * (_itemheight + Itempadding);
                    var r = new Rectangle(x, y, _itemwidth, _itemheight);
                    var gvc = _controls[ind];
                    if (_maximised!=null)
                        gvc = _maximised;
                   
                    int oy = r.Y + r.Height - 38;

                    if (gvc == null || gvc.ObjectIDs.Count == 0)
                    {
                        switch (Cg.ModeIndex)
                        {
                            case 0:
                            {
                                if (_overControlIndex == ind)
                                {
                                    gGrid.FillRectangle(_bOverlay, r.X, oy + 18, r.Width, 20);
                                }
                                string m = LocRm.GetString("AddObjects");
                                int txtOffline = Convert.ToInt32(gGrid.MeasureString(m,
                                                                                     Iconfont).Width);

                                gGrid.DrawString(m, Iconfont, OverlayBrush,
                                                 x + _itemwidth / 2 - (txtOffline / 2),
                                                 y + _itemheight / 2);
                            }
                                break;
                            case 1:
                                {
                                    const string m = "No Current Motion/Sound";
                                    int txtOffline = Convert.ToInt32(gGrid.MeasureString(m,
                                                                                         Iconfont).Width);

                                    gGrid.DrawString(m, Iconfont, OverlayBrush,
                                                     x + _itemwidth / 2 - (txtOffline / 2),
                                                     y + _itemheight / 2);
                                }
                                break;
                            case 2:
                            {
                                const string m = "No Current Alerts";
                                int txtOffline = Convert.ToInt32(gGrid.MeasureString(m,
                                                                                     Iconfont).Width);

                                gGrid.DrawString(m, Iconfont, OverlayBrush,
                                                 x + _itemwidth / 2 - (txtOffline / 2),
                                                 y + _itemheight / 2);
                            }
                                break;
                        }
                    }
                    else
                    {
                        if ((DateTime.Now - gvc.LastCycle).TotalSeconds > gvc.Delay)
                        {
                            if (!gvc.Hold)
                            {
                                gvc.CurrentIndex++;
                                gvc.LastCycle = DateTime.Now;
                            }
                        }
                        if (gvc.CurrentIndex >= gvc.ObjectIDs.Count)
                        {
                            gvc.CurrentIndex = 0;
                        }
                        var obj = gvc.ObjectIDs[gvc.CurrentIndex];
                        
                        var rFeed = r;
                        switch (obj.TypeID)
                        {
                            case 1:
                                var vl = MainClass.GetVolumeLevel(obj.ObjectID);
                                if (vl != null)
                                {
                                    if (vl.Micobject.settings.active && vl.Levels != null && !vl.AudioSourceErrorState)
                                    {
                                        var lgb = new SolidBrush(MainForm.VolumeLevelColor);
                                        int bh = (rFeed.Height) / vl.Micobject.settings.channels - (vl.Micobject.settings.channels - 1) * 2;
                                        if (bh <= 2)
                                            bh = 2;
                                        for (int m = 0; m < vl.Micobject.settings.channels; m++)
                                        {
                                            float f = 0f;
                                            if (m < vl.Levels.Length)
                                                f = vl.Levels[m];
                                            int drawW = Convert.ToInt32(Convert.ToDouble(rFeed.Width-1.0) * f);
                                            if (drawW < 1)
                                                drawW = 1;

                                            gGrid.FillRectangle(lgb, rFeed.X + 2, rFeed.Y + 2 + m * bh + (m * 2), drawW - 4, bh);

                                        }
                                        lgb.Dispose();
                                        var mx = rFeed.X+ (float)((Convert.ToDouble(rFeed.Width) / 100.00) * Convert.ToDouble(vl.Micobject.detector.sensitivity));
                                        gGrid.DrawLine(_vline, mx, rFeed.Y+1, mx, rFeed.Y + rFeed.Height-2);

                                        if (vl.Recording)
                                        {
                                            gGrid.FillEllipse(RecordBrush, new Rectangle(rFeed.X + rFeed.Width - 12, rFeed.Y + 4, 8, 8));
                                        }
                                    }
                                    else
                                    {
                                        string m = LocRm.GetString("Offline");
                                        if (vl.Micobject.settings.active)
                                            m = "Connecting...";

                                        int txtOffline =
                                            Convert.ToInt32(gGrid.MeasureString(m,
                                                                                Iconfont).Width);
                                        gGrid.DrawString(m, Iconfont,
                                                         OverlayBrush,
                                                         x + _itemwidth / 2 - (txtOffline / 2),
                                                         y + _itemheight / 2);
                                    }
                                    if (vl.Micobject != null)
                                    {

                                        gGrid.FillRectangle(_bOverlay, r.X, r.Y + r.Height - 20, r.Width, 20);
                                        gGrid.DrawString(vl.Micobject.name, Drawfont, OverlayBrush,
                                                         r.X + 5,
                                                         r.Y + r.Height - 16);
                                    }
                                }
                                else
                                {
                                    gvc.ObjectIDs.Remove(gvc.ObjectIDs[gvc.CurrentIndex]);
                                }
                                break;
                            case 2:
                                var cw = MainClass.GetCameraWindow(obj.ObjectID);
                                if (cw != null)
                                {                                    
                                    if (cw.Camera != null && !cw.LastFrameNull)
                                    {
                                        var bmp = cw.LastFrame;
                                        if (bmp != null)
                                        {
                                            if (!Cg.Fill)
                                            {
                                                rFeed = GetArea(x, y, _itemwidth, _itemheight, bmp.Width, bmp.Height);
                                            }
                                            gGrid.DrawImage(bmp, rFeed);
                                        }
                                        if (cw.Alerted)
                                        {
                                            gGrid.DrawRectangle(_pAlert, rFeed);
                                        }
                                        if (cw.Recording)
                                            gGrid.FillEllipse(RecordBrush, new Rectangle(rFeed.X + rFeed.Width - 12, rFeed.Y + 4, 8, 8));
                                    }
                                    else
                                    {
                                        string m = LocRm.GetString("Offline");
                                        if (cw.Camobject.settings.active)
                                            m = "Connecting...";
                                        
                                        int txtOffline =
                                            Convert.ToInt32(gGrid.MeasureString(m,
                                                                                Iconfont).Width);
                                        gGrid.DrawString(m, Iconfont,
                                                         OverlayBrush,
                                                         x + _itemwidth / 2 - (txtOffline / 2),
                                                         y + _itemheight / 2);
                                    }
                                    if (cw.Camobject != null)
                                    {
                                        
                                        gGrid.FillRectangle(_bOverlay, r.X, r.Y + r.Height - 20, r.Width, 20);
                                        gGrid.DrawString(cw.Camobject.name, Drawfont, OverlayBrush,
                                                         r.X + 5,
                                                         r.Y + r.Height - 16);

                                        
                                    }
                                }
                                else
                                {
                                    gvc.ObjectIDs.Remove(gvc.ObjectIDs[gvc.CurrentIndex]);
                                }
                                break;
                            case 3:
                                var fp = MainClass.GetFloorPlan(obj.ObjectID);
                                if (fp != null)
                                {
                                    if (fp.Fpobject != null && fp.ImgPlan != null)
                                    {
                                        var bmp = fp.ImgView;
                                        if (!Cg.Fill)
                                        {
                                            rFeed = GetArea(x, y, _itemwidth, _itemheight, bmp.Width, bmp.Height);
                                        }
                                        gGrid.DrawImage(bmp, rFeed);
                                        gGrid.FillRectangle(_bOverlay, r.X, r.Y + r.Height - 20, r.Width, 20);
                                        gGrid.DrawString(fp.Fpobject.name, Drawfont, OverlayBrush,
                                                         r.X + 5,
                                                         r.Y + r.Height - 16);
                                    }

                                }
                                else
                                {
                                    gvc.ObjectIDs.Remove(gvc.ObjectIDs[gvc.CurrentIndex]);
                                }
                                break;
                        }
                    }

                    
                    if (_overControlIndex == ind)
                    {
                            

                        if (gvc != null && gvc.ObjectIDs.Count != 0)
                        {
                                

                            gGrid.FillRectangle(_bOverlay, r.X, oy, r.Width, 18);
                            if (Cg.ModeIndex==0)
                                gGrid.DrawString("Add", Drawfont, OverlayBrush, r.X + 2, oy + 2);

                            gGrid.DrawString("Hold", Drawfont, gvc.Hold?IconBrushActive:OverlayBrush, r.X + 30, oy + 2);

                            if (Cg.ModeIndex == 0)
                                gGrid.DrawString("Next", Drawfont, OverlayBrush, r.X + 60, oy + 2);

                            switch (gvc.ObjectIDs[gvc.CurrentIndex].TypeID)
                            {
                                case 1:
                                    var vl = MainClass.GetVolumeLevel(gvc.ObjectIDs[gvc.CurrentIndex].ObjectID);
                                    if (vl != null)
                                    {
                                        gGrid.FillRectangle(_bOverlay, r.X, oy - 18, r.Width, 18);
                                        if (vl.Micobject.settings.active)
                                        {
                                            gGrid.DrawString("Listen", Drawfont, vl.Listening ? IconBrushActive : OverlayBrush, r.X + 90, oy + 2);

                                            gGrid.DrawString("Off", Drawfont, OverlayBrush, r.X + 2,
                                                oy - 18);

                                            gGrid.DrawString("Edit", Drawfont, OverlayBrush, r.X + 30,
                                                oy - 18);
                                            gGrid.DrawString("Rec", Drawfont, vl.Recording ? IconBrushActive : OverlayBrush, r.X + 60,
                                                oy - 18);
                                        }
                                        else
                                        {
                                            gGrid.DrawString("On", Drawfont, OverlayBrush, r.X + 2,
                                                oy - 18);
                                        }
                                        gGrid.DrawString("Files", Drawfont, OverlayBrush, r.X + 90, oy - 18);
                                    }
                                    break;
                                case 2:
                                    var cw = MainClass.GetCameraWindow(gvc.ObjectIDs[gvc.CurrentIndex].ObjectID);
                                    if (cw != null)
                                    {
                                        gGrid.FillRectangle(_bOverlay, r.X, oy - 18, r.Width, 18);
                                        if (cw.Camobject.settings.active)
                                        {
                                            gGrid.DrawString("Talk", Drawfont, cw.Talking ? IconBrushActive : OverlayBrush, r.X + 90, oy + 2);
                                            if (cw.VolumeControl!=null)
                                                gGrid.DrawString("Listen", Drawfont, cw.Listening ? IconBrushActive : OverlayBrush, r.X + 120, oy + 2);

                                            gGrid.DrawString("Off", Drawfont, OverlayBrush, r.X + 2,
                                                oy - 18);

                                            gGrid.DrawString("Edit", Drawfont, OverlayBrush, r.X + 30,
                                                oy - 18);
                                            gGrid.DrawString("Rec", Drawfont, cw.Recording ? IconBrushActive : OverlayBrush, r.X + 60,
                                                oy - 18);
                                            gGrid.DrawString("Snap", Drawfont, OverlayBrush, r.X + 90,
                                                oy - 18);
                                        }
                                        else
                                        {
                                            gGrid.DrawString("On", Drawfont, OverlayBrush, r.X + 2,
                                                oy - 18);
                                        }
                                        gGrid.DrawString("Files", Drawfont, OverlayBrush, r.X + 120, oy - 18);
                                        
                                    }
                                    break;
                                case 3:
                                    var fp = MainClass.GetFloorPlan(gvc.ObjectIDs[gvc.CurrentIndex].ObjectID);
                                    if (fp != null)
                                    {
                                        gGrid.FillRectangle(_bOverlay, r.X, oy - 18, r.Width, 18);
                                        gGrid.DrawString("Edit", Drawfont, OverlayBrush, r.X + 2,
                                                oy - 18);
                                    }
                                    break;
                            }
                                
                        }
                        else
                        {
                            if (Cg.ModeIndex == 0)
                            {
                                gGrid.FillRectangle(_bOverlay, r.X, oy, r.Width, 18);
                                gGrid.DrawString("Add", Drawfont, OverlayBrush, r.X + 2, oy + 2);
                            }
                        }
                    }


                    ind++;
                    j++;
                    if (j == cols)
                    {
                        j = 0;
                        k++;
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private Rectangle GetArea(int x, int y, int contW, int contH, int imageW, int imageH)
        {
            if (Height > 0 && Width > 0)
            {
                double arw = Convert.ToDouble(contW) / Convert.ToDouble(imageW);
                double arh = Convert.ToDouble(contH) / Convert.ToDouble(imageH);
                int w;
                int h;
                if (arh <= arw)
                {
                    w = Convert.ToInt32(((Convert.ToDouble(contW) * arh) / arw));
                    h = contH;
                }
                else
                {
                    w = contW;
                    h = Convert.ToInt32((Convert.ToDouble(contH) * arw) / arh);
                }
                int x2 = x+((contW - w) / 2);
                int y2 = y+((contH - h) / 2);
                return new Rectangle(x2, y2, w, h);
            }
            return Rectangle.Empty;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var row = -1;
            var col = -1;
            var x = e.Location.X;
            var y = e.Location.Y;

            for (var i = 1; i <= _cols; i++)
            {
                if (i * (_itemwidth + Itempadding) - Itempadding / 2 > x)
                {
                    col = i - 1;
                    break;
                }
            }
            for (var i = 1; i <= _rows; i++)
            {
                if (i * (_itemheight + Itempadding) - Itempadding / 2 > y)
                {
                    row = i - 1;
                    break;
                }
            }

            if (row != -1 && col != -1)
            {
                var io = row*_cols + col;
                _overControlIndex = io;
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _overControlIndex = -1;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Select();

            if (e.Button==MouseButtons.Left)
            {
                var row = -1;
                var col = -1;
                var x = e.Location.X;
                var y = e.Location.Y;
                int io = 0;

                GridViewConfig cgv = null;
                if (_maximised != null)
                {
                    cgv = _maximised;
                    row = 0;
                    col = 0;
                    int j = 0;
                    foreach (var obj in _controls)
                    {
                        if (obj!=null && obj.Equals(cgv))
                        {
                            io = j;
                            break;
                        }
                        j++;
                    }
                }
                else
                {

                    for (var i = 1; i <= _cols; i++)
                    {
                        if (i*(_itemwidth + Itempadding) - Itempadding/2 > x)
                        {
                            col = i - 1;
                            break;
                        }
                    }
                    for (var i = 1; i <= _rows; i++)
                    {
                        if (i*(_itemheight + Itempadding) - Itempadding/2 > y)
                        {
                            row = i - 1;
                            break;
                        }
                    }

                    if (row != -1 && col != -1)
                    {
                        cgv = _controls[row*_cols + col];
                        io = row*_cols + col;
                    }

                }

                int rx = col * (_itemwidth + Itempadding);
                int ry = row * (_itemheight + Itempadding);
                int ox = x - rx;

                if ((ry+_itemheight) - y < 38)
                {
                        
                    if (ox < 30)
                    {
                        //E
                        if (Cg.ModeIndex == 0)
                            List(cgv,io);
                    }
                    else
                    {
                        if (cgv != null)
                        {
                            if (ox < 60)
                            {
                                //Pause
                                cgv.Hold = !cgv.Hold;
                            }
                            else
                            {
                                if (ox < 90)
                                {
                                    //Next
                                    if (Cg.ModeIndex == 0)
                                    {
                                        int i = cgv.CurrentIndex + 1;
                                        if (i > cgv.ObjectIDs.Count)
                                            i = 0;
                                        cgv.CurrentIndex = i;
                                        cgv.LastCycle = DateTime.Now;
                                    }
                                }
                                else
                                {

                                    var gv = cgv.ObjectIDs[cgv.CurrentIndex];

                                    switch (gv.TypeID)
                                    {
                                        case 1:
                                            var vl = MainClass.GetVolumeLevel(gv.ObjectID);
                                            if (vl != null)
                                            {  
                                                if (ox < 120)
                                                {
                                                    //listen
                                                    cgv.Hold = true;
                                                    vl.Listening = !vl.Listening;
                                                }
                                            }
                                            break;
                                        case 2:
                                            var cw = MainClass.GetCameraWindow(gv.ObjectID);
                                            if (cw != null)
                                            {

                                                if (ox < 120)
                                                {
                                                    //talk
                                                    cgv.Hold = true;
                                                    cw.Talk(this);
                                                }
                                                else
                                                {
                                                    if (ox < 150)
                                                    {
                                                        //listen
                                                        cgv.Hold = true;
                                                        cw.Listen();
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    if ((ry + _itemheight) - y < 56 && cgv!=null)
                    {
                        var gv = cgv.ObjectIDs[cgv.CurrentIndex];

                        switch (gv.TypeID)
                        {
                            case 1:
                                var vl = MainClass.GetVolumeLevel(gv.ObjectID);
                                if (vl != null)
                                {
                                    if (ox < 30)
                                    {
                                        if (!vl.Micobject.settings.active)
                                            vl.Enable();
                                        else
                                            vl.Disable();
                                        break;
                                    }
                                    
                                    if (vl.IsEnabled)
                                    {
                                        if (ox < 60)
                                        {
                                            MainClass.EditMicrophone(vl.Micobject, this);
                                            break;
                                        }
                                        if (ox < 90)
                                        {
                                            //Rec
                                            vl.RecordSwitch(!vl.Recording);
                                            break;
                                        }
                                    }
                                    if (ox > 90 && ox < 120)
                                    {
                                        //getfiles`
                                        MainClass.ShowFiles(vl);
                                    }
                                }
                                break;
                            case 2:
                                var cw = MainClass.GetCameraWindow(gv.ObjectID);
                                if (cw != null)
                                {
                                    if (ox < 30)
                                    {
                                        if (!cw.Camobject.settings.active)
                                            cw.Enable();
                                        else
                                            cw.Disable();
                                        break;
                                    }
                                    
                                    if (cw.IsEnabled)
                                    {
                                        if (ox < 60)
                                        {
                                            MainClass.EditCamera(cw.Camobject,this);
                                            break;
                                        }
                                        if (ox < 90)
                                        {
                                            //Rec
                                            cw.RecordSwitch(!cw.Recording);
                                            break;
                                        }
                                        
                                        if (ox < 120)
                                        {
                                            string fn = cw.SaveFrame();
                                            if (fn != "")
                                                MainForm.OpenUrl(fn);
                                            break;
                                        }
                                    }
                                    if (ox > 120 && ox < 150)
                                    {
                                        MainClass.ShowFiles(cw);
                                    }
                                }
                                break;
                            case 3:
                                var fp = MainClass.GetFloorPlan(gv.ObjectID);
                                if (fp != null)
                                {
                                    if (ox < 30)
                                    {
                                         MainClass.EditFloorplan(fp.Fpobject, this);
                                    }
                                }
                                break;
                        }
                        
                            
                    }
                }
                    
            }

        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            if (_maximised!=null)
            {
                _maximised = null;
                return;
            }

            var row = -1;
            var col = -1;
            var x = e.Location.X;
            var y = e.Location.Y;

            for (var i = 1; i <= _cols; i++)
            {
                if (i * (_itemwidth + Itempadding) - Itempadding / 2 > x)
                {
                    col = i - 1;
                    break;
                }
            }
            for (var i = 1; i <= _rows; i++)
            {
                if (i * (_itemheight + Itempadding) - Itempadding / 2 > y)
                {
                    row = i - 1;
                    break;
                }
            }

            if (row != -1 && col != -1)
            {
                var io = row * _cols + col;
                int ry = row * (_itemheight + Itempadding);
                
                if ((ry + _itemheight) - y > 38)
                {
                    //only maximise if clicking above buttons
                    if (_controls[io].CurrentIndex > -1)
                        _maximised = _controls[io];
                }

                
            }
        }

        protected override void  OnMouseWheel(MouseEventArgs e)
        {
            var row = -1;
            var col = -1;
            var x = e.Location.X;
            var y = e.Location.Y;
            int io = 0;

            GridViewConfig cgv = null;
            if (_maximised != null)
            {
                cgv = _maximised;
                row = 0;
                col = 0;
                int j = 0;
                foreach (var obj in _controls)
                {
                    if (obj != null && obj.Equals(cgv))
                    {
                        io = j;
                        break;
                    }
                    j++;
                }
            }
            else
            {

                for (var i = 1; i <= _cols; i++)
                {
                    if (i * (_itemwidth + Itempadding) - Itempadding / 2 > x)
                    {
                        col = i - 1;
                        break;
                    }
                }
                for (var i = 1; i <= _rows; i++)
                {
                    if (i * (_itemheight + Itempadding) - Itempadding / 2 > y)
                    {
                        row = i - 1;
                        break;
                    }
                }

                if (row != -1 && col != -1)
                {
                    cgv = _controls[row * _cols + col];
                    io = row * _cols + col;
                }

            }
            if (cgv != null)
            {
                var gv = cgv.ObjectIDs[cgv.CurrentIndex];

                if (gv.TypeID != 2)
                {
                    return;
                }
                var cameraControl = MainClass.GetCameraWindow(gv.ObjectID);
                cameraControl.PTZNavigate = false;
                if (cameraControl.PTZ != null)
                {
                    cgv.Hold = true;

                    if (!cameraControl.PTZ.DigitalZoom)
                    {
                        cameraControl.Calibrating = true;
                        cameraControl.PTZ.SendPTZCommand(
                            e.Delta > 0 ? Enums.PtzCommand.ZoomIn : Enums.PtzCommand.ZoomOut,
                            true);
                        if (cameraControl.PTZ.IsContinuous)
                            cameraControl.PTZ.SendPTZCommand(Enums.PtzCommand.Stop);
                    }
                    else
                    {
                        Rectangle r = cameraControl.Camera.ViewRectangle;
                        //map location to point in the view rectangle

                        var pCell = new Point(col*(_itemwidth + Itempadding), row*(_itemheight + Itempadding));

                        var ox =
                            Convert.ToInt32((Convert.ToDouble(e.Location.X-pCell.X)/Convert.ToDouble(_itemwidth))*
                                            Convert.ToDouble(r.Width));
                        var oy =
                            Convert.ToInt32((Convert.ToDouble(e.Location.Y - pCell.Y) / Convert.ToDouble(_itemheight)) *
                                            Convert.ToDouble(r.Height));

                        cameraControl.Camera.ZPoint = new Point(r.Left + ox, r.Top + oy);
                        var f = cameraControl.Camera.ZFactor;
                        if (e.Delta > 0)
                        {
                            f += 0.2f;
                        }
                        else
                            f -= 0.2f;
                        if (f < 1)
                            f = 1;
                        cameraControl.Camera.ZFactor = f;
                    }
                    ((HandledMouseEventArgs) e).Handled = true;

                }
            }

        }
        
        private void List(GridViewConfig cgv, int io)
        {
            var gvc = new GridViewCamera();
            if (cgv != null)
            {
                gvc.Delay = cgv.Delay;
                gvc.SelectedIDs = cgv.ObjectIDs;
            }
            else
            {
                gvc.SelectedIDs = new List<GridItem>();
            }
            if (gvc.ShowDialog(this) == DialogResult.OK)
            {
                cgv = gvc.SelectedIDs.Count > 0 ? new GridViewConfig(gvc.SelectedIDs, gvc.Delay) : null;

                if (Cg != null)
                {
                    var gi = Cg.GridItem.FirstOrDefault(p => p.GridIndex == io);
                    if (gi == null)
                    {
                        gi = new configurationGridGridItem { CycleDelay = gvc.Delay, GridIndex = io };
                        var lgi = Cg.GridItem.ToList();
                        lgi.Add(gi);
                        Cg.GridItem = lgi.ToArray();
                    }

                    gi.CycleDelay = gvc.Delay;

                    var l = new List<configurationGridGridItemItem>();
                    foreach (var i in gvc.SelectedIDs)
                    {
                        l.Add(new configurationGridGridItemItem { ObjectID = i.ObjectID, TypeID = i.TypeID });
                    }

                    gi.Item = l.ToArray();
                }
                _controls[io] = cgv;
                Invalidate();
            }
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // GridView
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.MinimumSize = new System.Drawing.Size(160, 120);
            this.Size = new System.Drawing.Size(160, 120);
            this.ResumeLayout(false);
        }

        #endregion


        private class GridViewConfig
        {
            public readonly int Delay;
            public readonly List<GridItem> ObjectIDs;
            public DateTime LastCycle;
            public int CurrentIndex;
            public bool Hold;
            public GridViewConfig(List<GridItem> objectIDs, int delay)
            {
                ObjectIDs = objectIDs;
                Delay = delay;
                LastCycle = DateTime.Now;
                Hold = false;
            }
        }
   }


}