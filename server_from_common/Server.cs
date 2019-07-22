
using Newtonsoft.Json.Linq;

using System;
using System.Collections.Generic;

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
                    ReceivedMessage(socketHandler.ReceiveData());
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

        private void ClientHandler(Base socketHandler)
        {
            JObject data;
            while (true)
            {
                data = socketHandler.ReceiveData();
                ReceivedMessage(data);
            }
        }

        private void ReceivedMessage(JObject data)
        {

        }
    }
}
