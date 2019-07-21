using Newtonsoft.Json.Linq;

using System;
using System.Threading;

namespace all_in_one
{
    class Client
    {
        private Base socketHandler;

        string username = "user";

        public Client()
        {
            Console.WriteLine("Enter the ip/hostname to connect to. (Leave empty to use default)");
            string host = Console.ReadLine();
            Console.WriteLine("What port should be used? (Leave empty to use default)");
            string portString = Console.ReadLine();
            int port = 0;
            int.TryParse(portString, out port);

            socketHandler = new Base(host, port);

            Console.WriteLine("Please enter a username.");
            username = Console.ReadLine();

            JObject connectMessage = new JObject();
            connectMessage.Add("message", "");
            connectMessage.Add("username", username);

            socketHandler.Connect();
            socketHandler.SendData(connectMessage);

            ReceivedData(socketHandler.ReceiveData());

            new Thread(new ThreadStart(() => ListenForUserInput())).Start();
            ListenForServer();
        }

        private void ReceivedData(JObject json)
        {
            Console.WriteLine(json["username"] + ": " + json["message"]);
        }

        private void ListenForUserInput()
        {
            string message;
            JObject toSend;
            while (true)
            {
                message = Console.ReadLine();
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
    }
}
