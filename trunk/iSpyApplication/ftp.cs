using System;
using System.IO;
using System.Linq;
using System.Net;

namespace iSpyApplication
{
    public class AsynchronousFtpUpLoader
    {
        public bool FTP(string server, bool passive, string username, string password, string filename, int counter, byte[] contents, out string error)
        {
            bool failed = false;
            try
            {
                var target = new Uri(server);
                int i = 0;
                filename = filename.Replace("{C}", counter.ToString());

                while (filename.IndexOf("{") != -1 && i < 20)
                {
                    filename = String.Format(System.Globalization.CultureInfo.InvariantCulture, filename, DateTime.Now);
                    i++;
                }

                //try uploading
                //directory tree
                var filepath = filename.Split('/');
                var path = "";
                FtpWebRequest request;
                for (var iDir = 0; iDir < filepath.Length - 1; iDir ++)
                {
                    path += filepath[iDir] + "/";
                    request = (FtpWebRequest)WebRequest.Create(target + path);
                    request.Credentials = new NetworkCredential(username, password);
                    request.Method = WebRequestMethods.Ftp.MakeDirectory;
                    try {request.GetResponse();} catch
                    {
                        //directory exists
                    }
                }

                request = (FtpWebRequest)WebRequest.Create(target + filename);
                request.Credentials = new NetworkCredential(username, password);
                request.UsePassive = passive;
                request.Method = WebRequestMethods.Ftp.UploadFile;


                request.ContentLength = contents.Length;

                Stream requestStream = request.GetRequestStream();
                requestStream.Write(contents, 0, contents.Length);
                requestStream.Close();

                var response = (FtpWebResponse)request.GetResponse();
                if (response != null)
                {
                    if (response.StatusCode != FtpStatusCode.ClosingData)
                    {
                        MainForm.LogErrorToFile("FTP Failed: "+response.StatusDescription);
                        failed = true;
                    }

                    response.Close();
                }
                error = "";
            }
            catch (Exception ex)
            {
                error = ex.Message;
                failed = true;
            }
            return !failed;
        }

        public void FTP(object taskObject)
        {
            var task = (FTPTask) taskObject;
            int i = 0;
            while (task.FileName.IndexOf("{") != -1 && i < 20)
            {
                task.FileName = String.Format(System.Globalization.CultureInfo.InvariantCulture, task.FileName, DateTime.Now);
                i++;
            }
            string error;
            FTP(task.Server, task.UsePassive, task.Username, task.Password, task.FileName,task.Counter, task.Contents, out error);

            if (error!="")
            {
                MainForm.LogErrorToFile(error);
            }
            
            objectsCamera oc = MainForm.Cameras.SingleOrDefault(p => p.id == task.CameraId);
            if (oc != null)
            {
                oc.ftp.ready = true;
            }
        }

    }

    public struct FTPTask
    {
        public int CameraId;
        public byte[] Contents;
        public string FileName;
        public bool IsError;
        public string Password;
        public string Server;
        public bool UsePassive;
        public string Username;
        public int Counter;

        public FTPTask(string server, bool usePassive, string username, string password, string fileName,
                       byte[] contents, int cameraId, int counter)
        {
            Server = server;
            UsePassive = usePassive;
            Username = username;
            Password = password;
            FileName = fileName;
            Contents = contents;
            CameraId = cameraId;
            IsError = false;
            Counter = counter;
        }
    }
}