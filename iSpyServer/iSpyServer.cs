using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Collections;
using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using g711audio;
using iSpyServer;
using NAudio.Wave;

namespace iSpyServer
{
    public class RemoteCommandEventArgs : EventArgs
    {
        public string Command;
        public int ObjectID;
        public int ObjectTypeID;
        
        // Constructor
        public RemoteCommandEventArgs(string _command, int _objectid, int _objecttypeid)
        {
            Command = _command;
            ObjectID = _objectid;
            ObjectTypeID = _objecttypeid;
        }
    }
    public class iSpyLANServer
    {
        private TcpListener myListener = null;
        private static Random r = new Random();
        public string ServerRoot;

        private MainForm Parent;
        private Thread th = null;
        private static readonly List<Socket> MySockets = new List<Socket>();
        private static int _socketindex;
        public int numErr = 0;

        //The constructor which make the TcpListener start listening on the
        //given port. It also calls a Thread on the method StartListen(). 
        public iSpyLANServer(MainForm _parent)
        {
            Parent = _parent;
        }

        public bool Running
        {

            get
            {
                if (th == null)
                    return false;
                return th.IsAlive;
            }
        }
        public void StartServer()
        {
            try
            {
                myListener = new TcpListener(IPAddress.Any, iSpyServer.Default.LANPort) {ExclusiveAddressUse = false};
                myListener.Start(200);
                //start the thread which calls the method 'StartListen'
                if (th != null)
                {
                    while (th.ThreadState == ThreadState.AbortRequested)
                    {
                        Application.DoEvents();
                    }
                }
                th = new Thread(new ThreadStart(StartListen));
                th.Start();
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("StartServer Error: " + e.Message);
                MainForm.LogExceptionToFile(e);
            }
        }

        public void StopServer()
        {
            for (int i = 0; i < MySockets.Count; i++)
            {
                Socket mySocket = MySockets[i];
                if (mySocket != null)
                {
                    try
                    {
                        if (mySocket.Connected)
                            mySocket.Shutdown(SocketShutdown.Both);
                        mySocket.Close();
                        mySocket = null;
                    }
                    catch
                    {
                        try
                        {
                            mySocket.Close();
                        }
                        catch { }

                        mySocket = null;
                    }
                }
            }

            Application.DoEvents();
            if (myListener != null)
            {
                try
                {
                    myListener.Stop();
                    myListener = null;
                }
                catch (Exception)
                {

                }
            }
            Application.DoEvents();
            if (th != null)
            {
                try
                {
                    if (th.ThreadState == ThreadState.Running)
                        th.Abort();
                    //while (th.ThreadState == ThreadState.AbortRequested)
                    //{
                    //    Application.DoEvents();
                    //}
                }
                catch (Exception)
                {

                }
                Application.DoEvents();
                th = null;
            }
        }

        public void SendHeader(string sHttpVersion, string sMIMEHeader, int iTotBytes, string sStatusCode, int CacheDays,  ref Socket _socket)
        {

            String sBuffer = "";

            // if Mime type is not provided set default to text/html
            if (sMIMEHeader.Length == 0)
            {
                sMIMEHeader = "text/html";  // Default Mime Type is text/html
            }

            sBuffer += sHttpVersion + sStatusCode + "\r\n";
            sBuffer += "Server: iSpyServer\r\n";
            sBuffer += "Content-Type: " + sMIMEHeader + "\r\n";
            //sBuffer += "X-Content-Type-Options: nosniff\r\n";
            sBuffer += "Accept-Ranges: bytes\r\n";
            sBuffer += "Access-Control-Allow-Origin: *\r\n";
            sBuffer += "Content-Length: " + iTotBytes + "\r\n";
            //sBuffer += "Cache-Control:Date: Tue, 25 Jan 2011 08:18:53 GMT\r\nExpires: Tue, 08 Feb 2011 05:06:38 GMT\r\nConnection: keep-alive\r\n";
            if (CacheDays > 0)
            {
                //this is needed for video content to work in chrome/android
                DateTime d = DateTime.UtcNow;
                sBuffer += "Cache-Control: Date: " + d.ToUniversalTime().ToString("r") + "\r\nLast-Modified: Tue, 01 Jan 2011 12:00:00 GMT\r\nExpires: " + d.AddDays(CacheDays).ToUniversalTime().ToString("r") + "\r\nConnection: keep-alive\r\n";
            }

            sBuffer += "\r\n";

            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

            SendToBrowser(bSendData, ref _socket);
            //Console.WriteLine("Total Bytes : " + iTotBytes);

        }

