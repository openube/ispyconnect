using System.Globalization;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using AForge.Video;

namespace iSpyApplication.Video
{
    /// <summary>
    /// MJPEG video source.
    /// </summary>
    /// 
    /// <remarks><para>The video source downloads JPEG images from the specified URL, which represents
    /// MJPEG stream.</para>
    /// 
    /// <para>Sample usage:</para>
    /// <code>
    /// // create MJPEG video source
    /// MJPEGStream stream = new MJPEGStream2( "some url" );
    /// // set event handlers
    /// stream.NewFrame += new NewFrameEventHandler( video_NewFrame );
    /// // start the video source
    /// stream.Start( );
    /// // ...
    /// </code>
    /// 
    /// <para><note>Some cameras produce HTTP header, which does not conform strictly to
    /// standard, what leads to .NET exception. To avoid this exception the <b>useUnsafeHeaderParsing</b>
    /// configuration option of <b>httpWebRequest</b> should be set, what may be done using application
    /// configuration file.</note></para>
    /// <code>
    /// &lt;configuration&gt;
    /// 	&lt;system.net&gt;
    /// 		&lt;settings&gt;
    /// 			&lt;httpWebRequest useUnsafeHeaderParsing="true" /&gt;
    /// 		&lt;/settings&gt;
    /// 	&lt;/system.net&gt;
    /// &lt;/configuration&gt;
    /// </code>
    /// </remarks>
    /// 
    public class MJPEGStream2 : IVideoSource
    {
        // URL for MJPEG stream
        private string _source;
        // login and password for HTTP authentication
        private string _login;
        private string _password;
        // proxy information
        private IWebProxy _proxy;
        // received frames count
        private int _framesReceived;
        // recieved byte count
        private long _bytesReceived;
        // use separate HTTP connection group or use default
        private bool _useSeparateConnectionGroup = true;
        // timeout value for web request
        private int _requestTimeout = 10000;
        // if we should use basic authentication when connecting to the video source
        private bool _forceBasicAuthentication;
        private string _cookies = "";

        // buffer size used to download MJPEG stream
        private const int BufSize = 1024 * 1024;
        // size of portion to read at once
        private const int ReadSize = 1024;
        private bool _usehttp10;

        private Thread _thread;
        private ManualResetEvent _stopEvent;
        private ManualResetEvent _reloadEvent;

        private string _userAgent = "Mozilla/5.0";

        private bool _needsPrivacyEnabled;
        private DateTime _needsPrivacyEnabledTarget = DateTime.MinValue;

        public string Headers = "";

        /// <summary>
        /// New frame event.
        /// </summary>
        /// 
        /// <remarks><para>Notifies clients about new available frame from video source.</para>
        /// 
        /// <para><note>Since video source may have multiple clients, each client is responsible for
        /// making a copy (cloning) of the passed video frame, because the video source disposes its
        /// own original copy after notifying of clients.</note></para>
        /// </remarks>
        /// 
        public event NewFrameEventHandler NewFrame;

        /// <summary>
        /// Video source error event.
        /// </summary>
        /// 
        /// <remarks>This event is used to notify clients about any type of errors occurred in
        /// video source object, for example internal exceptions.</remarks>
        /// 
        public event VideoSourceErrorEventHandler VideoSourceError;

        /// <summary>
        /// Video playing finished event.
        /// </summary>
        /// 
        /// <remarks><para>This event is used to notify clients that the video playing has finished.</para>
        /// </remarks>
        /// 
        public event PlayingFinishedEventHandler PlayingFinished;

        /// <summary>
        /// Use or not separate connection group.
        /// </summary>
        /// 
        /// <remarks>The property indicates to open web request in separate connection group.</remarks>
        /// 
        public bool SeparateConnectionGroup
        {
            get { return _useSeparateConnectionGroup; }
            set { _useSeparateConnectionGroup = value; }
        }

        /// <summary>
        /// Use or not HTTP Protocol 1.0
        /// </summary>
        /// 
        /// <remarks>The property indicates to open web request using HTTP 1.0 protocol.</remarks>
        /// 
        public bool UseHTTP10
        {
            get { return _usehttp10; }
            set { _usehttp10 = value; }
        }

