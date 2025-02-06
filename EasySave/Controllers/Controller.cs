using EasySave.Models;
using EasySave.Views;
using EasySave.Logger;

namespace EasySave.Controllers
{
    public class Controller
    {
        public bool running = true;
        public MainView MainView = new();
        public StateLogger StateLogger;
        public SaveManager SaveManager;
        
        public Controller(string[] args) // Main args for CLI mode
        {
            // Interactive mode
            StateLogger = new StateLogger(this);
            SaveManager = new SaveManager(StateLogger);

            SaveManager.Saves = StateLogger.ReadState();
            StateLogger.WriteState(SaveManager.Saves);

            if (args.Length > 0)
            {
                ParseArguments(args);  // CLI mode
            }
            else
            {
                Start();
            }
        }

        // Analyze arguments
        private void ParseArguments(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.Trim().Contains("-run:")) // -run:1-3 or -run:1;3
                {
                    string param = arg.Split(':')[1];

                    if (param.Contains('-')) // List of saves (ex : 1-3)
                    {
                        var range = param.Split('-').Select(int.Parse).ToArray();
                        for (int i = range[0]; i <= range[1]; i++)
                        {
                            SaveManager.Saves[i].LoadSave();
                        }
                    }
                    else if (param.Contains(';')) // Sauvegardes spécifiques (ex : 1;3)
                    {
                        var saves = param.Split(';').Select(int.Parse);
                        foreach (var saveId in saves)
                        {
                            SaveManager.Saves[saveId].LoadSave();
                        }
                    }
                    else // Sauvegarde unique (ex : -run:2)
                    {
                        if (int.TryParse(param, out int saveId))
                        {
                            SaveManager.Saves[saveId].LoadSave();
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
            int languageChoice = MainView.DisplayLang();
            switch (languageChoice)
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
                        UpdateSave();
                        break;
                    case 3:
                        LoadSave();
                        break;
                    case 4:
                        MainView.Display(LanguageManager.GetText("leave"));
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
            MainView.Display(LanguageManager.GetText("create_save") + " :");
            string name = MainView.AskQuestion(LanguageManager.GetText("save_name"));
            int typeChoice = MainView.AskChoice(LanguageManager.GetText("save_type"), LanguageManager.GetTextArray("save_types").ToArray()) - 1;
            Save.SaveType saveType = (Save.SaveType)typeChoice;
            string source = MainView.AskQuestion(LanguageManager.GetText("source_directory"));
            string destination = MainView.AskQuestion(LanguageManager.GetText("destination_directory"));

            Save save = new(SaveManager, saveType, name, source, destination);
            if (save.IsRealDirectoryPathValid())
            {
                SaveManager.Saves.Add(save);
                save.CreateSave();
            }
            else
            {
                MainView.Display(LanguageManager.GetText("invalid_directory"));
            }

            MainView.Display("\n");
        }

        public void UpdateSave()
        {
            int saveIndex = -1;
            while (saveIndex < 0 || saveIndex >= SaveManager.Saves.Count)
            {
                MainView.Display(LanguageManager.GetText("update_save"));
                saveIndex = MainView.AskChoice(LanguageManager.GetText("update_save_question"), SaveManager.GetSaveNames()) - 1;
                if (saveIndex < 0 || saveIndex >= SaveManager.Saves.Count)
                {
                    MainView.Display(LanguageManager.GetText("invalid_choice") + "\n");
                }
            }
            Save save = SaveManager.Saves[saveIndex];
            if (save != null)
            {
                save.UpdateSave();
            }
            else
            {
                MainView.Display(LanguageManager.GetText("save_not_found"));
            }
            MainView.Display("\n");
        }

        public void LoadSave()
        {
            int saveIndex = -1;
            while (saveIndex < 0 || saveIndex >= SaveManager.Saves.Count)
            {
                MainView.Display(LanguageManager.GetText("execute_save"));
                saveIndex = MainView.AskChoice(LanguageManager.GetText("execute_save_question"), SaveManager.GetSaveNames()) - 1;
                if (saveIndex < 0 || saveIndex >= SaveManager.Saves.Count)
                {
                    MainView.Display(LanguageManager.GetText("invalid_choice") + "\n");
                }
            }

            Save save = SaveManager.Saves[saveIndex];
            if (save != null)
            {
                save.LoadSave();
            }
            else
            {
                MainView.Display(LanguageManager.GetText("save_not_found"));
            }
            MainView.Display("\n");
        }
    }
}
