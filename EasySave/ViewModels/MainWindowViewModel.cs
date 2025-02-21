using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Views;
using static EasySave.Logger.Logger;

namespace EasySave.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly INavigationService _navigationService;

        public Settings Settings { get; } = Settings.Instance;

        public ICommand DeleteSaveCommand { get; }
        public ICommand UpdateSaveCommand { get; }
        public ICommand LoadSaveCommand { get; }
        public ICommand CreateSaveCommand { get; }
        public ICommand PauseSaveCommand { get; }
        public ICommand StopSaveCommand { get; }
        public ICommand PlaySaveCommand { get; }
        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand OpenSettingsWindowCommand { get; }

        public string SaveName { get; set; } = String.Empty;
        public string SaveSource { get; set; } = String.Empty;
        public string SaveDestination { get; set; } = String.Empty;
        public string MySaveType { get; set; } = String.Empty;
        public ObservableCollection<string> SaveTypes { get; } = new ObservableCollection<string>();
        public Mutex LargeFileTransferMutex { get; } = new Mutex();

        public StateLogger StateLogger { get; }
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            // Logger
            SetLogDirectory(Settings.Instance.LogDirectoryPath);

            // Commands
            CreateSaveCommand = new RelayCommand(CreateSave);
            DeleteSaveCommand = new RelayCommand<Save>(DeleteSave);
            UpdateSaveCommand = new RelayCommand<Save>(UpdateSave);
            LoadSaveCommand = new RelayCommand<Save>(LoadSave);
            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);
            OpenSettingsWindowCommand = new RelayCommand(OpenSettingsWindow);
            CreateSaveCommand = new RelayCommand(CreateSave);
            PauseSaveCommand = new RelayCommand<Save>(PauseSave);
            StopSaveCommand = new RelayCommand<Save>(StopSave);
            PlaySaveCommand = new RelayCommand<Save>(PlaySave);

            // State logger
            StateLogger = new StateLogger(this);
            StateLogger.StateFilePath = Settings.Instance.StateFilePath;
            Saves.Clear();
            StateLogger.ReadState().ForEach(save => Saves.Add(save));

            // Save types
            for (int i = 0; i < Enum.GetNames(typeof(Save.SaveType)).Length; i++)
            {
                SaveTypes.Add(Enum.GetNames(typeof(Save.SaveType))[i]);
            }

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
                            ConsoleLogger.LogError($"No save found at index {saveId}");
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
                            ConsoleLogger.LogError($"No save found at index {saveId}");
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
                            ConsoleLogger.LogError($"No save found at index {saveId}");
                        }
                    }
                }
            }
        }

        public void CreateSave()
        {
            if (String.IsNullOrEmpty(SaveName) || String.IsNullOrEmpty(SaveSource) || String.IsNullOrEmpty(SaveDestination))
            {
                return;
            }
            if (Enum.TryParse(MySaveType, out Save.SaveType saveType))
            {
                Save save = new Save(this, saveType, SaveName, SaveSource, SaveDestination);
                if (save.IsRealDirectoryPathValid())
                {
                    Saves.Add(save);
                    save.CreateSave();
                    StateLogger.WriteState(Saves.ToList());
                }
            }
        }

        public void UpdateSave(Save save)
        {
            save.UpdateSave();
        }

        public void LoadSave(Save save)
        {
            save.LoadSave();
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

        public void PauseSave(Save save)
        {
            save.PauseSaveTransfer();
        }

        public void StopSave(Save save)
        {
            save.AbortSaveTransfer();
        }

        public void PlaySave(Save save)
        {
            save.ResumeSaveTransfer();
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
