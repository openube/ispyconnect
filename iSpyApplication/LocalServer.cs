using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Speech.Synthesis;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using iSpyApplication.Audio.streams;
using iSpyApplication.Audio.talk;
using iSpyApplication.Controls;
using iSpyApplication.Properties;
using iSpyApplication.Video;

using ThreadState = System.Threading.ThreadState;
using WaveFormat = NAudio.Wave.WaveFormat;

namespace iSpyApplication
{
    public class RemoteCommandEventArgs : EventArgs
    {
        public string Command;
        public int ObjectId;
        public int ObjectTypeId;

        // Constructor
        public RemoteCommandEventArgs(string command, int objectid, int objecttypeid)
        {
            Command = command;
            ObjectId = objectid;
            ObjectTypeId = objecttypeid;
        }
    }

    public class LocalServer
    {
        //private static readonly List<Socket> MySockets = new List<Socket>();
        private static List<String> _allowedIPs;
        //private static int _socketindex;
        private readonly MainForm _parent;
        public string ServerRoot;
        private Hashtable _mimetypes;
        private TcpListener _myListener;
        public int NumErr;
        private Thread _th;
        
        //The constructor which make the TcpListener start listening on the
        //given port. It also calls a Thread on the method StartListen(). 
        public LocalServer(MainForm parent)
        {
            _parent = parent;
        }

        public Hashtable MimeTypes
        {
            get
            {
                if (_mimetypes == null)
                {
                    _mimetypes = new Hashtable();
                    using (var sr = new StreamReader(ServerRoot + @"data\mime.Dat"))
                    {
                        string sLine;
                        while ((sLine = sr.ReadLine()) != null)
                        {
                            sLine = sLine.Trim();

                            if (sLine.Length > 0)
                            {
                                //find the separator
                                int iStartPos = sLine.IndexOf(";", StringComparison.Ordinal);

                                // Convert to lower case
                                sLine = sLine.ToLower();

                                string sMimeExt = sLine.Substring(0, iStartPos);
                                string sMimeType = sLine.Substring(iStartPos + 1);
                                _mimetypes.Add(sMimeExt, sMimeType);
                            }
                        }
                    }
                }
                return _mimetypes;
            }
        }

        public bool Running
        {
            get
            {
                if (_th == null)
                    return false;
                return _th.IsAlive;
            }
        }

        public string StartServer()
        {
            string message = "";
            try
            {
                if (MainForm.Conf.IPMode=="IPv6")
                {
                    _myListener = new TcpListener(IPAddress.IPv6Any, MainForm.Conf.LANPort) { ExclusiveAddressUse = false };
                     _myListener.AllowNatTraversal(true);
                }
                else
                {
                    _myListener = new TcpListener(IPAddress.Any, MainForm.Conf.LANPort)
                                        {ExclusiveAddressUse = false};
                }
                _myListener.Start(200);
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e);
                StopServer();
                message = "Could not start local iSpy server - please select a different LAN port in settings. The port specified is in use. See the log file for more information.";
            }
            if (message != "")
            {
                MainForm.LogMessageToFile(message);
                return message;
            }
            try 
            {
                //start the thread which calls the method 'StartListen'
                if (_th != null)
                {
                    while (_th.ThreadState == ThreadState.AbortRequested)
                    {
                        Application.DoEvents();
                    }
                }
                _th = new Thread(StartListen);
                _th.Start();
            }
            catch (Exception e)
            {
                message = e.Message;
                MainForm.LogExceptionToFile(e);
            }
            return message;
        }

        public void StopServer()
        {
            if (_connectedSockets != null)
            {
                ClientConnected.Set();
                try
                {
                    foreach (var sock in _connectedSockets.Values)
                    {
                        sock.Close();
                    }
                }
                catch (SocketException ex)
                {
                    //During one socket disconnected we can faced exception
                    MainForm.LogExceptionToFile(ex);
                }
            }

            if (_myListener != null && _myListener.Server!=null)
            {
                var t = new Thread(DoStopServer);
                t.Start();
                t.Join();
                _myListener = null;
            }

            Application.DoEvents();
        }

        private void DoStopServer()
        {
            if (_myListener!=null && _myListener.Server!=null)
                _myListener.Server.Close();
        }

        /// <summary>
        /// This function takes FileName as Input and returns the mime type..
        /// </summary>
        /// <param name="sRequestedFile">To indentify the Mime Type</param>
        /// <returns>Mime Type</returns>
        public string GetMimeType(string sRequestedFile)
        {
            if (sRequestedFile == "")
                return "";
            String sMimeType = "";

            // Convert to lowercase
            sRequestedFile = sRequestedFile.ToLower();

            int iStartPos = sRequestedFile.LastIndexOf(".", StringComparison.Ordinal);
            if (iStartPos == -1)
                return "text/javascript";
            string sFileExt = sRequestedFile.Substring(iStartPos);

            try
            {
                sMimeType = MimeTypes[sFileExt].ToString();
            }
            catch (Exception ex)
            {
                MainForm.LogErrorToFile("No mime type for request " + sRequestedFile+" ("+ex.Message+")");
            }


            return sMimeType;
        }

        public void SendHeader(string sHttpVersion, string sMimeHeader, int iTotBytes, string sStatusCode, int cacheDays,
                               ref Socket socket, bool gZip)
        {
            SendHeader(sHttpVersion, sMimeHeader, iTotBytes, sStatusCode, cacheDays, ref socket, "", gZip);
        }
        public void SendHeader(string sHttpVersion, string sMimeHeader, int iTotBytes, string sStatusCode, int cacheDays,
                               ref Socket socket)
        {
            SendHeader(sHttpVersion, sMimeHeader, iTotBytes, sStatusCode, cacheDays, ref socket, "", false);
        }
        public void SendHeader(string sHttpVersion, string sMimeHeader, int iTotBytes, string sStatusCode, int cacheDays,
                               ref Socket socket, string fileName, bool gZip)
        {
            String sBuffer = "";

            // if Mime type is not provided set default to text/html
            if (sMimeHeader.Length == 0)
            {
                sMimeHeader = "text/html"; // Default Mime Type is text/html
            }

            sBuffer += sHttpVersion + sStatusCode + "\r\n";
            sBuffer += "Server: iSpy\r\n";
            if (fileName!="")
            {
                sBuffer += "Content-Type: application/octet-stream\r\n";
                sBuffer += "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n";
            }
            else
                sBuffer += "Content-Type: " + sMimeHeader + "\r\n";
            //sBuffer += "X-Content-Type-Options: nosniff\r\n";
            sBuffer += "Accept-Ranges: bytes\r\n";
            sBuffer += "Access-Control-Allow-Origin: *\r\n";
            if (iTotBytes > -1)
                sBuffer += "Content-Length: " + iTotBytes + "\r\n";
            if (gZip)
            {
                sBuffer += "Content-Encoding: gzip\r\n";
            }
            //sBuffer += "Cache-Control:Date: Tue, 25 Jan 2011 08:18:53 GMT\r\nExpires: Tue, 08 Feb 2011 05:06:38 GMT\r\nConnection: keep-alive\r\n";
            if (cacheDays > 0)
            {
                //this is needed for video content to work in chrome/android
                DateTime d = DateTime.UtcNow;
                sBuffer += "Cache-Control: Date: " + d.ToUniversalTime().ToString("r") +
                           "\r\nLast-Modified: Tue, 01 Jan 2011 12:00:00 GMT\r\nExpires: " +
                           d.AddDays(cacheDays).ToUniversalTime().ToString("r") + "\r\nConnection: keep-alive\r\n";
            }
            else
            {
                sBuffer +=
                    "Pragma: no-cache\r\nExpires: Fri, 30 Oct 1998 14:19:41 GMT\r\nCache-Control: no-cache, must-revalidate\r\n";
            }


            sBuffer += "\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

            SendToBrowser(bSendData, socket);
        }


        public void SendHeaderWithRange(string sHttpVersion, string sMimeHeader, int iStartBytes, int iEndBytes,
                                        int iTotBytes, string sStatusCode, int cacheDays, Socket socket, string fileName)
        {
            String sBuffer = "";

            // if Mime type is not provided set default to text/html
            if (sMimeHeader.Length == 0)
            {
                sMimeHeader = "text/html"; // Default Mime Type is text/html
            }

            sBuffer += sHttpVersion + sStatusCode + "\r\n";
            sBuffer += "Server: iSpy\r\n";
            if (fileName != "")
            {
                sBuffer += "Content-Type: application/octet-stream\r\n";
                sBuffer += "Content-Disposition: attachment; filename=\"" + fileName + "\"\r\n";
            }
            else
                sBuffer += "Content-Type: " + sMimeHeader + "\r\n";

            //sBuffer += "X-Content-Type-Options: nosniff\r\n";
            sBuffer += "Accept-Ranges: bytes\r\n";
            sBuffer += "Content-Range: bytes " + iStartBytes + "-" + iEndBytes + "/" + (iTotBytes) + "\r\n";
            sBuffer += "Content-Length: " + (iEndBytes - iStartBytes + 1) + "\r\n";
            if (cacheDays > 0)
            {
                //this is needed for video content to work in chrome/android
                DateTime d = DateTime.UtcNow;
                sBuffer += "Cache-Control: Date: " + d.ToUniversalTime().ToString("r") +
                           "\r\nLast-Modified: Tue, 01 Jan 2011 12:00:00 GMT\r\nExpires: " +
                           d.AddDays(cacheDays).ToUniversalTime().ToString("r") + "\r\nConnection: keep-alive\r\n";
            }

            sBuffer += "\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

            SendToBrowser(bSendData, socket);
        }


        /// <summary>
        /// Overloaded Function, takes string, convert to bytes and calls 
        /// overloaded sendToBrowserFunction.
        /// </summary>
        /// <param name="sData">The data to be sent to the browser(client)</param>
        /// <param name="socket">Socket reference</param>
        public void SendToBrowser(String sData, Socket socket)
        {
            SendToBrowser(Encoding.ASCII.GetBytes(sData), socket);
        }


        /// <summary>
        /// Sends data to the browser (client)
        /// </summary>
        /// <param name="bSendData">Byte Array</param>
        /// <param name="socket">Socket reference</param>
        public void SendToBrowser(Byte[] bSendData, Socket socket)
        {
            try
            {
                if (socket.Connected)
                {
                    if (socket.Send(bSendData) == -1)
                        MainForm.LogExceptionToFile(new Exception("Socket Error cannot Send Packet"));
                }
            }
            catch (SocketException)
            {
                //connection error
            }
            catch (Exception e)
            {
                MainForm.LogExceptionToFile(e);
            }
        }

        public bool ThumbnailCallback()
        {
            return false;
        }

        public static AutoResetEvent ClientConnected = new AutoResetEvent(false);
        private Dictionary<IPEndPoint, Socket> _connectedSockets;
        private readonly object _connectedSocketsSyncHandle = new object();

        //This method Accepts new connection and
        //First it receives the welcome massage from the client,
        //Then it sends the Current date time to the Client.
        private void StartListen()
        {
            _connectedSockets = new Dictionary<IPEndPoint, Socket>();
            NumErr = 0;

            while (!MainForm.Reallyclose && NumErr < 5 && _myListener != null)
            {
                try
                {
                    _myListener.BeginAcceptSocket(DoAcceptSocketCallback, _myListener);

                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    break;
                }
                // Wait until a connection is made and processed before  
                // continuing.
                ClientConnected.WaitOne(); // Wait until a client has begun handling an event
                ClientConnected.Reset();
            }
        }

