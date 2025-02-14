using EasySave.Models;
using EasySave.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace EasySave.ViewModels
{
    internal class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private string _selectedLogFormat;
        private string _newSoftwareName = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand ChooseLogTypeCommand { get; }
        public ICommand AddBannedSoftwareCommand { get; }
        public ICommand RemoveBannedSoftwareCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public ObservableCollection<BannedSoftware> BannedSoftwares { get; } = new ObservableCollection<BannedSoftware>();
        public ObservableCollection<string> LogFormats { get; } = new ObservableCollection<string> { "JSON", "XML" };

        public string SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                if (_selectedLogFormat != value)
                {
                    _selectedLogFormat = value;
                    OnPropertyChanged(nameof(SelectedLogFormat));
                    ChooseLogType();
                }
            }
        }

        public string NewSoftwareName
        {
            get => _newSoftwareName;
            set
            {
                _newSoftwareName = value;
                OnPropertyChanged(nameof(NewSoftwareName));
            }
        }

        public SettingsWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);
            ChooseLogTypeCommand = new RelayCommand(ChooseLogType);
            AddBannedSoftwareCommand = new RelayCommand(AddBannedSoftware);
            RemoveBannedSoftwareCommand = new RelayCommand<BannedSoftware>(RemoveBannedSoftware);
            CloseWindowCommand = new RelayCommand(CloseWindow);

            _selectedLogFormat = Settings.Instance.LogType;
        }

        private void CloseWindow()
        {
            Application.Current.Windows
                .OfType<SettingsWindow>()
                .FirstOrDefault()
                ?.Close();
        }

        private void OpenLanguageWindow()
        {
            _navigationService.OpenWindow<LanguageWindow>();
        }

        private void ChooseLogType()
        {
            Settings.Instance.LogType = _selectedLogFormat;
            Settings.Instance.GetType().GetMethod("SaveSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { Settings.Instance });
        }


        // Exemple de mise à jour dans le ViewModel
        private void AddBannedSoftware()
        {
            if (string.IsNullOrWhiteSpace(NewSoftwareName)) return;

            var bannedSoftware = new BannedSoftware(NewSoftwareName, $"{NewSoftwareName}.exe");

            BannedSoftwares.Add(bannedSoftware);

            Settings.Instance.BannedSoftwares.Add(bannedSoftware.Software);
            SaveSettings();

            NewSoftwareName = string.Empty;
        }



        private void RemoveBannedSoftware(BannedSoftware bannedSoftware)
        {
            if (bannedSoftware == null) return;

            BannedSoftwares.Remove(bannedSoftware);

            Settings.Instance.BannedSoftwares.Remove(bannedSoftware.Software);

            SaveSettings();
        }

        private void SaveSettings()
        {
            Settings.Instance.GetType().GetMethod("SaveSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, new object[] { Settings.Instance });
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
