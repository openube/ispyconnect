using System.IO;
using System.Linq;
using System.Threading;
using iSpyApplication.Audio.streams;
using iSpyApplication.Audio.talk;
using iSpyApplication.Controls;
using NAudio.Wave;

namespace iSpyApplication.Audio
{
    static class AudioSynth
    {
        public static void Play(string fileName,CameraWindow cw)
        {
            var t = new Thread(() => SynthToCam(fileName, cw));
            t.Start();
        }
        private static void SynthToCam(string fileName, CameraWindow cw)
        {
            using (var waveStream = new MemoryStream())
            {

                //write some silence to the stream to allow camera to initialise properly
                var silence = new byte[1 * 22050];
                waveStream.Write(silence, 0, silence.Count());

                //read in and convert the wave stream into our format
                using (var reader = new WaveFileReader(fileName))
                {
                    var newFormat = new WaveFormat(11025, 16, 1);
                    byte[] buff = new byte[22050];
                    
                    using (var conversionStream = new WaveFormatConversionStream(newFormat, reader))
                    {
                        do
                        {
                            int i = conversionStream.Read(buff, 0, 22050);
                            waveStream.Write(buff, 0, i);
                            if (i < 22050)
                                break;
                        } while (true);
                    }
                }


                //write some silence to the stream to allow camera to end properly
                waveStream.Write(silence, 0, silence.Count());

                waveStream.Seek(0, SeekOrigin.Begin);

                ITalkTarget talkTarget;

                var ds = new DirectStream(waveStream) { RecordingFormat = new WaveFormat(11025, 16, 1) };
                switch (cw.Camobject.settings.audiomodel)
                {
                    case "Foscam":
                        ds.Interval = 40;
                        ds.PacketSize = 882; // (40ms packet at 22050 bytes per second)
                        talkTarget = new TalkFoscam(cw.Camobject.settings.audioip, cw.Camobject.settings.audioport,
                                                    cw.Camobject.settings.audiousername,
                                                    cw.Camobject.settings.audiopassword, ds);
                        break;
                    case "NetworkKinect":
                        ds.Interval = 40;
                        ds.PacketSize = 882;
                        talkTarget = new TalkNetworkKinect(cw.Camobject.settings.audioip, cw.Camobject.settings.audioport, ds);
                        break;
                    case "iSpyServer":
                        ds.Interval = 40;
                        ds.PacketSize = 882;
                        talkTarget = new TalkiSpyServer(cw.Camobject.settings.audioip,
                                                        cw.Camobject.settings.audioport,
                                                        ds);
                        break;
                    case "Axis":
                        talkTarget = new TalkAxis(cw.Camobject.settings.audioip, cw.Camobject.settings.audioport,
                                                    cw.Camobject.settings.audiousername,
                                                    cw.Camobject.settings.audiopassword, ds);
                        break;
                    default:
                        //local playback
                        talkTarget = new TalkLocal(ds);

                        break;
                }
                ds.Start();
                talkTarget.Start();
                while (ds.IsRunning)
                {
                    Thread.Sleep(100);
                }
                ds.Stop();
                if (talkTarget != null)
                    talkTarget.Stop();
                talkTarget = null;
                ds = null;

                waveStream.Close();
            }


        }
    }
}