        public void SendHeaderWithRange(string sHttpVersion, string sMIMEHeader, int iStartBytes, int iEndBytes, int iTotBytes, string sStatusCode, int CacheDays, ref Socket _socket)
        {

            String sBuffer = "";

            // if Mime type is not provided set default to text/html
            if (sMIMEHeader.Length == 0)
            {
                sMIMEHeader = "text/html";  // Default Mime Type is text/html
            }

            sBuffer += sHttpVersion + sStatusCode + "\r\n";
            sBuffer += "Server: iSpy\r\n";
            sBuffer += "Content-Type: " + sMIMEHeader + "\r\n";
            //sBuffer += "X-Content-Type-Options: nosniff\r\n";
            sBuffer += "Accept-Ranges: bytes\r\n";
            sBuffer += "Content-Range: bytes " + iStartBytes + "-" + iEndBytes + "/" + (iTotBytes) + "\r\n";
            sBuffer += "Content-Length: " + (iEndBytes - iStartBytes + 1) + "\r\n";
            if (CacheDays > 0)
            {
                //this is needed for video content to work in chrome/android
                DateTime d = DateTime.UtcNow;
                sBuffer += "Cache-Control: Date: " + d.ToUniversalTime().ToString("r") + "\r\nLast-Modified: Tue, 01 Jan 2011 12:00:00 GMT\r\nExpires: " + d.AddDays(CacheDays).ToUniversalTime().ToString("r") + "\r\nConnection: keep-alive\r\n";
            }

            sBuffer += "\r\n";
            Byte[] bSendData = Encoding.ASCII.GetBytes(sBuffer);

            SendToBrowser(bSendData, ref _socket);
            //Console.WriteLine("Total Bytes : " + iTotBytes);

        }



        /// <summary>
        /// Overloaded Function, takes string, convert to bytes and calls 
        /// overloaded sendToBrowserFunction.
        /// </summary>
        /// <param name="sData">The data to be sent to the browser(client)</param>
        /// <param name="_socket">Socket reference</param>
        public void SendToBrowser(String sData, ref Socket _socket)
        {
            SendToBrowser(Encoding.ASCII.GetBytes(sData), ref _socket);
        }



