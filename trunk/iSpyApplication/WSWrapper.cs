using System;
using System.Timers;
using System.Windows.Forms;
using iSpyApplication.iSpyWS;
using Timer = System.Timers.Timer;

namespace iSpyApplication
{
    public static class WsWrapper
    {
        private static Timer _reconnect;
        private static iSpySecure _wsa;
        private static string _externalIP = "";
        private static bool _websitelive = true;

        public static iSpySecure Wsa
        {
            get
            {
                if (_wsa != null)
                    return _wsa;

                _wsa = new iSpySecure
                    {
                        Url = MainForm.WebserverSecure + "/webservices/ispysecure.asmx",
                        Timeout = 20000,
                    };
                _wsa.Disposed += WsaDisposed;
                
                return _wsa;
            }
        }

        static void WsaDisposed(object sender, EventArgs e)
        {
            _wsa = null;
        }

        public static Timer ReconnectTimer
        {
            get
            {
                if (_reconnect == null)
                {
                    _reconnect = new Timer { Interval = 60 * 1000 };
                    _reconnect.Elapsed += ReconnectElapsed;
                    _reconnect.Disposed += ReconnectDisposed;
                }

                return _reconnect;
            }
        }

        static void ReconnectDisposed(object sender, EventArgs e)
        {
            _reconnect = null;
        }

        public static string WebservicesDisabledMessage
        {
            get { return LocRm.GetString("WebservicesDisabled"); }
        }

        public static bool WebsiteLive
        {
            get { return _websitelive; }
            set
            {
                _websitelive = value;
                if (!_websitelive)
                {
                    if (!ReconnectTimer.Enabled)
                    {
                        MainForm.LogErrorToFile("Starting reconnect timer");
                        ReconnectTimer.Start();
                    }
                }
            }
        }

        private static void ReconnectElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                string s = Wsa.Ping();
                if (s == "OK")
                {
                    ReconnectTimer.Stop();
                    
                    if (MainForm.Conf.ServicesEnabled)
                    {
                        MainForm.LogMessageToFile("Reconnecting...");
                        try
                        {
                            s = Connect(MainForm.Conf.Loopback);
                            if (s == "OK")
                            {
                                MainForm.StopAndStartServer();
                                ForceSync(MainForm.IPAddress, MainForm.Conf.LANPort, MainForm.MWS.GetObjectList());
                            }
                            WebsiteLive = true;
                            MainForm.LogMessageToFile("Connected");
                        }
                        catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                            ReconnectTimer.Start();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
            }
        }

        public static string SendAlert(string emailAddress, string subject, string message)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendAlert(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                        emailAddress, subject, message);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendContent(string emailAddress, string subject, string message)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendContent(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                         emailAddress, subject, message);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendAlertWithImage(string emailAddress, string subject, string message, byte[] imageData)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendAlertWithImage(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                                 emailAddress, subject, message, imageData);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendFrameGrab(string emailAddress, string subject, string message, byte[] imageData)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendFrameGrab(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                            emailAddress, subject, message, imageData);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }


        public static string ExternalIPv4(bool refresh)
        {
            if (_externalIP != "" && !refresh)
                return _externalIP;
            if (WebsiteLive)
            {
                try
                {
                    _externalIP = Wsa.RemoteAddress();
                    
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return _externalIP;
            }
            if (_externalIP != "")
                return _externalIP;

            return LocRm.GetString("Unavailable");
        }

        public static string ProductLatestVersion(int productId)
        {
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.ProductLatestVersionGet(productId);
                    WebsiteLive = true;
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendSms(string smsNumber, string message)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendSMS(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, smsNumber,
                                     message);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendTweet(string message)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendTweet(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, message);
                    if (r!="OK")
                        MainForm.LogMessageToFile(r);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string SendMms(string mobileNumber, string message, byte[] imageData)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                try
                {
                    r = Wsa.SendMMS(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                      mobileNumber, message, imageData);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static bool ForceSync()
        {
            return ForceSync(MainForm.IPAddress, MainForm.Conf.LANPort,
                             MainForm.MWS.GetObjectList());
        }

        private static bool _forcesyncprocessing;
        private static bool ForceSync(string internalIPAddress, int internalPort, string settings)
        {
            if (!MainForm.Conf.ServicesEnabled || _forcesyncprocessing)
                return false;
            if (WebsiteLive)
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;
                
                string ip = MainForm.IPAddressExternal;
                if (WebsiteLive && !_forcesyncprocessing)
                {
                    Wsa.SyncCompleted+=WsaSyncCompleted;
                    _forcesyncprocessing = true;
                    Wsa.SyncAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port,
                                        internalIPAddress, internalPort, settings, MainForm.Conf.IPMode == "IPv4", ip);
                    return true;
                }
                
            }
            return false;
        }

        public static void PingServer()
        {
            if (!MainForm.Conf.ServicesEnabled)
                return;
            
            try
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;

                Wsa.PingAlive4Completed += WsaPingAlive4Completed;
                Wsa.PingAlive4Async(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port, MainForm.Conf.IPMode == "IPv4", MainForm.IPAddressExternal, MainForm.IPAddress);

            }
            catch (Exception ex)
            {
                WebsiteLive = false;
                MainForm.LogExceptionToFile(ex);
            }

           
        }

