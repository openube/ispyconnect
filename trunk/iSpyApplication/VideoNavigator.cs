using System;
using System.Drawing;
using System.Windows.Forms;

namespace iSpyApplication
{
    public partial class VideoNavigator : UserControl
    {
        private FilesFile FF = null;
        public VideoNavigator()
        {
            InitializeComponent();
            ResizeRedraw = true;
        }

        private void VideoNavigator_Load(object sender, EventArgs e)
        {
            
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            if (FF != null)
            {
                var datapoints = FF.AlertData.Split(',');
                if (datapoints.Length>0)
                {
                    var pxStep = ((float)Width)/datapoints.Length;
                    float cx = 0;
                    var pAlarm = new Pen(Color.Red);
                    var pOk = new Pen(Color.Black);
                    var trigger = (float) FF.TriggerLevel;
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
                    pOk.Dispose();
                    pAlarm.Dispose();
                }
            }

        }

        public void Render(FilesFile FileData)
        {
            FF = FileData;
            Invalidate();
        }
    }
}
