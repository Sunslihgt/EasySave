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

        public static void Log(string message, bool debugOnly = false)
        {
            if (debugOnly && !Settings.DEBUG_MODE)
            {
                return;
            }

            lock (logLock)
            {
                Console.WriteLine(message);
            }
        }

        public static void Log(string message, ConsoleColor consoleColor, bool debugOnly = false)
        {
            if (debugOnly && !Settings.DEBUG_MODE)
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

        public static void LogWarning(string message, bool debugOnly = false)
        {
            Log(message, ConsoleColor.Yellow, debugOnly);
        }

        public static void LogError(string message, bool debugOnly = false)
        {
            Log(message, ConsoleColor.Red, debugOnly);
        }
    }
}
