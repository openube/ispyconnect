using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using iSpyApplication.Audio.streams;
using iSpyApplication.Audio.talk;
using iSpyApplication.Controls;
using NAudio.Wave;

namespace iSpyApplication.Audio
{
    static class SpeechSynth
    {
        public static void Say(string text,CameraWindow cw)
        {
            var t = new Thread(() => SynthToCam(Uri.UnescapeDataString(text), cw));
            t.Start();
        }
        private static void SynthToCam(string text, CameraWindow cw)
        {
            var synthFormat = new System.Speech.AudioFormat.SpeechAudioFormatInfo(System.Speech.AudioFormat.EncodingFormat.Pcm, 11025, 16, 1, 22100, 2, null);
            using (var synthesizer = new SpeechSynthesizer())
            {
                using (var waveStream = new MemoryStream())
                {

                    //write some silence to the stream to allow camera to initialise properly
                    var silence = new byte[1 * 22050];
                    waveStream.Write(silence, 0, silence.Count());

                    var pbuilder = new PromptBuilder();
                    var pStyle = new PromptStyle
                    {
                        Emphasis = PromptEmphasis.Strong,
                        Rate = PromptRate.Slow,
                        Volume = PromptVolume.ExtraLoud
                    };

                    pbuilder.StartStyle(pStyle);
                    pbuilder.StartParagraph();
                    pbuilder.StartVoice(VoiceGender.Male, VoiceAge.Adult, 2);
                    pbuilder.StartSentence();
                    pbuilder.AppendText(text);
                    pbuilder.EndSentence();
                    pbuilder.EndVoice();
                    pbuilder.EndParagraph();
                    pbuilder.EndStyle();

                    synthesizer.SetOutputToAudioStream(waveStream, synthFormat);
                    synthesizer.Speak(pbuilder);
                    synthesizer.SetOutputToNull();

                    //write some silence to the stream to allow camera to end properly
                    waveStream.Write(silence, 0, silence.Count());

                    waveStream.Seek(0, SeekOrigin.Begin);

                    ITalkTarget talkTarget = null;

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
}
