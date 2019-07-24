using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

using System;
using System.IO;
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
        private static bool UseBson = false;

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
            string dataToSend = data.ToString();
            if (UseBson)
            {
                dataToSend = ToBson(data);
            }
            socket.Send(Encoding.ASCII.GetBytes(dataToSend));
        }

        public JObject ReceiveData()
        {
            byte[] message = new byte[1024];
            int size = socket.Receive(message);

            string data = null;
            data += Encoding.ASCII.GetString(message, 0, size);

            JObject json = JObject.Parse(data);
            if (UseBson)
            {
                json = FromBson(data);
            }

            Log("Received: " + data + "\nFrom: " + endPoint.ToString(), LogLevel.All);
            Log("Received message of size: " + size.ToString() + " bytes", LogLevel.Basic);


            return json;
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

        public string ToBson(JObject value)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BsonDataWriter datawriter = new BsonDataWriter(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(datawriter, value);
                return Convert.ToBase64String(ms.ToArray());
            }

        }

        public JObject FromBson(string base64data)
        {
            byte[] data = Convert.FromBase64String(base64data);

            using (MemoryStream ms = new MemoryStream(data))
            using (BsonDataReader reader = new BsonDataReader(ms))
            {
                JsonSerializer serializer = new JsonSerializer();
                return serializer.Deserialize<JObject>(reader);
            }
        }
    }
}
