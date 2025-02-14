using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using EasySave.Models;
using EasySave.Views;
using System.Windows.Documents;

namespace EasySave.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly INavigationService _navigationService;

        public Settings Settings { get; } = Settings.Instance;

        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand OpenSettingsWindowCommand { get; }
        public ICommand DeleteSaveCommand { get; }

        public StateLogger StateLogger { get; }
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            Logger.Logger.SetLogDirectory(Settings.Instance.LogDirectoryPath);

            DeleteSaveCommand = new RelayCommand<Save>(DeleteSave);
            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);
            OpenSettingsWindowCommand = new RelayCommand(OpenSettingsWindow);

            StateLogger = new StateLogger(this);
            StateLogger.StateFilePath = Settings.Instance.StateFilePath;
            Saves.Clear();
            StateLogger.ReadState().ForEach(save => Saves.Add(save));
            
            // CLI execution mode (-run:0 or -run:0;2 or -run:0-2)
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                ParseArguments(args);
                App.Current.Shutdown();
            }
        }

        // Analyze arguments to load saves from CLI
        private void ParseArguments(string[] args)
        {
            string fullArg = String.Join("", args);
            
            if (fullArg.Trim().Contains("-run:")) // -run:1-3 or -run:1;3
            {
                string param = fullArg.Split("-run:")[1];

                if (param.Contains('-')) // List of saves (ex : 1-3)
                {
                    var range = param.Split('-').Select(int.Parse).ToArray();
                    for (int saveId = range[0]; saveId <= range[1]; saveId++)
                    {
                        try
                        {
                            Saves[saveId].LoadSave();
                        }
                        catch
                        {
                            Console.Error.WriteLine($"No save found at index {saveId}");
                        }
                    }
                }
                else if (param.Contains(';')) // Sauvegardes spécifiques (ex : 1;3)
                {
                    var saves = param.Split(';').Select(int.Parse);
                    foreach (var saveId in saves)
                    {
                        try
                        {
                            Saves[saveId].LoadSave();
                        }
                        catch
                        {
                            Console.Error.WriteLine($"No save found at index {saveId}");
                        }
                    }
                }
                else // Sauvegarde unique (ex : -run:2)
                {
                    if (int.TryParse(param, out int saveId))
                    {
                        try
                        {
                            Saves[saveId].LoadSave();
                        }
                        catch
                        {
                            Console.Error.WriteLine($"No save found at index {saveId}");
                        }
                    }
                }
            }
        }

        public void DeleteSave(Save save)
        {
            if (save.DeleteSave())
            {
                Saves.Remove(save);
                save.Dispose();
                StateLogger.WriteState(Saves.ToList());
            }
        }

        public void OpenLanguageWindow()
        {
            _navigationService.OpenWindow<LanguageWindow>();
        }
        public void OpenSettingsWindow()
        {
            _navigationService.OpenWindow<SettingsWindow>();
        }
    }
}
