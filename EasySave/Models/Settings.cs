using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static EasySave.Logger.Logger;

namespace EasySave.Models
{
    public class Settings
    {
        private static readonly Lazy<Settings> _instance = new(() => LoadSettings());
        public static Settings Instance => _instance.Value;

        private static readonly string CONFIG_FILE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "config.json");

        private static readonly string CryptoSoftExeName = "CryptoSoft.exe";

        public LanguageManager.Language Language { get; set; } = LanguageManager.Language.EN;
        public string StateFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "state.json");
        public string LogDirectoryPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "Logs");
        [JsonConverter(typeof(StringEnumConverter))] // Tell the serializer this property is an enum and not an int in the config file
        public LogFormat LogFormat { get; set; } = LogFormat.JSON;
        public string[] EncryptExtensions { get; set; } = { ".txt", ".xls", ".xlsx" };
        public List<string> BannedSoftwares { get; set; } = new List<string>();
        public string CryptoSoftPath { get; set; } = String.Empty; // Default value will be set in the constructor if not found
        public string CryptoKey { get; set; } = Cryptography.GenerateCryptoKey(64);

        private Settings() { } // Default constructor (uses the default values)

        private static Settings LoadSettings()
        {
            Settings? newSettings = null;
            if (File.Exists(CONFIG_FILE_PATH))
            {
                string json = File.ReadAllText(CONFIG_FILE_PATH);
                newSettings = JsonConvert.DeserializeObject<Settings>(json);
            }

            if (newSettings == null)
            {
                newSettings = new Settings();
                SaveSettings(newSettings);
            }
            else if (string.IsNullOrEmpty(newSettings.CryptoSoftPath)) // If the CryptoSoft path is not set, try to find it
            {
                newSettings.CryptoSoftPath = FindCryptoSoftExe();
                if (!string.IsNullOrEmpty(newSettings.CryptoSoftPath)) // If found, save the new settings
                {
                    SaveSettings(newSettings);
                }
            }

            // Set the logger settings
            SetLogDirectory(newSettings.LogDirectoryPath);
            SetLogFormat(newSettings.LogFormat);

            return newSettings;
        }

        private static void SaveSettings(Settings settings)
        {
            string? directory = Path.GetDirectoryName(CONFIG_FILE_PATH);
            if (directory == null)
            {
                throw new FileNotFoundException("Config file directory not found: " + CONFIG_FILE_PATH);
            }

            Directory.CreateDirectory(directory);
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            File.WriteAllText(CONFIG_FILE_PATH, json);
        }

        private static string FindCryptoSoftExe()
        {
            // Check if it's in the same folder as the app
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CryptoSoftExeName);
            if (File.Exists(localPath)) return localPath;

            // Search in system PATH
            string? pathSearch = SearchInSystemPath(CryptoSoftExeName);
            if (!string.IsNullOrEmpty(pathSearch)) return pathSearch;

            // Search the entire C: drive
            // (slow and might not work if not enough rights)
            string? diskSearch = SearchForExe(CryptoSoftExeName, @"C:\");
            if (!string.IsNullOrEmpty(diskSearch)) return diskSearch;

            Console.Error.WriteLine($"{CryptoSoftExeName} not found.");

            return string.Empty;
        }

        private static string? SearchInSystemPath(string fileName)
        {
            var paths = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
            if (paths == null) return null;

            // Check if file is found in each path directory
            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return null;
        }

        private static string? SearchForExe(string fileName, string searchPath)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(searchPath, fileName, SearchOption.AllDirectories))
                {
                    return file;
                }
            }
            catch { }

            return null;
        }
    }
    public class BannedSoftware
    {
        public string Name { get; set; }
        public string Software { get; set; }

        public BannedSoftware(string name, string software)
        {
            Name = name;
            Software = software;
        }
    }

}