        /// <summary>
        /// Sends data to the browser (client)
        /// </summary>
        /// <param name="bSendData">Byte Array</param>
        /// <param name="_socket">Socket reference</param>
        public void SendToBrowser(Byte[] bSendData, ref Socket _socket)
        {
            try
            {
                if (_socket.Connected)
                {
                    int _sent = _socket.Send(bSendData);
                    if (_sent < bSendData.Length)
                    {
                        System.Diagnostics.Debug.WriteLine("Only sent " + _sent+" of "+bSendData.Length);
                    }
                    if (_sent==-1)
                        MainForm.LogExceptionToFile(new Exception("Socket Error cannot Send Packet"));
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Send To Browser Error: " + e.Message);
                MainForm.LogExceptionToFile(e);
            }
        }
        public bool ThumbnailCallback()
        {
            return false;
        }


        //This method Accepts new connection and
        //First it receives the welcome massage from the client,
        //Then it sends the Current date time to the Client.
        public void StartListen()
        {       

            while (Running && numErr<5)
            {
                //Accept a new connection
                try
                {
                    Socket mySocket = myListener.AcceptSocket();
                    if (MySockets.Count() < _socketindex + 1)
                    {
                        MySockets.Add(mySocket);
                    }
                    else
                        MySockets[_socketindex] = mySocket;

                    if (mySocket.Connected)
                    {
                        mySocket.NoDelay = true;
                        mySocket.ReceiveBufferSize = 8192;
                        mySocket.ReceiveTimeout = iSpyServer.Default.ServerReceiveTimeout;

                        try
                        {
                            //make a byte array and receive data from the client 
                            string sBuffer;
                            string sHttpVersion;

                            Byte[] bReceive = new Byte[1024];
                            mySocket.Receive(bReceive);
                            sBuffer = Encoding.ASCII.GetString(bReceive);                         

                            if (sBuffer.Substring(0,4) == "TALK")
                            {
                                var socket = mySocket;
                                var feed = new Thread(p => AudioIn(socket));
                                _socketindex++;
                                feed.Start();
                                continue;
                            }

                            if (sBuffer.Substring(0, 3) != "GET")
                            {
                                continue;
                            }

                            int iStartPos = sBuffer.IndexOf("HTTP", 1);

                            sHttpVersion = sBuffer.Substring(iStartPos, 8);
                            

                            int cid = -1, vid=-1, camid=-1;
                            int w = -1, h = -1;

                            string qs = sBuffer.Substring(4);
                            qs = qs.Substring(0, qs.IndexOf(" ")).Trim('/').Trim('?');
                            string[] nvs = qs.Split('&');

                            foreach (string s in nvs)
                            {
                                string[] nv = s.Split('=');
                                switch (nv[0].ToLower())
                                {
                                    case "c":
                                        cid = Convert.ToInt32(nv[1]);
                                        break;
                                    case "w":
                                        w = Convert.ToInt32(nv[1]);
                                        break;
                                    case "h":
                                        h = Convert.ToInt32(nv[1]);
                                        break;
                                    case "camid":
                                        camid = Convert.ToInt32(nv[1]); //mjpeg
                                        break;
                                    case "micid":
                                        vid = Convert.ToInt32(nv[1]);
                                        break;

                                }
                            }
                            if (cid != -1)
                                SendLiveFeed(cid, w, h, sHttpVersion, ref mySocket);
                            else
                            {
                                if (camid != -1)
                                {
                                    CameraWindow cw = Parent.GetCameraWindow(Convert.ToInt32(camid));
                                    if (cw.Camobject.settings.active)
                                    {
                                        String sResponse = "";

                                        sResponse += "HTTP/1.1 200 OK\r\n";
                                        sResponse += "Server: iSpy\r\n";
                                        sResponse += "Expires: 0\r\n";
                                        sResponse += "Pragma: no-cache\r\n";
                                        sResponse += "Content-Type: multipart/x-mixed-replace;boundary=--myboundary";


                                        Byte[] bSendData = Encoding.ASCII.GetBytes(sResponse);
                                        SendToBrowser(bSendData, mySocket);
                                        cw.OutSockets.Add(mySocket);
                                        _socketindex++;
                                        continue;
                                    }
                                }
                                else
                                {
                                    if (vid != -1)
                                    {
                                        VolumeLevel vl = Parent.GetMicrophone(Convert.ToInt32(vid));
                                        if (vl != null)
                                        {
                                            String sResponse = "";

                                            sResponse += "HTTP/1.1 200 OK\r\n";
                                            sResponse += "Server: iSpy\r\n";
                                            sResponse += "Expires: 0\r\n";
                                            sResponse += "Pragma: no-cache\r\n";
                                            sResponse += "Content-Type: multipart/x-mixed-replace;boundary=--myboundary";
                                            sResponse += "\r\n\r\n";
                                            Byte[] bSendData = Encoding.ASCII.GetBytes(sResponse);
                                            SendToBrowser(bSendData, mySocket);
                                            vl.OutSockets.Add(mySocket);

                                            _socketindex++;
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        string _resp = "iSpy server is running";
                                        SendHeader(sHttpVersion, "", _resp.Length, " 200 OK", 0, ref mySocket);
                                        SendToBrowser(_resp, ref mySocket);
                                    }
                                }
                            }
                            numErr = 0;
                        }
                        catch (SocketException ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Server Error (socket): " + ex.Message);
                            MainForm.LogExceptionToFile(ex);
                            numErr++;
                        }
                        mySocket.Close();
                        mySocket = null;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Server Error (generic): " + ex.Message);
                    MainForm.LogExceptionToFile(ex);
                    numErr++;
                }
            }
        }

       
        private void AudioIn(Socket mySocket)
        {
            var wf = new WaveFormat(8000,16,1);
            DirectSoundOut dso;
            if (String.IsNullOrEmpty(iSpyServer.Default.AudioOutDevice))
                dso = new DirectSoundOut(100);
            else
            {
                dso = new DirectSoundOut(Guid.Parse(iSpyServer.Default.AudioOutDevice));
            }
            var bwp = new BufferedWaveProvider(wf);
            dso.Init(bwp);
            dso.Play();
            var bBuffer = new byte[3200];
            try
            {
                while (mySocket.Connected)
                {
                    int i = mySocket.Receive(bBuffer, 0, 3200, SocketFlags.None);
                    byte[] dec;
                    ALawDecoder.ALawDecode(bBuffer, i, out dec);
                    bwp.AddSamples(dec, 0, dec.Length);
                    Thread.Sleep(100);
                }
            }
            catch(Exception ex)
            {
                mySocket.Close();
                mySocket = null;
            }
            dso.Stop();
            dso.Dispose();

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
                    int sent = socket.Send(bSendData);
                    if (sent < bSendData.Length)
                    {
                        //Debug.WriteLine("Only sent " + sent + " of " + bSendData.Length);
                    }
                    if (sent == -1)
                        MainForm.LogExceptionToFile(new Exception("Socket Error cannot Send Packet"));
                }
            }
            catch (Exception e)
            {
                //Debug.WriteLine("Send To Browser Error: " + e.Message);
                MainForm.LogExceptionToFile(e);
            }
        }
        
        private void SendLiveFeed(int CameraID, int Width, int Height, string sHttpVersion, ref Socket mySocket)
        {
            MemoryStream imageStream = null;
            try
            {
                CameraWindow _cw = this.Parent.GetCameraWindow(Convert.ToInt32(CameraID));
                if (_cw!=null && !_cw.Camera.LastFrameNull)
                {
                    if (!String.IsNullOrEmpty(_cw.Camobject.encodekey))
                    {
                        string msg = "Cannot view live feed when the stream is encrypted";
                        SendHeader(sHttpVersion, "text/html", msg.Length, " 200 OK", 0, ref mySocket);
                        SendToBrowser(msg, ref mySocket);
                        return;
                    }
                    imageStream = new MemoryStream();
                    Bitmap _b = _cw.Camera.LastFrame;

                    if (Width != -1)
                    {
                        Image.GetThumbnailImageAbort myCallback = new Image.GetThumbnailImageAbort(ThumbnailCallback);

                        Image myThumbnail = _b.GetThumbnailImage(Width, Height, myCallback, IntPtr.Zero);

                        // put the image into the memory stream

                        myThumbnail.Save(imageStream, ImageFormat.Jpeg);
                        myThumbnail.Dispose();
                    }
                    else
                    {
                        _b.Save(imageStream, ImageFormat.Jpeg);
                    }

                    // make byte array the same size as the image
                    byte[] imageContent;

                    imageContent = new Byte[imageStream.Length];
                    imageStream.Position = 0;
                    // load the byte array with the image
                    imageStream.Read(imageContent, 0, (int)imageStream.Length);

                    // rewind the memory stream


                    SendHeader(sHttpVersion, ".jpg", (int)imageStream.Length, " 200 OK", 0,ref mySocket);

                    SendToBrowser(imageContent, ref mySocket);
                    _b.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Server Error (livefeed): " + ex.Message);
                MainForm.LogExceptionToFile(ex);
            }
            if (imageStream!=null)
                imageStream.Dispose();
        }

    }

}