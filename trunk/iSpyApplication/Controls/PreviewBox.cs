using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using iSpyApplication.Properties;
using iSpyApplication.Video;

namespace iSpyApplication.Controls
{
    public class PreviewBox: AForge.Controls.PictureBox
    {
        private readonly Brush _bPlay = new SolidBrush(Color.FromArgb(90,0,0,0));
        public bool Selected;
        public string FileName = "";
        public string DisplayName;
        public DateTime CreatedDate = DateTime.MinValue;
        public int Duration;
        private bool _linkPlay, _linkHover;
        public bool ShowThumb = true;

        protected override void Dispose(bool disposing)
        {
            if (Image != null)
            {
                Image.Dispose();
            }
            _bPlay.Dispose();

            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);
            var g = pe.Graphics;

            if (ShowThumb)
            {
                if (_linkPlay)
                {
                    g.FillRectangle(_bPlay, 0, 0, Width, Height - 20);
                }
                if (Selected)
                {
                    g.DrawImage(Resources.checkbox, Width - 17, Height - 19, 17, 16);
                }
                else
                {
                    if (_linkHover)
                    {
                        g.DrawImage(Resources.checkbox_off, Width - 17, Height - 19, 17, 16);
                    }
                }

                if (_linkPlay)
                {
                    g.DrawString(">", MainForm.DrawfontBig, Brushes.White, Width/2 - 10, 20);
                }

                g.DrawString(
                    CreatedDate.Hour + ":" + ZeroPad(CreatedDate.Minute) + ":" + ZeroPad(CreatedDate.Second) + " (" +
                    RecordTime(Duration) + ")", MainForm.Drawfont, Brushes.White, 0, Height - 18);
            }
        }
        private static string RecordTime(decimal sec)
        {
            var hr = Math.Floor(sec / 3600);
            var min = Math.Floor((sec - (hr * 3600)) / 60);
            sec -= ((hr * 3600) + (min * 60));
            string m = min.ToString(CultureInfo.InvariantCulture);
            string s = sec.ToString(CultureInfo.InvariantCulture);
            while (m.Length < 2) { m = "0" + m; }
            while (s.Length < 2) { s = "0" + s; }
            string h = (hr!=0) ? hr + ":" : "";
            return h + m + ':' + s;
        }
        private static string ZeroPad(int i)
        {
            if (i < 10)
                return "0" + i;
            return i.ToString(CultureInfo.InvariantCulture);
        }
        
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool last = _linkPlay;
            bool last2 = _linkHover;
            _linkPlay = e.Location.Y < Height - 20;
            _linkHover = e.Location.Y > Height - 20;
            if (last != _linkPlay || last2 != _linkHover)
                Invalidate();
        }
        protected override void OnMouseLeave(EventArgs e)
        {
            _linkPlay = false;
            _linkHover = false;

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
                    Selected = !Selected;
                    Invalidate();

                    if ((ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        if (TopLevelControl != null)
                            ((MainForm)TopLevelControl).SelectMediaRange(this, (PreviewBox)(Parent.Tag));
                    }
                    else
                    {
                        Parent.Tag = this;
                    }
                }
                else
                {
                    PlayMedia(MainForm.Conf.PlaybackMode);
                    
                }
            }
        }
        public void Upload(bool Public)
        {
            if (!MainForm.Conf.Subscribed)
            {
                MessageBox.Show(this, LocRm.GetString("SubscribersOnly"));
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
                MessageBox.Show(this, LocRm.GetString("OnlyUploadMP4Files"));

            }
        }
        public void PlayMedia(int mode)
        {
            if (mode < 0)
                MainForm.Conf.PlaybackMode = 0;
            if (!VlcHelper.VlcInstalled && mode == 1)
            {
                MessageBox.Show(this, LocRm.GetString("VLCNotInstalled"));
                MainForm.Conf.PlaybackMode = mode = 0;
            }

           
            string movie = FileName;
            int j = mode;
            if (MainForm.Conf.PlaybackMode == 0 && movie.EndsWith(".avi"))
            {
                j = 1;
            }
            if (movie.EndsWith(".mp3") || movie.EndsWith(".wav"))
            {
                if (j!=3 && j!=1)
                    j = 2;
            }
            if (!File.Exists(movie))
            {
                MessageBox.Show(this, LocRm.GetString("FileNotFound"));
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
                case 3:
                    if (TopLevelControl != null)
                        ((MainForm)TopLevelControl).Play(movie, Convert.ToInt32(id), DisplayName);
                    break;
                case 2:
                    try
                    {
                        Process.Start(movie);
                    }
                    catch (Exception ex)
                    {
                        MainForm.LogExceptionToFile(ex);
                        MessageBox.Show(LocRm.GetString("NoPlayerForThisFile"));
                    }
                    break;
            }
        }
        
    }
}