        public void DoAcceptSocketCallback(IAsyncResult ar)
        {
            ClientConnected.Set();

            

            String sRequest;
            String sMyWebServerRoot = ServerRoot;
            String sPhysicalFilePath;

            try
            {
                var listener = (TcpListener) ar.AsyncState;
                Socket mySocket = listener.EndAcceptSocket(ar);

                var endPoint = (IPEndPoint)mySocket.RemoteEndPoint;
                lock (_connectedSocketsSyncHandle)
                {
                    if (_connectedSockets.ContainsKey(endPoint))
                    {
                        _connectedSockets[endPoint].Close();
                    }

                    SetDesiredKeepAlive(mySocket);
                    _connectedSockets[endPoint] = mySocket;
                }


                if (MainForm.Conf.IPMode== "IPv6")
                    mySocket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
                    

                //MySockets.Add(mySocket);
                    
                if (mySocket.Connected)
                {
                    mySocket.NoDelay = true;
                    mySocket.ReceiveBufferSize = 8192;
                    mySocket.ReceiveTimeout = mySocket.SendTimeout = 4000;
                    try
                    {
                        //make a byte array and receive data from the client 
                        string sHttpVersion;
                        string resp;
                        String sMimeType;
                        bool bServe;

                        var bReceive = new Byte[1024];
                        mySocket.Receive(bReceive);
                        string sBuffer = Encoding.ASCII.GetString(bReceive);
                        //Debug.WriteLine(sBuffer);

                        if (sBuffer.Substring(0, 4) == "TALK")
                        {
                            string[] cfg = sBuffer.Substring(0, 10).Split(',');
                            int cid = Convert.ToInt32(cfg[1]);
                                
                            var socket = mySocket;
                            var feed = new Thread(p => AudioIn(socket, cid));
                            feed.Start();
                            return;
                        }
                        if (sBuffer.StartsWith("<policy-file-request/>"))
                        {
                            mySocket.SendFile(Program.AppPath + @"WebServerRoot\crossdomain.xml");
                            goto Finish;
                        }
                        if (sBuffer.Substring(0, 3) != "GET")
                        {
                            goto Finish;
                        }

                        try
                        {
                            String sRequestedFile;
                            String sErrorMessage;
                            String sLocalDir;
                            String sDirName;
                            string sFileName;
                            bool bHasAuth;
                            ParseRequest(sMyWebServerRoot, sBuffer, out sRequest, out sRequestedFile,
                                            out sErrorMessage,
                                            out sLocalDir, out sDirName, out sPhysicalFilePath, out sHttpVersion,
                                            out sFileName, out sMimeType, out bServe, out bHasAuth, ref mySocket);
                        }
                        catch (Exception)
                        {
                            goto Finish;
                        }
                            
                        if (!bServe)
                        {
                            resp = "//Access this server locally through "+MainForm.Website+Environment.NewLine+"try{Denied();} catch(e){}";
                            SendHeader(sHttpVersion, "text/javascript", resp.Length, " 200 OK", 0, ref mySocket);
                            SendToBrowser(resp, mySocket);
                            goto Finish;
                        }
                        
                        resp = ProcessCommandInternal(sRequest);

                        if (resp != "")
                        {
                            bool gzip = resp.Length > 400 && MainForm.Conf.EnableGZip && HeaderEnabled(sBuffer, "Accept-Encoding", "gzip");

                            if (gzip)
                            {
                                var arr = Gzip(Encoding.UTF8.GetBytes(resp));
                                SendHeader(sHttpVersion, "text/javascript", arr.Length, " 200 OK", 0, ref mySocket, true);
                                SendToBrowser(arr, mySocket);
                            }
                            else
                            {
                                SendHeader(sHttpVersion, "text/javascript", resp.Length, " 200 OK", 0, ref mySocket, false);
                                SendToBrowser(resp, mySocket);
                            }
                        }
                        else //not a js request
                        {
                            string cmd = sRequest.Trim('/').ToLower();
                            int i = cmd.IndexOf("?", StringComparison.Ordinal);
                            if (i>-1)
                                cmd = cmd.Substring(0,i );
                            if (cmd.StartsWith("get /"))
                                cmd = cmd.Substring(5);

                            int oid, otid;
                            int.TryParse(GetVar(sRequest, "oid"), out oid);
                            int.TryParse(GetVar(sRequest, "ot"), out otid);
                            switch(cmd)
                            {
                                case "logfile":
                                    SendLogFile(sHttpVersion, ref mySocket);
                                    break;
                                case "getlogfile":
                                    SendLogFile(sPhysicalFilePath,sHttpVersion, ref mySocket);
                                    break;
                                case "livefeed":
                                    SendLiveFeed(sPhysicalFilePath, sHttpVersion, ref mySocket);
                                    break;
                                case "loadgrab":
                                case "loadgrab.jpg":
                                        SendGrab(sPhysicalFilePath, sHttpVersion, ref mySocket);
                                    break;
                                case "loadimage":
                                case "loadimage.jpg":
                                    SendImage(sPhysicalFilePath, sHttpVersion, ref mySocket);
                                    break;
                                case "floorplanfeed":
                                    SendFloorPlanFeed(sPhysicalFilePath, sHttpVersion, ref mySocket);
                                    break;
                                //case "audiofeed.m4a":
                                //    SendAudioFeed(Enums.AudioStreamMode.M4A, sBuffer, sPhysicalFilePath, mySocket);
                                //    break;
                                case "audiofeed.mp3":
                                    SendAudioFeed(Enums.AudioStreamMode.MP3, sBuffer, sPhysicalFilePath, mySocket);
                                    return;
                                //case "audiofeed.wav":
                                //    SendAudioFeed(Enums.AudioStreamMode.PCM, sBuffer, sPhysicalFilePath, mySocket);
                                //    return;
                                case "video.mjpg":
                                case "video.cgi":
                                case "video.mjpeg":
                                case "video.jpg":
                                case "mjpegfeed":
                                    SendMJPEGFeed(sPhysicalFilePath, mySocket);
                                    return;
                                case "loadclip.flv":
                                case "loadclip.fla":
                                case "loadclip.mp3":
                                case "loadclip.mp4":
                                case "loadclip.avi":
                                    SendClip(sPhysicalFilePath, sBuffer, sHttpVersion, ref mySocket,false);
                                    break;
                                case "downloadclip.avi":
                                case "downloadclip.mp3":
                                case "downloadclip.mp4":
                                    SendClip(sPhysicalFilePath, sBuffer, sHttpVersion, ref mySocket, true);
                                    break;
                                default:
                                    if (sPhysicalFilePath.IndexOf('?') != -1)
                                    {
                                        sPhysicalFilePath = sPhysicalFilePath.Substring(0, sPhysicalFilePath.IndexOf('?'));
                                    }

                                    if (!File.Exists(sPhysicalFilePath))
                                    {
                                        ServeNotFound(sHttpVersion, ref mySocket);
                                    }
                                    else
                                    {
                                        ServeFile(sHttpVersion, sPhysicalFilePath, sMimeType, ref mySocket);
                                    }
                                    break;
                            }
                        }

                        Finish:
                            DisconnectSocket(mySocket);
                            NumErr = 0;
                    }
                    catch (SocketException ex)
                    {
                        //ignore connection timeout errors
                        if (ex.ErrorCode != 10060)
                        {
                            MainForm.LogExceptionToFile(ex);
                            NumErr++;

                        }
                        DisconnectSocket(mySocket);
                    }
                }
            }
            catch(ObjectDisposedException)
            {
                //socket closed already
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                NumErr++;
            }
        }

        private static bool HeaderEnabled(string req, string header, string val)
        {
            header = header.ToLower();
            req = req.ToLower();
            val = val.ToLower();

            var p = req.Split(Environment.NewLine.ToCharArray());
            foreach(var s in p)
            {
                if (!s.StartsWith(header)) continue;
                var v = s.Split(':');
                if (v.Length>1)
                {
                    string[] l = v[1].Split(',');

                    return l.Any(lp => lp.Trim() == val);
                }
                return false;
            }
            return false;
        }

        private static byte[] Gzip(byte[] bytes)
        {
            using (var ms = new MemoryStream())
            {
                using (var gs = new GZipStream(ms,CompressionMode.Compress, true))
                {
                    gs.Write(bytes, 0, bytes.Length);
                }
                ms.Position = 0L;
                return ms.ToArray();
            }
        }

        private static void SetDesiredKeepAlive(Socket socket)
        {
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            const uint time = 10000;
            const uint interval = 20000;
            SetKeepAlive(socket, true, time, interval);
        }
        static void SetKeepAlive(Socket s, bool on, uint time, uint interval)
        {
            /* the native structure
            struct tcp_keepalive {
            ULONG onoff;
            ULONG keepalivetime;
            ULONG keepaliveinterval;
            };
            */

            // marshal the equivalent of the native structure into a byte array
            const uint dummy = 0;
            var inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes(time).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
            BitConverter.GetBytes(interval).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
            // of course there are other ways to marshal up this byte array, this is just one way

            // call WSAIoctl via IOControl
            s.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

        }

