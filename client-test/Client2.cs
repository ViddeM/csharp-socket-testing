using Newtonsoft.Json.Linq;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace client_test
{
    class Client2
    {
        public static void Main(string[] args)
        {
            new Client2();
        }

        string message = null;
        byte[] server_message = null;
        int server_message_size = 0;
        string username = "user";

        public Client2()
        {
            int port = 11111;
            string ipAddress = "127.0.0.1";
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ipAddress), port);
            Console.WriteLine("Please enter a username to be used on the server");
            username = Console.ReadLine();

            clientSocket.Connect(endPoint);
            Console.WriteLine("Client connected to: " + clientSocket.RemoteEndPoint.ToString());



            server_message = new byte[1024];
            server_message_size = clientSocket.Receive(server_message);
            Console.WriteLine("Server: " + Encoding.ASCII.GetString(server_message, 0, server_message_size));

            new Thread(new ThreadStart(() => UserInputListener(clientSocket))).Start();

            while (true)
            {
                server_message = new byte[1024];
                server_message_size = clientSocket.Receive(server_message);
                Console.WriteLine("Server: " + Encoding.ASCII.GetString(server_message, 0, server_message_size));
            }
        }

        private void UserInputListener(Socket clientSocket)
        {
            while (true)
            {
                Console.WriteLine("Enter a message to send to the server");
                message = Console.ReadLine();
                JObject json = new JObject();
                json.Add("message", message);

                SendMessageToServer(clientSocket, json);
            }
        }

        private void SendMessageToServer(Socket socket, JObject json)
        {
            socket.Send(Encoding.ASCII.GetBytes(json.ToString()));
        }
    }
}
