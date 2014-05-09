using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v2;
using Google.Apis.Drive.v2.Data;
using Google.Apis.Services;
using Google.Apis.Upload;
using File = Google.Apis.Drive.v2.Data.File;

namespace iSpyApplication.Cloud
{
    public static class Drive
    {
        private static DriveService _service;
        private static volatile bool _uploading;

        private static string _refreshToken = "";
        private static List<LookupPair> _lookups = new List<LookupPair>();
        private static List<UploadEntry> _upload = new List<UploadEntry>();
        private static readonly object Lock = new object();
        private static List<UploadEntry> UploadList
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


        public static DriveService Service
        {
            get
            {
                if (_service != null)
                {
                    return _service;
                }
                _refreshToken = MainForm.Conf.GoogleDriveConfig;
                if (!String.IsNullOrEmpty(_refreshToken))
                {
                    var token = new TokenResponse { RefreshToken = _refreshToken };
                    var credential = new UserCredential(new GoogleAuthorizationCodeFlow(
                        new GoogleAuthorizationCodeFlow.Initializer
                        {
                            ClientSecrets = new ClientSecrets
                            {
                                ClientId = "648753488389.apps.googleusercontent.com",
                                ClientSecret = "Guvru7Ug8DrGcOupqEs6fTB1"
                            },
                        }), "user", token);
                    _service = new DriveService(new BaseClientService.Initializer
                                                {
                                                    HttpClientInitializer = credential,
                                                    ApplicationName = "iSpy",
                                                });
                    return _service;
                }
                return null;
            }
        }

        private static CancellationTokenSource _tCancel;

        public static bool Authorise()
        {
            if (_service != null)
            {
                _service.Dispose();
            }
            _service = null;

            try
            {
                if (_tCancel != null)
                    _tCancel.Cancel(true);

                _tCancel = new CancellationTokenSource();
                var t = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets
                    {
                        ClientId = "648753488389.apps.googleusercontent.com",
                        ClientSecret = "Guvru7Ug8DrGcOupqEs6fTB1"
                    },
                    new[] {DriveService.Scope.Drive},
                    "user", _tCancel.Token);

                t.ContinueWith(p =>
                               {
                                   if (!p.IsCompleted || p.IsCanceled || p.IsFaulted) return;
                                   var credential = t.Result;

                                   _service = new DriveService(new BaseClientService.Initializer
                                                               {
                                                                   HttpClientInitializer = credential,
                                                                   ApplicationName = "iSpy",
                                                               });
                                   if (credential != null && credential.Token != null &&
                                       credential.Token.RefreshToken != null)
                                   {

                                       MainForm.Conf.GoogleDriveConfig =
                                           _refreshToken = credential.Token.RefreshToken;
                                   }
                                   _lookups = new List<LookupPair>();
                                   _upload = new List<UploadEntry>();
                               });
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                return false;
            }

            return true;
        }

        private static string GetOrCreateFolder(string path)
        {
            if (!Authorised)
            {
                return "";
            }
            var c = _lookups.FirstOrDefault(p => p.Path == path);
            if (c != null)
                return c.ID;

            string id = "root";
            var l = path.Split('\\');

            var req = Service.Files.List();
            req.Q = "mimeType='application/vnd.google-apps.folder' and trashed=false";
            FileList filelist;
            try
            {
                filelist = req.Execute();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                return "";
            }
            bool first = true;
            foreach (string f in l)
            {
                if (f != "")
                {
                    bool found = false;
                    foreach (var cr in filelist.Items)
                    {
                        if (cr.Title == f && cr.Parents.Count > 0 && (cr.Parents[0].Id == id || (first && Convert.ToBoolean(cr.Parents[0].IsRoot))))
                        {
                            found = true;
                            id = cr.Id;
                            break;
                        }
                    }
                    if (!found)
                    {
                        var body = new File
                                   {
                                       Title = f,
                                       MimeType = "application/vnd.google-apps.folder",
                                       Description = "iSpy Folder",
                                       Parents = new List<ParentReference> { new ParentReference { Id = id } }
                                   };
                        File newFolder = Service.Files.Insert(body).Execute();
                        id = newFolder.Id;
                    }
                    first = false;
                }
            }
            //add id to list
            _lookups.Add(new LookupPair { ID = id, Path = path });
            return id;

        }

        private class LookupPair
        {
            public string ID;
            public string Path;
        }

        private class UploadEntry
        {
            public string SourceFilename;
            public string DestinationPath;
        }

        public static bool Authorised
        {
            get { return Service != null; }
        }

        public static string Upload(string filename, string path)
        {
            if (!Authorised)
            {
                MainForm.LogMessageToFile("Authorise google drive in settings");
                return LocRm.GetString("CloudAddSettings");
            }
            if (UploadList.SingleOrDefault(p => p.SourceFilename == filename) != null)
                return LocRm.GetString("FileInQueue");

            if (UploadList.Count >= CloudGateway.MaxUploadQueue)
                return LocRm.GetString("UploadQueueFull");

            UploadList.Add(new UploadEntry { DestinationPath = "iSpy\\" + path.Replace("/", "\\").Trim('\\'), SourceFilename = filename });
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

            UploadEntry entry;

            try
            {
                var l = UploadList.ToList();
                entry = l[0];//could have been cleared by Authorise
                l.RemoveAt(0);
                UploadList = l.ToList();
            }
            catch
            {
                _uploading = false;
                return;
            }

            FileInfo fi;
            byte[] byteArray;
            try
            {
                fi = new FileInfo(entry.SourceFilename);
                byteArray = System.IO.File.ReadAllBytes(fi.FullName);
            }
            catch
            {
                //file doesn't exist
                Upload(null);
                return;
            }
            var mt = MimeTypes.GetMimeType(fi.Extension);

            var body = new File { Title = fi.Name, Description = "iSpy", MimeType = mt };
            string fid = GetOrCreateFolder(entry.DestinationPath);
            bool retry = fid == "";

            if (!retry)
            {
                var stream = new MemoryStream(byteArray);
                body.Parents = new List<ParentReference> { new ParentReference { Id = fid } }; //id of ispy directory
                var request = Service.Files.Insert(body, stream, mt);
                request.ProgressChanged += RequestProgressChanged;
                try
                {
                    var task = request.UploadAsync();
                    task.ContinueWith(t =>
                                      {
                                          stream.Dispose();
                                          Upload(null);
                                      });
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    //network down? - add it back into the queue and wait for next upload to try again

                    retry = true;
                }
            }

            if (retry)
            {
                UploadList.Add(entry);
                _uploading = false;
            }


        }

        private static void RequestProgressChanged(IUploadProgress obj)
        {
            switch (obj.Status)
            {
                case UploadStatus.Completed:
                    MainForm.LogMessageToFile("Uploaded file to google drive");
                    break;
                case UploadStatus.Failed:
                    if (obj.Exception!=null)
                        MainForm.LogErrorToFile("Upload to google drive failed ("+obj.Exception.Message+")");
                    else
                    {
                        MainForm.LogErrorToFile("Upload to google drive failed");
                    }
                    break;
            }

            if (obj.Exception != null)
            {
                MainForm.LogExceptionToFile(obj.Exception);
            }


        }

    }
}
