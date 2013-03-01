using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Declarations;
using Declarations.Events;
using Declarations.Media;
using Declarations.Players;
using Implementation;

namespace iSpyApplication
{
    public partial class Player : Form
    {
        readonly IMediaPlayerFactory _mFactory;
        readonly IDiskPlayer _mPlayer;
        IMedia _mMedia;

        private bool _needsSize = true;
        public int ObjectID = -1;

        private void RenderResources()
        {
            btnPlayPause.Text = LocRm.GetString("Pause");
            btnStop.Text = LocRm.GetString("StopPlayer");
            linkLabel1.Text = LocRm.GetString("OpenLocalFolder");
        }

        public Player()
        {
            InitializeComponent();

            _mFactory = new MediaPlayerFactory();
            _mPlayer = _mFactory.CreatePlayer<IDiskPlayer>();

            _mPlayer.Events.PlayerPositionChanged += EventsPlayerPositionChanged;
            _mPlayer.Events.TimeChanged += EventsTimeChanged;
            _mPlayer.Events.MediaEnded += EventsMediaEnded;
            _mPlayer.Events.PlayerStopped += EventsPlayerStopped;

            _mPlayer.WindowHandle = pnlMovie.Handle;
            trackBar2.Value = _mPlayer.Volume;
            RenderResources();

        }

        private void Player_Load(object sender, EventArgs e)
        {
            UISync.Init(this);
            _mPlayer.MouseInputEnabled = true;
            vNav.Seek += vNav_Seek;
        }

        void vNav_Seek(object sender, float percent)
        {
            if (!_mPlayer.IsPlaying)
            {
                if (_mPlayer.PlayerWillPlay)
                    _mPlayer.Play();
                else
                    Play(_filename);
            }

            _mPlayer.Position = percent / 100;
        }

        private string _filename = "";

        private delegate void PlayDelegate(string filename);
        public void Play(string filename)
        {
            if (InvokeRequired)
                Invoke(new PlayDelegate(Play), filename);
            else
            {
                _needsSize = _filename != filename;
                _filename = filename;
                _mMedia = _mFactory.CreateMedia<IMedia>(filename);
                _mMedia.Events.DurationChanged += EventsDurationChanged;
                _mMedia.Events.StateChanged += EventsStateChanged;
                _mMedia.Events.ParsedChanged += Events_ParsedChanged;
                _mPlayer.Open(_mMedia);
                _mMedia.Parse(true);

                _mPlayer.Play();
                
                string[] parts = filename.Split('\\');
                string fn = parts[parts.Length - 1];
                FilesFile ff =
                    ((MainForm) Owner).GetCameraWindow(ObjectID).FileList.FirstOrDefault(p => p.Filename.EndsWith(fn));
                if (ff!=null)
                    vNav.Render(ff);
            }
        }
        void EventsPlayerStopped(object sender, EventArgs e)
        {
            UISync.Execute(InitControls);
        }

        void EventsMediaEnded(object sender, EventArgs e)
        {
            UISync.Execute(InitControls);
        }

        private void InitControls()
        {
            lblTime.Text = "00:00:00";
            lblDuration.Text = "00:00:00";
        }

        void EventsTimeChanged(object sender, MediaPlayerTimeChanged e)
        {
            UISync.Execute(() => lblTime.Text = TimeSpan.FromMilliseconds(e.NewTime).ToString().Substring(0, 8));
        }

        void EventsPlayerPositionChanged(object sender, MediaPlayerPositionChanged e)
        {
            var newpos = (int) (e.NewPosition*100);
            if (newpos<0)
                newpos = 0;
            if (newpos>100)
                newpos = 100;
            UISync.Execute(() => vNav.Value = newpos);
            if (_needsSize)
            {
                Size sz = _mPlayer.GetVideoSize(0);
                if (sz.Width > 0)
                {
                    if (sz.Width < 320)
                        sz.Width = 320;
                    if (sz.Height < 240)
                        sz.Height = 240;

                    if (Width != sz.Width)
                        UISync.Execute(() => Width = sz.Width);
                    if (Height != sz.Height + tableLayoutPanel1.Height)
                        UISync.Execute(() => Height = sz.Height + tableLayoutPanel1.Height);
                    _needsSize = false;
                }
            }
        }


        void EventsStateChanged(object sender, MediaStateChange e)
        {
            UISync.Execute(() => label1.Text = e.NewState.ToString());
            switch (e.NewState)
            {
                case MediaState.Playing:
                    UISync.Execute(() => btnPlayPause.Text = LocRm.GetString("Pause"));
                    break;
                default:
                    UISync.Execute(() => btnPlayPause.Text = LocRm.GetString("Play"));
                    break;
            }
        }

        void EventsDurationChanged(object sender, MediaDurationChange e)
        {
            UISync.Execute(() => lblDuration.Text = TimeSpan.FromMilliseconds(e.NewDuration).ToString().Substring(0, 8));
        }


        void Events_ParsedChanged(object sender, MediaParseChange e)
        {
            Console.WriteLine(e.Parsed);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            _mPlayer.Volume = trackBar2.Value;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            _mPlayer.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_mPlayer.IsPlaying)
            {
                _mPlayer.Pause();
            }
            else
            {
                if (_mPlayer.PlayerWillPlay)
                    _mPlayer.Play();
                else
                    Play(_filename);
            }
        }

        private class UISync
        {
            private static ISynchronizeInvoke Sync;

            public static void Init(ISynchronizeInvoke sync)
            {
                Sync = sync;
            }

            public static void Execute(Action action)
            {
                try {Sync.BeginInvoke(action, null);}
                catch{}
            }
        }

        private void Player_FormClosing(object sender, FormClosingEventArgs e)
        {
            try {_mPlayer.Stop();} catch
            {
            }
            _mFactory.Dispose();
            _mMedia.Dispose();
            _mPlayer.Dispose();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string argument = @"/select, " + _filename;
            Process.Start("explorer.exe", argument);
        }

        private void tbSpeed_Scroll(object sender, EventArgs e)
        {
            _mPlayer.PlaybackRate = ((float) tbSpeed.Value)/10;
        }
    }
}
