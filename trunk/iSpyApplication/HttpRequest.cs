using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace iSpyApplication
{
    public class HttpRequest
    {
        public TcpClient TcpClient;
        public IPEndPoint EndPoint;
        public Stream Stream; //SSLStream or NetworkStream depending on client
        public RestartableReadStream RestartableStream;
        public byte[] Buffer;
        public string ASCII="";

        public void Destroy()
        {
            try
            {
                if (TcpClient != null && TcpClient.Client != null)
                {
                    TcpClient.Client.Close();
                    TcpClient = null;
                }

                if (RestartableStream != null)
                {
                    RestartableStream.Close();
                    RestartableStream = null;
                }
                if (Stream != null)
                {
                    Stream.Close();
                    Stream = null;
                }
                Buffer = null;
            }
            catch
            {
                
            }



        }
    }
}
