
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Chat.Server
{
    class Server
    {
        Base socketHandler;
        List<Base> clients;

        public Server()
        {
            clients = new List<Base>();
            try
            {
                socketHandler = new Base();
                socketHandler.Listen();
                Console.WriteLine("Listening for clients...");

                int userCounter = 0;

                Base newUser;
                while (true)
                {
                    userCounter++;
                    newUser = socketHandler.Accept();
                    Console.WriteLine("User connected " + newUser.GetRemoteEndPoint());
                    JObject receivedData = newUser.ReceiveData();
                    ReceivedMessage(receivedData);
                    clients.Add(newUser);

                    new Thread(new ThreadStart(() => ClientHandler(newUser, (string)receivedData["username"]))).Start();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\n The application encountered an exception, shutting down server.");
                Console.WriteLine("\n " + e);
            }

            Console.WriteLine("\n Press enter to continue...");
            Console.Read();
        }

        private void ClientHandler(Base clientSocket, string username)
        {
            JObject welcomeMessage = new JObject();
            welcomeMessage.Add("message", "Welcome to the server " + username + "!");
            welcomeMessage.Add("username", "Server");
            clientSocket.SendData(welcomeMessage);

            JObject fromFile = JObject.Parse(File.ReadAllText("./big_map.json"));
            fromFile.Add("message", "Testing size limits");
            fromFile.Add("username", "Server");
            clientSocket.SendData(fromFile);

            JObject data;
            while (true)
            {

                data = clientSocket.ReceiveData();
                ReceivedMessage(data);
            }
        }

        private void ReceivedMessage(JObject data)
        {
            if (data == null)
            {
                Logger.Log("Data cannot be null!", LogLevel.Error, LogLevel.Error);
                return;
            }

            Console.WriteLine((string)data["username"] + ": " + (string)data["message"]);
            foreach (Base client in clients)
            {
                client.SendData(data);
            }
        }
    }
}
