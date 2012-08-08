using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.GData.Client;
using Google.GData.Client.ResumableUpload;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace iSpyApplication
{
    internal static class YouTubeUploader
    {
        private static readonly Queue<UserState> UploadFiles = new Queue<UserState>(40);
        private static Thread _uploader;

        public static string AddUpload(int objectId, string filename, bool @public)
        {
            return AddUpload(objectId, filename, @public, "", "");
        }

        public static string AddUpload(int objectId, string filename, bool @public, string emailOnComplete,
                                       string message)
        {
            if (string.IsNullOrEmpty(MainForm.Conf.YouTubeUsername))
            {
                return LocRm.GetString("YouTubeAddSettings");
            }
            if (UploadFiles.SingleOrDefault(p => p.Filename == filename) != null)
                return LocRm.GetString("YouTubeMovieInQueue");

            if (UploadFiles.Count == 40)
                return LocRm.GetString("YouTubeQueueFull");

            int i = MainForm.Conf.UploadedVideos.IndexOf(filename);
            if (i != -1)
            {
                if (emailOnComplete != "")
                {
                    string cfg = MainForm.Conf.UploadedVideos.Substring(i);
                    string vid = cfg.Substring(cfg.IndexOf("|") + 1);
                    if (vid.IndexOf(",") != -1)
                        vid = vid.Substring(0, vid.IndexOf(","));
                    SendYouTubeMails(emailOnComplete, message, vid);
                    return LocRm.GetString("YouTubUploadedAlreadyNotificationsSent");
                }
                return LocRm.GetString("YouTubUploadedAlready");
            }

            var us = new UserState(objectId, filename, emailOnComplete, message, @public);
            UploadFiles.Enqueue(us);

            if (_uploader == null || !_uploader.IsAlive)
            {
                _uploader = new Thread(Upload) { Name = "YouTube Uploader", IsBackground = false, Priority = ThreadPriority.Normal };
                _uploader.Start();
            }

            return LocRm.GetString("YouTubeMovieAdded");
        }

        private static void Upload()
        {           
            UserState us = UploadFiles.Dequeue();
            Console.WriteLine("youtube: upload " + us.AbsoluteFilePath);

            var settings = new YouTubeRequestSettings("iSpy", MainForm.Conf.YouTubeKey, MainForm.Conf.YouTubeUsername, MainForm.Conf.YouTubePassword);
            var request = new YouTubeRequest(settings);

            var v = new Google.YouTube.Video
                        {
                            Title = "iSpy: " + us.CameraData.name,
                            Description = MainForm.Website+": free open source surveillance software: " +
                                          us.CameraData.description
                        };
            if (us.CameraData == null)
            {
                if (UploadFiles.Count > 0)
                    Upload();
                return;
            }
            v.Keywords = us.CameraData.settings.youtube.tags;
            if (v.Keywords.Trim() == "")
                v.Keywords = "ispyconnect"; //must specify at least one keyword
            v.Tags.Add(new MediaCategory(us.CameraData.settings.youtube.category));
            v.YouTubeEntry.Private = !us.Ispublic;
            v.Media.Categories.Add(new MediaCategory(us.CameraData.settings.youtube.category));
            v.Private = !us.Ispublic;
            v.Author = "iSpyConnect.com - Camera Security Software (open source)";

            if (us.EmailOnComplete != "")
                v.Private = false;

            string contentType = MediaFileSource.GetContentTypeForFileName(us.AbsoluteFilePath);
            v.YouTubeEntry.MediaSource = new MediaFileSource(us.AbsoluteFilePath, contentType);

            // add the upload uri to it
            //var link =
            //    new AtomLink("http://uploads.gdata.youtube.com/resumable/feeds/api/users/" +
            //                 MainForm.Conf.YouTubeAccount + "/uploads") {Rel = ResumableUploader.CreateMediaRelation};
            //v.YouTubeEntry.Links.Add(link);

            bool success = false;
            ((GDataRequestFactory)request.Service.RequestFactory).Timeout = 60 * 60 * 1000;
            Google.YouTube.Video vCreated = null;
            try
            {
                vCreated = request.Upload(v);
                success = true;
            }
            catch (GDataRequestException ex1)
            {
                MainForm.LogErrorToFile("YouTube Uploader: " + ex1.ResponseString+" ("+ex1.Message+")");
                if (ex1.ResponseString=="NoLinkedYouTubeAccount")
                {
                    MainForm.LogMessageToFile(
                        "This is because the Google account you connected has not been linked to YouTube yet. The simplest way to fix it is to simply create a YouTube channel for that account: http://www.youtube.com/create_channel");
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            if (success)
            {
                Console.WriteLine("Uploaded: http://www.youtube.com/watch?v=" + vCreated.VideoId);

                string msg = "YouTube video uploaded: <a href=\"http://www.youtube.com/watch?v=" + vCreated.VideoId + "\">" +
                                vCreated.VideoId + "</a>";
                if (vCreated.Private)
                    msg += " (private)";
                else
                    msg += " (public)";
                MainForm.LogMessageToFile(msg);

                if (us.EmailOnComplete != "" && us.Ispublic)
                {
                    SendYouTubeMails(us.EmailOnComplete, us.Message, vCreated.VideoId);
                }
                //check against most recent uploaded videos
                MainForm.Conf.UploadedVideos += "," + us.AbsoluteFilePath + "|" + vCreated.VideoId;
                if (MainForm.Conf.UploadedVideos.Length > 10000)
                    MainForm.Conf.UploadedVideos = "";
            }
            if (UploadFiles.Count>0)
                Upload();
        }


        private static void SendYouTubeMails(string addresses, string message, string videoid)
        {
            string[] emails = addresses.Split('|');
            foreach (string email in emails)
            {
                string em = email.Trim();
                if (em.IsValidEmail())
                {
                    string body;
                    if (em != MainForm.EmailAddress)
                    {
                        body = LocRm.GetString("YouTubeShareMailBody").Replace("[USERNAME]",
                                                                                MainForm.Conf.WSUsername);
                        body = body.Replace("[EMAIL]", MainForm.EmailAddress);
                        body = body.Replace("[MESSAGE]", message);
                        body = body.Replace("[INFO]", videoid);
                        WsWrapper.SendContent(em,
                                                 LocRm.GetString("YouTubeShareMailSubject").Replace("[EMAIL]",
                                                                                                    MainForm.
                                                                                                        EmailAddress),
                                                 body);
                    }
                    else
                    {
                        body = LocRm.GetString("YouTubeUploadMailBody").Replace("[USERNAME]",
                                                                                 MainForm.Conf.WSUsername);
                        body = body.Replace("[INFO]", videoid);
                        WsWrapper.SendContent(em, LocRm.GetString("YouTubeUploadMailSubject"), body);
                    }
                }
            }
        }

        #region Nested type: UserState

        internal class UserState
        {
            private readonly int _objectid;
            public string EmailOnComplete;
            public bool Ispublic;
            public string Message;
            public string Filename;


            internal UserState(int objectId, string filename, string emailOnComplete, string message,
                               bool @public)
            {
                _objectid = objectId;
                CurrentPosition = 0;
                RetryCounter = 0;
                Filename = filename;
                EmailOnComplete = emailOnComplete;
                Message = message;
                Ispublic = @public;
            }

            internal string AbsoluteFilePath
            {
                get
                {
                    return MainForm.Conf.MediaDirectory + "video\\" + CameraData.directory + "\\" +
                           Filename;
                }
            }

            internal objectsCamera CameraData
            {
                get { return MainForm.Cameras.SingleOrDefault(p => p.id == _objectid); }
            }

            internal long CurrentPosition { get; set; }


            internal string Error { get; set; }

            internal int RetryCounter { get; set; }


            internal string HttpVerb { get; set; }

            internal Uri ResumeUri { get; set; }
        }

        #endregion
    }
}