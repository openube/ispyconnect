using System;
using System.Drawing;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public partial class VideoNavigator : UserControl
    {
        private bool _mouseDown;
        private FilesFile _ff;
        public event SeekEventHandler Seek;
        public delegate void SeekEventHandler(object sender, float percent);

        public VideoNavigator()
        {
            InitializeComponent();
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);
            Margin = new Padding(0, 0, 0, 0);
            Padding = new Padding(0, 0, 5, 5);
            BorderStyle = BorderStyle.None;

            ResizeRedraw = true;
        }

        private int _value;
        public int Value
        {
            get { return _value; }
            set { _value = value;
                Invalidate();
            }
        }

        private void VideoNavigator_Load(object sender, EventArgs e)
        {
            
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (_ff != null)
            {
                var datapoints = _ff.AlertData.Split(',');
                if (datapoints.Length>0)
                {
                    var pxStep = ((float)Width)/datapoints.Length;
                    float cx = 0;
                    var pAlarm = new Pen(Color.Red);
                    var pOk = new Pen(Color.Black);
                    var trigger = (float) _ff.TriggerLevel;
                    pe.Graphics.Clear(Color.White);
                    var h = (float) Height;
                    for(int i=0;i<datapoints.Length;i++)
                    {
                        float d;
                        if (float.TryParse(datapoints[i], out d))
                        {
                            if (d > trigger)
                            {
                                pe.Graphics.DrawLine(pAlarm, cx, h, cx, h - (d * (h / 100)));
                            }
                            else
                                pe.Graphics.DrawLine(pOk, cx, h, cx, h - (d * (h / 100)));
                        }
                        cx += pxStep;
                    }
                    pe.Graphics.DrawLine(pOk,0,h/2,Width,h/2);

                    var pxCursor = ((float) Value/100)*Width;
                    Brush bPosition = new SolidBrush(Color.Black);
                    pe.Graphics.FillEllipse(bPosition,pxCursor-4,h/2-4,8,8);
                    
                    pOk.Dispose();
                    pAlarm.Dispose();
                    bPosition.Dispose();
                }
            }

        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_mouseDown)
            {
                var v = (float)e.Location.X;
                var val = (v/Width)*100;
                if (Seek != null) Seek(this, val);
            }
        }

        
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = true;
                var v = (float)e.Location.X;
                var val = (v / Width) * 100;
                if (Seek != null) Seek(this, val);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left)
            {
                _mouseDown = false;
            }
        }

        public void Render(FilesFile FileData)
        {
            _ff = FileData;
            Invalidate();
        }
    }
}