        private void SendClip(String sPhysicalFilePath, string sBuffer, string sHttpVersion, ref Socket mySocket, bool downloadFile)
        {
            int oid = Convert.ToInt32(GetVar(sPhysicalFilePath, "oid"));
            int ot =  Convert.ToInt32(GetVar(sPhysicalFilePath, "ot"));

            
            string dir = MainForm.Conf.MediaDirectory;
            if (ot==1)
            {
                dir += @"audio\"+MainForm.Microphones.Single(p => p.id == oid).directory + @"\";
            }
            if (ot==2)
            {
                dir += @"video\"+MainForm.Cameras.Single(p => p.id == oid).directory + @"\";
            }
            string fn = dir+GetVar(sPhysicalFilePath, "fn");

            int iStartBytes = 0;
            int iEndBytes = 0;
            bool isrange = false;

            if (sBuffer.IndexOf("Range: bytes=", StringComparison.Ordinal) != -1)
            {
                string[] headers = sBuffer.Split(Environment.NewLine.ToCharArray());
                foreach (string h in headers)
                {
                    if (h.StartsWith("Range:"))
                    {
                        string[] range = (h.Substring(h.IndexOf("=", StringComparison.Ordinal) + 1)).Split('-');
                        iStartBytes = Convert.ToInt32(range[0]);
                        if (range[1] != "")
                        {
                            iEndBytes = Convert.ToInt32(range[1]);
                        }
                        else
                        {
                            iEndBytes = -1;
                        }
                        isrange = true;
                        break;
                    }
                }
            }


            var fi = new FileInfo(fn);
            int iTotBytes = Convert.ToInt32(fi.Length);
            if (iEndBytes == -1)
                iEndBytes = iTotBytes - 1;
            if (!File.Exists(fn))
            {
                SendHeader(sHttpVersion, "text/HTML",0, " 440 OK", 0, ref mySocket);
                return;
            }
            
            byte[] bytes;
            using (var fs =
                new FileStream(fn, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                // Create a reader that can read bytes from the FileStream.

                using (var reader = new BinaryReader(fs))
                {

                    if (!isrange)
                    {
                        bytes = new byte[fs.Length];
                        while ((reader.Read(bytes, 0, bytes.Length)) != 0)
                        {
                        }
                    }
                    else
                    {
                        bytes = new byte[iEndBytes - iStartBytes + 1];
                        reader.BaseStream.Seek(iStartBytes, SeekOrigin.Begin);
                        bytes = reader.ReadBytes(bytes.Length);
                    }

                    reader.Close();
                }
                fs.Close();
            }
            string sMimeType = GetMimeType(fn);

            string filename = fi.Name;

            if (downloadFile)
                filename = fi.Name.Replace("_", "").Replace("-", "");

            if (isrange)
            {
                SendHeaderWithRange(sHttpVersion, sMimeType, iStartBytes, iEndBytes, iTotBytes, " 206 Partial Content", 20, mySocket, filename);
            }
            else
            {
                SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", 20, ref mySocket, filename, false);
            }
                


            SendToBrowser(bytes, mySocket);
        }

        private void ServeNotFound(string sHttpVersion, ref Socket mySocket)
        {
            const string resp = "iSpy server is running";
            SendHeader(sHttpVersion, "", resp.Length, " 200 OK", 0, ref mySocket);
            SendToBrowser(resp, mySocket);
        }
        
        public static List<String> AllowedIPs
        {
            get
            {
                if (_allowedIPs != null)
                    return _allowedIPs;

                _allowedIPs = MainForm.Conf.AllowedIPList.Split(',').ToList();
                _allowedIPs.Add("127.0.0.1");
                _allowedIPs.RemoveAll(p => p == "");
                return _allowedIPs;
            }
            set { _allowedIPs = value; }
        }

        private void ParseRequest(String sMyWebServerRoot, string sBuffer, out String sRequest,
                                  out String sRequestedFile, out String sErrorMessage, out String sLocalDir,
                                  out String sDirName, out String sPhysicalFilePath, out string sHttpVersion,
                                  out string sFileName, out String sMimeType, out bool bServe, out bool bHasAuth, ref Socket mySocket)
        {
            sErrorMessage = "";
            string sClientIP = mySocket.RemoteEndPoint.ToString();

            sClientIP = sClientIP.Substring(0, sClientIP.LastIndexOf(":", StringComparison.Ordinal)).Trim();
            sClientIP = sClientIP.Replace("[", "").Replace("]", "");

            bServe = false;
            foreach(var ip in AllowedIPs)
            {
                if (Regex.IsMatch(sClientIP,ip))
                {
                    bServe = true;
                    break;
                }
            }

            int iStartPos = sBuffer.IndexOf("HTTP", 1, StringComparison.Ordinal);

            sHttpVersion = sBuffer.Substring(iStartPos, 8);
            sRequest = sBuffer.Substring(0, iStartPos - 1);
            sRequest = sRequest.Replace("\\", "/");

            if (sRequest.IndexOf("command.txt", StringComparison.Ordinal) != -1)
            {
                sRequest = sRequest.Replace("Video/", "Video|");
                sRequest = sRequest.Replace("Audio/", "Audio|");
            }
            iStartPos = sRequest.LastIndexOf("/", StringComparison.Ordinal) + 1;
            sRequestedFile = Uri.UnescapeDataString(sRequest.Substring(iStartPos));
            GetDirectoryPath(sRequest, sMyWebServerRoot, out sLocalDir, out sDirName);


            if (sLocalDir.Length == 0)
            {
                sErrorMessage = "<H2>Error!! Requested Directory does not exists</H2><Br>";
                SendHeader(sHttpVersion, "", sErrorMessage.Length, " 404 Not Found", 0, ref mySocket);
                SendToBrowser(sErrorMessage, mySocket);
                throw new Exception("Requested Directory does not exist (" + sLocalDir + ")");
            }

            ParseMimeType(sRequestedFile, out sFileName, out sMimeType);

            sPhysicalFilePath = (sLocalDir + sRequestedFile).Replace("%20", " ").ToLower();

            bHasAuth = sPhysicalFilePath.EndsWith("crossdomain.xml") || CheckAuth(sPhysicalFilePath);
            if (!bServe)
                bServe = bHasAuth;
        }

        private void ServeFile(string sHttpVersion, string sFileName, String sMimeType,
                               ref Socket mySocket)
        {
            var fi = new FileInfo(sFileName);
            int iTotBytes = Convert.ToInt32(fi.Length);

            byte[] bytes;
            using (var fs =
                new FileStream(sFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                using (var reader = new BinaryReader(fs))
                {

                    bytes = new byte[fs.Length];
                    while ((reader.Read(bytes, 0, bytes.Length)) != 0)
                    {
                    }
                    reader.Close();
                }
                fs.Close();
            }

            SendHeader(sHttpVersion, sMimeType, iTotBytes, " 200 OK", 20, ref mySocket);
            SendToBrowser(bytes, mySocket);
        }

        private static string GetVar(string url, string var)
        {
            url = url.ToLower();
            var = var.ToLower();

            int i = url.IndexOf("&"+ var + "=", StringComparison.Ordinal);
            if (i == -1)
                i = url.IndexOf("?" + var + "=", StringComparison.Ordinal);
            if (i == -1)
            {
                i = url.IndexOf(var, StringComparison.Ordinal);
                if (i == -1)
                    return "";
                i--;
            }

            string txt = url.Substring(i + var.Length + 1).Trim('=');
            if (txt.IndexOf("&", StringComparison.Ordinal) != -1)
                txt = txt.Substring(0, txt.IndexOf("&", StringComparison.Ordinal));

            return txt;
        }

        internal string ProcessCommandInternal(string sRequest)
        {
            string cmd = sRequest.Trim('/').ToLower().Trim();
            string resp = "";
            
            //hack for axis server commands
            
            if (cmd.StartsWith("get /") || cmd.StartsWith("get ?"))
                cmd = cmd.Substring(5);

            cmd = cmd.Trim('?');

            int i = cmd.IndexOf("?", StringComparison.Ordinal);
            if (i != -1)
                cmd = cmd.Substring(0, i);

            int oid, otid;
            int.TryParse(GetVar(sRequest, "oid"), out oid);
            int.TryParse(GetVar(sRequest, "ot"), out otid);
            string func = GetVar(sRequest, "jsfunc").Replace("%27","'");
            string fn = GetVar(sRequest, "fn");
            string temp="", folderpath;
            string[] files;

            long sdl = 0, edl = 0;
            string sd, ed;
            int page;

            switch (cmd)
            {
                case "command.txt": //legacy (test connection)
                case "connect":
                    resp = MainForm.Identifier + ",OK";
                    break;
                case "recordswitch":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);

                        if (vw != null)
                        {
                            bool sw = !vw.Recording;
                            resp = vw.RecordSwitch(sw) + ",OK";
                        }
                        else
                            resp = "stopped,Microphone not found,OK";
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);

                        if (cw != null)
                        {
                            bool sw = !cw.Recording;
                            resp = cw.RecordSwitch(sw) + ",OK";
                        }
                        else
                            resp = "stopped,Camera not found,OK";
                    }
                    break;
                case "record":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            resp = vw.RecordSwitch(true) + ",OK";
                        }
                        else
                            resp = "Microphone not found,OK";
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            resp = cw.RecordSwitch(true) + ",OK";
                        }
                        else
                            resp = "Camera not found,OK";
                    }
                    if (otid == 0)
                    {
                        _parent.RecordAll(true);
                    }
                    break;
                case "alert":
                    if (otid == 1)
                    {
                        var vl = _parent.GetVolumeLevel(oid);
                        if (vl != null)
                        {
                            vl.MicrophoneAlarm(this, EventArgs.Empty);
                            resp = "OK";
                        }
                        else
                            resp = "Microphone not found,OK";
                    }

                    if (otid == 2)
                    {
                        var cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.CameraAlarm(this, EventArgs.Empty);
                            resp = "OK";
                        }
                        else
                            resp = "Camera not found,OK";
                    }

                    break;
                case "recordoff":
                case "recordstop":
                    if (otid == 1)
                    {
                        var vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            resp = vw.RecordSwitch(false) + ",OK";
                        }
                        else
                            resp = "Microphone not found,OK";
                    }
                    if (otid == 2)
                    {
                        var cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            resp = cw.RecordSwitch(false) + ",OK";
                        }
                        else
                            resp = "Camera not found,OK";
                    }
                    if (otid == 0)
                    {
                        _parent.RecordAll(false);
                    }
                    break;
                case "snapshot":
                    if (otid == 2)
                    {
                        var cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.SaveFrame();
                        }
                    }
                    else
                    {
                        _parent.SnapshotAll();
                    }
                    break;
                case "ping":
                    resp = "OK";
                    break;
                case "allon":
                    _parent.SwitchObjects(false, true);
                    resp = "OK";
                    break;
                case "alloff":
                    _parent.SwitchObjects(false, false);
                    resp = "OK";
                    break;
                case "recordondetecton":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Micobject.detector.recordondetect = true;
                            vw.Micobject.detector.recordonalert = false;
                        }
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Camobject.detector.recordondetect = true;
                            cw.Camobject.detector.recordonalert = false;
                        }
                    }
                    if (otid == 0)
                    {
                        _parent.RecordOnDetect(true);
                    }

                    break;
                case "shutdown":
                    (new Thread(() => _parent.ExternalClose())).Start();
                    break;
                case "recordonalerton":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Micobject.detector.recordonalert = true;
                            vw.Micobject.detector.recordondetect = false;
                        }
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Camobject.detector.recordonalert = true;
                            cw.Camobject.detector.recordondetect = false;
                        }
                    }
                    if (otid == 0)
                    {
                        _parent.RecordOnAlert(true);
                    }

                    break;
                case "recordingoff":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Micobject.detector.recordonalert = false;
                            vw.Micobject.detector.recordondetect = false;
                        }
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Camobject.detector.recordonalert = false;
                            cw.Camobject.detector.recordondetect = false;
                        }
                    }
                    if (otid == 0)
                    {
                        _parent.RecordOnAlert(false);
                        _parent.RecordOnDetect(false);
                    }
                    break;
                case "alerton":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Micobject.alerts.active = true;
                        }
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Camobject.alerts.active = true;
                        }
                    }
                    if (otid == 0)
                    {
                        _parent.AlertsActive(true);
                    }

                    break;
                case "alertoff":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Micobject.alerts.active = false;
                        }
                    }
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Camobject.alerts.active = false;
                        }
                    }
                    if (otid == 0)
                    {
                        _parent.AlertsActive(false);
                    }
                    break;
                case "setmask":
                    resp = "NOK";
                    if (otid == 2)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);

                        if (cw != null)
                        {
                            if (File.Exists(fn))
                            {
                                cw.Camobject.settings.maskimage = fn;
                                try
                                {
                                    cw.Camobject.settings.maskimage = fn;
                                    if (cw.Camera != null)
                                        cw.Camera.Mask = (Bitmap)Image.FromFile(fn);
                                    resp = "OK";
                                }
                                catch (Exception)
                                {
                                }
                            }
                            else
                            {
                                cw.Camobject.settings.maskimage = "";
                                if (cw.Camera != null)
                                    cw.Camera.Mask = null;
                                resp = "Mask not found";
                            }
                        }
                    }
                    break;
                case "allscheduledon":
                    _parent.SwitchObjects(true, true);
                    resp = "OK";
                    break;
                case "allscheduledoff":
                    _parent.SwitchObjects(true, false);
                    resp = "OK";
                    break;
                case "applyschedule":
                    _parent.ApplySchedule();
                    resp = "OK";
                    break;
                case "bringonline":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Enable();
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Enable();
                        }
                    }
                    resp = "OK";
                    break;
                case "triggeralarm":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.MicrophoneAlarm(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.CameraAlarm(this,EventArgs.Empty);
                        }
                    }
                    resp = "OK";
                    break;
                case "triggerdetect":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.TriggerDetect();
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null && cw.Camera!=null)
                        {
                            cw.Camera.TriggerDetect();
                        }
                    }
                    resp = "OK";
                    break;
                case "triggerplugin":
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null && cw.Camera != null)
                        {
                            cw.Camera.TriggerPlugin();
                        }
                    }
                    resp = "OK";
                    break;
                case "smscmd":
                case "executecmd":
                    int commandIndex = Convert.ToInt32(GetVar(sRequest, "id"));
                    objectsCommand oc = MainForm.RemoteCommands.SingleOrDefault(p => p.id == commandIndex);

                    if (oc != null)
                    {
                        try
                        {
                            if (oc.command.StartsWith("ispy ") || oc.command.StartsWith("ispy.exe "))
                            {
                                string cmd2 = oc.command.Substring(oc.command.IndexOf(" ", StringComparison.Ordinal) + 1).Trim();

                                int k = cmd2.ToLower().IndexOf("commands ", StringComparison.Ordinal);
                                if (k != -1)
                                {
                                    cmd2 = cmd2.Substring(k + 9);
                                }
                                cmd2 = cmd2.Trim('"');
                                string[] commands = cmd2.Split('|');
                                foreach (string command2 in commands)
                                {
                                    if (!String.IsNullOrEmpty(command2))
                                    {
                                        MainForm.ProcessCommandInternal(command2.Trim('"'));
                                    }
                                }
                            }
                            else
                            {
                                Process.Start(oc.command);
                            }

                            resp = "Command Executed.,OK";
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                            resp = "Command Failed: " + ex.Message + ",OK";
                        }
                    }
                    else
                        resp = "OK";
                    break;
                case "takeoffline":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            vw.Disable();
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            cw.Disable();
                        }
                    }
                    resp = "OK";
                    break;
                case "deletefile":
                    if (otid == 1)
                    {
                        try
                        {
                            string subdir = GetDirectory(1, oid);
                            FileOperations.Delete(MainForm.Conf.MediaDirectory + "audio\\" + subdir + @"\" + fn);
                            var vl = _parent.GetVolumeLevel(oid);
                            if (vl != null)
                            {
                                vl.RemoveFile(fn);
                            }
                        }
                        catch (Exception e)
                        {
                            MainForm.LogExceptionToFile(e);
                        }

                    }
                    if (otid == 2)
                    {
                        try
                        {
                            _parent.RemovePreviewByFileName(fn);
                        }
                        catch (Exception e)
                        {
                            MainForm.LogExceptionToFile(e);
                        }
                    }
                    resp = "OK";
                    break;
                case "deleteall":
                    string objdir = GetDirectory(otid, oid);

                    Helper.DeleteAllContent(otid, objdir);
                    if (otid == 1)
                        _parent.GetVolumeLevel(oid).ClearFileList();
                    if (otid == 2)
                    {
                        _parent.GetCameraWindow(oid).ClearFileList();
                        _parent.NeedsMediaRefresh = DateTime.Now;
                    }
                    resp = "OK";
                    break;
                case "uploadyoutube":
                    bool @public = Convert.ToBoolean(GetVar(sRequest, "public"));
                    resp = YouTubeUploader.AddUpload(oid, fn, @public) + ",OK";
                    break;
                case "sendbyemail":
                    string email = GetVar(sRequest, "email");
                    string message = GetVar(sRequest, "message").Replace("%20", " ");
                    resp = YouTubeUploader.AddUpload(oid, fn, true, email, message) + ",OK";
                    break;
                case "kinect_tilt_up":
                    {
                        var c = _parent.GetCameraWindow(oid);
                        if (c != null)
                        {
                            try
                            {
                                ((KinectStream) c.Camera.VideoSource).Tilt += 4;
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex);
                            }
                        }

                        resp = "OK";
                    }
                    break;
                case "kinect_tilt_down":
                    {
                        var c = _parent.GetCameraWindow(oid);
                        if (c != null)
                        {
                            try
                            {
                                ((KinectStream) c.Camera.VideoSource).Tilt -= 4;
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex);
                            }
                        }
                        resp = "OK";
                    }
                    break;
                case "removeobject":
                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        if (vw != null)
                        {
                            _parent.RemoveMicrophone(vw, false);
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        if (cw != null)
                        {
                            _parent.RemoveCamera(cw, false);
                        }
                    }
                    MainForm.NeedsSync = true;
                    resp = "OK";
                    break;
                case "addobject":
                    int sourceIndex = Convert.ToInt32(GetVar(sRequest, "stid"));
                    int width = Convert.ToInt32(GetVar(sRequest, "w"));
                    int height = Convert.ToInt32(GetVar(sRequest, "h"));
                    string name = GetVar(sRequest, "name");
                    string url = GetVar(sRequest, "url").Replace("\\", "/");
                    _parent.AddObjectExternal(otid, sourceIndex, width, height, name, url);
                    MainForm.NeedsSync = true;
                    resp = "OK";
                    break;
                case "synthtocam":
                    var txt = GetVar(sRequest, "text");
                    var t = new Thread(() => SynthToCam(Uri.UnescapeDataString(txt), oid));
                    t.Start();
                    resp = "OK";
                    break;
                case "changesetting":
                    string field = GetVar(sRequest, "field");
                    string value = GetVar(sRequest, "value");

                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        switch (field)
                        {
                            case "notifyondisconnect":
                                vw.Micobject.settings.notifyondisconnect = Convert.ToBoolean(value);
                                break;
                            case "recordondetect":
                                vw.Micobject.detector.recordondetect = Convert.ToBoolean(value);
                                if (vw.Micobject.detector.recordondetect)
                                    vw.Micobject.detector.recordonalert = false;
                                break;
                            case "recordonalert":
                                vw.Micobject.detector.recordonalert = Convert.ToBoolean(value);
                                if (vw.Micobject.detector.recordonalert)
                                    vw.Micobject.detector.recordondetect = false;
                                break;
                            case "recordoff":
                                vw.Micobject.detector.recordonalert = false;
                                vw.Micobject.detector.recordondetect = false;
                                break;
                            case "scheduler":
                                vw.Micobject.schedule.active = Convert.ToBoolean(value);
                                break;
                            case "alerts":
                                vw.Micobject.alerts.active = Convert.ToBoolean(value);
                                break;
                            //case "sendemailonalert":
                            //    vw.Micobject.notifications.sendemail = Convert.ToBoolean(value);
                            //    break;
                            //case "sendsmsonalert":
                            //    vw.Micobject.notifications.sendsms = Convert.ToBoolean(value);
                            //    break;
                            case "minimuminterval":
                                int mi;
                                int.TryParse(value, out mi);
                                vw.Micobject.alerts.minimuminterval = mi;
                                break;
                            case "accessgroups":
                                vw.Micobject.settings.accessgroups = value;
                                break;
                        }
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        switch (field)
                        {
                            case "youtube":
                                cw.Camobject.settings.youtube.autoupload = Convert.ToBoolean(value);
                                break;
                            case "notifyondisconnect":
                                cw.Camobject.settings.notifyondisconnect = Convert.ToBoolean(value);
                                break;
                            case "ftpenabled":
                            case "ftp":
                                cw.Camobject.ftp.enabled = Convert.ToBoolean(value);
                                break;
                            case "recordondetect":
                                cw.Camobject.detector.recordondetect = Convert.ToBoolean(value);
                                if (cw.Camobject.detector.recordondetect)
                                    cw.Camobject.detector.recordonalert = false;
                                break;
                            case "recordonalert":
                                cw.Camobject.detector.recordonalert = Convert.ToBoolean(value);
                                if (cw.Camobject.detector.recordonalert)
                                    cw.Camobject.detector.recordondetect = false;
                                break;
                            case "recordoff":
                                cw.Camobject.detector.recordonalert = false;
                                cw.Camobject.detector.recordondetect = false;
                                break;
                            case "scheduler":
                                cw.Camobject.schedule.active = Convert.ToBoolean(value);
                                break;
                            case "alerts":
                                cw.Camobject.alerts.active = Convert.ToBoolean(value);
                                break;
                            //case "sendemailonalert":
                            //    cw.Camobject.notifications.sendemail = Convert.ToBoolean(value);
                            //    break;
                            //case "sendsmsonalert":
                            //    cw.Camobject.notifications.sendsms = Convert.ToBoolean(value);
                            //    //if (cw.Camobject.notifications.sendsms)
                            //    //    cw.Camobject.notifications.sendmms = false;
                            //    break;
                            //case "sendmmsonalert":
                            //    cw.Camobject.notifications.sendmms = Convert.ToBoolean(value);
                            //    if (cw.Camobject.notifications.sendmms)
                            //        cw.Camobject.notifications.sendsms = false;
                            //    break;
                            //case "emailframeevery":
                            //    int gi;
                            //    int.TryParse(value, out gi);
                            //    cw.Camobject.notifications.emailgrabinterval = gi;
                            //    break;
                            case "timelapseon":
                                cw.Camobject.recorder.timelapseenabled = Convert.ToBoolean(value);
                                break;
                            case "timelapse":
                                int tl;
                                int.TryParse(value, out tl);
                                cw.Camobject.recorder.timelapse = tl;
                                break;
                            case "timelapseframes":
                                int tlf;
                                int.TryParse(value, out tlf);
                                cw.Camobject.recorder.timelapseframes = tlf;
                                break;
                            case "localsaving":
                                cw.Camobject.ftp.savelocal = Convert.ToBoolean(value);
                                break;
                            case "ptz":
                                if (value != "")
                                {
                                    try
                                    {
                                        value = Uri.UnescapeDataString(value);
                                        cw.Calibrating = true;
                                        if (value.StartsWith("ispydir_"))
                                        {
                                            cw.PTZ.SendPTZCommand(
                                                (Enums.PtzCommand) Convert.ToInt32(value.Replace("ispydir_", "")));
                                        }
                                        else
                                            cw.PTZ.SendPTZCommand(value, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        MainForm.LogErrorToFile(LocRm.GetString("Validate_Camera_PTZIPOnly") + ": " +
                                                                ex.Message);
                                    }
                                }
                                break;
                            case "minimuminterval":
                                int mi;
                                int.TryParse(value, out mi);
                                cw.Camobject.alerts.minimuminterval = mi;
                                break;
                            case "accessgroups":
                                cw.Camobject.settings.accessgroups = value;
                                break;
                        }
                    }
                    resp = "OK";
                    break;
                case "getcontentlist":
                    page = Convert.ToInt32(GetVar(sRequest, "page"));

                    sd = GetVar(sRequest, "startdate");
                    ed = GetVar(sRequest, "enddate");
                    int pageSize = Convert.ToInt32(GetVar(sRequest, "pagesize"));
                    int ordermode = Convert.ToInt32(GetVar(sRequest, "ordermode"));
                    if (sd != "")
                        sdl = Convert.ToInt64(sd);
                    if (ed != "")
                        edl = Convert.ToInt64(ed);


                    switch (otid)
                    {
                        case 1:
                            VolumeLevel vl = _parent.GetVolumeLevel(oid);
                            if (vl != null)
                            {
                                List<FilesFile> lFi = vl.FileList.Where(f => f.Filename.EndsWith(".mp3")).ToList();
                                if (sdl > 0)
                                    lFi = lFi.FindAll(f => f.CreatedDateTicks > sdl).ToList();
                                if (edl > 0)
                                    lFi = lFi.FindAll(f => f.CreatedDateTicks < edl).ToList();
                                func = func.Replace("resultcount", lFi.Count.ToString(CultureInfo.InvariantCulture));

                                switch (ordermode)
                                {
                                    case 1:
                                        //default
                                        break;
                                    case 2:
                                        lFi = lFi.OrderByDescending(p => p.DurationSeconds).ToList();
                                        break;
                                    case 3:
                                        lFi = lFi.OrderByDescending(p => p.MaxAlarm).ToList();
                                        break;
                                    case 4:
                                        lFi = lFi.OrderByDescending(p => p.CreatedDateTicks).ToList();
                                        break;
                                }


                                var lResults = lFi.Skip(pageSize*page).Take(pageSize).ToList();
                                temp = lResults.Aggregate("",
                                                          (current, fi) =>
                                                          current +
                                                          (fi.Filename + "|" + FormatBytes(fi.SizeBytes) + "|" +
                                                           String.Format(
                                                               CultureInfo.InvariantCulture,
                                                               "{0:0.000}", fi.MaxAlarm) + ","));
                                resp = temp.Trim(',');
                            }
                            break;
                        case 2:
                            CameraWindow cw = _parent.GetCameraWindow(oid);
                            if (cw != null)
                            {
                                List<FilesFile> lFi2 = cw.FileList.ToList();
                                if (sdl > 0)
                                    lFi2 = lFi2.FindAll(f => f.CreatedDateTicks > sdl).ToList();
                                if (edl > 0)
                                    lFi2 = lFi2.FindAll(f => f.CreatedDateTicks < edl).ToList();
                                func = func.Replace("resultcount", lFi2.Count.ToString(CultureInfo.InvariantCulture));

                                switch (ordermode)
                                {
                                    case 1:
                                        //default
                                        break;
                                    case 2:
                                        lFi2 = lFi2.OrderByDescending(p => p.DurationSeconds).ToList();
                                        break;
                                    case 3:
                                        lFi2 = lFi2.OrderByDescending(p => p.MaxAlarm).ToList();
                                        break;
                                    case 4:
                                        lFi2 = lFi2.OrderByDescending(p => p.CreatedDateTicks).ToList();
                                        break;
                                }

                                var lResults2 = lFi2.Skip(pageSize*page).Take(pageSize).ToList();
                                temp = lResults2.Aggregate("",
                                                           (current, fi) =>
                                                           current +
                                                           (fi.Filename + "|" + FormatBytes(fi.SizeBytes) + "|" +
                                                            String.Format(
                                                                CultureInfo.InvariantCulture,
                                                                "{0:0.000}", fi.MaxAlarm) + ","));
                                resp = temp.Trim(',');
                            }
                            break;

                    }
                    break;
                case "getcontentcounts":
                    sd = GetVar(sRequest, "startdate");
                    ed = GetVar(sRequest, "enddate");
                    if (sd != "")
                        sdl = Convert.ToInt64(sd);
                    if (ed != "")
                        edl = Convert.ToInt64(ed);
                    string oclall = "";
                    foreach (objectsCamera oc1 in MainForm.Cameras)
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oc1.id);

                        List<FilesFile> lFi2 = cw.FileList.ToList();
                        if (sdl > 0)
                            lFi2 = lFi2.FindAll(f => f.CreatedDateTicks > sdl).ToList();
                        if (edl > 0)
                            lFi2 = lFi2.FindAll(f => f.CreatedDateTicks < edl).ToList();
                        oclall += "2," + oc1.id + "," + lFi2.Count + "|";

                    }
                    foreach (objectsMicrophone om1 in MainForm.Microphones)
                    {
                        VolumeLevel vl = _parent.GetVolumeLevel(om1.id);
                        List<FilesFile> lFi = vl.FileList.Where(f => f.Filename.EndsWith(".mp3")).ToList();
                        if (sdl > 0)
                            lFi = lFi.FindAll(f => f.CreatedDateTicks > sdl).ToList();
                        if (edl > 0)
                            lFi = lFi.FindAll(f => f.CreatedDateTicks < edl).ToList();
                        oclall += "1," + om1.id + "," + lFi.Count + "|";
                    }
                    resp = oclall.Trim('|');
                    break;
                case "getfloorplanalerts":
                    foreach (objectsFloorplan ofp in MainForm.FloorPlans)
                    {
                        FloorPlanControl fpc = _parent.GetFloorPlan(ofp.id);
                        if (fpc != null && fpc.ImgPlan != null)
                        {
                            temp += ofp.id + "," + fpc.LastAlertTimestamp.ToString(CultureInfo.InvariantCulture) + "," + fpc.LastRefreshTimestamp.ToString(CultureInfo.InvariantCulture) + "," +
                                    fpc.LastOid + "," + fpc.LastOtid + "|";
                        }
                    }
                    resp = temp.Trim('|');
                    break;
                case "getfloorplanalerts2":
                    {
                        string cfg = "";

                        foreach (objectsFloorplan ofp in MainForm.FloorPlans)
                        {
                            FloorPlanControl fpc = _parent.GetFloorPlan(ofp.id);
                            if (fpc != null && fpc.ImgPlan != null)
                            {
                                cfg += "{oid:" + ofp.id + ",alertTimestamp:" + fpc.LastAlertTimestamp.ToString(CultureInfo.InvariantCulture) +
                                       ",refreshTimestamp:" + fpc.LastRefreshTimestamp.ToString(CultureInfo.InvariantCulture) + ",last_oid:" + fpc.LastOid +
                                       ",last_otid:" + fpc.LastOtid + "},";
                            }
                        }
                        func = func.Replace("data", "[" + cfg.Trim(',') + "]");
                    }

                    resp = "OK";
                    break;
                case "getfloorplans":
                    foreach (objectsFloorplan ofp in MainForm.FloorPlans)
                    {
                        FloorPlanControl fpc = _parent.GetFloorPlan(ofp.id);
                        if (fpc != null && fpc.ImgPlan != null)
                        {
                            temp += ofp.id + "," + ofp.name.Replace(",", "").Replace("|", "").Replace("^", "") + "," +
                                    ofp.width + "," + ofp.height + "|";

                            temp = ofp.objects.@object.Aggregate(temp,
                                                                 (current, ofpo) =>
                                                                 current +
                                                                 (ofpo.id + "," + ofpo.type + "," + (ofpo.x) + "," +
                                                                  (ofpo.y) + "_"));
                            temp = temp.Trim('_');
                            temp += "^";
                        }
                    }
                    resp = temp.Replace("\"", "");
                    break;
                case "getfloorplans2":
                    {
                        string cfg = "";

                        foreach (objectsFloorplan ofp in MainForm.FloorPlans)
                        {
                            FloorPlanControl fpc = _parent.GetFloorPlan(ofp.id);
                            if (fpc != null && fpc.ImgPlan != null)
                            {
                                cfg += "{oid: " + ofp.id + ", name: \"" +
                                       ofp.name.Replace("\"", "") + "\", refreshTimestamp: " + fpc.LastRefreshTimestamp.ToString(CultureInfo.InvariantCulture) + ", alertTimestamp: " + fpc.LastAlertTimestamp.ToString(CultureInfo.InvariantCulture) + ", width:" + fpc.ImageWidth + ", height:" + fpc.ImageHeight + ", groups:\"" + ofp.accessgroups.Replace("\n", " ").Replace("\"", "") + "\",areas:[";

                                cfg += ofp.objects.@object.Aggregate(temp,
                                                                     (current, ofpo) =>
                                                                     current +
                                                                     ("{oid: " + ofpo.id + ",ot: " + (ofpo.type == "camera" ? 2 : 1) + ", x:" + (ofpo.x) + ",y:" + (ofpo.y) + "},"));
                                cfg = cfg.Trim(',');
                                cfg += "]},";
                            }
                        }
                        func = func.Replace("data", "[" + cfg.Trim(',') + "]");
                    }
                    resp = "OK";
                    break;
                case "getgraph":
                    FilesFile ff = null;
                    switch (otid)
                    {
                        case 1:
                            VolumeLevel vl = _parent.GetVolumeLevel(oid);
                            if (vl != null)
                            {
                                ff = vl.FileList.FirstOrDefault(p => p.Filename == fn);
                            }
                            break;
                        case 2:
                            CameraWindow cw = _parent.GetCameraWindow(oid);
                            if (cw != null)
                            {
                                ff = cw.FileList.FirstOrDefault(p => p.Filename == fn);
                            }
                            break;
                    }
                    if (ff != null)
                    {
                        func = func.Replace("data", "\"" + ff.AlertData + "\"");
                        func = func.Replace("duration", "\"" + ff.DurationSeconds + "\"");
                        func = func.Replace("threshold",
                                            String.Format(CultureInfo.InvariantCulture, "{0:0.000}",
                                                          ff.TriggerLevel));
                    }
                    else
                    {
                        func = func.Replace("data", "\"\"");
                        func = func.Replace("duration", "0");
                        func = func.Replace("threshold", "0");

                    }
                    resp = "OK";
                    break;
                case "graphall":
                    {
                        List<FilesFile> ffs = null;
                        switch (otid)
                        {
                            case 1:
                                VolumeLevel vl = _parent.GetVolumeLevel(oid);
                                if (vl != null)
                                {
                                    ffs = vl.FileList.ToList();
                                }
                                break;
                            case 2:
                                CameraWindow cw = _parent.GetCameraWindow(oid);
                                if (cw != null)
                                {
                                    ffs = cw.FileList.ToList();
                                }
                                break;
                        }
                        if (ffs != null)
                        {

                            sd = GetVar(sRequest, "startdate");
                            ed = GetVar(sRequest, "enddate");

                            if (sd != "")
                                sdl = Convert.ToInt64(sd);
                            if (ed != "")
                                edl = Convert.ToInt64(ed);

                            if (sdl > 0)
                                ffs = ffs.FindAll(f => f.CreatedDateTicks > sdl).ToList();
                            if (edl > 0)
                                ffs = ffs.FindAll(f => f.CreatedDateTicks < edl).ToList();

                            var sb = new StringBuilder();
                            foreach (FilesFile f in ffs)
                            {
                                sb.Append((f.CreatedDateTicks.UnixTicks())).Append("|").Append(String.Format(CultureInfo.InvariantCulture, "{0:0.000}",f.MaxAlarm)).Append("|").Append(f.DurationSeconds.ToString(CultureInfo.InvariantCulture)).Append("|").Append(f.Filename).Append(",");
                            }
                            temp = sb.ToString();
                            func = func.Replace("data", "\"" + temp.Trim(',') + "\"");

                        }
                        else
                        {
                            func = func.Replace("data", "\"\"");

                        }
                        resp = "OK";
                    }
                    break;
                case "getevents":
                    {
                        string num = GetVar(sRequest, "num");
                        if (num == "")
                            num = "500";
                        int n = Convert.ToInt32(num);


                        List<FilePreview> ffs =
                            MainForm.MasterFileList.OrderByDescending(p => p.CreatedDateTicks).ToList();
                        
                        sd = GetVar(sRequest, "startdate");
                        ed = GetVar(sRequest, "enddate");

                        sdl = sd != "" ? Convert.ToInt64(sd) : 0;
                        edl = ed != "" ? Convert.ToInt64(ed) : long.MaxValue;

                        if (sdl > 0)
                            ffs = ffs.FindAll(f => f.CreatedDateTicks > sdl);//.ToList();
                        if (edl < long.MaxValue)
                            ffs = ffs.FindAll(f => f.CreatedDateTicks < edl);//.ToList();


                        //return max of 1000 at a time
                        ffs = ffs.Take(n).ToList();
                        var sb = new StringBuilder();
                        sb.Append("[");
                        foreach(var f in ffs)
                        {
                            sb.Append("{ot:");
                            sb.Append(f.ObjectTypeId);
                            sb.Append(",oid:");
                            sb.Append(f.ObjectId);
                            sb.Append(",created:");
                            sb.Append(String.Format(CultureInfo.InvariantCulture, "{0:0.00}",
                                                    f.CreatedDateTicks.UnixTicks()));
                            sb.Append(",maxalarm:");
                            sb.Append(String.Format(CultureInfo.InvariantCulture, "{0:0.0}",
                                                    f.MaxAlarm));
                            sb.Append(",duration: ");
                            sb.Append(f.Duration);
                            sb.Append(",filename:\"");
                            sb.Append(f.Filename);
                            sb.Append("\"},");
                        }
                        temp = sb.ToString().Trim(',') + "]";
                        func = func.Replace("data", temp);
                    }
                    resp = "OK";
                    break;
                case "getgrabs":
                    sd = GetVar(sRequest, "startdate");
                    ed = GetVar(sRequest, "enddate");

                    if (sd != "")
                        sdl = Convert.ToInt64(sd);
                    if (ed != "")
                        edl = Convert.ToInt64(ed);

                    string grabs = "";
                    foreach (objectsCamera oc1 in MainForm.Cameras)
                    {
                        var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "video\\" +
                                                        oc1.directory + "\\grabs\\");

                        var lFi = new List<FileInfo>();
                        lFi.AddRange(dirinfo.GetFiles());
                        lFi =
                            lFi.FindAll(
                                f =>
                                f.Extension.ToLower() == ".jpg" && (sdl == 0 || f.CreationTime.Ticks > sdl) &&
                                (edl == 0 || f.CreationTime.Ticks < edl));
                        lFi = lFi.OrderByDescending(f => f.CreationTime).ToList();

                        int max = 25;
                        if (lFi.Count > 0)
                        {
                            foreach (var f in lFi)
                            {
                                grabs += (oc1.name + "|" + oc1.id + "|" + f.Name + ",");
                                max--;
                                if (max == 0)
                                    break;
                            }
                        }

                    }
                    func = func.Replace("data", "\"" + grabs.Trim(',') + "\"");
                    resp = "OK";
                    break;
                case "getlogfilelist":
                    {
                        string logs = "";
                        var dirinfo = new DirectoryInfo(Program.AppDataPath);
                        var lFi = new List<FileInfo>();
                        lFi.AddRange(dirinfo.GetFiles());
                        lFi = lFi.FindAll(f => f.Extension.ToLower() == ".htm" && f.Name.StartsWith("log_"));
                        lFi = lFi.OrderByDescending(f => f.CreationTime).ToList();
                        foreach(var f in lFi)
                        {
                            logs += f.Name + ",";
                        }
                        func = func.Replace("data", "\"" + logs.Trim(',') + "\"");
                        resp = "OK";
                    }
                break;
                case "getcameragrabs":
                    sd = GetVar(sRequest, "startdate");
                    ed = GetVar(sRequest, "enddate");
                    int pagesize = Convert.ToInt32(GetVar(sRequest, "pagesize"));
                    page = Convert.ToInt32(GetVar(sRequest, "page"));
                    if (sd != "")
                        sdl = Convert.ToInt64(sd);
                    if (ed != "")
                        edl = Convert.ToInt64(ed);

                    var grablist = new StringBuilder("");
                    var ocgrab = MainForm.Cameras.FirstOrDefault(p => p.id == oid);
                    if (ocgrab != null)
                    {
                        var dirinfo = new DirectoryInfo(MainForm.Conf.MediaDirectory + "video\\" +
                                                ocgrab.directory + "\\grabs\\");

                        var lFi = new List<FileInfo>();
                        lFi.AddRange(dirinfo.GetFiles());
                        lFi = lFi.FindAll(f => f.Extension.ToLower() == ".jpg" && (sdl == 0 || f.CreationTime.Ticks > sdl) && (edl == 0 || f.CreationTime.Ticks < edl));
                        lFi = lFi.OrderByDescending(f => f.CreationTime).ToList();
                        func = func.Replace("total", lFi.Count.ToString(CultureInfo.InvariantCulture));
                        lFi = lFi.Skip(page*pagesize).Take(pagesize).ToList();

                        int max = 10000;
                        if (lFi.Count > 0)
                        {
                            foreach (var f in lFi)
                            {
                                grablist.Append(f.Name);
                                grablist.Append(",");
                                max--;
                                if (max == 0)
                                    break;
                            }
                        }

                    }
                    func = func.Replace("data", "\"" + grablist.ToString().Trim(',') + "\"");
                    resp = "OK";
                    break;
                case "getptzcommands":
                    int ptzid = Convert.ToInt32(GetVar(sRequest, "ptzid"));
                    string cmdlist = "";

                    switch (ptzid)
                    {
                        default:
                            PTZSettings2Camera ptz = MainForm.PTZs.SingleOrDefault(p => p.id == ptzid);
                            if (ptz != null)
                            {
                       
                                if (ptz.ExtendedCommands != null && ptz.ExtendedCommands.Command != null)
                                {
                                    cmdlist = ptz.ExtendedCommands.Command.Aggregate("",
                                                                                     (current, extcmd) =>
                                                                                     current +
                                                                                     ("<option value=\\\"" + extcmd.Value +
                                                                                      "\\\">" + extcmd.Name.Trim() +
                                                                                      "</option>"));
                                }
                            }
                            break;
                        case -2:
                        case -1: //digital (none)
                            break;
                        case -3:
                        case -4:
                            cmdlist = PTZController.PelcoCommands.Aggregate(cmdlist, (current, c) => current + ("<option value=\\\"" + c + "\\\">" + c + "</option>"));
                            break;
                        case -5:
                            CameraWindow cw = _parent.GetCameraWindow(oid);
                            if (cw != null && cw.PTZ!=null)
                            {
                                if (cw.PTZ.ONVIFPresets.Length>0)
                                {
                                    cmdlist = cw.PTZ.ONVIFPresets.Aggregate(cmdlist, (current, c) => current + ("<option value=\\\"" + c + "\\\">" + c + "</option>"));
                                }
                            }
                            break;
                    }
                    func = func.Replace("data", "\"" + cmdlist.Trim(',') + "\"");                   
                    resp = "OK";                       
                    break;
                case "massdeletegrabs":
                    files = GetVar(sRequest, "filelist").Trim('|').Split('|');

                    folderpath = MainForm.Conf.MediaDirectory + "video\\" +
                                   GetDirectory(otid, oid) + "\\grabs\\";
                   
                    foreach(string fn3 in files)
                    {
                        FileOperations.Delete(folderpath + fn3);
                    }
                    resp = "OK";
                    break;
                case "massdelete":
                    files = GetVar(sRequest, "filelist").Trim('|').Split('|');
                    string dir = "audio";
                    if (otid == 2)
                        dir = "video";

                    folderpath = MainForm.Conf.MediaDirectory + dir + "\\" +
                                   GetDirectory(otid, oid) + "\\";

                    VolumeLevel vlUpdate = null;
                    CameraWindow cwUpdate = null;
                    if (otid == 1)
                    {
                        vlUpdate = _parent.GetVolumeLevel(oid);
                        if (vlUpdate == null)
                        {
                            resp = "OK";
                            break;
                        }
                    }
                    if (otid == 2)
                    {
                        cwUpdate = _parent.GetCameraWindow(oid);
                        if (cwUpdate==null)
                        {
                            resp = "OK";
                            break;
                        }
                    }
                    foreach(string fn3 in files)
                    {
                        var fi = new FileInfo(folderpath +
                                                     fn3);
                        string ext = fi.Extension.Trim();
                        FileOperations.Delete(folderpath + fn3);
                        if (otid == 2)
                        {
                            FileOperations.Delete(folderpath + "thumbs\\" + fn3.Replace(ext, ".jpg"));
                            FileOperations.Delete(folderpath + "thumbs\\" + fn3.Replace(ext, "_large.jpg"));
                        }
                        string filename1 = fn3;
                        if (otid==1)
                        {
                            if (vlUpdate != null)
                            {
                                vlUpdate.RemoveFile(filename1);
                            }
                        }
                        if (otid == 2)
                        {
                            if (cwUpdate != null)
                            {
                                cwUpdate.RemoveFile(filename1);
                            }
                        }
                        
                    }
                    if (otid == 2)
                        _parent.NeedsMediaRefresh = DateTime.Now;

                    resp = "OK";
                    break;
                case "getobjectlist":
                    //for 3rd party APIs
                    resp = GetObjectList();
                    break;
                case "getservername":
                    resp = MainForm.Conf.ServerName+",OK";
                    break;
                case "getcontrolpanel":
                    int port = Convert.ToInt32(GetVar(sRequest, "port"));

                    string disabled = "";
                    if (!MainForm.Conf.Subscribed)
                        disabled = " disabled=\"disabled\" title=\"Not Subscribed\"";

                    if (otid == 1)
                    {
                        VolumeLevel vw = _parent.GetVolumeLevel(oid);
                        string html = "<table cellspacing=\"3px\">";
                        string strChecked = "";


                        if (vw.Micobject.alerts.active) strChecked = "checked=\"checked\"";
                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("Alerts") + "</strong></td></tr>";
                        html += "<tr><td>" + LocRm.GetString("AlertsEnabled") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'alerts',this.checked)\" " + strChecked + "/></td></tr>";

                        //strChecked = "";
                        //if (vw.Micobject.notifications.sendemail) strChecked = "checked=\"checked\"";

                        //html += "<tr><td>" + LocRm.GetString("SendEmailOnAlert") + "</td><td><input type=\"checkbox\"" +
                        //         disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                        //         ",'sendemailonalert',this.checked)\" " + strChecked + "/> " +
                        //         vw.Micobject.settings.emailaddress + "</td></tr>";

                        //strChecked = "";
                        //if (vw.Micobject.notifications.sendsms) strChecked = "checked=\"checked\"";

                        //html += "<tr><td>" + LocRm.GetString("SendSmsOnAlert") + "</td><td><input type=\"checkbox\"" +
                        //         disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                        //         ",'sendsmsonalert',this.checked)\" " + strChecked + "/> " + vw.Micobject.settings.smsnumber +
                        //         "</td></tr>";

                        strChecked = "";
                        if (vw.Micobject.settings.notifyondisconnect) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("SendEmailOnDisconnect") + "</td><td><input type=\"checkbox\"" +
                                 disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                                 ",'notifyondisconnect',this.checked)\" " + strChecked + "/></td></tr>";

                        html += "<tr><td>" + LocRm.GetString("DistinctAlertInterval") +
                                 "</td><td><input style=\"width:50px\" type=\"text\" value=\"" +
                                 vw.Micobject.alerts.minimuminterval + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'minimuminterval',this.value)\"/> " + LocRm.GetString("Seconds") + "</td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("AccessGroups") + "</strong></td></tr>";
                        html += "<tr><td>" + LocRm.GetString("AccessGroups") +
                                 "</td><td><input style=\"width:100px\" type=\"text\" value=\"" +
                                 vw.Micobject.settings.accessgroups + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'accessgroups',this.value)\"/></td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("Scheduler") + "</strong></td></tr>";
                        strChecked = "";
                        if (vw.Micobject.schedule.active) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("ScheduleActive") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'scheduler',this.checked)\" " + strChecked + "/>";

                        string schedule = "";
                        for (int index = 0; index < vw.ScheduleDetails.Length; index++)
                        {
                            string s = vw.ScheduleDetails[index];
                            if (s != "")
                            {
                                schedule += s + "<br/>";
                            }
                        }
                        if (schedule != "")
                            html +=
                                "<div style=\"width:450px;height:100px;overflow-y:auto;background-color:#ddd;padding:5px\">" +
                                schedule + "</div>";
                        html += "</td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("RecordingSettings") + "</strong></td></tr>";

                        strChecked = "";

                        if (!vw.Micobject.detector.recordondetect && !vw.Micobject.detector.recordondetect)
                            strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("NoRecord") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordoff',this.checked)\" " + strChecked + "/></td></tr>";

                        strChecked = "";
                        if (vw.Micobject.detector.recordondetect) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("RecordOnDetect") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordondetect',this.checked)\" " + strChecked + "/></td></tr>";

                        strChecked = "";
                        if (vw.Micobject.detector.recordonalert) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("RecordOnAlert") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordonalert',this.checked)\" " + strChecked + "/></td></tr>";


                        html += "</table>";
                        resp += html.Replace("\"", "\\\"");
                    }
                    else
                    {
                        CameraWindow cw = _parent.GetCameraWindow(oid);
                        string html = "<table cellspacing=\"3px\">";
                        string strChecked = "";
                        if (cw.Camobject.alerts.active) strChecked = "checked=\"checked\"";
                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("Alerts") + "</strong></td></tr>";
                        html += "<tr><td>" + LocRm.GetString("AlertsEnabled") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'alerts',this.checked)\" " + strChecked + "/></td></tr>";

                        //strChecked = "";
                        //if (cw.Camobject.notifications.sendemail) strChecked = "checked=\"checked\"";

                        //html += "<tr><td>" + LocRm.GetString("SendEmailOnAlert") + "</td><td><input type=\"checkbox\"" +
                        //         disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                        //         ",'sendemailonalert',this.checked)\" " + strChecked + "/> " + cw.Camobject.settings.emailaddress +
                        //         "</td></tr>";


                        //strChecked = "";
                        //if (cw.Camobject.notifications.sendsms) strChecked = "checked=\"checked\"";

                        //html += "<tr><td>" + LocRm.GetString("SendSmsOnAlert") + "</td><td><input type=\"checkbox\"" +
                        //         disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                        //         ",'sendsmsonalert',this.checked)\" " + strChecked + "/> " + cw.Camobject.settings.smsnumber +
                        //         "</td></tr>";

                        //strChecked = "";
                        //if (cw.Camobject.notifications.sendmms) strChecked = "checked=\"checked\"";

                        //html += "<tr><td>" + LocRm.GetString("SendAsMmsWithImage2Credit") + "</td><td><input type=\"checkbox\"" +
                        //         disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                        //         ",'sendmmsonalert',this.checked)\" " + strChecked + "/> " + cw.Camobject.settings.smsnumber +
                        //         "</td></tr>";

                        strChecked = "";
                        if (cw.Camobject.settings.notifyondisconnect) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("SendEmailOnDisconnect") + "</td><td><input type=\"checkbox\"" +
                                 disabled + " onclick=\"send_changesetting(" + otid + "," + oid + "," + port +
                                 ",'notifyondisconnect',this.checked)\" " + strChecked + "/></td></tr>";

                        html += "<tr><td>" + LocRm.GetString("DistinctAlertInterval") +
                                 "</td><td><input style=\"width:50px\" type=\"text\" value=\"" +
                                 cw.Camobject.alerts.minimuminterval + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'minimuminterval',this.value)\"/> " +LocRm.GetString("Seconds")+ "</td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("AccessGroups") + "</strong></td></tr>";

                        html += "<tr><td>" + LocRm.GetString("AccessGroups") +
                                 "</td><td><input style=\"width:100px\" type=\"text\" value=\"" +
                                 cw.Camobject.settings.accessgroups + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'accessgroups',this.value)\"/></td></tr>";


                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("Scheduler") + "</strong></td></tr>";
                        strChecked = "";
                        if (cw.Camobject.schedule.active) strChecked = "checked=\"checked\"";

                        html += "<tr><td valign=\"top\">" + LocRm.GetString("ScheduleActive") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'scheduler',this.checked)\" " + strChecked + "/>";
                        string schedule = "";
                        for (int index = 0; index < cw.ScheduleDetails.Length; index++)
                        {
                            string s = cw.ScheduleDetails[index];
                            if (s != "")
                            {
                                schedule += s + "<br/>";
                            }
                        }
                        if (schedule != "")
                            html +=
                                "<div class=\"settings_scheduler\">" +
                                schedule + "</div>";
                        html += "</td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("RecordingSettings") + "</strong></td></tr>";
                        
                        strChecked = "";

                        if (!cw.Camobject.detector.recordondetect && !cw.Camobject.detector.recordondetect)
                            strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("NoRecord") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordoff',this.checked)\" " + strChecked + "/></td></tr>";

                        strChecked = "";

                        if (cw.Camobject.detector.recordondetect) strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("RecordOnDetect") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordondetect',this.checked)\" " + strChecked + "/></td></tr>";

                        strChecked = "";
                        
                        if (cw.Camobject.detector.recordonalert) strChecked = "checked=\"checked\"";
                        
                        html += "<tr><td>" + LocRm.GetString("RecordOnAlert") +
                                 "</td><td><input type=\"radio\" name=\"record_opts\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'recordonalert',this.checked)\" " + strChecked + "/></td></tr>";
                       
                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("TimelapseRecording") +
                                 "</strong></td></tr>";

                        strChecked = "";
                        if (cw.Camobject.recorder.timelapseenabled) strChecked = "checked=\"checked\"";
                        html += "<tr><td>" + LocRm.GetString("TimelapseRecording") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'timelapseon',this.checked)\" " + strChecked + "/></td></tr>";
                        html += "<tr><td>" + LocRm.GetString("Movie") +
                                 "</td><td><input style=\"width:50px\" type=\"text\" value=\"" +
                                 cw.Camobject.recorder.timelapse + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'timelapse',this.value)\"/> " +
                                 LocRm.GetString("savesAFrameToAMovieFileNS") + "</td></tr>";
                        html += "<tr><td>" + LocRm.GetString("Images") +
                                 "</td><td><input style=\"width:50px\" type=\"text\" value=\"" +
                                 cw.Camobject.recorder.timelapseframes + "\" onblur=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'timelapseframes',this.value)\"/> " +
                                 LocRm.GetString("savesAFrameEveryNSecondsn") + "</td></tr>";

                        html += "<tr><td colspan=\"2\"><strong>" + LocRm.GetString("SaveFramesFtp") +
                                 "</strong></td></tr>";


                        strChecked = "";

                        if (cw.Camobject.ftp.enabled)
                            strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("FtpEnabled") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'ftpenabled',this.checked)\" " + strChecked + "/></td></tr>";

                        strChecked = "";

                        if (cw.Camobject.ftp.savelocal)
                            strChecked = "checked=\"checked\"";

                        html += "<tr><td>" + LocRm.GetString("LocalSavingEnabled") +
                                 "</td><td><input type=\"checkbox\" onclick=\"send_changesetting(" + otid + "," +
                                 oid + "," + port + ",'localsaving',this.checked)\" " + strChecked + "/></td></tr>";


                        html += "</table>";
                        resp += html.Replace("\"", "\\\"");
                    }
                    break;
                case "getcmdlist":
                    var l = "";
                    foreach (objectsCommand ocmd in MainForm.RemoteCommands)
                    {
                        string n = ocmd.name;
                        if (n.StartsWith("cmd_"))
                        {
                            n = LocRm.GetString(ocmd.name);
                        }
                        l += ocmd.id + "|" + n.Replace("|", " ").Replace(",", " ") + ",";
                    }
                    resp = l.Trim(',');
                    break;
                case "previewlist":
                    resp = "";
                    var top100 =
                        MainForm.MasterFileList.Where(f => f.ObjectTypeId==2).OrderByDescending(
                            p => p.CreatedDateTicks).Take(MainForm.Conf.PreviewItems).ToList();
                    foreach (var file in top100)
                    {
                        resp += file.Filename + "|" + file.Name.Replace("|","")+"|"+file.Duration + ","; //AlertData is name of camera
                    }
                    resp = resp.Trim(',');
                    if (resp == "")
                        resp = "OK";
                    break;
                case "getobjectconfig":
                    {
                        string cfg = "";
                        switch (otid)
                        {
                            case 1:
                                VolumeLevel vl = _parent.GetVolumeLevel(oid);
                                if (vl != null)
                                {
                                    cfg = "ot: 1, oid:" + oid + ", port: " + MainForm.Conf.ServerPort + ", online: " +
                                          vl.IsEnabled.ToString().ToLower() + ",recording: " +
                                          vl.ForcedRecording.ToString().ToLower() + ", width:320, height:40";
                                }
                                break;
                            case 2:
                                CameraWindow cw = _parent.GetCameraWindow(oid);
                                if (cw != null)
                                {
                                    string[] res = cw.Camobject.resolution.Split('x');
                                    string micpairid = "-1";
                                    if (cw.VolumeControl != null)
                                        micpairid = cw.VolumeControl.Micobject.id.ToString(CultureInfo.InvariantCulture);
                                    cfg = "ot: 2, oid:" + oid + ", micpairid: " + micpairid + ", port: " +
                                          MainForm.Conf.ServerPort + ",online: " + cw.IsEnabled.ToString().ToLower() +
                                          ",recording: " + cw.ForcedRecording.ToString().ToLower() + ", width:" + res[0] +
                                          ", height:" + res[1] + ", talk:" +
                                          (cw.Camobject.settings.audiomodel != "None").ToString().ToLower();
                                }
                                break;
                        }
                        func = func.Replace("cfg", "{" + cfg + "}");
                        resp = "OK";
                    }
                    break;
                case "togglealertmode":
                    {
                        switch (otid)
                        {
                            case 1:
                                VolumeLevel vl = _parent.GetVolumeLevel(oid);
                                if (vl != null)
                                {
                                        switch (vl.Micobject.alerts.mode)
                                        {
                                            case "sound":
                                                vl.Micobject.alerts.mode = "nosound";
                                                break;
                                            case "nosound":
                                                vl.Micobject.alerts.mode = "sound";
                                                break;
                                        }
                                }
                                break;
                            case 2:
                                CameraWindow cw = _parent.GetCameraWindow(oid);
                                if (cw != null)
                                {
                                    switch (cw.Camobject.alerts.mode)
                                    {
                                        case "movement":
                                            cw.Camobject.alerts.mode = "nomovement";
                                            break;
                                        case "nomovement":
                                            cw.Camobject.alerts.mode = "movement";
                                            break;
                                    }
                                }
                                break;
                        }
                        resp = "OK";
                    }
                    break;
            }
            if (func!="")
                resp = func.Replace("result", "\"" + resp + "\"");
            return resp;
        }

        private static string GetDirectory(int objectTypeId, int objectId)
        {
            if (objectTypeId == 1)
            {
                return MainForm.Microphones.Single(p => p.id == objectId).directory;
            }
            return MainForm.Cameras.Single(p => p.id == objectId).directory;
        }

        private static void GetDirectoryPath(String sRequest, String sMyWebServerRoot, out String sLocalDir,
                                             out String sDirName)
        {
            try
            {
                sDirName = sRequest.Substring(sRequest.IndexOf("/", StringComparison.Ordinal));
                sDirName = sDirName.Substring(0, sDirName.LastIndexOf("/", StringComparison.Ordinal));

                if (sDirName == "/")
                    sLocalDir = sMyWebServerRoot;
                else
                {
                    if (sDirName.ToLower().StartsWith(@"/video/"))
                    {
                        sLocalDir = MainForm.Conf.MediaDirectory + "video\\";
                        string sfile = sRequest.Substring(sRequest.LastIndexOf("/", StringComparison.Ordinal) + 1);
                        int iind = Convert.ToInt32(sfile.Substring(0, sfile.IndexOf("_", StringComparison.Ordinal)));
                        sLocalDir += GetDirectory(2, iind) + "\\";
                        if (sfile.Contains(".jpg"))
                            sLocalDir += "thumbs\\";
                    }
                    else
                    {
                        if (sDirName.ToLower().StartsWith(@"/audio/"))
                        {
                            sLocalDir = MainForm.Conf.MediaDirectory + "audio\\";
                            string sfile = sRequest.Substring(sRequest.LastIndexOf("/", StringComparison.Ordinal) + 1);
                            int iind = Convert.ToInt32(sfile.Substring(0, sfile.IndexOf("_", StringComparison.Ordinal)));
                            sLocalDir += GetDirectory(1, iind) + "\\";
                        }
                        else
                            sLocalDir = sMyWebServerRoot + sDirName.Replace("../", "").Replace("/", @"\");
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogErrorToFile("Failed to get path for request: "+sRequest+" ("+sMyWebServerRoot+") - "+ex.Message);
                sLocalDir = "";
                sDirName = "";
            }
        }

        private void ParseMimeType(String sRequestedFile, out string sFileName, out String sMimeType)
        {
            sFileName = sRequestedFile;

            int i = sFileName.IndexOf("?", StringComparison.Ordinal);
            if (i != -1)
                sFileName = sFileName.Substring(0, i);
            i = sFileName.IndexOf("&", StringComparison.Ordinal);
            if (i != -1)
                sFileName = sFileName.Substring(0, i);
            
            sMimeType = GetMimeType(sFileName);
            if (sMimeType=="")
                sMimeType = "text/javascript";
        }

        private static bool CheckAuth(String sPhysicalFilePath)
        {
            return GetVar(sPhysicalFilePath,"auth") == MainForm.Identifier;
        }

        private void SendLogFile(string sHttpVersion, ref Socket mySocket)
        {
            var fi = new FileInfo(Program.AppDataPath + "log_" + MainForm.NextLog + ".htm");
            int iTotBytes = Convert.ToInt32(fi.Length);
            byte[] bytes;
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                using (var reader = new BinaryReader(fs))
                {
                    bytes = new byte[iTotBytes];
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    bytes = reader.ReadBytes(bytes.Length);
                    reader.Close();
                }
                fs.Close();
            }

            SendHeader(sHttpVersion, "text/html", iTotBytes, " 200 OK", 20, ref mySocket);
            SendToBrowser(bytes, mySocket);
        }

        private void SendLogFile(String sPhysicalFilePath, string sHttpVersion, ref Socket mySocket)
        {
            string fn = GetVar(sPhysicalFilePath, "fn");
            //prevent filesystem access
            if (fn.IndexOf("./", StringComparison.Ordinal) != -1)
                return;

            var fi = new FileInfo(Program.AppDataPath + fn);
            int iTotBytes = Convert.ToInt32(fi.Length);
            byte[] bytes;
            using (var fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {

                using (var reader = new BinaryReader(fs))
                {
                    bytes = new byte[iTotBytes];
                    reader.BaseStream.Seek(0, SeekOrigin.Begin);
                    bytes = reader.ReadBytes(bytes.Length);
               }
            }

            SendHeader(sHttpVersion, "text/html", iTotBytes, " 200 OK", 20, ref mySocket);
            SendToBrowser(bytes, mySocket);
        }
        
        private static byte[] _cameraRemoved;
        private static byte[] CameraRemoved
        {
            get
            {
                if (_cameraRemoved == null)
                {
                    using (var ms = new MemoryStream())
                    {
                        Resources.cam_removed.Save(ms, ImageFormat.Jpeg);
                        _cameraRemoved = new Byte[ms.Length];
                        ms.Position = 0;
                        // load the byte array with the image
                        ms.Read(_cameraRemoved, 0, (int)ms.Length);
                        ms.Close();
                    }
                }
                return _cameraRemoved;
            }   
        }

            
        private static byte[] _cameraConnecting;
        private static byte[] CameraConnecting
        {
            get
            {
                if (_cameraConnecting == null)
                {
                    using (var ms = new MemoryStream())
                    {
                        Resources.cam_connecting.Save(ms, ImageFormat.Jpeg);
                        _cameraConnecting = new Byte[ms.Length];
                        ms.Position = 0;
                        // load the byte array with the image
                        ms.Read(_cameraConnecting, 0, (int) ms.Length);
                    }
                }
                return _cameraConnecting;
            }   
        }

        private static byte[] _cameraOffline;
        private static byte[] CameraOffline
        {
            get
            {
                if (_cameraOffline == null)
                {
                    using (var ms = new MemoryStream())
                    {
                        Resources.cam_offline.Save(ms, ImageFormat.Jpeg);
                        _cameraOffline = new Byte[ms.Length];
                        ms.Position = 0;
                        // load the byte array with the image
                        ms.Read(_cameraOffline, 0, (int)ms.Length);
                    }
                }
                return _cameraOffline;
            }
        }

        private void SendLiveFeed(String sPhysicalFilePath, string sHttpVersion, ref Socket mySocket)
        {
            string cameraId = GetVar(sPhysicalFilePath, "oid");
            
            try
            {
                CameraWindow cw = _parent.GetCameraWindow(Convert.ToInt32(cameraId));
                if (cw == null)
                {
                    SendHeader(sHttpVersion, "image/jpeg", CameraRemoved.Length, " 200 OK", 0, ref mySocket);
                    SendToBrowser(CameraRemoved, mySocket);
                }
                else
                {
                    if (!cw.Camobject.settings.active)
                    {
                        SendHeader(sHttpVersion, "image/jpeg", CameraOffline.Length, " 200 OK", 0, ref mySocket);
                        SendToBrowser(CameraOffline, mySocket);
                    }
                    else
                    {
                        if (cw.LastFrameNull)
                        {
                            SendHeader(sHttpVersion, "image/jpeg", CameraConnecting.Length, " 200 OK", 0, ref mySocket);
                            SendToBrowser(CameraConnecting, mySocket);
                        }
                        else
                        {
                            Bitmap b = cw.LastFrame;
                            using (var imageStream = new MemoryStream())
                            {

                                int w = 320, h = 240;
                                bool done = false;
                                if (sPhysicalFilePath.IndexOf("thumb", StringComparison.Ordinal) != -1)
                                {
                                    w = 96;
                                    h = 72;
                                }
                                else
                                {
                                    if (sPhysicalFilePath.IndexOf("full", StringComparison.Ordinal) != -1)
                                    {
                                        b.Save(imageStream, ImageFormat.Jpeg);
                                        done = true;
                                    }
                                    else
                                    {
                                        string size = GetVar(sPhysicalFilePath, "size");
                                        GetWidthHeight(size, out w, out h);
                                    }
                                }

                                if (!done)
                                {
                                    Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
                                    Image myThumbnail = b.GetThumbnailImage(w, h, myCallback, IntPtr.Zero);

                                    // put the image into the memory stream

                                    myThumbnail.Save(imageStream, ImageFormat.Jpeg);
                                    myThumbnail.Dispose();
                                }


                                // make byte array the same size as the image

                                var imageContent = new Byte[imageStream.Length];
                                imageStream.Position = 0;
                                // load the byte array with the image
                                imageStream.Read(imageContent, 0, (int) imageStream.Length);

                                // rewind the memory stream


                                SendHeader(sHttpVersion, "image/jpeg", (int) imageStream.Length, " 200 OK", 0,
                                           ref mySocket);

                                SendToBrowser(imageContent, mySocket);
                                b.Dispose();
                                imageStream.Close();
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void SendImage(String sPhysicalFilePath, string sHttpVersion, ref Socket mySocket)
        {
            int oid = Convert.ToInt32(GetVar(sPhysicalFilePath, "oid"));
            string fn = GetVar(sPhysicalFilePath, "fn");
            
            try
            {
                CameraWindow cw = _parent.GetCameraWindow(Convert.ToInt32(oid));
                if (cw == null)
                {
                    SendHeader(sHttpVersion, "image/jpeg", CameraRemoved.Length, " 200 OK", 0, ref mySocket);
                    SendToBrowser(CameraRemoved, mySocket);
                }
                else
                {
                    string sFileName = MainForm.Conf.MediaDirectory + "Video/" + cw.Camobject.directory +
                                       "/thumbs/" + fn;

                    if (!File.Exists(sFileName))
                    {
                        sFileName = Program.AppPath + @"WebServerRoot\notfound.jpg";
                    }


                    using (var fs = new FileStream(sFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        string size = GetVar(sPhysicalFilePath, "size");
                        byte[] bytes;
                        if (size != "")
                        {
                            int w,h;
                            
                            GetWidthHeight(size, out w, out h);
                            Image myThumbnail = Image.FromStream(fs).GetThumbnailImage(w, h, ThumbnailCallback,
                                                                                       IntPtr.Zero);

                            // put the image into the memory stream
                            using (var ms = new MemoryStream())
                            {
                                myThumbnail.Save(ms, ImageFormat.Jpeg);
                                myThumbnail.Dispose();

                                bytes = new Byte[ms.Length];
                                ms.Position = 0;
                                // load the byte array with the image
                                ms.Read(bytes, 0, (int) ms.Length);
                            }
                        }
                        else
                        {

                            using (var reader = new BinaryReader(fs))
                            {
                                bytes = new byte[fs.Length];
                                while ((reader.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                }
                            }
                        }
                        SendHeader(sHttpVersion, "image/jpeg", bytes.Length, " 200 OK", 30, ref mySocket);
                        SendToBrowser(bytes, mySocket);
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void GetWidthHeight(string size, out int w, out int h)
        {
            string[] wh = size.Split('x');
            w = 320;
            h = 240;
            if (wh.Length == 2)
            {
                double dw, dh;
                double.TryParse(wh[0], out dw);
                double.TryParse(wh[1], out dh);
                w = Convert.ToInt32(dw);
                h = Convert.ToInt32(dh);
            }
        }

        private void SendGrab(String sPhysicalFilePath, string sHttpVersion, ref Socket mySocket)
        {
            int oid = Convert.ToInt32(GetVar(sPhysicalFilePath, "oid"));
            string fn = GetVar(sPhysicalFilePath, "fn");
            try
            {
                CameraWindow cw = _parent.GetCameraWindow(Convert.ToInt32(oid));
                if (cw == null)
                {
                    SendHeader(sHttpVersion, "image/jpeg", CameraRemoved.Length, " 200 OK", 0, ref mySocket);
                    SendToBrowser(CameraRemoved, mySocket);
                }
                else
                {
                    string sFileName = MainForm.Conf.MediaDirectory + "Video/" + cw.Camobject.directory +
                                       "/grabs/" + fn;

                    if (!File.Exists(sFileName))
                    {
                        sFileName = Program.AppPath + @"WebServerRoot\notfound.jpg";
                    }
                    using (var fs =
                        new FileStream(sFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // Create a reader that can read bytes from the FileStream.
                        string size = GetVar(sPhysicalFilePath, "size");
                        byte[] bytes;
                        if (size != "")
                        {
                            int w, h;
                            GetWidthHeight(size, out w, out h);
                            Image myThumbnail = Image.FromStream(fs).GetThumbnailImage(w, h, ThumbnailCallback,
                                                                                       IntPtr.Zero);

                            // put the image into the memory stream
                            var ms = new MemoryStream();
                            myThumbnail.Save(ms, ImageFormat.Jpeg);
                            myThumbnail.Dispose();

                            bytes = new Byte[ms.Length];
                            ms.Position = 0;
                            // load the byte array with the image
                            ms.Read(bytes, 0, (int) ms.Length);
                            ms.Close();
                            ms.Dispose();
                        }
                        else
                        {

                            using (var reader = new BinaryReader(fs))
                            {
                                bytes = new byte[fs.Length];
                                while ((reader.Read(bytes, 0, bytes.Length)) != 0)
                                {
                                }


                                reader.Close();
                                fs.Close();
                            }
                        }
                        SendHeader(sHttpVersion, "image/jpeg", bytes.Length, " 200 OK", 30, ref mySocket);
                        SendToBrowser(bytes, mySocket);
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void SendFloorPlanFeed(String sPhysicalFilePath, string sHttpVersion, ref Socket mySocket)
        {
            string floorplanid = GetVar(sPhysicalFilePath, "floorplanid");
            try
            {
                FloorPlanControl fpc = _parent.GetFloorPlan(Convert.ToInt32(floorplanid));
                if (fpc == null)
                {
                    SendHeader(sHttpVersion, "image/jpeg", CameraRemoved.Length, " 200 OK", 0, ref mySocket);
                    SendToBrowser(CameraRemoved, mySocket);

                }
                else
                {
                    if (fpc.ImgPlan==null)
                    {
                        SendHeader(sHttpVersion, "image/jpeg", CameraConnecting.Length, " 200 OK", 0, ref mySocket);
                        SendToBrowser(CameraConnecting, mySocket);
                    }
                    else
                    {
                        int w = 320, h = 240;
                        bool done = false;
                        using (var ms = new MemoryStream())
                        {
                                if (sPhysicalFilePath.IndexOf("thumb", StringComparison.Ordinal) != -1)
                                {
                                w = 96;
                                h = 72;
                                }
                                else
                                {
                                    if (sPhysicalFilePath.IndexOf("full", StringComparison.Ordinal) != -1)
                                    {
                                        fpc.ImgView.Save(ms, ImageFormat.Jpeg);
                                        done = true;
                                    }
                                    else
                                    {
                                        string size = GetVar(sPhysicalFilePath, "size");
                                        if (size!="")
                                        {
                                            GetWidthHeight(size, out w, out h);
                                        }
                                    }
                                }


                            if (!done)
                            {
                                Image.GetThumbnailImageAbort myCallback = ThumbnailCallback;
                                var img = (Image) fpc.ImgView.Clone();
                                var myThumbnail = img.GetThumbnailImage(w, h, myCallback, IntPtr.Zero);

                                // put the image into the memory stream

                                myThumbnail.Save(ms, ImageFormat.Jpeg);
                                myThumbnail.Dispose();
                                img.Dispose();
                            }


                            // make byte array the same size as the image

                            var imageContent = new Byte[ms.Length];
                            ms.Position = 0;
                            // load the byte array with the image
                            ms.Read(imageContent, 0, (int) ms.Length);

                            // rewind the memory stream


                            SendHeader(sHttpVersion, "image/jpeg", (int) ms.Length, " 200 OK", 0, ref mySocket);

                            SendToBrowser(imageContent, mySocket);
                            ms.Close();
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void SendMJPEGFeed(String sPhysicalFilePath, Socket mySocket)
        {
            string scamid = GetVar(sPhysicalFilePath,"oid");
            string size = GetVar(sPhysicalFilePath, "size");
            bool basicCt = GetVar(sPhysicalFilePath, "basicct") != "";
            bool maintainAR = GetVar(sPhysicalFilePath, "keepAR") == "true";
            int w = 320, h = 240;
            
            if (size != "")
            {
                GetWidthHeight(size, out w, out h);           
            }
            if (sPhysicalFilePath.IndexOf("thumb", StringComparison.Ordinal) != -1)
            {
                w = 96;
                h = 72;
            }
            else
            {
                if (sPhysicalFilePath.IndexOf("full", StringComparison.Ordinal) != -1)
                {
                    w = -1;
                    h = -1;
                }
            }

            try
            {
                var feed2 = new Thread(p => MJPEGFeedMulti(scamid, mySocket, w, h, basicCt, maintainAR));
                feed2.Start();
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        private void MJPEGFeedMulti(string cameraids, Socket mySocket, int w, int h, bool basicContentType, bool maintainAspectRatio)
        {
            String sResponse = "";

            sResponse += "HTTP/1.1 200 OK\r\n";
            sResponse += "Server: iSpy\r\n";
            sResponse += "Expires: 0\r\n";
            sResponse += "Pragma: no-cache\r\n";
            sResponse += "Cache-Control: no-cache, must-revalidate\r\n";
            if (!basicContentType)
                sResponse += "Content-Type: multipart/x-mixed-replace; boundary=--myboundary";
            else
                sResponse += "Content-Type: text/html; boundary=--myboundary";
            var overlayBackgroundBrush = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
            var drawfont = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Regular, GraphicsUnit.Pixel);
            try
            {
                var cams = new List<CameraWindow>();
                string[] camids = cameraids.Split(',');
                bool nw = w == -1;
                if (nw)
                {
                    w = 0;
                    h = 0;
                }
                foreach(string c in camids)
                {
                    if (!String.IsNullOrEmpty(c))
                    {
                        var cw = _parent.GetCameraWindow(Convert.ToInt32(c));
                        if (cw != null)
                        {
                            if (nw)
                            {
                                w += cw.Camobject.width;
                                h += cw.Camobject.height;
                            }
                            cams.Add(cw);
                        }
                    }
                }
                if (cams.Count == 0)
                {
                    throw new Exception("No cameras found");
                }
                int cols = Convert.ToInt32(Math.Ceiling(Math.Sqrt(cams.Count)));
                int rows = Convert.ToInt32(Math.Ceiling(Convert.ToDouble(cams.Count)/cols));
                
                int camw = Convert.ToInt32(Convert.ToDouble(w)/Convert.ToDouble(cols));
                int camh = Convert.ToInt32(Convert.ToDouble(h) / Convert.ToDouble(rows));

                
                while (mySocket.Connected)
                {
                    var bmpFinal = new Bitmap(w, h);
                    Graphics g = Graphics.FromImage(bmpFinal);
                    g.CompositingQuality = CompositingQuality.HighSpeed;
                    g.PixelOffsetMode = PixelOffsetMode.Half;
                    g.SmoothingMode = SmoothingMode.None;
                    g.InterpolationMode = InterpolationMode.Default;
                    g.Clear(Color.White);
                    int j = 0, k = 0;

                    foreach (CameraWindow cw in cams)
                    {
                        int x = j*camw;
                        int y = k*camh;
                        j++;
                        if (j == cols)
                        {
                            j = 0;
                            k++;
                        }
                        Image img = Resources.cam_removed;
                        if (!cw.IsDisposed &&  !cw.Disposing)
                        {
                            if (cw.LastFrameNull)
                            {
                                img = Resources.cam_offline;
                            }
                            else
                            {
                                img = cw.LastFrame;
                            }
                        }
                        if (maintainAspectRatio)
                        {
                            double ar = Convert.ToDouble(img.Height)/Convert.ToDouble(img.Width);
                            int neww = camw;
                            int newh = Convert.ToInt32(camw*ar);
                            if (newh > camh)
                            {
                                newh = camh;
                                neww = Convert.ToInt32(camh/ar);
                            }
                            //offset for centering
                            try
                            {
                                g.DrawImage(img, x + (camw - neww)/2, y + (camh - newh)/2, neww, newh);
                            }
                            catch (Exception)
                            {
                                //cam offline?
                            }
                        }
                        else
                        {
                            try
                            {
                                g.DrawImage(img, x, y, camw, camh);
                            }
                            catch (Exception)
                            {
                                //cam offline?
                            }
                        }
                        g.FillRectangle(overlayBackgroundBrush, x, y + camh - 20, camw, y + camh);
                        g.DrawString(cw.Camobject.name,drawfont,Brushes.White,x+2,y+camh-17);
                        

                    }

                    using (var imageStream = new MemoryStream())
                    {
                        bmpFinal.Save(imageStream, ImageFormat.Jpeg);

                        imageStream.Position = 0;
                        // load the byte array with the image             
                        bmpFinal.Dispose();
                        Byte[] imageArray = imageStream.GetBuffer();
                        sResponse +=
                            "\r\n\r\n--myboundary\r\nContent-type: image/jpeg\r\nContent-length: " +
                            imageArray.Length + "\r\n\r\n";

                        Byte[] bSendData = Encoding.ASCII.GetBytes(sResponse);

                        SendToBrowser(bSendData, mySocket);
                        sResponse = "";
                        SendToBrowser(imageArray, mySocket);
                        imageStream.Close();
                    }
                    Thread.Sleep(MainForm.Conf.MJPEGStreamInterval); //throttle it
                }

            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
            overlayBackgroundBrush.Dispose();
            drawfont.Dispose();
            DisconnectSocket(mySocket);

        }

        private void DisconnectSocket(Socket mySocket)
        {
            IPEndPoint endPoint;
            try
            {
                endPoint = (IPEndPoint) mySocket.RemoteEndPoint;
            }
            catch (ObjectDisposedException)
            {
                //can happen on shutdown
                return;
            }

            lock (_connectedSocketsSyncHandle)
            {
                _connectedSockets.Remove(endPoint);
            }

            mySocket.Close();

            //OnSocketDisconnected(endPoint);
            //try
            //{
            //    var lingerOption = new LingerOption(false,0);
            //    mySocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, lingerOption);
            //    mySocket.Shutdown(SocketShutdown.Send);
            //    try
            //    {
            //        var recBuff = new byte[1000];
            //        //clear pending buffer
            //        mySocket.ReceiveTimeout = 100;
            //        while (mySocket.Receive(recBuff) > 0)
            //        { }
            //    }
            //    catch
            //    {
            //        //ignore
            //    }
            //    mySocket.Close();
            //}
            //catch
            //{

            //}
            //MySockets.Remove(mySocket);
        }

        private void SendAudioFeed(Enums.AudioStreamMode streamMode, String sBuffer, String sPhysicalFilePath, Socket mySocket)
        {
            string micId = GetVar(sPhysicalFilePath, "micid");
            try
            {
                VolumeLevel vl = _parent.GetVolumeLevel(Convert.ToInt32(micId));
                if (vl.Micobject.settings.active && vl.AudioSource!=null)
                {
                    String sResponse = "";

                    sResponse += "HTTP/1.1 200 OK\r\n";
                    sResponse += "Server: iSpy\r\n";

                    bool sendend = false;

                    int iStartBytes = 0;
                    if (sBuffer.IndexOf("Range: bytes=", StringComparison.Ordinal) != -1)
                    {
                        var headers = sBuffer.Split(Environment.NewLine.ToCharArray());
                        for (int index = 0; index < headers.Length; index++)
                        {
                            string h = headers[index];
                            if (h.StartsWith("Range:"))
                            {
                                string[] range = (h.Substring(h.IndexOf("=", StringComparison.Ordinal) + 1)).Split('-');
                                iStartBytes = Convert.ToInt32(range[0]);
                                break;
                            }
                        }
                    }
                    if (iStartBytes != 0)
                    {
                        sendend = true;
                    }

                    switch (streamMode)
                    {
                        //case Enums.AudioStreamMode.PCM:
                        //    sResponse += "Content-Type: audio/x-wav\r\n";
                        //    sResponse += "Transfer-Encoding: chunked\r\n";
                        //    sResponse += "Connection: close\r\n";
                        //    sResponse += "\r\n";
                        //    break;
                        case Enums.AudioStreamMode.MP3:
                            sResponse += "Content-Type: audio/mpeg\r\n";
                            sResponse += "Transfer-Encoding: chunked\r\n";
                            sResponse += "Connection: close\r\n";
                            sResponse += "\r\n";
                            break;
                        //case Enums.AudioStreamMode.M4A:
                        //    sResponse += "Content-Type: audio/aac\r\n";
                        //    sResponse += "Transfer-Encoding: chunked\r\n";
                        //    sResponse += "Connection: close\r\n";
                        //    sResponse += "\r\n";
                        //    break;
                    }


                    Byte[] bSendData = Encoding.ASCII.GetBytes(sResponse);

                    SendToBrowser(bSendData, mySocket);

                    if (sendend)
                    {
                        SendToBrowser(Encoding.ASCII.GetBytes(0.ToString("X") + "\r\n"), mySocket);
                    }
                    else
                    {
                        //MySockets.Remove(mySocket);
                        vl.OutSockets.Add(mySocket);
                    }
                }
                else
                {
                    DisconnectSocket(mySocket);
                    NumErr = 0;
                
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }
        
        public string FormatBytes(long bytes)
        {
            const int scale = 1024;
            var orders = new[] {"GB", "MB", "KB", "Bytes"};
            var max = (long) Math.Pow(scale, orders.Length - 1);

            foreach (string order in orders)
            {
                if (bytes > max)
                    return String.Format(CultureInfo.InvariantCulture, "{0:##.##} {1}",
                                         decimal.Divide(bytes, max), order);

                max /= scale;
            }
            return "0 Bytes";
        }

        internal string GetObjectList()
        {
            
            string resp = "";
            if (MainForm.Cameras != null)
            {
                foreach (objectsCamera oc in MainForm.Cameras.OrderBy(p=>p.name))
                {
                    CameraWindow cw = _parent.GetCameraWindow(oc.id);
                    if (cw != null)
                    {
                        bool onlinestatus = !(!oc.settings.active || cw.VideoSourceErrorState);
                        bool talkconfigured = oc.settings.audiomodel != "None";
                        resp += "2," + oc.id + "," + onlinestatus.ToString().ToLower() + "," +
                                oc.name.Replace(",", "&comma;") + "," + GetStatus(onlinestatus) + "," +
                                oc.description.Replace(",", "&comma;").Replace("\n", " ") + "," +
                                oc.settings.accessgroups.Replace(",", "&comma;").Replace("\n", " ") + "," + oc.ptz + "," + talkconfigured.ToString().ToLower() +"," + oc.settings.micpair + Environment.NewLine;
                    }
                }
            }
            if (MainForm.Microphones != null)
            {
                foreach (objectsMicrophone om in MainForm.Microphones.OrderBy(p => p.name))
                {
                    VolumeLevel vl = _parent.GetVolumeLevel(om.id);
                    if (vl!=null)
                    {
                        bool onlinestatus = !(!om.settings.active || vl.AudioSourceErrorState);
                        resp += "1," + om.id + "," + onlinestatus.ToString().ToLower() + "," +
                            om.name.Replace(",", "&comma;") + "," + GetStatus(om.settings.active) + "," +
                            om.description.Replace(",", "&comma;").Replace("\n", " ") + "," +
                            om.settings.accessgroups.Replace(",", "&comma;").Replace("\n", " ") + Environment.NewLine;
                    }
                }
            }

            resp += "OK";
            return resp;
        }

        internal static string GetStatus(bool active)
        {
            string sts = "Online";
            if (!active)
            {
                sts = "Offline";
            }
            return sts;
        }

        private void AudioIn(Socket mySocket, int cameraId)
        {
            CameraWindow cw = _parent.GetCameraWindow(cameraId);

            ITalkTarget talkTarget = null;
            var ds = new AudioInStream { RecordingFormat = new WaveFormat(22050, 16, 1) };

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
            }
            if (talkTarget != null)
            {
                ds.Start();
                talkTarget.Start();
                ds.PacketSize = 4410;
                var bBuffer = new byte[ds.PacketSize*4];
                //IWavePlayer WaveOut = new DirectSoundOut(100);
                //WaveOut.Init(ds.WaveOutProvider);
                //WaveOut.Play();
                try
                {
                    int j = 0;
                    //DateTime dtStart = DateTime.Now;
                    bool pktComplete = false;
                    DateTime dt = DateTime.Now;
                    while (mySocket.Connected) // && talkTarget.Connected)
                    {
                        while (!pktComplete && mySocket.Connected)
                        {
                           // DateTime sR = DateTime.Now;

                            int i = mySocket.Receive(bBuffer, j, ds.PacketSize, SocketFlags.None);
                            if (i == 0)
                                goto Finish;
                            j += i;
                            while (j >= ds.PacketSize)
                            {
                                var data = new byte[ds.PacketSize];
                                Buffer.BlockCopy(bBuffer, 0, data, 0, ds.PacketSize);
                                ds.AddSamples(data);
                                int ms = Convert.ToInt32((DateTime.Now - dt).TotalMilliseconds);
                                if (ms < 40)
                                    Thread.Sleep(40 - ms);
                                dt = DateTime.Now;
                                pktComplete = true;
                                Buffer.BlockCopy(bBuffer, ds.PacketSize, bBuffer, 0, j - ds.PacketSize);
                                j = j - ds.PacketSize;

                            }
                        }
                        pktComplete = false;

                        //Thread.Sleep(50);
                    }
                }
                catch
                {
                    
                }
            Finish:
                DisconnectSocket(mySocket);
                ds.Stop();
                talkTarget.Stop();
                talkTarget = null;
                ds = null;
            }
        }


        private void SynthToCam(string text, int cameraId)
        {
            var synthFormat = new System.Speech.AudioFormat.SpeechAudioFormatInfo(System.Speech.AudioFormat.EncodingFormat.Pcm, 11025, 16, 1, 22100, 2, null);
            using (var synthesizer = new SpeechSynthesizer())
            {
                using (var waveStream = new MemoryStream())
                {

                    //write some silence to the stream to allow camera to initialise properly
                    var silence = new byte[1*22050];
                    waveStream.Write(silence, 0, silence.Count());

                    var pbuilder = new PromptBuilder();
                    var pStyle = new PromptStyle
                                     {
                                         Emphasis = PromptEmphasis.Strong,
                                         Rate = PromptRate.Slow,
                                         Volume = PromptVolume.Loud
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
                    CameraWindow cw = _parent.GetCameraWindow(cameraId);

                    ITalkTarget talkTarget = null;

                    var ds = new DirectStream(waveStream) {RecordingFormat = new WaveFormat(11025, 16, 1)};
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
                            talkTarget = new TalkNetworkKinect(cw.Camobject.settings.audioip,cw.Camobject.settings.audioport,ds);
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
                    }
                    if (talkTarget != null)
                    {
                        ds.Start();
                        talkTarget.Start();
                        while (ds.IsRunning)
                        {
                            Thread.Sleep(100);
                        }
                        ds.Stop();
                        talkTarget.Stop();
                        talkTarget = null;
                        ds = null;
                    }
                    waveStream.Close();    
                }
            }


        }
    }
}