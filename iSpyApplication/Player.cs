using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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
        IMediaPlayerFactory m_factory;
        IDiskPlayer m_player;
        IMedia m_media;

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

            m_factory = new MediaPlayerFactory(false);
            m_player = m_factory.CreatePlayer<IDiskPlayer>();

            m_player.Events.PlayerPositionChanged += new EventHandler<MediaPlayerPositionChanged>(Events_PlayerPositionChanged);
            m_player.Events.TimeChanged += new EventHandler<MediaPlayerTimeChanged>(Events_TimeChanged);
            m_player.Events.MediaEnded += new EventHandler(Events_MediaEnded);
            m_player.Events.PlayerStopped += new EventHandler(Events_PlayerStopped);

            m_player.WindowHandle = pnlMovie.Handle;
            trackBar2.Value = m_player.Volume;
            RenderResources();

        }

        private void Player_Load(object sender, EventArgs e)
        {
            UISync.Init(this);
            m_player.MouseInputEnabled = true;
        }

        private string _filename = "";

        private delegate void PlayDelegate(string Filename);
        public void Play(string Filename)
        {
            if (InvokeRequired)
                Invoke(new PlayDelegate(Play), Filename);
            else
            {
                _filename = Filename;
                m_media = m_factory.CreateMedia<IMedia>(Filename);
                m_media.Events.DurationChanged += new EventHandler<MediaDurationChange>(Events_DurationChanged);
                m_media.Events.StateChanged += new EventHandler<MediaStateChange>(Events_StateChanged);
                m_media.Events.ParsedChanged += new EventHandler<MediaParseChange>(Events_ParsedChanged);
                m_player.Open(m_media);
                m_media.Parse(true);

                m_player.Play();
                _needsSize = true;
                
                string[] parts = Filename.Split('\\');
                string fn = parts[parts.Length - 1];
                FilesFile ff =
                    ((MainForm) Owner).GetCameraWindow(ObjectID).FileList.FirstOrDefault(p => p.Filename.EndsWith(fn));
                if (ff!=null)
                    vNav.Render(ff);
            }
        }
        void Events_PlayerStopped(object sender, EventArgs e)
        {
            UISync.Execute(() => InitControls());
        }

        void Events_MediaEnded(object sender, EventArgs e)
        {
            UISync.Execute(() => InitControls());
        }

        private void InitControls()
        {
            trackBar1.Value = 0;
            lblTime.Text = "00:00:00";
            lblDuration.Text = "00:00:00";
        }

        void Events_TimeChanged(object sender, MediaPlayerTimeChanged e)
        {
            UISync.Execute(() => lblTime.Text = TimeSpan.FromMilliseconds(e.NewTime).ToString().Substring(0, 8));
        }

        void Events_PlayerPositionChanged(object sender, MediaPlayerPositionChanged e)
        {
            var newpos = (int) (e.NewPosition*100);
            if (newpos<0)
                newpos = 0;
            if (newpos>100)
                newpos = 100;
            UISync.Execute(() => trackBar1.Value = newpos);
            if (_needsSize)
            {
                Size sz = m_player.GetVideoSize(0);
                if (sz.Width > 0)
                {
                    if (sz.Width < 320)
                        sz.Width = 320;
                    if (sz.Height < 240)
                        sz.Height = 240;

                    if (this.Width != sz.Width)
                        UISync.Execute(() => this.Width = sz.Width);
                    if (this.Height != sz.Height + tableLayoutPanel1.Height)
                        UISync.Execute(() => this.Height = sz.Height + tableLayoutPanel1.Height);
                    _needsSize = false;
                }
            }
        }


        void Events_StateChanged(object sender, MediaStateChange e)
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

        void Events_DurationChanged(object sender, MediaDurationChange e)
        {
            UISync.Execute(() => lblDuration.Text = TimeSpan.FromMilliseconds(e.NewDuration).ToString().Substring(0, 8));
        }


        void Events_ParsedChanged(object sender, MediaParseChange e)
        {
            Console.WriteLine(e.Parsed);
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            m_player.Volume = trackBar2.Value;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            m_player.Position = (float)trackBar1.Value / 100;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            m_player.Stop();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (m_player.IsPlaying)
            {
                m_player.Pause();
            }
            else
            {
                if (m_player.PlayerWillPlay)
                    m_player.Play();
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
            try {m_player.Stop();} catch
            {
            }
            m_factory.Dispose();
            m_media.Dispose();
            m_player.Dispose();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            string argument = @"/select, " + _filename;
            Process.Start("explorer.exe", argument);
        }

        private void tbSpeed_Scroll(object sender, EventArgs e)
        {
            m_player.PlaybackRate = ((float) tbSpeed.Value)/10;
        }
    }
}
