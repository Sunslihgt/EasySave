using EasySave.Monitoring.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySave.Monitoring.Models
{
    public class Save : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public enum SaveType
        {
            Complete,
            Differential
        }

        // Dispose
        private bool _disposed = false;

        // Save properties
        public SaveType Type { get; set; }
        public string Name { get; set; } = "";
        public string RealDirectoryPath { get; set; } = "";
        public string CopyDirectoryPath { get; set; } = "";

        // Transfer state
        public DateTime? Date { get; set; }
        public bool Transfering { get; set; } = false;
        public int FilesRemaining { get; set; } = 0;
        public long SizeRemaining { get; set; } = 0;
        public long TotalSize { get; set; } = 0;
        public string CurrentSource { get; set; } = "";
        public string CurrentDestination { get; set; } = "";
        //public TransferType TransferType { get; set; } = TransferType.Idle;
        private double _progress = 100;
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged();
            }
        }
        public bool PauseTransfer { get; set; } = false;

        public MainWindowViewModel? MainWindowViewModel { get; set; }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Save()
        {
            Dispose();
        }

        public void UpdateState(Save save)
        {
            Date = save.Date;
            Transfering = save.Transfering;
            PauseTransfer = save.PauseTransfer;
            FilesRemaining = save.FilesRemaining;
            SizeRemaining = save.SizeRemaining;
            CurrentSource = save.CurrentSource;
            CurrentDestination = save.CurrentDestination;
            Progress = save.Progress;
        }

        public bool CreateSave()
        {
            throw new NotImplementedException();
        }

        public void UpdateSave()
        {
            if (!Transfering)
            {
                Progress = 0;
                MainWindowViewModel?.Client.SendMessage($"UPLOAD|{Name}");
            }
        }

        public void LoadSave()
        {
            if (!Transfering)
            {
                Progress = 0;
                MainWindowViewModel?.Client.SendMessage($"DOWNLOAD|{Name}");
            }
        }

        public void DeleteSave()
        {
            if (!Transfering)
            {
                MainWindowViewModel?.Client.SendMessage($"DELETE|{Name}");
            }
        }

        public void PauseSaveTransfer()
        {
            Console.WriteLine($"PauseTransfer={PauseTransfer}");
            if (Transfering && !PauseTransfer)
            {
                MainWindowViewModel?.Client.SendMessage($"PAUSE|{Name}");
            }
        }

        public void ResumeSaveTransfer()
        {
            Console.WriteLine($"PauseTransfer={PauseTransfer}");
            if (Transfering && PauseTransfer)
            {
                MainWindowViewModel?.Client.SendMessage($"RESUME|{Name}");
            }
        }

        public void AbortSaveTransfer()
        {
            if (Transfering)
            {
                Progress = 100;
                MainWindowViewModel?.Client.SendMessage($"ABORT|{Name}");
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}