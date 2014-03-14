using System;
using System.Security.Cryptography.X509Certificates;

namespace iSpyApplication
{
    class X509
    {
        private static X509Certificate _sslCertificate;

        public static X509Certificate SslCertificate
        {
            get { return _sslCertificate; }
        }

        public static bool SslEnabled
        {
            get { return MainForm.Conf.SSLEnabled && _sslCertificate != null; }
        }

        public static string LoadCertificate(string fileName)
        {
            try
            {
                _sslCertificate = X509Certificate.CreateFromCertFile(fileName);
                MainForm.LogMessageToFile("Loaded SSL Certificate: " + _sslCertificate.ToString(false));
                return "OK";
            }
            catch (Exception ex)
            {
                MainForm.LogExceptionToFile(ex);
                return ex.Message;
            }
        }
    }
}
