// A C# program for Client 
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{

    class Program
    {
        /*

        // Main Method 
        static void Main(string[] args)
        {
            new Program().StartClient();
        }

        */

        void StartClient()
        {

            try
            {

                // Establish the remote endpoint  
                // for the socket. This example  
                // uses port 11111 on the local  
                // computer. 
                string hostname = Dns.GetHostName();
                Console.WriteLine("Please enter hostname of desired remote.");
                hostname = Console.ReadLine();
                Console.WriteLine("Looking for remote at {0}", hostname);
                IPHostEntry ipHost = Dns.GetHostEntry(hostname);
                //byte[] address = new byte[] { 172, 17, 0, 144 };
                //IPAddress ipAddr = new IPAddress(address);
                IPAddress ipAddr = ipHost.AddressList[0];
                Console.WriteLine("Ip address {0}", ipAddr);
                IPEndPoint localEndPoint = new IPEndPoint(ipAddr, 11111);
                DateTime now;
                try
                {
                    while (true)
                    {
                        Console.WriteLine("Write something!");
                        string text = Console.ReadLine();
                        now = DateTime.Now;
                        Console.WriteLine("{0} :: {1}\n", GetFormattedDate(now), text);

                        // Creation TCP/IP Socket using  
                        // Socket Class Costructor 
                        Socket sender = new Socket(ipAddr.AddressFamily,
                                   SocketType.Stream, ProtocolType.Tcp);

                        // Connect Socket to the remote  
                        // endpoint using method Connect() 
                        sender.Connect(localEndPoint);
                        // We print EndPoint information  
                        // that we are connected 
                        Console.WriteLine("Socket connected to -> {0} ",
                                      sender.RemoteEndPoint.ToString());

                        // Creation of messagge that 
                        // we will send to Server 
                        byte[] messageSent = Encoding.ASCII.GetBytes(text + "<EOF>");
                        int byteSent = sender.Send(messageSent);

                        // Data buffer 
                        byte[] messageReceived = new byte[1024];

                        // We receive the messagge using  
                        // the method Receive(). This  
                        // method returns number of bytes 
                        // received, that we'll use to  
                        // convert them to string 
                        int byteRecv = sender.Receive(messageReceived);
                        text = Encoding.ASCII.GetString(messageReceived, 0, byteRecv);

                        now = DateTime.Now;
                        Console.WriteLine("{0} Server :: {1}\n", GetFormattedDate(now), text);


                        // Close Socket using  
                        // the method Close() 
                        Console.WriteLine("Shutting down connection to server");
                        sender.Shutdown(SocketShutdown.Both);
                        sender.Close();
                    }
                }

                // Manage of Socket's Exceptions 
                catch (ArgumentNullException ane)
                {

                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }

                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }

                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }

            catch (Exception e)
            {

                Console.WriteLine(e.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Program finished; Press any key to exit");
            Console.ReadKey();
        }

        string GetFormattedDate(DateTime time)
        {
            string hour = time.Hour.ToString();
            hour = time.Hour < 10 ? "0" + hour : hour;
            string minute = time.Minute.ToString();
            minute = time.Minute < 10 ? "0" + minute : minute;
            string second = time.Second.ToString();
            second = time.Second < 10 ? "0" + second : second;

            return hour + ":" + minute + ":" + second + ":" + time.Millisecond.ToString();
        }
    }
}