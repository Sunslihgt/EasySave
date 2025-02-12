using Newtonsoft.Json;
using System.Xml.Serialization;

public class LogEntry
{
    public string Name { get; set; }
    public string FileSource { get; set; }
    public string FileTarget { get; set; }
    public long FileSize { get; set; }
    public double FileTransferTime { get; set; }
    public string Time { get; set; }
}

public static class Logger
{
    public enum LogFormat
    {
        JSON,
        XML
    }

    private static LogFormat _logFormat = LogFormat.JSON;  // Format par défaut
    private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Logs");

    public static void SetLogFormat(LogFormat format)
    {
        _logFormat = format;
    }

    public static void Log(string backupName, string sourcePath, string destinationPath, long fileSize, double transferTime)
    {
        string logFileName = $"{DateTime.Now:yyyy-MM-dd}.{_logFormat.ToString().ToLower()}"; // fichier JSON ou XML
        string logFilePath = Path.Combine(LogDirectory, logFileName);

        LogEntry logEntry = new LogEntry
        {
            Name = backupName,
            FileSource = sourcePath,
            FileTarget = destinationPath,
            FileSize = fileSize,
            FileTransferTime = transferTime,
            Time = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")
        };

        if (_logFormat == LogFormat.JSON)
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
                    Console.WriteLine($"⚠️ Erreur lecture fichier log : {ex.Message}");
                }
            }

            logs.Add(logEntry);
            File.WriteAllText(logFilePath, JsonConvert.SerializeObject(logs, Formatting.Indented));
        }
        else if (_logFormat == LogFormat.XML)
        {
            List<LogEntry> logs = new List<LogEntry>();

            if (File.Exists(logFilePath))
            {
                try
                {
                    using (FileStream fs = new FileStream(logFilePath, FileMode.Open))
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(List<LogEntry>));
                        logs = (List<LogEntry>)serializer.Deserialize(fs) ?? new List<LogEntry>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Erreur lecture fichier log : {ex.Message}");
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