        /// <summary>
        /// Video source.
        /// </summary>
        /// 
        /// <remarks>URL, which provides MJPEG stream.</remarks>
        /// 
        public string Source
        {
            get { return _source; }
            set
            {
                _source = value;
                // signal to reload
                if (_thread != null)
                    _reloadEvent.Set();
            }
        }

        /// <summary>
        /// Login value.
        /// </summary>
        /// 
        /// <remarks>Login required to access video source.</remarks>
        /// 
        public string Login
        {
            get { return _login; }
            set { _login = value; }
        }

        /// <summary>
        /// Password value.
        /// </summary>
        /// 
        /// <remarks>Password required to access video source.</remarks>
        /// 
        public string Password
        {
            get { return _password; }
            set { _password = value; }
        }

        /// <summary>
        /// Gets or sets proxy information for the request.
        /// </summary>
        /// 
        /// <remarks><para>The local computer or application config file may specify that a default
        /// proxy to be used. If the Proxy property is specified, then the proxy settings from the Proxy
        /// property overridea the local computer or application config file and the instance will use
        /// the proxy settings specified. If no proxy is specified in a config file
        /// and the Proxy property is unspecified, the request uses the proxy settings
        /// inherited from Internet Explorer on the local computer. If there are no proxy settings
        /// in Internet Explorer, the request is sent directly to the server.
        /// </para></remarks>
        /// 
        public IWebProxy Proxy
        {
            get { return _proxy; }
            set { _proxy = value; }
        }

        /// <summary>
        /// User agent to specify in HTTP request header.
        /// </summary>
        /// 
        /// <remarks><para>Some IP cameras check what is the requesting user agent and depending
        /// on it they provide video in different formats or do not provide it at all. The property
        /// sets the value of user agent string, which is sent to camera in request header.
        /// </para>
        /// 
        /// <para>Default value is set to "Mozilla/5.0". If the value is set to <see langword="null"/>,
        /// the user agent string is not sent in request header.</para>
        /// </remarks>
        /// 
        public string HttpUserAgent
        {
            get { return _userAgent; }
            set { _userAgent = value; }
        }

        /// <summary>
        /// Received frames count.
        /// </summary>
        /// 
        /// <remarks>Number of frames the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public int FramesReceived
        {
            get
            {
                int frames = _framesReceived;
                _framesReceived = 0;
                return frames;
            }
        }

        /// <summary>
        /// Received bytes count.
        /// </summary>
        /// 
        /// <remarks>Number of bytes the video source provided from the moment of the last
        /// access to the property.
        /// </remarks>
        /// 
        public long BytesReceived
        {
            get
            {
                long bytes = _bytesReceived;
                _bytesReceived = 0;
                return bytes;
            }
        }

        /// <summary>
        /// Request timeout value.
        /// </summary>
        /// 
        /// <remarks>The property sets timeout value in milliseconds for web requests.
        /// Default value is 10000 milliseconds.</remarks>
        /// 
        public int RequestTimeout
        {
            get { return _requestTimeout; }
            set { _requestTimeout = value; }
        }

        /// <summary>
        /// State of the video source.
        /// </summary>
        /// 
        /// <remarks>Current state of video source object - running or not.</remarks>
        /// 
        public bool IsRunning
        {
            get
            {
                if (_thread != null)
                {
                    // check thread status
                    if (!_thread.Join(TimeSpan.Zero))
                        return true;

                    // the thread is not running, so free resources
                    Free();
                }
                return false;
            }
        }

        /// <summary>
        /// Force using of basic authentication when connecting to the video source.
        /// </summary>
        /// 
        /// <remarks><para>For some IP cameras (TrendNET IP cameras, for example) using standard .NET's authentication via credentials
        /// does not seem to be working (seems like camera does not request for authentication, but expects corresponding headers to be
        /// present on connection request). So this property allows to force basic authentication by adding required HTTP headers when
        /// request is sent.</para>
        /// 
        /// <para>Default value is set to <see langword="false"/>.</para>
        /// </remarks>
        /// 
        public bool ForceBasicAuthentication
        {
            get { return _forceBasicAuthentication; }
            set { _forceBasicAuthentication = value; }
        }

