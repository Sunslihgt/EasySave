using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace EasySave.Logger
{
    public class LogEntry
    {
        public required string Name { get; set; }
        public required string FileSource { get; set; }
        public required string FileTarget { get; set; }
        public required long FileSize { get; set; }
        public required int FileTransferTime { get; set; }
        public required int CryptoTime { get; set; }
        public required string Time { get; set; }
    }


    public static class Logger
    {
        public enum LogFormat
        {
            JSON,
            XML
        }

        private static LogFormat logFormat = LogFormat.JSON;  // Default log format
        private static string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Sources");

        private static Mutex mutex = new Mutex();

        static Logger()
        {
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
        }

        public static void SetLogFormat(LogFormat format)
        {
            logFormat = format;
        }

        public static void SetLogDirectory(string path)
        {
            logDirectory = path;
        }

        public static void Log(string backupName, string sourcePath, string destinationPath, long fileSize, int transferTime, int cryptoTime)
        {
            LogEntry logEntry = new LogEntry
            {
                Name = backupName,
                FileSource = sourcePath,
                FileTarget = destinationPath,
                FileSize = fileSize,
                FileTransferTime = transferTime,
                CryptoTime = cryptoTime,
                Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
            };

            Log(logEntry);
        }

        public static void Log(LogEntry logEntry)
        {
            mutex.WaitOne();
            string logFileName = $"{DateTime.Now:yyyy-MM-dd}.{logFormat.ToString().ToLower()}"; // File name format: yyyy-MM-dd.{json | xml}
            string logFilePath = Path.Combine(logDirectory, logFileName);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            switch (logFormat)
            {
                case LogFormat.JSON:
                    LogJson(logFilePath, logEntry);
                    break;
                case LogFormat.XML:
                    LogXml(logFilePath, logEntry);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"Unknown log type {logFormat.ToString()}");
            }
            mutex.ReleaseMutex();
        }

        private static void LogJson(string logFilePath, LogEntry logEntry)
        {
            List<LogEntry> logs = new List<LogEntry>();

            if (File.Exists(logFilePath))
            {
                try
                {
                    string existingLogs = File.ReadAllText(logFilePath);
                    logs = JsonConvert.DeserializeObject<List<LogEntry>>(existingLogs) ?? new List<LogEntry>();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error reading log file: {ex.Message}");
                }
            }

            logs.Add(logEntry);
            File.WriteAllText(logFilePath, JsonConvert.SerializeObject(logs, Formatting.Indented));
        }

        private static void LogXml(string logFilePath, LogEntry logEntry)
        {
            List<LogEntry> logs = new List<LogEntry>();

            if (File.Exists(logFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(logFilePath, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>));
                        logs = (List<LogEntry>) (serializer.Deserialize(fs) ?? new List<LogEntry>());
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error reading log file: {ex.Message}");
                }
            }

            logs.Add(logEntry);

            using (StreamWriter writer = new StreamWriter(logFilePath))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>));
                serializer.Serialize(writer, logs);
            }
        }
    }
}