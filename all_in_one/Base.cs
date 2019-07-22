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

        public Base(string ip = "", int port = 11111)
        {
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

            Console.WriteLine("Received: " + data + "\nFrom: " + socket.RemoteEndPoint + "\nOf size: " + size.ToString() + "bytes");

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
    }
}
