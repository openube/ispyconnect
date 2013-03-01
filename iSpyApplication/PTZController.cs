using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using iSpyApplication.Controls;

namespace iSpyApplication
{
    public class PTZController
    {
        private readonly CameraWindow _cameraControl;
        private HttpWebRequest _request;
        const double Arc = Math.PI / 8;
        private string _nextcommand = "";

        public PTZController(CameraWindow cameraControl)
        {
            _cameraControl = cameraControl;
        }

        public void SendPTZDirection(double angle, int repeat)
        {
            for (int i = 0; i < repeat; i++)
            {
                SendPTZDirection(angle);
            }
        }

        public void SendPTZDirection(double angle)
        {
            if (_cameraControl.Camobject.settings.ptzrotate90)
            {
                angle -= (Math.PI/2);
                if (angle < -Math.PI)
                {
                    angle += (2*Math.PI);
                }
            }

            if (_cameraControl.Camobject.settings.ptzflipx)
            {
                if (angle <= 0)
                    angle = -Math.PI - angle;
                else
                    angle = Math.PI - angle;
            }
            if (_cameraControl.Camobject.settings.ptzflipy)
            {
                angle = angle*-1;
            }

            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(q => q.id == _cameraControl.Camobject.ptz);
            if (ptz==null)
                return;
            
            string command = ptz.Commands.Center;
            string diag = "";

            if (angle < Arc && angle > -Arc)
            {
                command = ptz.Commands.Left;

            }
            if (angle >= Arc && angle < 3 * Arc)
            {
                command = ptz.Commands.LeftUp;
                diag = "leftup";
            }
            if (angle >= 3 * Arc && angle < 5 * Arc)
            {
                command = ptz.Commands.Up;
            }
            if (angle >= 5 * Arc && angle < 7 * Arc)
            {
                command = ptz.Commands.RightUp;
                diag = "rightup";
            }
            if (angle >= 7 * Arc || angle < -7 * Arc)
            {
                command = ptz.Commands.Right;
            }
            if (angle <= -5 * Arc && angle > -7 * Arc)
            {
                command = ptz.Commands.RightDown;
                diag = "rightdown";
            }
            if (angle <= -3 * Arc && angle > -5 * Arc)
            {
                command = ptz.Commands.Down;
            }
            if (angle <= -Arc && angle > -3 * Arc)
            {
                command = ptz.Commands.LeftDown;
                diag = "leftdown";
            }

            if (String.IsNullOrEmpty(command)) //some PTZ cameras dont have diagonal controls, this fixes that
            {
                switch (diag)
                {
                    case "leftup":
                        _nextcommand = ptz.Commands.Up;
                        SendPTZCommand(ptz.Commands.Left);
                        break;
                    case "rightup":
                        _nextcommand = ptz.Commands.Up;
                        SendPTZCommand(ptz.Commands.Right);
                        break;
                    case "rightdown":
                        _nextcommand = ptz.Commands.Down;
                        SendPTZCommand(ptz.Commands.Right);
                        break;
                    case "leftdown":
                        _nextcommand = ptz.Commands.Down;
                        SendPTZCommand(ptz.Commands.Left);
                        break;
                }
            }
            else
                SendPTZCommand(command);

        }

        public void SendPTZCommand(Enums.PtzCommand command)
        {
            SendPTZCommand(command,false);
        }

