using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;

namespace Chat
{
    public class Base
    {
        private Socket socket;
        private IPEndPoint endPoint;
        private LogLevel logLevel;
        private static bool UseBson = false;
        // 10 million = 10s;
        private int timeoutTime = 10 * 1000 * 1000; // The timeout time in micro-seconds before disconnecting a client.

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

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            }
            catch (SocketException socketE)
            {
                Logger.Log("A socket exception occured whilst setting up a new socket: \n" + socketE.ToString(), LogLevel.Error, logLevel);
            }

            try
            {
                endPoint = new IPEndPoint(address, port);
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured whilst setting up a new endpoint: \n" + e.ToString(), LogLevel.Error, logLevel);
            }
        }

        public void Connect()
        {
            try
            {
                socket.Connect(endPoint);
            }
            catch (SocketException socketE)
            {
                Logger.Log("SocketException: " + socketE.ToString(), LogLevel.Error, logLevel);
            }
            catch (SecurityException securityE)
            {
                Logger.Log("SecurityException: " + securityE.ToString(), LogLevel.Error, logLevel);
            }
            catch (Exception e)
            {
                Logger.Log(e.ToString(), LogLevel.Error, logLevel);
            }
        }

        public void SendData(JObject data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            string dataToSend = data.ToString();
            if (UseBson)
            {
                dataToSend = ToBson(data);
            }

            try
            {
                socket.Send(Encoding.ASCII.GetBytes(dataToSend + "<EOF>"));
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured whilst trying to send the following data: " + data.ToString() + "\n\n" + e.ToString(), LogLevel.Error, logLevel);
            }
        }

        public JObject ReceiveData()
        {
            byte[] message = new byte[1024 * 16];
            string data = null;
            int totalSize = 0;

            Stopwatch sw = new Stopwatch();

            sw.Start();

            try
            {
                while (true)
                {
                    int size = socket.Receive(message);
                    data += Encoding.ASCII.GetString(message, 0, size);
                    totalSize += size;

                    if (data.IndexOf("<EOF>") > -1)
                    {
                        data = data.Remove(data.Length - 5, 5);
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured whilst trying to recieve data: \n" + e.ToString(), LogLevel.Error, logLevel);
            }

            if (data == null)
            {
                Logger.Log("Unable to receieve data", LogLevel.Error, logLevel);
                return null;
            }

            sw.Stop();

            Logger.Log("Receiving message took: " + sw.Elapsed.ToString() + " (H:M:S:MS)", LogLevel.Basic, logLevel);
            Logger.Log("Received message of (total) size: " + totalSize.ToString("N0") + " bytes", LogLevel.Basic, logLevel);

            JObject json = null;


            if (UseBson)
            {
                json = FromBson(data);
            }
            else
            {
                try
                {
                    json = JObject.Parse(data);
                }
                catch (JsonReaderException jsonE)
                {
                    Logger.Log("Unable to parse json: \n" + jsonE.ToString(), LogLevel.Error, logLevel);
                }
            }

            Logger.Log("Received: " + data + "\nFrom: " + endPoint.ToString(), LogLevel.All, logLevel);

            return json;
        }

        public void Listen(int backlogSize = 10)
        {
            try
            {
                socket.Bind(endPoint);
                socket.Listen(backlogSize);
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured: " + e.ToString(), LogLevel.Error, logLevel);
            }
        }

        public Base Accept()
        {
            Base newBase = new Base();
            newBase.socket = socket.Accept();
            return newBase;
        }

        public EndPoint GetRemoteEndPoint()
        {
            if (socket == null)
            {
                Logger.Log("Socket is null!", LogLevel.Error, logLevel);
                return null;
            }

            return socket.RemoteEndPoint;
        }

        private string ToBson(JObject value)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BsonDataWriter datawriter = new BsonDataWriter(ms))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    serializer.Serialize(datawriter, value);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured whilst trying to convert json to bson: \n" + e.ToString(), LogLevel.Error, logLevel);
                return null;
            }
        }

        private JObject FromBson(string base64data)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64data);

                using (MemoryStream ms = new MemoryStream(data))
                using (BsonDataReader reader = new BsonDataReader(ms))
                {
                    JsonSerializer serializer = new JsonSerializer();
                    return serializer.Deserialize<JObject>(reader);
                }
            }
            catch (Exception e)
            {
                Logger.Log("An exception occured whilst trying to convert a (base64 string) of bson to json: \n" + e.ToString(), LogLevel.Error, logLevel);
                return null;
            }
        }

        public void Disconnect()
        {

        }

        public void StartHeartBeat()
        {
            new Thread(new ThreadStart(() => HeartBeat())).Start();
        }

        private void HeartBeat()
        {
            JObject heartBeatJson = new JObject();
            heartBeatJson.Add("heartbeat", 0);

            while (true)
            {
                if (socket.Connected == false)
                {
                    Logger.Log("Socket disconnected", LogLevel.Basic, logLevel);
                    return;
                }

                socket.Poll(timeoutTime, SelectMode.SelectRead);
            }
        }
    }
}
