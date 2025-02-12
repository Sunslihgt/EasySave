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
        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand DeleteSaveCommand { get; }

        public StateLogger StateLogger { get; }
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();

        //public MainWindowViewModel()
        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            DeleteSaveCommand = new RelayCommand<Save>(DeleteSave);
            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);

            StateLogger = new StateLogger(this);
            Saves.Clear();
            StateLogger.ReadState().ForEach(save => Saves.Add(save));

            //Console.WriteLine(SaveManager.GetSaveInfos());
            Console.WriteLine(Saves.Count());
            Saves.ToList().ForEach(save =>
            {
                Console.WriteLine($"{save.Name} {save.RealDirectoryPath} {save.CopyDirectoryPath} {save.Transfering}");
            });
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
