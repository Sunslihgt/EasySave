using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace EasySave.Models
{
    public class Settings
    {
        private static readonly Lazy<Settings> _instance = new(() => LoadSettings());
        public static Settings Instance => _instance.Value;

        private static readonly string CONFIG_FILE_PATH = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "config.json");

        public LanguageManager.Language Language { get; set; } = LanguageManager.Language.EN;
        public string StateFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EasySave", "state.json");
        public string LogFilesPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Logs");
        public string LogType { get; set; } = "JSON";
        public string[] EncryptExtensions { get; set; } = { ".txt", ".xls", ".xlsx" };

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
    }
}

