using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AForge.Imaging;
using Declarations;
using Declarations.Events;
using NAudio.Wave;
using iSpyApplication.Controls;
using iSpyApplication.Video;

namespace iSpyApplication
{
    public partial class Player : Form
    {
        private FFMPEGStream _mStream;
        public DirectSoundOut WaveOut;
        private readonly string _titleText;

        public int ObjectID = -1;

        private readonly FolderSelectDialog _fbdSaveTo = new FolderSelectDialog()
        {
            Title = "Select a folder to copy the file to"
        };

        private void RenderResources()
        {
            openFolderToolStripMenuItem.Text = LocRm.GetString("OpenLocalFolder");
            saveAsToolStripMenuItem.Text = LocRm.GetString("SaveAs");
        }

         public Player(string titleText)
        {
            
            InitializeComponent();
            RenderResources();
             _titleText = titleText;

        }

        private void Player_Load(object sender, EventArgs e)
        {
            videoPlayback1.Seek += VNavSeek;
            videoPlayback1.VolumeChanged += videoPlayback1_VolumeChanged;
            videoPlayback1.SpeedChanged += VideoPlayback1SpeedChanged;
            videoPlayback1.PlayPause += videoPlayback1_PlayPause;
            Text = _titleText;
        }

        void videoPlayback1_PlayPause(object sender)
        {
            if (_mStream!=null)
            {
                if (_mStream.IsRunning)
                {
                    if (_mStream.IsPaused)
                    {
                        _mStream.Play();
                        videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Playing;
                    }
                    else
                    {
                        _mStream.Pause();
                        videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Paused;
                    }
                }
                else
                {
                    _mStream.Start();
                    _mStream.Play();
                    videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Playing;
                }
            }
        }

        void VideoPlayback1SpeedChanged(object sender, int percent)
        {
            if (_mStream != null)
            {
                _mStream.PlaybackRate = Convert.ToDouble((percent*2))/100.0d;
            }
        }

        void videoPlayback1_VolumeChanged(object sender, int percent)
        {
            if (WaveOut != null)
            {
                _mStream.VolumeProvider.Volume = (float)(Convert.ToDouble(percent*2) / 100.0);
            }
        }

        void VNavSeek(object sender, float percent)
        {
            if (!_mStream.IsRunning)
            {
                _mStream.Start();
                _mStream.Play();
            }
            if (_mStream.IsPaused)
            {
                _mStream.Play();
            }
            _mStream.Seek(percent / 100);

            videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Playing;
        }

        private string _filename = "";

        private delegate void PlayDelegate(string filename);
        public void Play(string filename)
        {
            if (InvokeRequired)
                Invoke(new PlayDelegate(Play), filename);
            else
            {
                if (_mStream != null)
                {
                    _mStream.Stop();
                }

                if (WaveOut != null)
                {
                    WaveOut.Stop();
                    WaveOut.Dispose();
                    WaveOut = null;
                }

                _mStream = new FFMPEGStream(filename);
                _mStream.NewFrame += MStreamNewFrame;
                _mStream.DataAvailable += _mStream_DataAvailable;
                _mStream.LevelChanged += _mStream_LevelChanged;
                _mStream.PlayingFinished += _mStream_PlayingFinished;
                _mStream.RecordingFormat = null;

                _firstFrame = true;

                _filename = filename;
                _mStream.Start();
                _mStream.Play();

                string[] parts = filename.Split('\\');
                string fn = parts[parts.Length - 1];
                FilesFile ff =
                    ((MainForm) Owner).GetCameraWindow(ObjectID).FileList.FirstOrDefault(p => p.Filename.EndsWith(fn));
                videoPlayback1.Init(ff);
                

                videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Playing;
                
                
            }
        }

        void _mStream_PlayingFinished(object sender, AForge.Video.ReasonToFinishPlaying reason)
        {
            videoPlayback1.CurrentState = VideoPlayback.PlaybackState.Stopped;
        }

        void _mStream_LevelChanged(object sender, Audio.LevelChangedEventArgs eventArgs)
        {
            //throw new NotImplementedException();
        }

        void _mStream_DataAvailable(object sender, Audio.DataAvailableEventArgs eventArgs)
        {
            //throw new NotImplementedException();
        }


        private bool _firstFrame = true;
        
        void MStreamNewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {  
            if (eventArgs.Frame != null)
            {
                
                UnmanagedImage umi = UnmanagedImage.FromManagedImage(eventArgs.Frame);
                videoPlayback1.LastFrame = umi.ToManagedImage();

                
            }

            if (_firstFrame)
            {
                videoPlayback1.ResetActivtyGraph();
                videoPlayback1.Duration =  TimeSpan.FromMilliseconds(_mStream.Duration).ToString().Substring(0, 8);
                
                _firstFrame = false;

                if (_mStream.RecordingFormat != null)
                {
                    _mStream.Listening = true;
                    _mStream.WaveOutProvider.BufferLength = _mStream.WaveOutProvider.WaveFormat.AverageBytesPerSecond*2;
                    _mStream.VolumeProvider = new VolumeWaveProvider16New(_mStream.WaveOutProvider);
                    WaveOut = new DirectSoundOut(100);
                    WaveOut.Init(_mStream.VolumeProvider);
                    WaveOut.Play();

                }

            }
            
            videoPlayback1.Time = TimeSpan.FromMilliseconds(_mStream.Time).ToString().Substring(0, 8);

            var pc = Convert.ToDouble(_mStream.Time)/_mStream.Duration;

            var newpos = pc * 100d;
            if (newpos < 0)
                newpos = 0;
            if (newpos > 100)
                newpos = 100;
            videoPlayback1.Value = newpos;
        }

        

        private void Player_FormClosing(object sender, FormClosingEventArgs e)
        {
            try {_mStream.Stop();} catch
            {
            }

            if (WaveOut != null)
            {
                WaveOut.Stop();
                WaveOut.Dispose();
                WaveOut = null;
            }
        }

        private void openFolderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string argument = @"/select, " + _filename;
            Process.Start("explorer.exe", argument);
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var fi = new FileInfo(_filename);

                if (_fbdSaveTo.ShowDialog(Handle))
                {
                    File.Copy(_filename, _fbdSaveTo.FileName + @"\" + fi.Name);
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }
    }
}
