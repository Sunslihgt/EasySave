using EasySave.Models;
using EasySave.Views;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using static EasySave.Logger.Logger;

namespace EasySave.ViewModels
{
    internal class SettingsWindowViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;
        private string _selectedLogFormat;
        private string _newSoftwareName = string.Empty;
        private string _newExtensionName = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand OpenLanguageWindowCommand { get; }
        public ICommand ChooseLogFormatCommand { get; }
        public ICommand AddBannedSoftwareCommand { get; }
        public ICommand RemoveBannedSoftwareCommand { get; }
        public ICommand AddExtensionCommand { get; }
        public ICommand RemoveExtensionCommand { get; }
        public ICommand CloseWindowCommand { get; }

        public ObservableCollection<BannedSoftware> BannedSoftwares { get; private set; } = new ObservableCollection<BannedSoftware>();
        public ObservableCollection<string> LogFormats { get; } = new ObservableCollection<string> { "JSON", "XML" };
        public ObservableCollection<string> EncryptExtensions { get; private set; } = new ObservableCollection<string>();

        public string SelectedLogFormat
        {
            get => _selectedLogFormat;
            set
            {
                if (_selectedLogFormat != value)
                {
                    _selectedLogFormat = value;
                    OnPropertyChanged(nameof(SelectedLogFormat));
                    ChooseLogFormat();
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
        public string NewExtensionName
        {
            get => _newExtensionName;
            set
            {
                _newExtensionName = value;
                OnPropertyChanged(nameof(NewExtensionName));
            }
        }

        public SettingsWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));

            OpenLanguageWindowCommand = new RelayCommand(OpenLanguageWindow);
            ChooseLogFormatCommand = new RelayCommand(ChooseLogFormat);
            AddBannedSoftwareCommand = new RelayCommand(AddBannedSoftware);
            RemoveBannedSoftwareCommand = new RelayCommand<BannedSoftware>(RemoveBannedSoftware);
            AddExtensionCommand = new RelayCommand<string>(AddExtension);
            RemoveExtensionCommand = new RelayCommand<string>(RemoveExtension);
            CloseWindowCommand = new RelayCommand(CloseWindow);

            BannedSoftwares.Clear();
            Settings.Instance.BannedSoftwares.ForEach((bannedSoftware) => BannedSoftwares.Add(bannedSoftware));

            EncryptExtensions.Clear();
            foreach (var extension in Settings.Instance.EncryptExtensions)
            {
                EncryptExtensions.Add(extension);
            }

            _selectedLogFormat = Settings.Instance.LogFormat.ToString();
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

        private void ChooseLogFormat()
        {
            if (Enum.TryParse(_selectedLogFormat, out LogFormat logFormat))
            {
                Settings.Instance.UpdateLoggerSetting(logFormat, Settings.Instance.LogDirectoryPath);
            }

            Settings.Instance.GetType().GetMethod("SaveSettings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.Invoke(null, new object[] { Settings.Instance });
        }

        private void AddBannedSoftware()
        {
            if (string.IsNullOrWhiteSpace(NewSoftwareName)) return;

            string softwareNameExe = (NewSoftwareName.EndsWith(".exe") ? NewSoftwareName : $"{NewSoftwareName}.exe");
            string softwareName = softwareNameExe.Replace(".exe", "");

            var bannedSoftware = new BannedSoftware(softwareName, softwareNameExe);

            BannedSoftwares.Add(bannedSoftware);

            Settings.Instance.BannedSoftwares.Add(bannedSoftware);
            SaveSettings();

            NewSoftwareName = string.Empty;
        }

        private void RemoveBannedSoftware(BannedSoftware bannedSoftware)
        {
            if (bannedSoftware == null) return;

            BannedSoftwares.Remove(bannedSoftware);
            Settings.Instance.BannedSoftwares.Remove(bannedSoftware);

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

        private void AddExtension(string extensionName)
        {
            if (string.IsNullOrWhiteSpace(extensionName)) return;

            EncryptExtensions.Add(extensionName);

            var extensionsList = new List<string>(Settings.Instance.EncryptExtensions);
            extensionsList.Add(extensionName);
            Settings.Instance.EncryptExtensions = extensionsList.ToArray();

            SaveSettings();

            NewExtensionName = string.Empty; // Réinitialise le champ de texte
        }


        private void RemoveExtension(string extension)
        {
            if (string.IsNullOrWhiteSpace(extension)) return;

            EncryptExtensions.Remove(extension);

            var extensionsList = new List<string>(Settings.Instance.EncryptExtensions);
            extensionsList.Remove(extension);
            Settings.Instance.EncryptExtensions = extensionsList.ToArray();

            SaveSettings();
        }
    }
}