        public void SendPTZCommand(Enums.PtzCommand command, bool wait)
        {
            if (_cameraControl.Camera == null)
                return;
            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(q => q.id == _cameraControl.Camobject.ptz);
            bool d = (ptz == null || ptz.Commands == null);

            if (!d)
            {
                if (command == Enums.PtzCommand.ZoomIn)
                {
                    if (String.IsNullOrEmpty(ptz.Commands.ZoomIn))
                        d = true;
                }
                if (command == Enums.PtzCommand.ZoomOut)
                {
                    if (String.IsNullOrEmpty(ptz.Commands.ZoomOut))
                        d = true;
                }
            }

            if (!d)
            {
                _cameraControl.CalibrateCount = 0;
                _cameraControl.Calibrating = true;

                switch (command)
                {
                    case Enums.PtzCommand.Left:
                        SendPTZDirection(0);
                        break;
                    case Enums.PtzCommand.Upleft:
                        SendPTZDirection(Math.PI/4);
                        break;
                    case Enums.PtzCommand.Up:
                        SendPTZDirection(Math.PI / 2);
                        break;
                    case Enums.PtzCommand.UpRight:
                        SendPTZDirection(3 * Math.PI / 4);
                        break;
                    case Enums.PtzCommand.Right:
                        SendPTZDirection(Math.PI);
                        break;
                    case Enums.PtzCommand.DownRight:
                        SendPTZDirection(-3*Math.PI / 4);
                        break;
                    case Enums.PtzCommand.Down:
                        SendPTZDirection(-Math.PI / 2);
                        break;
                    case Enums.PtzCommand.DownLeft:
                        SendPTZDirection(-Math.PI / 4);
                        break;
                    case Enums.PtzCommand.ZoomIn:
                        SendPTZCommand(ptz.Commands.ZoomIn, wait);
                        break;
                    case Enums.PtzCommand.ZoomOut:
                        SendPTZCommand(ptz.Commands.ZoomOut, wait);
                        break;
                    case Enums.PtzCommand.Center:
                        SendPTZCommand(ptz.Commands.Center, wait);
                        break;
                    case Enums.PtzCommand.Stop:
                        SendPTZCommand(ptz.Commands.Stop, wait);
                        break;
                }
            }
            else
            {
                Rectangle r = _cameraControl.Camera.ViewRectangle;
                if (r != Rectangle.Empty)
                {
                    if (command == Enums.PtzCommand.ZoomOut || command == Enums.PtzCommand.ZoomIn)
                        _cameraControl.Camera.ZPoint = new Point(r.Left + r.Width / 2, r.Top + r.Height / 2);
                    double angle = 0;
                    bool isangle = true;
                    switch (command)
                    {
                        case Enums.PtzCommand.Left:
                            angle = 0;
                            break;
                        case Enums.PtzCommand.Upleft:
                            angle = Math.PI / 4;
                            break;
                        case Enums.PtzCommand.Up:
                            angle = Math.PI / 2;
                            break;
                        case Enums.PtzCommand.UpRight:
                            angle = 3 * Math.PI / 4;
                            break;
                        case Enums.PtzCommand.Right:
                            angle = Math.PI;
                            break;
                        case Enums.PtzCommand.DownRight:
                            angle = -3 * Math.PI / 4;
                            break;
                        case Enums.PtzCommand.Down:
                            angle = -Math.PI / 2;
                            break;
                        case Enums.PtzCommand.DownLeft:
                            angle = -Math.PI / 4;
                            break;
                        case Enums.PtzCommand.ZoomIn:
                            isangle = false;
                            _cameraControl.Camera.ZFactor += 0.2f;
                            break;
                        case Enums.PtzCommand.ZoomOut:
                            isangle = false;
                            _cameraControl.Camera.ZFactor -= 0.2f;
                            if (_cameraControl.Camera.ZFactor < 1)
                                _cameraControl.Camera.ZFactor = 1;
                            break;
                        case Enums.PtzCommand.Center:
                            isangle = false;
                            _cameraControl.Camera.ZFactor = 1;
                            break;

                    }
                    if (isangle)
                    {
                        _cameraControl.Camera.ZPoint.X -= Convert.ToInt32(15 * Math.Cos(angle));
                        _cameraControl.Camera.ZPoint.Y -= Convert.ToInt32(15 * Math.Sin(angle));
                    }

                }
            }
        }
        public void SendPTZCommand(string cmd)
        {
            SendPTZCommand(cmd,false);
        }

        public void SendPTZCommand(string cmd, bool wait)
        {
            if (String.IsNullOrEmpty(cmd))
                return;
            if (_request != null)
            {
                if (!wait)
                    return;
                _request.Abort();
            }
            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(q => q.id == _cameraControl.Camobject.ptz);
            if (ptz == null)
                return;
            Uri uri;
            bool absURL = false;
            string url = _cameraControl.Camobject.settings.videosourcestring;

            if (_cameraControl.Camobject.settings.ptzurlbase.Contains("://"))
            {
                url = _cameraControl.Camobject.settings.ptzurlbase;
                absURL = true;
            }

            if (cmd.Contains("://"))
            {
                url = cmd;
                absURL = true;
            }

            try
            {
                uri = new Uri(url);
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e);
                return;
            }
            if (!absURL)
            {
                url = uri.AbsoluteUri.Replace(uri.PathAndQuery, "/");

                const string s = "http";
                //if (!String.IsNullOrEmpty(ptz.Prefix))
                //    s = ptz.Prefix;
                const int p = 80;
                //if (ptz.Port > 0)
                //    p = ptz.Port;
                
                if (!uri.Scheme.ToLower().StartsWith("http")) //rtsp/mrl replace
                    url = url.Replace(":" + uri.Port + "/", ":" + p + "/");

                url = url.Replace(uri.Scheme + "://", s + "://");              

                url = url.Trim('/');

                if (!cmd.StartsWith("/"))
                {
                    url += _cameraControl.Camobject.settings.ptzurlbase;

                    if (cmd != "")
                    {
                        if (!url.EndsWith("/"))
                        {
                            string ext = "?";
                            if (url.IndexOf("?", StringComparison.Ordinal) != -1)
                                ext = "&";
                            url += ext + cmd;
                        }
                        else
                        {
                            url += cmd;
                        }

                    }
                }
                else
                {
                    url += cmd;
                }
            }
            else
            {
                if (!String.IsNullOrEmpty(cmd))
                {
                    if (!cmd.Contains("://"))
                    {
                        if (!url.EndsWith("/"))
                        {
                            string ext = "?";
                            if (url.IndexOf("?", StringComparison.Ordinal) != -1)
                                ext = "&";
                            url += ext + cmd;
                        }
                        else
                        {
                            url += cmd;
                        }
                    }
                    else
                    {
                        url = cmd;
                    }

                }
            }


