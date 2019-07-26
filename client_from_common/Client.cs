
using Newtonsoft.Json.Linq;

using System;
using System.Threading;

namespace Chat.Client
{
    class Client
    {
        private Base socketHandler;

        string username = "user";

        public Client()
        {
            Console.WriteLine("Enter the ip/hostname to connect to. (Leave empty to use default)");
            string host = Console.ReadLine();
            if (host == "")
            {
                host = "127.0.0.1";
            }
            Console.WriteLine("What port should be used? (Leave empty to use default)");
            string portString = Console.ReadLine();
            int port = 0;
            int.TryParse(portString, out port);

            socketHandler = new Base(host, port, LogLevel.Error);

            Console.WriteLine("Please enter a username.");
            username = Console.ReadLine();

            socketHandler.Connect();
            Console.WriteLine("Connected to " + socketHandler.GetRemoteEndPoint().ToString());

            JObject connectMessage = new JObject();
            connectMessage.Add("message", "connected");
            connectMessage.Add("username", username);

            socketHandler.SendData(connectMessage);

            ReceivedData(socketHandler.ReceiveData());

            new Thread(new ThreadStart(() => ListenForUserInput())).Start();
            ListenForServer();
        }

        private void ReceivedData(JObject json)
        {
            if (json["username"].ToString() != username)
            {
                WriteToConsole(json["username"] + ": " + json["message"]);
            }
        }

        private void ListenForUserInput()
        {
            string message;
            JObject toSend;
            while (true)
            {
                message = Console.ReadLine();
                Console.Write(username + ": ");
                toSend = new JObject();
                toSend.Add("username", username);
                toSend.Add("message", message);

                socketHandler.SendData(toSend);
            }
        }

        private void ListenForServer()
        {
            JObject serverMessage;
            while (true)
            {
                serverMessage = socketHandler.ReceiveData();
                ReceivedData(serverMessage);
            }
        }

        private void WriteToConsole(string message)
        {
            ClearConsole();
            Console.WriteLine(message);
            Console.Write(username + ": ");
        }

        private void ClearConsole()
        {
            int currentLineCursor = Console.CursorTop;
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, currentLineCursor);
        }
    }
}
