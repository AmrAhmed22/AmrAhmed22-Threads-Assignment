using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Shared.Logging
{

    public static class Log
    {

        static readonly Object Sync = new();

        public static void Info(string message) => Write(message, color: null);

        public static void Success(string message) => Write(message, ConsoleColor.Green);

        public static void Error(string message) => Write(message, ConsoleColor.Red);

        static void Write(string message, ConsoleColor? color)
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            int threadId = Environment.CurrentManagedThreadId;
            string line = $"[{time}] [Thread {threadId}] {message}";

            lock (Sync)
            {
                if (color is not null)
                    Console.ForegroundColor = color.Value;

                Console.WriteLine(line);

                if (color is not null)
                    Console.ResetColor();
            }
        }
    }

}