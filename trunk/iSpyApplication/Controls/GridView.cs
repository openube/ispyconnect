using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
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

        internal MainForm _parent;

        private const int Itempadding = 5;
        private const int MaxItems = 36;
        private readonly List<GridViewConfig> _controls;
        private int _itemwidth;
        private int _itemheight;
        private readonly Timer _tmrRefresh;
        public configurationGrid Cg;

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

            _controls = new List<GridViewConfig>(MaxItems);
            for (int i = 0; i < MaxItems; i++)
                _controls.Add(null);

            

            Text = cg.name;

            if (cg.GridItem == null)
                cg.GridItem = new configurationGridGridItem[]{};

            foreach (var o in cg.GridItem)
            {
                if (o.Item != null)
                {
                    var li = new List<int>();
                    foreach (var c in o.Item)
                    {
                        if (c.TypeID == 2)
                            li.Add(c.ObjectID);
                    }
                    if (li.Count == 0)
                        _controls[o.GridIndex] = null;
                    else
                    {
                        _controls[o.GridIndex] = new GridViewConfig(li, o.CycleDelay);
                    }
                }
            }

            _tmrRefresh = new Timer(200);
            _tmrRefresh.Elapsed += TmrRefreshElapsed;
            _tmrRefresh.Start();

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
           
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            Monitor.Enter(this);
            Graphics gGrid = pe.Graphics;
            
            var pline = new Pen(Color.Gray, 2);
            var pAlert = new Pen(Color.Red, 2);
            var bOverlay = new SolidBrush(Color.FromArgb(100, Color.Black));
            try
            {
                Rectangle rc = ClientRectangle;
                _itemwidth = (rc.Width - Cg.Columns * Itempadding) / Cg.Columns;
                _itemheight = (rc.Height - Cg.Rows * Itempadding) / Cg.Rows;

                //draw lines
                for (var i = 0; i < Cg.Columns; i++)
                {
                    var x = (i*(_itemwidth + Itempadding) - Itempadding/2);
                    gGrid.DrawLine(pline, x, 0, x, rc.Height);
                }
                for (var i = 0; i < Cg.Rows; i++)
                {
                    var y = (i*(_itemheight + Itempadding) - Itempadding/2);

                    gGrid.DrawLine(pline, 0, y, rc.Width, y);
                }

                var ind = 0;
                var j = 0;
                var k = 0;
                for (var i = 0; i < Cg.Columns * Cg.Rows; i++)
                {
                    var x = j * (_itemwidth + Itempadding);
                    var y = k * (_itemheight + Itempadding);

                    var gvc = _controls[ind];
                    if (gvc == null || gvc.CameraIDs.Count==0)
                    {
                        gGrid.DrawString("+ Cameras", MainForm.Iconfont, MainForm.OverlayBrush, x + _itemwidth / 2 - 40,
                                         y + _itemheight/2);
                    }
                    else
                    {
                        
                        if ((DateTime.Now -gvc.LastCycle).TotalSeconds>gvc.Delay)
                        {
                            gvc.CurrentIndex++;
                            gvc.LastCycle = DateTime.Now;
                        }
                        if (gvc.CurrentIndex >= gvc.CameraIDs.Count)
                        {
                            gvc.CurrentIndex = 0;
                        }
                        var cw = _parent.GetCameraWindow(gvc.CameraIDs[gvc.CurrentIndex]);
                        if (cw!=null)
                        {
                            if (cw.Camera != null && !cw.Camera.LastFrameNull)
                            {
                                gGrid.DrawImage(cw.Camera.LastFrame, x, y, _itemwidth, _itemheight);
                                if (cw.Alerted)
                                {
                                    gGrid.DrawRectangle(pAlert,x-1,y-1,_itemwidth+2,_itemheight+2);
                                }
                            }
                            else
                            {
                                gGrid.DrawString("Offline", MainForm.Iconfont, MainForm.OverlayBrush, x + _itemwidth / 2 - 40,
                                        y + _itemheight/2);
                            }
                            if (cw.Camobject != null)
                            {
                                gGrid.FillRectangle(bOverlay, x, y + _itemheight - 20, _itemwidth, 20);
                                gGrid.DrawString(cw.Camobject.name, MainForm.Drawfont, MainForm.OverlayBrush, x + 5,
                                                 y + _itemheight - 16);
                            }

                        }
                        else
                        {
                            gvc.CameraIDs.Remove(gvc.CameraIDs[gvc.CurrentIndex]);
                        }
                    }
                    ind ++;
                    j++;
                    if (j==Cg.Columns)
                    {
                        j = 0;
                        k++;
                    }
                }
            }
            catch(Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            Monitor.Exit(this);
            pline.Dispose();
            pAlert.Dispose();
            bOverlay.Dispose();
            base.OnPaint(pe);
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

                for (var i = 1; i <= Cg.Columns; i++)
                {
                    if (i * (_itemwidth + Itempadding) - Itempadding / 2 > x)
                    {
                        col = i - 1;
                        break;
                    }
                }
                for (var i = 1; i <= Cg.Rows; i++)
                {
                    if (i * (_itemheight + Itempadding) - Itempadding / 2 > y)
                    {
                        row = i - 1;
                        break;
                    }
                }

                if (row != -1 && col != -1)
                {
                    var io = row * Cg.Columns + col;
                    var cgv = _controls[io];
                    var gvc = new GridViewCamera();
                    if (cgv!=null)
                    {
                        gvc.Delay = cgv.Delay;
                        gvc.SelectedIDs = cgv.CameraIDs;
                    }
                    else
                    {
                        gvc.SelectedIDs = new List<int>();
                    }
                    if (gvc.ShowDialog(this)==DialogResult.OK)
                    {
                        cgv = gvc.SelectedIDs.Count>0 ? new GridViewConfig(gvc.SelectedIDs,gvc.Delay) : null;

                        if (Cg != null)
                        {
                            var gi = Cg.GridItem.FirstOrDefault(p => p.GridIndex == io);
                            if (gi == null)
                            {
                                gi = new configurationGridGridItem {CycleDelay = gvc.Delay, GridIndex = io};
                                var lgi = Cg.GridItem.ToList();
                                lgi.Add(gi);
                                Cg.GridItem = lgi.ToArray();
                            }

                            gi.CycleDelay = gvc.Delay;

                            var l = new List<configurationGridGridItemItem>();
                            foreach (int i in gvc.SelectedIDs)
                            {
                                l.Add(new configurationGridGridItemItem {ObjectID = i, TypeID = 2});
                            }

                            gi.Item = l.ToArray();
                        }
                        _controls[io] = cgv;
                        Invalidate();
                    }
                }
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
            public readonly List<int> CameraIDs;
            public DateTime LastCycle;
            public int CurrentIndex;
            public GridViewConfig(List<int> cameraIDs, int delay)
            {
                CameraIDs = cameraIDs;
                Delay = delay;
                LastCycle = DateTime.Now;
            }
        }
   }


}