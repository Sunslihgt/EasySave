using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using EasySave.Monitoring.Models;
using System.Windows;
using System.Net;

namespace EasySave.Monitoring.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _saveName = string.Empty;
        private string _saveSource = string.Empty;
        private string _saveDestination = string.Empty;
        private string _mySaveType = string.Empty;

        public ICommand ConnectCommand { get; }
        public ICommand DeleteSaveCommand { get; }
        public ICommand UpdateSaveCommand { get; }
        public ICommand LoadSaveCommand { get; }
        public ICommand CreateSaveCommand { get; }
        public ICommand PauseSaveCommand { get; }
        public ICommand StopSaveCommand { get; }
        public ICommand PlaySaveCommand { get; }

        public Visibility ShowDisonnected{ get; set; } = Visibility.Visible;
        public Visibility ShowWaiting{ get; set; } = Visibility.Collapsed;
        public Visibility ShowLoggedOut { get; set; } = Visibility.Collapsed;
        public Visibility ShowConnected{ get; set; } = Visibility.Collapsed;

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

        public string IP { get; set; } = "127.0.0.1";
        public string Port { get; set; } = "8888";
        public string Password { get; set; } = "";

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
        
        public ObservableCollection<Save> Saves { get; } = new ObservableCollection<Save>();
        public ObservableCollection<string> SaveTypes { get; } = new ObservableCollection<string>();

        public Client Client { get; set; }

        public MainWindowViewModel()
        {
            // Commands
            ConnectCommand = new RelayCommand(Connect);
            CreateSaveCommand = new RelayCommand(CreateSave);
            DeleteSaveCommand = new RelayCommand<Save>(DeleteSave);
            UpdateSaveCommand = new RelayCommand<Save>(UpdateSave);
            LoadSaveCommand = new RelayCommand<Save>(LoadSave);
            CreateSaveCommand = new RelayCommand(CreateSave);
            PauseSaveCommand = new RelayCommand<Save>(PauseSave);
            StopSaveCommand = new RelayCommand<Save>(StopSave);
            PlaySaveCommand = new RelayCommand<Save>(PlaySave);

            // Save types
            for (int i = 0; i < Enum.GetNames(typeof(Save.SaveType)).Length; i++)
            {
                SaveTypes.Add(Enum.GetNames(typeof(Save.SaveType))[i]);
            }

            // Client
            Client = new Client(this);
        }

        public void UpdateState(List<Save> newSaves)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Saves.ToList().ForEach(save =>
                {
                    if (!newSaves.Any(newSave => newSave.Name == save.Name))
                    {
                        save.Dispose();
                        Saves.Remove(save);
                    }
                    else
                    {
                        var newSave = newSaves.First(newSave => newSave.Name == save.Name);
                        save.UpdateState(newSave);
                    }
                });

                newSaves.ForEach(newSave =>
                {
                    if (!Saves.Any(save => save.Name == newSave.Name))
                    {
                        newSave.MainWindowViewModel = this;
                        Saves.Add(newSave);
                    }
                });
            }));
        }

        public void ShowConnection(string connectionState)
        {
            ShowDisonnected = Visibility.Collapsed;
            ShowWaiting = Visibility.Collapsed;
            ShowLoggedOut = Visibility.Collapsed;
            ShowConnected = Visibility.Collapsed;

            switch (connectionState)
            {
                case "DISCONNECTED":
                    ShowDisonnected = Visibility.Visible;
                    break;
                case "WAITING":
                    ShowWaiting = Visibility.Visible;
                    break;
                case "LOGGEDOUT":
                    ShowLoggedOut = Visibility.Visible;
                    break;
                case "CONNECTED":
                    ShowConnected = Visibility.Visible;
                    break;
                default:
                    Console.WriteLine($"Invalid connection state view update: {connectionState}");
                    break;
            }

            OnPropertyChanged(nameof(ShowDisonnected));
            OnPropertyChanged(nameof(ShowWaiting));
            OnPropertyChanged(nameof(ShowLoggedOut));
            OnPropertyChanged(nameof(ShowConnected));
        }

        public void Connect()
        {
            if (IP != "" && IPAddress.TryParse(IP, out IPAddress? ipAdress) && Port != "" && int.TryParse(Port, out int portInt) && Password != "")
            {
                Saves.Clear();
                Client.Connect(ipAdress, portInt, Password);
            }
        }

        public void CreateSave()
        {
            if (SaveName != "" && SaveSource != "" && SaveDestination != "" && MySaveType != "")
            {
                Client.CreateSave(SaveName, SaveSource, SaveDestination, MySaveType);
            }
        }

        public static void UpdateSave(Save save)
        {
            save.UpdateSave();
        }

        public static void LoadSave(Save save)
        {
            save.LoadSave();
        }

        public static void DeleteSave(Save save)
        {
            save.DeleteSave();
        }

        public static void PauseSave(Save save)
        {
            save.PauseSaveTransfer();
        }

        public static void StopSave(Save save)
        {
            save.AbortSaveTransfer();
        }

        public static void PlaySave(Save save)
        {
            save.ResumeSaveTransfer();
        }

        public void ResetCreateSaveForm()
        {
            SaveName = string.Empty;
            SaveSource = string.Empty;
            SaveDestination = string.Empty;
            MySaveType = string.Empty;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
