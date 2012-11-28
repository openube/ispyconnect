using System;
using System.Drawing;
using System.Windows.Forms;

namespace iSpyApplication.Controls
{
    public class LayoutPanel:Panel
    {
        public LayoutPanel()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint | 
                ControlStyles.ResizeRedraw | 
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint, true);

            UpdateStyles();
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            if (BrandedImage != null)
            {
                BrandedImage.Left = Width / 2 - BrandedImage.Width / 2;
                BrandedImage.Top = Height / 2 - BrandedImage.Height / 2;
            }
            Invalidate();
            base.OnScroll(se);
        }

        public PictureBox BrandedImage;

        protected override void OnPaint(PaintEventArgs pe)
        {
            if (BrandedImage != null)
            {
                BrandedImage.Left = Width / 2 - BrandedImage.Width / 2;
                BrandedImage.Top = Height / 2 - BrandedImage.Height / 2;
            }

            foreach (Control c in Controls)
            {
                if (c is CameraWindow)
                {
                    var cw = (CameraWindow) c;
                    if (cw.Camobject.settings.micpair > -1 && TopLevelControl!=null)
                    {
                        var vc = ((MainForm)TopLevelControl).GetVolumeLevel(cw.Camobject.settings.micpair);
                        if (vc!=null)
                        {
                            vc.Location = new Point(c.Location.X,c.Location.Y+c.Height);
                            vc.Width = c.Width;
                            vc.Height = 40;
                        }
                    }
                }
            }

            
            base.OnPaint(pe);
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            Invalidate();
            base.OnSizeChanged(e);
        }
   }
}
