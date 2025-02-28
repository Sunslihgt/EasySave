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
        public string RealDirectoryPathDisplayed
        {
            get
            {
                return RealDirectoryPath.Length > 50 ? ShortenPath(RealDirectoryPath) : RealDirectoryPath;
            }
        }
        public string CopyDirectoryPathDisplayed
        {
            get
            {
                return CopyDirectoryPath.Length > 50 ? ShortenPath(CopyDirectoryPath) : CopyDirectoryPath;
            }
        }
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

        private static string ShortenPath(string path)
        {
            if (path.Length < 50)
            {
                return path;
            }

            string slashChar = "";
            if (path.Contains("\\"))
            {
                slashChar = "\\";
            }
            else if (path.Contains("/"))
            {
                slashChar = "/";
            }

            if (slashChar == "")
            {
                return path;
            }

            int slashes = path.Length - path.Replace(slashChar, "").Length;
            //has 2 slashes and no double (C:/.../...) or more than 2 slashes (C://.../...)
            if ((slashes == 2 && !path.Contains($"{slashChar}{slashChar}")) || slashes > 2)
            {
                string pathStart = path.Split(slashChar)[0];
                string pathEnd = path.Split(slashChar)[^1];
                return $"{pathStart}\\...\\{pathEnd}";
            }
            return path;
        }

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}