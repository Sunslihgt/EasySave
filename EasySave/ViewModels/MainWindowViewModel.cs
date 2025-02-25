using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Windows.Input;
using EasySave.Models;
using EasySave.Views;
using System.ComponentModel;
using System.Windows;
using static EasySave.Logger.Logger;

namespace EasySave.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private string _saveName = string.Empty;
        private string _saveSource = string.Empty;
        private string _saveDestination = string.Empty;
        private string _mySaveType = string.Empty;

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
        public ICommand OpenSourceFolderCommand { get; }
        public ICommand OpenDestinationFolderCommand { get; }

        public string SaveName
        {
            get => _saveName;
            set
            {
                _saveName = value;
                OnPropertyChanged(nameof(SaveName));
            }
        }

        public string SaveSource
        {
            get => _saveSource;
            set
            {
                _saveSource = value;
                OnPropertyChanged(nameof(SaveSource));
            }
        }

        public string SaveDestination
        {
            get => _saveDestination;
            set
            {
                _saveDestination = value;
                OnPropertyChanged(nameof(SaveDestination));
            }
        }

        public string MySaveType
        {
            get => _mySaveType;
            set
            {
                _mySaveType = value;
                OnPropertyChanged(nameof(MySaveType));
            }
        }

        public ObservableCollection<string> SaveTypes { get; } = new ObservableCollection<string>();
        public object LargeFileTransferLock { get; } = new object();

        public StateLogger StateLogger { get; }
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();
        public Server Server { get; }

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
            OpenSourceFolderCommand = new RelayCommand(OpenSourceFolder);
            OpenDestinationFolderCommand = new RelayCommand(OpenDestinationFolder);
            CreateSaveCommand = new RelayCommand(CreateSave);
            PauseSaveCommand = new RelayCommand<Save>(PauseSave);
            StopSaveCommand = new RelayCommand<Save>(StopSave);
            PlaySaveCommand = new RelayCommand<Save>(PlaySave);

            // Server
            Server = new Server(this);

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
            string fullArg = string.Join("", args);

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
            if (string.IsNullOrEmpty(SaveName) || SaveName.Contains('|') || string.IsNullOrEmpty(SaveSource) || string.IsNullOrEmpty(SaveDestination))
            {
                return;
            }
            if (Saves.Any(s => s.Name.Equals(SaveName, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }
            if (Enum.TryParse(MySaveType, out Save.SaveType saveType))
            {
                Save save = new Save(this, saveType, SaveName, SaveSource, SaveDestination);
                if (save.IsRealDirectoryPathValid())
                {
                    Saves.Add(save);
                    bool createSuccess = save.CreateSave();
                    if (createSuccess)
                    {
                        StateLogger.WriteState(Saves.ToList());
                        SaveName = string.Empty;
                        SaveDestination = string.Empty;
                        SaveSource = string.Empty;
                        MySaveType = string.Empty;
                    }
                }
            }
        }

        public bool CreateSaveThreaded(string saveName, string saveSource, string saveDestination, Save.SaveType saveType)
        {
            // Use dispatcher to avoid cross-threading issues
            return Application.Current.Dispatcher.Invoke(new Func<bool>(() =>
            {
                if (string.IsNullOrEmpty(saveName) || saveName.Contains('|') || string.IsNullOrEmpty(saveSource) || string.IsNullOrEmpty(saveDestination))
                {
                    return false;
                }
                if (Saves.Any(s => s.Name.Equals(saveName, StringComparison.OrdinalIgnoreCase)))
                {
                    return false;
                }
                Save save = new Save(this, saveType, saveName, saveSource, saveDestination);
                if (save.IsRealDirectoryPathValid())
                {
                    Saves.Add(save);
                    bool createSuccess = save.CreateSave();
                    if (createSuccess)
                    {
                        StateLogger.WriteState(Saves.ToList());
                    }
                    return createSuccess;
                }
                return false;
            }));
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

        public void DeleteSaveThreaded(Save save)
        {
            // Use dispatcher to avoid cross-threading issues
            Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                if (save.DeleteSave())
                {
                    Saves.Remove(save);
                    save.Dispose();
                    StateLogger.WriteState(Saves.ToList());
                }
            }));
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

        private void OpenSourceFolder()
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };
            if (folderDialog?.ShowDialog() == CommonFileDialogResult.Ok)
            {
                Console.WriteLine(folderDialog?.FileName);
                SaveSource = folderDialog?.FileName ?? "";
            }
        }

        private void OpenDestinationFolder()
        {
            var folderDialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (folderDialog?.ShowDialog() == CommonFileDialogResult.Ok)
            {
                SaveDestination = folderDialog?.FileName ?? "";
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}