            string un = _cameraControl.Camobject.settings.login;
            string pwd = _cameraControl.Camobject.settings.password;
            if (!String.IsNullOrEmpty(_cameraControl.Camobject.settings.ptzusername))
            {
                un = _cameraControl.Camobject.settings.ptzusername;
                pwd = _cameraControl.Camobject.settings.ptzpassword;
            }
            else
            {
                if (_cameraControl.Camobject.settings.login == string.Empty)
                {

                    //get from url
                    if (!String.IsNullOrEmpty(uri.UserInfo))
                    {
                        string[] creds = uri.UserInfo.Split(':');
                        if (creds.Length >= 2)
                        {
                            un = creds[0];
                            pwd = creds[1];
                        }
                    }
                }
            }

            url = url.Replace("[USERNAME]", un);
            url = url.Replace("[PASSWORD]", pwd);
            url = url.Replace("[CHANNEL]", _cameraControl.Camobject.settings.ptzchannel);

            _request = (HttpWebRequest) WebRequest.Create(url);
            _request.Timeout = 5000;
            _request.AllowAutoRedirect = true;
            _request.KeepAlive = true;
            _request.SendChunked = false;
            _request.AllowWriteStreamBuffering = true;
            _request.UserAgent = _cameraControl.Camobject.settings.useragent;
            //
            
            //get credentials
            
            // set login and password

            string authInfo = "";
            if (!String.IsNullOrEmpty(un))
            {
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(un + ":" + pwd));
                _request.Headers["Authorization"] = "Basic " + authInfo;
            }
            
            string ckies = _cameraControl.Camobject.settings.cookies ?? "";
            if (!String.IsNullOrEmpty(ckies))
            {
                ckies = ckies.Replace("[USERNAME]", _cameraControl.Camobject.settings.login);
                ckies = ckies.Replace("[PASSWORD]", _cameraControl.Camobject.settings.password);
                ckies = ckies.Replace("[CHANNEL]", _cameraControl.Camobject.settings.ptzchannel);
                ckies = ckies.Replace("[AUTH]", authInfo);
                var myContainer = new CookieContainer();
                string[] coll = ckies.Split(';');
                foreach (var ckie in coll)
                {
                    if (!String.IsNullOrEmpty(ckie))
                    {
                        string[] nv = ckie.Split('=');
                        if (nv.Length == 2)
                        {
                            var cookie = new Cookie(nv[0].Trim(), nv[1].Trim());
                            myContainer.Add(new Uri(_request.RequestUri.ToString()), cookie);
                        }
                    }
                }
                _request.CookieContainer = myContainer;
            }

            if (ptz.POST)
            {
               
                var i = url.IndexOf("?", StringComparison.Ordinal);
                if (i>-1 && i<url.Length)
                {
                    var encoding = new ASCIIEncoding();
                    string postData = url.Substring(i + 1);
                    byte[] data = encoding.GetBytes(postData);

                    _request.Method = "POST";
                    _request.ContentType = "application/x-www-form-urlencoded";
                    _request.ContentLength = data.Length;

                    using (Stream stream = _request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }    
                }
            }


            var myRequestState = new RequestState {Request = _request};
            _request.BeginGetResponse(FinishPTZRequest, myRequestState);
        }

        private void FinishPTZRequest(IAsyncResult result)
        {
            var myRequestState = (RequestState) result.AsyncState;
            WebRequest myWebRequest = myRequestState.Request;
            // End the Asynchronous request.
            try
            {
                myRequestState.Response = myWebRequest.EndGetResponse(result);
                myRequestState.Response.Close();
            }
            catch(Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            myRequestState.Response = null;
                myRequestState.Request = null;
            
            _request = null;
            if (_nextcommand!="")
            {
                string nc = _nextcommand;
                _nextcommand = "";
                SendPTZCommand(nc);
            }
        }

        #region Nested type: RequestState

        public class RequestState
        {
            // This class stores the request state of the request.
            public WebRequest Request;
            public WebResponse Response;

            public RequestState()
            {
                Request = null;
                Response = null;
            }
        }

        #endregion
    }
}