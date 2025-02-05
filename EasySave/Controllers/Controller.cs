using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EasySave.Models;
using EasySave.Views;
using EasySave.Logger;

namespace EasySave.Controllers
{
    public class Controller
    {
        public bool running = true;
        public MainView MainView = new();

        // ✅ Ajout des arguments dans le constructeur
        public Controller(string[] args)
        {
            if (args.Length > 0)
            {
                ParseArguments(args);  // Mode automatique via la ligne de commande
            }
            else
            {
                Start();  // Mode interactif si aucun argument
            }
        }

        // 🚀 Méthode pour analyser les arguments
        private void ParseArguments(string[] args)
        {
            foreach (var arg in args)
            {
                if (arg.Contains("-run:")) // Ex: -run:1-3 ou -run:1;3
                {
                    string param = arg.Split(':')[1];

                    if (param.Contains('-')) // Plage de sauvegardes (ex : 1-3)
                    {
                        var range = param.Split('-').Select(int.Parse).ToArray();
                        for (int i = range[0]; i <= range[1]; i++)
                        {
                            ExecuteSave(i);
                        }
                    }
                    else if (param.Contains(';')) // Sauvegardes spécifiques (ex : 1;3)
                    {
                        var saves = param.Split(';').Select(int.Parse);
                        foreach (var saveId in saves)
                        {
                            ExecuteSave(saveId);
                        }
                    }
                    else // Sauvegarde unique (ex : -run:2)
                    {
                        if (int.TryParse(param, out int saveId))
                        {
                            ExecuteSave(saveId);
                        }
                        else
                        {
                            MainView.Display($"❌ ID de sauvegarde invalide : {param}");
                        }
                    }
                }
                else
                {
                    MainView.Display($"⚠️ Argument non reconnu : {arg}");
                }
            }
        }

        private void Start()
        {
            MainView.Display("""
            ===========================
            Bienvenue sur EasySave v1.0
            ===========================
            """);

            int langueChoice = MainView.DisplayLang();
            switch (langueChoice)
            {
                case 1: LanguageManager.SetLanguage(LanguageManager.Language.EN); break;
                case 2: LanguageManager.SetLanguage(LanguageManager.Language.FR); break;
                case 3: LanguageManager.SetLanguage(LanguageManager.Language.ES); break;
                default: LanguageManager.SetLanguage(LanguageManager.Language.EN); break;
            }
            MainLoop();
        }

        private void MainLoop()
        {
            while (running)
            {
                int menuChoice = MainView.DisplayMenu();
                switch (menuChoice)
                {
                    case 1:
                        CreateSave();
                        break;
                    case 2:
                        LoadSave();
                        break;
                    case 3:
                        MainView.Display("Charger une sauvegarde");
                        break;
                    case 4:
                        MainView.Display("Exécuter une/des sauvegardes");
                        ExecuteSavePrompt();
                        break;
                    case 5:
                        MainView.Display("Fermeture du programme...");
                        running = false;
                        break;
                    default:
                        MainView.Display("❓ Choix inconnu");
                        break;
                }
            }
        }

        public void CreateSave()
        {
<<<<<<< HEAD
            MainView.Display("📦 Créer une sauvegarde");
=======
            MainView.Display("Créer une sauvegarde");

>>>>>>> 26358401f8323e5e6dfb750104820904b3b971aa
            // Exemple de sauvegarde fictive
            string backupName = "Save1";
            string sourcePath = @"\\SourcePath\File.txt";
            string destinationPath = @"\\DestinationPath\File.txt";
            long fileSize = 1024; // Taille fictive en octets
            double transferTime = 58.002; // Temps fictif en ms

            EasySave.Logger.Logger.Log(backupName, sourcePath, destinationPath, fileSize, transferTime);
            MainView.Display("Sauvegarde enregistrée dans le journal !");
<<<<<<< HEAD
            // Implémentation de la création de sauvegarde
        }

        public void LoadSave()
        {
            MainView.Display("📂 Charger une sauvegarde");
            // Implémentation pour charger une sauvegarde existante
        }

        // ⚡ Méthode d'exécution de sauvegarde
        public void ExecuteSave(int saveId)
        {
            MainView.Display($"🚀 Exécution de la sauvegarde {saveId}...");
            // Implémentation de la logique de sauvegarde
        }

        // 🗂️ Demande à l'utilisateur quelles sauvegardes exécuter
        public void ExecuteSavePrompt()
        {
            MainView.Display("Entrez l'ID des sauvegardes à exécuter (ex : 1-3 ou 1;3) : ");
            string input = Console.ReadLine();
            ParseArguments(new string[] { $"-run:{input}" });
        }

=======
        }

>>>>>>> 26358401f8323e5e6dfb750104820904b3b971aa
    }
}
