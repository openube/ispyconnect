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

        private static readonly object Sync = new object();
        private static iSpySecure _wsa;

        public static iSpySecure Wsa
        {
            get
            {
                if (_wsa == null)
                    lock (Sync)
                        if (_wsa == null)
                            _wsa = new iSpySecure { Url = MainForm.WebserverSecure + "/webservices/ispysecure.asmx", Timeout = 20000 };
                return _wsa;
            }
            set
            {
                lock (Sync)
                    _wsa = value;
            }
        }

        public static Timer ReconnectTimer
        {
            get
            {
                if (_reconnect == null)
                    lock (Sync)
                        if (_reconnect == null)
                        {
                            _reconnect = new Timer {Interval = 60*1000};
                            _reconnect.Elapsed += ReconnectElapsed;
                        }
                return _reconnect;
            }
        }

        private static string _externalIP = "";

        private static bool _websitelive = true;


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
                    MainForm.LogErrorToFile("Disconnected");
                    if (!ReconnectTimer.Enabled)
                        ReconnectTimer.Start();
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
                    MainForm.LogMessageToFile("Reconnecting...");
                    if (MainForm.Conf.ServicesEnabled)
                    {
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

        public static void ForceSync()
        {
            ForceSync(MainForm.IPAddress, MainForm.Conf.LANPort,
                             MainForm.MWS.GetObjectList());
        }

        private static void ForceSync(string internalIPAddress, int internalPort, string settings)
        {
            if (!MainForm.Conf.ServicesEnabled)
                return;
            if (WebsiteLive)
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;
                
                string ip = MainForm.IPAddressExternal;
                if (WebsiteLive)
                {
                    Wsa.SyncCompleted += WsaSyncCompleted;
                    Wsa.SyncAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port,
                                        internalIPAddress, internalPort, settings, MainForm.Conf.IPMode == "IPv4", ip);
                }
                
            }
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
                _websitelive = false;
                MainForm.LogExceptionToFile(ex);
            }

           
        }

        static void WsaPingAlive4Completed(object sender, PingAlive4CompletedEventArgs e)
        {
            bool islive = _websitelive;
            if (e.Error == null)
            {
                string[] r = e.Result;
                
                if (r.Length > 1)
                {
                    _websitelive = true;
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
                    _websitelive = false;
                }
            }
            else
            {
                _websitelive = false;
            }
            if (!islive && _websitelive)
            {
                Connect();
                ForceSync();
            }
        }

        

        static void WsaSyncCompleted(object sender, SyncCompletedEventArgs e)
        {
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
                    r = Wsa.Connect(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port,
                                      MainForm.Identifier, tryLoopback, Application.ProductVersion,
                                      MainForm.Conf.ServerName, MainForm.Conf.IPMode=="IPv4", MainForm.IPAddressExternal);
                    if (r == "OK" && tryLoopback)
                        MainForm.LoopBack = true;
                }
                catch (Exception ex)
                {
                    MainForm.LogExceptionToFile(ex);
                    WebsiteLive = false;
                }
                if (WebsiteLive && r != "OK")
                    return LocRm.GetString(r);
                if (WebsiteLive)
                    return r;
            }
            return LocRm.GetString("iSpyDown");
        }

        public static string[] TestConnection(string username, string password, bool tryLoopback)
        {
            var r = new string[] {};

            int port = MainForm.Conf.ServerPort;
            if (MainForm.Conf.IPMode == "IPv6")
                port = MainForm.Conf.LANPort;

            try
            {
                _websitelive = true;
                r = Wsa.TestConnection(username, password, port, MainForm.Identifier, tryLoopback, MainForm.Conf.IPMode == "IPv4", MainForm.IPAddressExternal);
            }
            catch (Exception ex)
            {
                _websitelive = false;
                MainForm.LogExceptionToFile(ex);
            }
            if (_websitelive)
            {
                if (r.Length == 1 && r[0] != "OK") //login failed
                    r[0] = LocRm.GetString(r[0]);
                if (r.Length > 3 && r[3] != "")
                {
                    r[3] = LocRm.GetString(r[3]);
                }
                return r;
            }
            return new[] { LocRm.GetString("iSpyDown") };
        }
    }
}