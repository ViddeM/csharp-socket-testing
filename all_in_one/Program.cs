using System;
using System.Net;

namespace all_in_one
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Can I parse a hostname? " + IPAddress.Parse("vidarmagnusson.com").ToString());
        }
    }
}
