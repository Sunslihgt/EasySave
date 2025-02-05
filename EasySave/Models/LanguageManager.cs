using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace EasySave.Models
{
    internal static class LanguageManager
    {
        private static Dictionary<string, Dictionary<string, object>> languages;
        public enum Language
        {
            EN,
            FR,
            ES
        }
        private static Language currentLanguage = Language.EN;

        static LanguageManager()
        {
            LoadLanguages();
        }

        private static void LoadLanguages()
        {
            try
            {
                string jsonPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Sources", "languages.json"));

                if (File.Exists(jsonPath))
                {
                    string jsonText = File.ReadAllText(jsonPath, Encoding.UTF8);
                    languages = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(jsonText);
                }
                else
                {
                    Console.WriteLine("⚠️ Language file not found! Using default English.");
                    languages = new Dictionary<string, Dictionary<string, object>>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error loading language file: {ex.Message}");
                languages = new Dictionary<string, Dictionary<string, object>>();
            }
        }

        public static void SetLanguage(Language language)
        {
            if (languages.ContainsKey(language.ToString().ToLower()))
            {
                currentLanguage = language;
            }
            else
            {
                Console.WriteLine("⚠️ Language not found! Defaulting to English.");
                currentLanguage = Language.EN;
            }
        }

        public static string GetText(string key)
        {
            string langKey = currentLanguage.ToString().ToLower();
            if (languages.ContainsKey(langKey) && languages[langKey].ContainsKey(key))
            {
                var value = languages[langKey][key];
                return value is string text ? text : $"[MISSING:{key}]";
            }
            return $"[MISSING:{key}]";
        }

        public static List<string> GetTextArray(string key)
        {
            string langKey = currentLanguage.ToString().ToLower();
            if (languages.ContainsKey(langKey) && languages[langKey].ContainsKey(key))
            {
                try
                {
                    var value = languages[langKey][key];
                    if (value is JArray jsonArray)
                    {
                        return jsonArray.ToObject<List<string>>();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Error parsing language key '{key}': {ex.Message}");
                }
            }
            return new List<string> { "[MISSING]" };
        }

    }
}