        public string Cookies
        {
            get { return _cookies; }
            set
            {
                _cookies = value;

            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MJPEGStream"/> class.
        /// </summary>
        /// 
        public MJPEGStream2() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="MJPEGStream"/> class.
        /// </summary>
        /// 
        /// <param name="source">URL, which provides MJPEG stream.</param>
        /// 
        public MJPEGStream2(string source)
        {
            _source = source;
        }

        public string DecodeKey;

        /// <summary>
        /// Start video source.
        /// </summary>
        /// 
        /// <remarks>Starts video source and return execution to caller. Video source
        /// object creates background thread and notifies about new frames with the
        /// help of <see cref="NewFrame"/> event.</remarks>
        /// 
        /// <exception cref="ArgumentException">Video source is not specified.</exception>
        /// 
        public void Start()
        {
            if (!IsRunning)
            {
                // check source
                if (string.IsNullOrEmpty(_source))
                    throw new ArgumentException("Video source is not specified.");

                _framesReceived = 0;
                _bytesReceived = 0;

                // create events
                _stopEvent = new ManualResetEvent(false);
                _reloadEvent = new ManualResetEvent(false);

                // create and start new thread
                _thread = new Thread(WorkerThread) {Name = _source};
                _thread.Start();
            }
        }

        /// <summary>
        /// Signal video source to stop its work.
        /// </summary>
        /// 
        /// <remarks>Signals video source to stop its background thread, stop to
        /// provide new frames and free resources.</remarks>
        /// 
        public void SignalToStop()
        {
            // stop thread
            if (_thread != null)
            {
                // signal to stop
                _stopEvent.Set();
            }
        }

        /// <summary>
        /// Wait for video source has stopped.
        /// </summary>
        /// 
        /// <remarks>Waits for source stopping after it was signalled to stop using
        /// <see cref="SignalToStop"/> method.</remarks>
        /// 
        public void WaitForStop()
        {
            if (IsRunning)
            {
                // wait for thread stop
                _stopEvent.Set();
                _thread.Join(MainForm.ThreadKillDelay);
                if (_thread != null && !_thread.Join(TimeSpan.Zero))
                    _thread.Abort();
                Free();
            }
        }

        /// <summary>
        /// Stop video source.
        /// </summary>
        /// 
        /// <remarks><para>Stops video source aborting its thread.</para>
        /// 
        /// <para><note>Since the method aborts background thread, its usage is highly not preferred
        /// and should be done only if there are no other options. The correct way of stopping camera
        /// is <see cref="SignalToStop">signaling it stop</see> and then
        /// <see cref="WaitForStop">waiting</see> for background thread's completion.</note></para>
        /// </remarks>
        /// 
        public void Stop()
        {
            WaitForStop();
        }

        /// <summary>
        /// Free resource.
        /// </summary>
        /// 
        private void Free()
        {
            _thread = null;

            // release events
            if (_stopEvent != null)
            {
                _stopEvent.Close();
                _stopEvent.Dispose();
            }
            _stopEvent = null;
            if (_reloadEvent != null)
            {
                _reloadEvent.Close();
                _reloadEvent.Dispose();
            }
            _reloadEvent = null;
        }

        // Worker thread
        private void WorkerThread()
        {
            // buffer to read stream
            var buffer = new byte[BufSize];
            // JPEG magic number
            var jpegMagic = new byte[] { 0xFF, 0xD8, 0xFF };


            var encoding = new ASCIIEncoding();
            var res = ReasonToFinishPlaying.StoppedByUser;

            while (!_stopEvent.WaitOne(0, false))
            {
                // reset reload event
                _reloadEvent.Reset();

                // HTTP web request
                HttpWebRequest request = null;
                // web responce
                WebResponse response = null;
                // stream for MJPEG downloading
                Stream stream = null;
                // boundary betweeen images (string and binary versions)
                byte[] boundary;
                string boudaryStr = null;
                // length of boundary
                // flag signaling if boundary was checked or not
                bool boundaryIsChecked = false;
                // read amounts and positions
                int todo = 0, total = 0, pos = 0, align = 1;
                int start = 0;

                // align
                //  1 = searching for image start
                //  2 = searching for image end

                try
                {
                    // create request
                    request = GenerateRequest(_source);
                    // get response
                    response = request.GetResponse();

                    // check content type
                    string contentType = response.ContentType;
                    string[] contentTypeArray = contentType.Split('/');

                    // "application/octet-stream"
                    int boundaryLen;
                    if ((contentTypeArray[0] == "application") && (contentTypeArray[1] == "octet-stream"))
                    {
                        boundaryLen = 0;
                        boundary = new byte[0];
                    }
                    else if ((contentTypeArray[0] == "multipart") && (contentType.Contains("mixed")))
                    {
                        // get boundary
                        int boundaryIndex = contentType.IndexOf("boundary", 0, StringComparison.Ordinal);
                        if (boundaryIndex != -1)
                        {
                            boundaryIndex = contentType.IndexOf("=", boundaryIndex + 8, StringComparison.Ordinal);
                        }

                        if (boundaryIndex == -1)
                        {
                            // try same scenario as with octet-stream, i.e. without boundaries
                            boundaryLen = 0;
                            boundary = new byte[0];
                        }
                        else
                        {
                            boudaryStr = contentType.Substring(boundaryIndex + 1);
                            // remove spaces and double quotes, which may be added by some IP cameras
                            boudaryStr = boudaryStr.Trim(' ', '"');

                            boundary = encoding.GetBytes(boudaryStr);
                            boundaryLen = boundary.Length;
                            boundaryIsChecked = false;
                        }
                    }
                    else
                    {
                        if (contentType=="text/html")
                        {
                            try
                            {
                                //read body
                                var sr = new StreamReader(response.GetResponseStream());
                                var html = sr.ReadToEnd();
                                if (html.IndexOf("setup_kakulens.html", StringComparison.Ordinal) != -1)
                                {
                                    //hack for panasonic cameras that redirect on reboot in privacy mode - POST a command to disable privacy
                                    if (DisablePrivacy(request))
                                    {
                                        _needsPrivacyEnabledTarget = Helper.Now.AddSeconds(4);
                                        _needsPrivacyEnabled = true;
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                MainForm.LogExceptionToFile(ex);
                            }
                            //continue to throw the invalid content type error as the next retry should connect
                        }
                        throw new Exception("Invalid content type.");
                    }

                    // get response stream
                    stream = response.GetResponseStream();
                    stream.ReadTimeout = _requestTimeout;

                    // loop
                    while ((!_stopEvent.WaitOne(0, false)) && (!_reloadEvent.WaitOne(0, false)))
                    {
                        // check total read
                        if (total > BufSize - ReadSize)
                        {
                            total = pos = todo = 0;
                        }

                        // read next portion from stream
                        int read;
                        if ((read = stream.Read(buffer, total, ReadSize)) == 0)
                            throw new ApplicationException();

                        total += read;
                        todo += read;

                        // increment received bytes counter
                        _bytesReceived += read;

                        // do we need to check boundary ?
                        if ((boundaryLen != 0) && (!boundaryIsChecked))
                        {
                            // some IP cameras, like AirLink, claim that boundary is "myboundary",
                            // when it is really "--myboundary". this needs to be corrected.

                            pos = ByteArrayUtils.Find(buffer, boundary, 0, todo);
                            // continue reading if boudary was not found
                            if (pos == -1)
                                continue;

                            for (int i = pos - 1; i >= 0; i--)
                            {
                                byte ch = buffer[i];

                                if ((ch == (byte)'\n') || (ch == (byte)'\r'))
                                {
                                    break;
                                }

                                boudaryStr = (char)ch + boudaryStr;
                            }

                            boundary = encoding.GetBytes(boudaryStr);
                            boundaryLen = boundary.Length;
                            boundaryIsChecked = true;
                        }

                        // search for image start
                        if ((align == 1) && (todo >= jpegMagic.Length))
                        {
                            start = ByteArrayUtils.Find(buffer, jpegMagic, pos, todo);
                            if (start != -1)
                            {
                                // found JPEG start
                                pos = start + jpegMagic.Length;
                                todo = total - pos;
                                align = 2;
                            }
                            else
                            {
                                // delimiter not found
                                todo = jpegMagic.Length - 1;
                                pos = total - todo;
                            }
                        }

                        // search for image end ( boundaryLen can be 0, so need extra check )
                        while ((align == 2) && (todo != 0) && (todo >= boundaryLen))
                        {
                            int stop = ByteArrayUtils.Find(buffer,
                                                           (boundaryLen != 0) ? boundary : jpegMagic,
                                                           pos, todo);

                            if (stop != -1)
                            {
                                // increment frames counter
                                _framesReceived++;

                                // image at stop
                                if ((NewFrame != null) && (!_stopEvent.WaitOne(0, false)))
                                {
                                    Bitmap bitmap;
                                    if (!String.IsNullOrEmpty(DecodeKey))
                                    {
                                        byte[] marker = Encoding.ASCII.GetBytes(DecodeKey);

                                        using (var ms = new MemoryStream(buffer, start + jpegMagic.Length, jpegMagic.Length+marker.Length))
                                        {
                                            var key = new byte[marker.Length];
                                            ms.Read(key, 0, marker.Length);

                                            if (!ByteArrayUtils.UnsafeCompare(marker, key))
                                            {
                                                throw (new Exception("Image Decode Failed - Check the decode key matches the encode key on ispy server"));
                                            }
                                        }


                                        using (var ms = new MemoryStream(buffer, start + marker.Length, stop - start - marker.Length))
                                        {  
                                            ms.Seek(0, SeekOrigin.Begin);
                                            ms.WriteByte(jpegMagic[0]);
                                            ms.WriteByte(jpegMagic[1]);
                                            ms.WriteByte(jpegMagic[2]);
                                            ms.Seek(0, SeekOrigin.Begin);
                                            bitmap = (Bitmap) Image.FromStream(ms);
                                            NewFrame(this, new NewFrameEventArgs(bitmap));
                                            
                                        }
                                    }
                                    else
                                    {
                                        using (var ms = new MemoryStream(buffer, start, stop - start))
                                        {
                                            bitmap = (Bitmap) Image.FromStream(ms);
                                            NewFrame(this, new NewFrameEventArgs(bitmap));
                                        }
                                    }
                                        
                                    // notify client
                                    
                                    // release the image
                                    bitmap.Dispose();
                                    bitmap = null;
                                    if (_needsPrivacyEnabled && _needsPrivacyEnabledTarget < Helper.Now)
                                    {
                                        if (EnablePrivacy(request))
                                        {
                                            _needsPrivacyEnabled = false;
                                        }
                                    }
                                }

                                // shift array
                                pos = stop + boundaryLen;
                                todo = total - pos;
                                System.Array.Copy(buffer, pos, buffer, 0, todo);

                                total = todo;
                                pos = 0;
                                align = 1;
                            }
                            else
                            {
                                // boundary not found
                                if (boundaryLen != 0)
                                {
                                    todo = boundaryLen - 1;
                                    pos = total - todo;
                                }
                                else
                                {
                                    todo = 0;
                                    pos = total;
                                }
                            }
                        }
                    }
                }
                catch (ApplicationException)
                {
                    // do nothing for Application Exception, which we raised on our own
                    // wait for a while before the next try
                    Thread.Sleep(250);
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // provide information to clients
                    MainForm.LogExceptionToFile(ex);
                    res = ReasonToFinishPlaying.DeviceLost;
                    break;
                    // wait for a while before the next try
                    //Thread.Sleep(250);
                }
                finally
                {
                    // abort request
                    if (request != null)
                    {
                        try
                        {
                            request.Abort();
                        }
                        catch {}
                        request = null;
                    }
                    // close response stream
                    if (stream != null)
                    {
                        stream.Close();
                        stream = null;
                    }
                    // close response
                    if (response != null)
                    {
                        response.Close();
                        response = null;
                    }
                }

                // need to stop ?
                if (_stopEvent.WaitOne(0, false))
                    break;
            }

            if (PlayingFinished != null)
            {
                PlayingFinished(this, res);
            }
        }
        
        private bool DisablePrivacy(HttpWebRequest request)
        {
            
            string uri = request.RequestUri.AbsoluteUri;
            uri = uri.Substring(0, uri.IndexOf(request.RequestUri.AbsolutePath, StringComparison.Ordinal));
            var res = false;

            for (int i = 0; i < 5; i++)
            {
                var request2 = GenerateRequest(uri + "/Set?Func=Powerdown&Kind=1&Data=0");
                request2.Method = "GET";
                request2.ContentType = "application/x-www-form-urlencoded";
                request2.Timeout = 4000;
                try
                {
                    var resp = (HttpWebResponse) request2.GetResponse();
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        var str = resp.GetResponseStream();
                        if (str != null)
                        {
                            var r = new StreamReader(str, true);
                            var t = r.ReadToEnd().Trim();
                            if (t == "Return:0")
                            {
                                res = true;
                                break;
                            }
                        }
                    }
                    resp.Close();
                    
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
                Thread.Sleep(2000);
            }

            return res;
        }

        private bool EnablePrivacy(HttpWebRequest request)
        {
            string uri = request.RequestUri.AbsoluteUri;
            uri = uri.Substring(0, uri.IndexOf(request.RequestUri.AbsolutePath, StringComparison.Ordinal));
            var res = false;

            for (int i = 0; i < 5; i++)
            {
                var request2 = GenerateRequest(uri + "/Set?Func=Powerdown&Kind=1&Data=1");
                request2.Method = "GET";
                request2.ContentType = "application/x-www-form-urlencoded";

                try
                {
                    var resp = (HttpWebResponse) request2.GetResponse();
                    if (resp.StatusCode == HttpStatusCode.OK)
                    {
                        var str = resp.GetResponseStream();
                        if (str != null)
                        {
                            var r = new StreamReader(str, true);
                            var t = r.ReadToEnd().Trim();
                            if (t == "Return:0")
                            {
                                res = true;
                                break;
                            }
                        }
                    }
                    resp.Close();
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                }
                Thread.Sleep(2000);
            }

            return res;
        }

        private HttpWebRequest GenerateRequest(string source)
        {
            var request = (HttpWebRequest)WebRequest.Create(source);
            // set user agent
            if (_userAgent != null)
            {
                request.UserAgent = _userAgent;
            }

            // set proxy
            if (_proxy != null)
            {
                request.Proxy = _proxy;
            }

            if (_usehttp10)
                request.ProtocolVersion = HttpVersion.Version10;

            // set timeout value for the request
            request.Timeout = _requestTimeout;
            request.AllowAutoRedirect = true;

            // set login and password
            if ((_login != null) && (_password != null) && (_login != string.Empty))
                request.Credentials = new NetworkCredential(_login, _password);
            // set connection group name
            if (_useSeparateConnectionGroup)
                request.ConnectionGroupName = GetHashCode().ToString(CultureInfo.InvariantCulture);
            // force basic authentication through extra headers if required

            var authInfo = "";
            if (!String.IsNullOrEmpty(_login))
            {
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(_login + ":" + _password));
                request.Headers["Authorization"] = "Basic " + authInfo;
            }


            if (!String.IsNullOrEmpty(_cookies))
            {
                _cookies = _cookies.Replace("[AUTH]", authInfo);
                var myContainer = new CookieContainer();
                string[] coll = _cookies.Split(';');
                foreach (var ckie in coll)
                {
                    if (!String.IsNullOrEmpty(ckie))
                    {
                        string[] nv = ckie.Split('=');
                        if (nv.Length == 2)
                        {
                            var cookie = new Cookie(nv[0].Trim(), nv[1].Trim());
                            myContainer.Add(new Uri(request.RequestUri.ToString()), cookie);
                        }
                    }
                }
                request.CookieContainer = myContainer;
            }

            if (!String.IsNullOrEmpty(Headers))
            {
                Headers = Headers.Replace("[AUTH]", authInfo);
                string[] coll = _cookies.Split(';');
                foreach (var hdr in coll)
                {
                    if (!String.IsNullOrEmpty(hdr))
                    {
                        string[] nv = hdr.Split('=');
                        if (nv.Length == 2)
                        {
                            request.Headers.Add(nv[0], nv[1]);
                        }
                    }
                }
            }

            return request;
        }
    }

    class Certificates
    {
        public static bool ValidateRemoteCertificate(
            object sender,
                X509Certificate certificate,
                X509Chain chain,
                SslPolicyErrors policyErrors
            )
        {
            return true;
        }
    }

}
