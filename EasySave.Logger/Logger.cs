using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace EasySave.Logger
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Sources", "DailyLog");

        static Logger()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public static void Log(string backupName, string sourcePath, string destinationPath, long fileSize, double transferTime)
        {
            string logFileName = $"{DateTime.Now:yyyy-MM-dd}.json";
            string logFilePath = Path.Combine(LogDirectory, logFileName);

            var logEntry = new
            {
                Name = backupName,
                FileSource = sourcePath,
                FileTarget = destinationPath,
                FileSize = fileSize,
                FileTransferTime = transferTime,
                Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            List<object> logs = new List<object>();

            if (File.Exists(logFilePath))
            {
                try
                {
                    string existingLogs = File.ReadAllText(logFilePath);
                    logs = JsonConvert.DeserializeObject<List<object>>(existingLogs) ?? new List<object>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error reading log file: {ex.Message}");
                }
            }

            logs.Add(logEntry);
            File.WriteAllText(logFilePath, JsonConvert.SerializeObject(logs, Formatting.Indented));
        }
    }
}