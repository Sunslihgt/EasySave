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

        public static string ShortenPath(string path, char slashChar = '/')
        {
            int slashes = path.Count(p => p == slashChar);
            // has 2 slashes and no double (C:/.../...) or more than 2 slashes (C://.../...)
            if ((slashes == 2 && !path.Contains($"{slashChar}{slashChar}")) || slashes > 2)
            {
                string pathStart = path.Split(slashChar)[0];
                string pathEnd = path.Split(slashChar)[^0];
                return $"{pathStart}//.../{pathEnd}";
            }
            return path;
        }
    }
}
