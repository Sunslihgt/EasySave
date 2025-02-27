using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasySave.Models
{
    public static class LanguageManager
    {
        private static Dictionary<string, Dictionary<string, object>> languages = new Dictionary<string, Dictionary<string, object>>();
        public enum Language
        {
            EN,
            FR,
            ES
        }
        public static Language CurrentLanguage { get; private set; } = Language.EN;

        public static event Action? LanguageChanged;

        static LanguageManager()
        {
            LoadLanguages();
        }

        private static void LoadLanguages()
        {
            try
            {
                string jsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sources", "languages.json");
                if (File.Exists(jsonPath))
                {
                    string jsonText = File.ReadAllText(jsonPath, System.Text.Encoding.UTF8);
                    languages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonText) ?? new Dictionary<string, Dictionary<string, object>>();
                }
                else
                {
                    ConsoleLogger.Log("Language file not found! Using default English.");
                    languages = new Dictionary<string, Dictionary<string, object>>();
                }
            }
            catch (Exception ex)
            {
                ConsoleLogger.LogError($"Error loading language file: {ex.Message}");
                languages = new Dictionary<string, Dictionary<string, object>>();
            }
        }

        public static void SetLanguage(Language language)
        {
            if (languages.ContainsKey(language.ToString().ToLower()))
            {
                CurrentLanguage = language;
                LanguageChanged?.Invoke();
            }
            else
            {
                ConsoleLogger.Log("Language not found! Defaulting to English.");
                CurrentLanguage = Language.EN;
                LanguageChanged?.Invoke();
            }
        }

        public static string GetText(string key)
        {
            string langKey = CurrentLanguage.ToString().ToLower();
            if (languages.ContainsKey(langKey) && languages[langKey].ContainsKey(key))
            {
                var value = languages[langKey][key];
                return value is string text ? text : $"[MISSING:{key}]";
            }
            return $"[MISSING:{key}]";
        }
    }
}
