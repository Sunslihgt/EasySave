using System.Collections.ObjectModel;
using System.Windows.Input;
using System;
using EasySave.Models;
using EasySave.Views;

namespace EasySave.ViewModels
{
    public class MainWindowViewModel
    {
        private readonly INavigationService _navigationService;

        public Settings Settings { get; } = Settings.Instance;

        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand DeleteSaveCommand { get; }

        public StateLogger StateLogger { get; }
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            Logger.Logger.SetLogDirectory(Settings.Instance.LogDirectoryPath);

            DeleteSaveCommand = new RelayCommand<Save>(DeleteSave);
            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);

            StateLogger = new StateLogger(this);
            StateLogger.StateFilePath = Settings.Instance.StateFilePath;
            Saves.Clear();
            StateLogger.ReadState().ForEach(save => Saves.Add(save));
        }

        public void DeleteSave(Save save)
        {
            Saves.Remove(save);
            save.Dispose();

            StateLogger.WriteState(Saves.ToList());
        }

        public void OpenLanguageWindow()
        {
            _navigationService.OpenWindow<LanguageWindow>();
        }
    }
}
