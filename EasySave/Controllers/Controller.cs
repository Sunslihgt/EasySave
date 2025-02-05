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

        public Controller()
        {
            Start();
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
                case 1:
                    LanguageManager.SetLanguage(LanguageManager.Language.EN);
                    break;
                case 2:
                    LanguageManager.SetLanguage(LanguageManager.Language.FR);
                    break;
                case 3:
                    LanguageManager.SetLanguage(LanguageManager.Language.ES);
                    break;
                default:
                    LanguageManager.SetLanguage(LanguageManager.Language.EN);
                    break;
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
                        CreateSave();
                        break;
                    case 3:
                        MainView.Display("Charger une sauvegarde");
                        break;
                    case 4:
                        MainView.Display("Exécuter une/des sauvegardes");
                        break;
                    case 5:
                        MainView.Display("Fermeture du programme...");
                        running = false;
                        break;
                    default:
                        MainView.Display("Choix inconnu");
                        break;
                }
            }
        }

        public void CreateSave()
        {
            MainView.Display("Créer une sauvegarde");

            // Exemple de sauvegarde fictive
            string backupName = "Save1";
            string sourcePath = @"\\SourcePath\File.txt";
            string destinationPath = @"\\DestinationPath\File.txt";
            long fileSize = 1024; // Taille fictive en octets
            double transferTime = 58.002; // Temps fictif en ms

            EasySave.Logger.Logger.Log(backupName, sourcePath, destinationPath, fileSize, transferTime);
            MainView.Display("Sauvegarde enregistrée dans le journal !");
        }

    }
}
