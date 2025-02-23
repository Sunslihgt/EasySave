using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public static class ConsoleLogger
    {
        private static object logLock = new object();

        public static void Log(string message)
        {
            if (!Settings.DEBUG_MODE)
            {
                return;
            }

            lock (logLock)
            {
                Console.WriteLine(message);
            }
        }

        public static void Log(string message, ConsoleColor consoleColor)
        {
            if (!Settings.DEBUG_MODE)
            {
                return;
            }

            lock (logLock)
            {
                Console.ForegroundColor = consoleColor;
                Console.WriteLine(message);
                Console.ResetColor();
            }
        }

        public static void LogWarning(string message)
        {
            Log(message, ConsoleColor.Yellow);
        }

        public static void LogError(string message)
        {
            Log(message, ConsoleColor.Red);
        }
    }
}
