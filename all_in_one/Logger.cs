using System;

namespace Chat
{
    public static class Logger
    {
        public static void Log(string message, LogLevel messageLevel, LogLevel acceptedLevel)
        {
            if (messageLevel <= acceptedLevel)
            {
                switch (messageLevel)
                {
                    case LogLevel.Error:
                    {
                        Console.WriteLine("Error: " + message);
                        break;
                    }
                    case LogLevel.Basic:
                    {
                        Console.WriteLine("Info: " + message);
                        break;
                    }
                    case LogLevel.All:
                    {
                        Console.WriteLine("Info: " + message);
                        break;
                    }
                    default:
                    {
                        Console.WriteLine("Unknown: " + message);
                        break;
                    }
                }
            }
        }

    }
}
