using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Text;

namespace Chat
{
    public class Base
    {
        private Socket socket;
        private IPEndPoint endPoint;
        private LogLevel logLevel;
        private static bool UseBson;
        private int timeoutTime; // The timeout time in milli-seconds before disconnecting a client.
        private DateTime lastHeartbeat;
        private readonly JObject baseJson;
        private Dictionary<string, int> statusCodesMap;

        public Base(string ip = "", int port = 11111, LogLevel logging = LogLevel.Basic)
        {
            if (File.Exists("./BaseConfig.json") == false)
            {
                throw new FileNotFoundException("Unable to find config file (BaseConfig.json)");
            }

            ReadConfig();

            baseJson = new JObject();
            baseJson.Add("status");
            baseJson.Add("baseData");
            baseJson.Add("data");
            logLevel = logging;
            lastHeartbeat = new DateTime(0);
            Setup(ip, port);
        }

        private void ReadConfig()
        {
            try
            {
                JObject config = JObject.Parse(File.ReadAllText("./BaseConfig.json"));
                timeoutTime = (int)config["timeoutTime"];
                UseBson = (bool)config["useBson"];

                statusCodesMap = new Dictionary<string, int>();

                JObject statusCodes = (JObject)config["statusCodes"];
                foreach (KeyValuePair<string, JToken> status in statusCodes)
                {
                    statusCodesMap.Add(status.Key, (int)status.Value);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Unable to read config: \n" + e.ToString());
            }
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

        public void Connect(JObject connectInfo)
        {
            try
            {
                socket.Connect(endPoint);
                SendData(connectInfo, statusCodesMap["connect"]);
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
            SendData(data, statusCodesMap["sendData"]);
        }

        private void SendData(JObject data, int status, JObject baseData = null)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            if (IsConnected() == false)
            {
                throw new Exception("Socket is disconnected");
            }

            JObject jsonToSend = new JObject(baseJson);
            jsonToSend["data"] = data;
            jsonToSend["status"] = status;
            jsonToSend["baseData"] = baseData;
            string dataToSend = jsonToSend.ToString();
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
            lastHeartbeat = DateTime.Now;

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
            return HandleReceivedJson(json);
        }

        private JObject HandleReceivedJson(JObject json)
        {
            int status = (int)json["status"];
            JObject baseJson = (JObject)json["baseJson"];

            if (status == statusCodesMap["disconnect"])
            {
                socket.Disconnect(false);
            }



            return (JObject)json["data"];
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

        public bool IsConnected()
        {
            if (DateTime.Now.Millisecond - lastHeartbeat.Millisecond > timeoutTime ||
                socket.Connected == false)
            {
                return false;
            }

            return true;
        }
    }
}