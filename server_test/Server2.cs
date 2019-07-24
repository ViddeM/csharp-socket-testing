
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Server
{
    class Server2
    {
        public static void Main(string[] args)
        {
            new Server2().ListenForClients();
        }

        public Server2()
        {

        }

        private class ServerClient
        {
            public Socket socket;
            public ServerClient(Socket socket)
            {
                this.socket = socket;
            }

            public void Start(Action<string> messageReceived)
            {
                while (true)
                {
                    byte[] message = new byte[1024];
                    int size = socket.Receive(message);

                    string data = null;
                    data += Encoding.ASCII.GetString(message, 0, size);
                    Console.WriteLine("Received message " + data + "\n\n of size: " + size.ToString() + "bytes");

                    JObject parsedJson = JObject.Parse(data);

                    messageReceived(parsedJson["username"].ToString() + ": " + parsedJson["message"].ToString());
                    // socket.Send(message, 0, size, SocketFlags.None);
                }
            }
        }

        List<ServerClient> serverClients;

        private void ListenForClients()
        {
            int port = 11111;
            serverClients = new List<ServerClient>();

            try
            {
                Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, port);
                listener.Bind(endPoint);
                listener.Listen(10);

                Console.WriteLine("Listening for connecting clients.");

                Socket clientSocket = default(Socket);
                int counter = 0;
                ServerClient userServer;

                while (true)
                {
                    counter++;
                    clientSocket = listener.Accept();
                    Console.WriteLine(counter + " Client connected");
                    userServer = new ServerClient(clientSocket);
                    serverClients.Add(userServer);
                    Thread userThread = new Thread(new ThreadStart(() => userServer.Start(ReceivedMessage)));
                    userThread.Start();

                    SendToClients("User connected, total users: " + counter.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\n\n The application encounterd an exception, shutting down server.");
                Console.WriteLine("\n " + e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
        }

        private void ReceivedMessage(string message)
        {
            SendToClients(message);
        }

        private void SendToClients(string message)
        {

            foreach (ServerClient client in serverClients)
            {
                Console.WriteLine("Trying to send message:: {0} to client:: {1}", message, client.socket.RemoteEndPoint.ToString());
                client.socket.Send(Encoding.ASCII.GetBytes(message));
            }
        }
    }
}
