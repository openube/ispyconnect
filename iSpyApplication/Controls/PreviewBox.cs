using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using iSpyApplication.Properties;
using iSpyApplication.Video;

namespace iSpyApplication
{
    class PreviewBox: AForge.Controls.PictureBox
    {
        public bool Selected;
        public string FileName = "";
        public DateTime CreatedDate = DateTime.MinValue;
        public int Duration;
        private bool _linkPlay;

        protected override void Dispose(bool disposing)
        {
            if (this.Image != null)
            {
                this.Image.Dispose();
                this.Image = null;
            }

            base.Dispose(disposing);
        }
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            var brush = new SolidBrush(Color.White);
            var bSel = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
            Color c = Color.FromArgb(255, 78,78,78);
            if (_linkPlay)
                c = Color.FromArgb(255, 41, 176, 211);
            var bPlay = new SolidBrush(c);
            var g = pe.Graphics;
            
            
            g.FillRectangle(bPlay,0,Height-20,Width,20);
            
            if (Selected)
            {
                g.DrawImage(Resources.checkbox, Width - 18, Height-35, 16, 14);
            }

            if (_linkPlay)
                g.DrawString(">", MainForm.Drawfont, brush, Width/2-8, Height - 18);
            else
                g.DrawString(CreatedDate.Hour + ":" + ZeroPad(CreatedDate.Minute) + " (" + RecordTime(Duration) + ")", MainForm.Drawfont, brush, 2, Height - 18);

            bSel.Dispose();
            bPlay.Dispose();
            brush.Dispose();

        }
        private static string RecordTime(decimal sec)
        {
            var hr = Math.Floor(sec / 3600);
            var min = Math.Floor((sec - (hr * 3600)) / 60);
            sec -= ((hr * 3600) + (min * 60));
            string m = min.ToString();
            string s = sec.ToString();
            while (m.Length < 2) { m = "0" + m; }
            while (s.Length < 2) { s = "0" + s; }
            string h = (hr!=0) ? hr + ":" : "";
            return h + m + ':' + s;
        }
        private static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString();
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool last = _linkPlay;
            _linkPlay = e.Location.Y > Height - 20;
            if (last!=_linkPlay)
                Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _linkPlay = false;
            Invalidate();
        }

        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            PlayMedia(MainForm.Conf.PlaybackMode);
        }

        protected override void  OnMouseClick(MouseEventArgs e)
        {
 	        base.OnMouseClick(e);
            if (e.Button == MouseButtons.Left)
            {
                if (e.Y > Height - 20)
                {
                    PlayMedia(MainForm.Conf.PlaybackMode);
                }
                else
                {
                    Selected = !Selected;
                    Invalidate();

                    if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        if (TopLevelControl != null)
                            ((MainForm) TopLevelControl).SelectMediaRange(this, (PreviewBox) (Parent.Tag));
                    }
                    else
                    {
                        Parent.Tag = this;
                    }
                }
            }
        }
        public void Upload(bool Public)
        {
            if (!MainForm.Conf.Subscribed)
            {
                MessageBox.Show(this, "Subscribers only");
                return;
            }

            if (FileName.EndsWith(".mp4"))
            {
                string[] parts = FileName.Split('\\');
                string fn = parts[parts.Length - 1];
                int id = Convert.ToInt32(fn.Substring(0, fn.IndexOf('_')));

                MessageBox.Show(this, YouTubeUploader.AddUpload(id, parts[parts.Length-1], Public, "", ""));
            }
            else
            {
                MessageBox.Show(this, "Can only upload MP4 Files.");

            }
        }
        public void PlayMedia(int Mode)
        {
            if (Mode < 0)
                MainForm.Conf.PlaybackMode = 0;
            if (!VlcHelper.VlcInstalled && Mode == 1)
            {
                MessageBox.Show(this, "VLC player is not installed ("+VlcHelper.VMin+" or greater required). Using the web player instead. Install VLC and then see settings to enable ispy local playback.");
                MainForm.Conf.PlaybackMode = Mode = 0;
            }

            //play video          
            
            string movie = FileName;
            int j = Mode;
            if (MainForm.Conf.PlaybackMode == 0 && movie.EndsWith(".avi"))
            {
                if (VlcHelper.VlcInstalled)
                    j = 1;
                else
                {
                    j = 2;
                }
            }
            if (!File.Exists(movie))
            {
                MessageBox.Show(this, "Movie could not be found on disk.");
                return;                
            }
            string[] parts = FileName.Split('\\');
            string fn = parts[parts.Length - 1];
            string id = fn.Substring(0, fn.IndexOf('_'));
            switch(j)
            {
                case 0:
                    string url = MainForm.Webserver + "/MediaViewer.aspx?oid=" + id + "&ot=2&fn=" + fn + "&port=" + MainForm.Conf.ServerPort;
                    if (WsWrapper.WebsiteLive && MainForm.Conf.ServicesEnabled)
                    {
                        MainForm.OpenUrl(url);
                    }
                    else
                    {
                        if (!WsWrapper.WebsiteLive)
                        {
                            MessageBox.Show(this, LocRm.GetString("iSpyDown"));
                        }
                        else
                        {
                            if (TopLevelControl != null)
                                ((MainForm) TopLevelControl).Connect(url, false);
                        }
                    }
                    break;
                case 1:
                    if (TopLevelControl != null)
                        ((MainForm) TopLevelControl).Play(movie,Convert.ToInt32(id));
                    break;
                case 2:
                    try
                    {
                        Process.Start(movie);
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                        MessageBox.Show("Could not find a player for this file. Try using iSpyConnect or install VLC and use that instead ("+ex.Message+")");
                    }
                    break;
            }
        }
        
    }
}
