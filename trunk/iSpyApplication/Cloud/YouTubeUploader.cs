using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Google.GData.Client;
using Google.GData.Extensions.MediaRss;
using Google.YouTube;

namespace iSpyApplication.Cloud
{
    internal static class YouTubeUploader
    {
        private static readonly object Lock = new object();
        private static List<UserState> _upload = new List<UserState>();
        private static List<UserState> UploadList
        {
            get
            {
                return _upload;
            }
            set
            {
                lock (Lock)
                {
                    _upload = value;
                }
            }
        } 

        private static volatile bool _uploading;

        public static string Upload(int objectId, string filename)
        {
            if (string.IsNullOrEmpty(MainForm.Conf.YouTubeUsername))
            {
                return LocRm.GetString("YouTubeAddSettings");
            }
            if (UploadList.SingleOrDefault(p => p.Filename == filename) != null)
                return LocRm.GetString("FileInQueue");

            if (UploadList.Count >= CloudGateway.MaxUploadQueue)
                return LocRm.GetString("UploadQueueFull");

            int i = MainForm.Conf.UploadedVideos.IndexOf(filename, StringComparison.Ordinal);
            if (i != -1)
            {
                return LocRm.GetString("AlreadyUploaded");
            }

            var us = new UserState(objectId, filename);
            UploadList.Add(us);

            if (!_uploading)
            {
                _uploading = true;
                ThreadPool.QueueUserWorkItem(Upload, null);
            }

            return LocRm.GetString("AddedToQueue");
        }

        private static void Upload(object state)
        {
            if (UploadList.Count == 0)
            {
                _uploading = false;
                return;
            }

            UserState us;

            try
            {
                var l = UploadList.ToList();
                us = l[0];//could have been cleared by Authorise
                l.RemoveAt(0);
                UploadList = l.ToList();
            }
            catch
            {
                _uploading = false;
                return;
            }


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
                if (UploadList.Count > 0)
                    Upload(null);
                return;
            }
            v.Keywords = us.CameraData.settings.youtube.tags;
            if (v.Keywords.Trim() == "")
                v.Keywords = "ispyconnect"; //must specify at least one keyword
            v.Tags.Add(new MediaCategory(us.CameraData.settings.youtube.category));
            v.YouTubeEntry.Private = !us.CameraData.settings.youtube.@public;
            v.Media.Categories.Add(new MediaCategory(us.CameraData.settings.youtube.category));
            v.Private = !us.CameraData.settings.youtube.@public;
            v.Author = "iSpyConnect.com - Camera Security Software (open source)";

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

                string msg = "YouTube video uploaded: <a href=\"http://www.youtube.com/watch?v=" + vCreated.VideoId + "\">" +
                                vCreated.VideoId + "</a>";
                if (vCreated.Private)
                    msg += " (private)";
                else
                    msg += " (public)";
                MainForm.LogMessageToFile(msg);

                MainForm.Conf.UploadedVideos += "," + us.AbsoluteFilePath + "|" + vCreated.VideoId;
                if (MainForm.Conf.UploadedVideos.Length > 1000)
                    MainForm.Conf.UploadedVideos = "";
            }
            Upload(null);
        }

        #region Nested type: UserState

        internal class UserState
        {
            private readonly int _objectid;
            public string Filename;


            internal UserState(int objectId, string filename)
            {
                _objectid = objectId;
                CurrentPosition = 0;
                RetryCounter = 0;
                Filename = filename;
            }

            internal string AbsoluteFilePath
            {
                get
                {
                    return Helper.GetMediaDirectory(2, _objectid) + "video\\" + CameraData.directory + "\\" +
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