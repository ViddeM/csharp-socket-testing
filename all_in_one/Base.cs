using Newtonsoft.Json.Linq;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Chat
{
    public class Base
    {
        private Socket socket;
        private IPEndPoint endPoint;
        private LogLevel logLevel;

        public Base(string ip = "", int port = 11111, LogLevel logging = LogLevel.Basic)
        {
            logLevel = logging;
            Setup(ip, port);
        }

        public void Setup(string ip, int port)
        {
            IPAddress address = IPAddress.Any;
            if (ip != "")
            {
                if (IPAddress.TryParse(ip, out address) == false)
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                    if (hostEntry.AddressList.Length > 0)
                    {
                        address = hostEntry.AddressList[0];
                    }
                }
            }

            if (port == 0)
            {
                port = 11111;
            }

            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            endPoint = new IPEndPoint(address, port);
        }

        public void Connect()
        {
            socket.Connect(endPoint);
        }

        public void SendData(JObject data)
        {
            socket.Send(Encoding.ASCII.GetBytes(data.ToString()));
        }

        public JObject ReceiveData()
        {
            byte[] message = new byte[1024];
            int size = socket.Receive(message);

            string data = null;
            data += Encoding.ASCII.GetString(message, 0, size);

            Log("Received: " + data + "\nFrom: " + endPoint.ToString(), LogLevel.All);
            Log("Received message of size: " + size.ToString() + " bytes", LogLevel.Basic);


            return JObject.Parse(data);
        }

        public void Listen(int backlogSize = 10)
        {
            socket.Bind(endPoint);
            socket.Listen(backlogSize);
        }

        public Base Accept()
        {
            Base newBase = new Base();
            newBase.socket = socket.Accept();
            return newBase;
        }

        public EndPoint GetRemoteEndPoint()
        {
            return socket.RemoteEndPoint;
        }

        private void Log(string message, LogLevel level)
        {
            if (level <= logLevel)
            {
                switch (level)
                {
                    case LogLevel.Error:
                    {
                        Console.WriteLine("Error: " + message);
                        break;
                    }
                    case LogLevel.Basic:
                    {
                        Console.WriteLine("Info: " + message);
                        break;
                    }
                    case LogLevel.All:
                    {
                        Console.WriteLine("Info: " + message);
                        break;
                    }
                    default:
                    {
                        Console.WriteLine("Unknown: " + message);
                        break;
                    }
                }
            }
        }
    }
}
