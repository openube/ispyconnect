using System;
using System.Diagnostics;
using System.Windows.Forms;
using iSpyApplication.iSpyWS;

namespace iSpyApplication
{
    public static class WsWrapper
    {
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
                        Timeout = 15000,
                    };
                _wsa.Disposed += WsaDisposed;
                _wsa.SyncCompleted += WsaSyncCompleted;
                _wsa.PingAlive4Completed += WsaPingAlive4Completed;
                _wsa.SendAlertCompleted += WsaSendAlertCompleted;
                _wsa.SendContentCompleted += WsaSendContentCompleted;
                _wsa.SendAlertWithImageCompleted += WsaSendAlertWithImageCompleted;
                _wsa.SendSMSCompleted += WsaSendSMSCompleted;
                _wsa.SendTweetCompleted += WsaSendTweetCompleted;
                
                return _wsa;
            }
        }

        static void WsaDisposed(object sender, EventArgs e)
        {
            _wsa = null;
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
                if (_websitelive && !value)
                {
                    //disconnected
                    if (!String.IsNullOrEmpty(MainForm.Conf.AlertOnDisconnect))
                    {
                        try
                        {
                            Process.Start(MainForm.Conf.AlertOnDisconnect);
                        }catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                    }
                }
                if (!_websitelive && value)
                {
                    //reconnected
                    if (!String.IsNullOrEmpty(MainForm.Conf.AlertOnReconnect))
                    {
                        try
                        {
                            Process.Start(MainForm.Conf.AlertOnReconnect);
                        }catch (Exception ex)
                        {
                            MainForm.LogExceptionToFile(ex);
                        }
                    }
                    
                    _websitelive = true;

                    if (Connect()=="OK")
                        ForceSync();
                }
                _websitelive = value;
            }
        }

        public static void SendAlert(string emailAddress, string subject, string message)
        {
            if (!Enabled)
                return;
            
            Wsa.SendAlertAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                        emailAddress, subject, message, Guid.NewGuid());
        }

        public static void SendContent(string emailAddress, string subject, string message)
        {
            if (!Enabled)
                return;

            if (WebsiteLive)
            {                
                Wsa.SendContentAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                         emailAddress, subject, message,Guid.NewGuid());
            }
        }
        private static bool Enabled
        {
            get { return MainForm.Conf.ServicesEnabled && MainForm.Conf.Subscribed && WebsiteLive && !LoginFailed; }
        }

        public static void SendAlertWithImage(string emailAddress, string subject, string message, byte[] imageData)
        {
            if (!Enabled)
                return;

            Wsa.SendAlertWithImageAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword,
                                                emailAddress, subject, message, imageData,Guid.NewGuid());
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

        public static void SendSms(string smsNumber, string message)
        {
            if (!Enabled)
                return;
            Wsa.SendSMSAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, smsNumber, message, Guid.NewGuid());
        }

        public static void SendTweet(string message)
        {
            if (!Enabled)
                return;
            Wsa.SendTweetAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, message, Guid.NewGuid());
        }

        public static void ForceSync()
        {
            ForceSync(MainForm.IPAddress, MainForm.Conf.LANPort, MainForm.MWS.GetObjectList());
        }

        private static void ForceSync(string internalIPAddress, int internalPort, string settings)
        {
            if (LoginFailed || !WebsiteLive || !MainForm.Conf.ServicesEnabled)
                return;

            MainForm.NeedsSync = false;
            int port = MainForm.Conf.ServerPort;
            if (MainForm.Conf.IPMode == "IPv6")
                port = MainForm.Conf.LANPort;
                
            string ip = MainForm.IPAddressExternal;
            Wsa.SyncAsync(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port, internalIPAddress, internalPort, settings, MainForm.Conf.IPMode == "IPv4", ip, Guid.NewGuid());
        }

        public static void PingServer()
        {
            if (!MainForm.Conf.ServicesEnabled || LoginFailed)
                return;
            
            try
            {
                int port = MainForm.Conf.ServerPort;
                if (MainForm.Conf.IPMode == "IPv6")
                    port = MainForm.Conf.LANPort;

                Wsa.PingAlive4Async(MainForm.Conf.WSUsername, MainForm.Conf.WSPassword, port, MainForm.Conf.IPMode == "IPv4", MainForm.IPAddressExternal, MainForm.IPAddress, Guid.NewGuid());

            }
            catch (Exception ex)
            {
                WebsiteLive = false;
                MainForm.LogExceptionToFile(ex);
            }

           
        }

        static void WsaPingAlive4Completed(object sender, PingAlive4CompletedEventArgs e)
        {
            if (e.Error == null)
            {
                string[] r = e.Result;
                
                if (r.Length > 1)
                {
                    
                    if (MainForm.Conf.ServicesEnabled)
                    {
                        if (!MainForm.MWS.Running)
                        {
                            MainForm.StopAndStartServer();
                        }
                    }
                    
                    WebsiteLive = true;

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
        }

        static void WsaSendAlertCompleted(object sender, SendAlertCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MainForm.LogExceptionToFile(e.Error);
            }

        }

        static void WsaSendContentCompleted(object sender, SendContentCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MainForm.LogExceptionToFile(e.Error);
            }

        }

        static void WsaSendAlertWithImageCompleted(object sender, SendAlertWithImageCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MainForm.LogExceptionToFile(e.Error);
            }

        }

        static void WsaSendSMSCompleted(object sender, SendSMSCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MainForm.LogExceptionToFile(e.Error);
            }

        }

        static void WsaSendTweetCompleted(object sender, SendTweetCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                MainForm.LogExceptionToFile(e.Error);
            }

        }

        static void WsaSyncCompleted(object sender, SyncCompletedEventArgs e)
        {
            if (e.Error != null)
            {
                WebsiteLive = false;
                MainForm.NeedsSync = true;
                MainForm.LogExceptionToFile(e.Error);
            }               
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

                if (WebsiteLive)
                {
                    LoginFailed = (r == "Webservices_LoginFailed");
                    if (r != "OK")
                    {
                        MainForm.LogErrorToFile("Webservices: " + r[0]);
                        return LocRm.GetString(r);
                    }
                    return r;
                }
            }
            return LocRm.GetString("iSpyDown");
        }

        public static bool LoginFailed = false;

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
                LoginFailed = (r[0] == "Webservices_LoginFailed");
                if (r.Length == 1 && r[0] != "OK")
                {
                    r[0] = LocRm.GetString(r[0]);                    
                    MainForm.LogErrorToFile("Webservices: "+r[0]);
                }
                if (r.Length > 3 && r[3] != "")
                {
                    r[3] = LocRm.GetString(r[3]);
                    MainForm.LogErrorToFile("Webservices: " + r[3]);
                }
                return r;
            }
            return new[] { LocRm.GetString("iSpyDown") };
        }
    }
}