        static void WsaPingAlive4Completed(object sender, PingAlive4CompletedEventArgs e)
        {
            bool islive = WebsiteLive;
            if (e.Error == null)
            {
                string[] r = e.Result;
                
                if (r.Length > 1)
                {
                    WebsiteLive = true;
                    if (MainForm.Conf.ServicesEnabled)
                    {
                        if (!MainForm.MWS.Running)
                        {
                            MainForm.StopAndStartServer();
                        }
                    }
                    if (MainForm.Conf.IPMode == "IPv4")
                        _externalIP = r[1];
                }
                else
                {
                    WebsiteLive = false;
                }
            }
            else
            {
                WebsiteLive = false;
            }
            if (!islive && WebsiteLive)
            {
                Connect();
                ForceSync();
            }
        }

        static void WsaSyncCompleted(object sender, SyncCompletedEventArgs e)
        {
            _forcesyncprocessing = false;
            if (e.Error != null)
            {
                WebsiteLive = false;
            }
            else
                if (e.Result=="OK")
                    WebsiteLive = true;
        }

        public static string Disconnect()
        {

            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;
                try
                {
                    r = Wsa.Disconnect(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string Connect()
        {
            return Connect(MainForm.LoopBack);
        }

        public static string Connect(bool tryLoopback)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return WebservicesDisabledMessage;
            string r = "";
            if (WebsiteLive)
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;

                try
                {
                    r = Wsa.Connect2(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port,
                                      MainForm.Identifier, tryLoopback, Application.ProductVersion,
                                      MainForm.Conf.ServerName, MainForm.Conf.IPMode == "IPv4", MainForm.IPAddressExternal, MainForm.AFFILIATEID);
                    if (r == "OK" && tryLoopback)
                    {
                        MainForm.LoopBack = true;
                    }
                    //MainForm.LogMessageToFile("Webservices: " + r);
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                LoginFailed = false;
                if (WebsiteLive && r != "OK")
                {
                    LoginFailed = true;
                    return LocRm.GetString(r);
                }
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static bool LoginFailed = true;

        public static string[] TestConnection(string username, string password, bool tryLoopback)
        {
            var r = new string[] {};

            int port = MainForm.Conf.ServerPort;
            if (MainForm.Conf.IPMode == "IPv6")
                port = MainForm.Conf.LANPort;

            try
            {
                r = Wsa.TestConnection(username, password, port, MainForm.Identifier, tryLoopback, MainForm.Conf.IPMode == "IPv4", MainForm.IPAddressExternal);
                WebsiteLive = true;
            }
            catch (Exception ex)
            {
                WebsiteLive = false;
                MainForm.LogExceptionToFile(ex);
            }
            if (WebsiteLive)
            {
                LoginFailed = false;
                if (r.Length == 1 && r[0] != "OK")
                {
                    r[0] = LocRm.GetString(r[0]);
                    LoginFailed = true;
                    MainForm.LogErrorToFile("Webservices: "+r[0]);
                }
                if (r.Length > 3 && r[3] != "")
                {
                    r[3] = LocRm.GetString(r[3]);
                    LoginFailed = true;
                    MainForm.LogErrorToFile("Webservices: " + r[3]);
                }
                return r;
            }
            return new[] { LocRm.GetString("iSpyDown") };
        }
    }
}