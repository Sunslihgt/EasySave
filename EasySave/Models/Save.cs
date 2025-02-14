using EasySave.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;
using static EasySave.Logger.Logger;

namespace EasySave.Models
{
    public class Save : IDisposable
    {
        public enum SaveType
        {
            Complete,
            Differential
        }

        private bool _disposed = false;

        public SaveType Type { get; set; }
        public string Name { get; set; }
        public string RealDirectoryPath { get; set; }
        public string CopyDirectoryPath { get; set; }

        public DateTime? Date { get; set; }
        public bool Transfering { get; set; } = false;
        public int FilesRemaining { get; set; } = 0;
        public int SizeRemaining { get; set; } = 0;
        public string CurrentSource { get; set; } = "";
        public string CurrentDestination { get; set; } = "";

        public ICommand UpdateSaveCommand { get; }
        public ICommand LoadSaveCommand { get; }

        public MainWindowViewModel MainWindowViewModel { get; }

        public Save(MainWindowViewModel MainWindowViewModel, SaveType saveType, string name, string realDirectoryPath, string copyDirectoryPath, DateTime? date = null, bool transfering = false, int filesRemaining = 0, int sizeRemaining = 0, string currentSource = "", string currentDestination = "")
        {
            this.MainWindowViewModel = MainWindowViewModel;
            this.Type = saveType;
            this.Name = name;
            this.RealDirectoryPath = realDirectoryPath;
            this.CopyDirectoryPath = copyDirectoryPath;
            this.Date = date;
            this.Transfering = transfering;
            this.FilesRemaining = filesRemaining;
            this.SizeRemaining = sizeRemaining;
            this.CurrentSource = currentSource;
            this.CurrentDestination = currentDestination;

            UpdateSaveCommand = new RelayCommand(UpdateSave);
            LoadSaveCommand = new RelayCommand(LoadSave);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DeleteSave();
                _disposed = true;
            }
            GC.SuppressFinalize(this);
        }

        ~Save()
        {
            Dispose();
        }

        public void CreateSave()
        {
            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected. Cannot use save.");
                return;
            }
            Copy(RealDirectoryPath, CopyDirectoryPath, true);
            Console.WriteLine($"Uploaded save {Name}.");
        }

        public void UpdateSave()
        {
            CreateSave();
        }

        public void LoadSave()
        {
            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected. Cannot use save.");
                return;
            }
            Copy(CopyDirectoryPath, RealDirectoryPath, false);
            Console.WriteLine($"Downloaded save {Name}.");
        }

        private void Copy(string source, string destination, bool createSave, bool rootSave = true)
        {
            DirectoryInfo sourceInfo = new DirectoryInfo(source);
            DirectoryInfo destinationInfo = new DirectoryInfo(destination);

            if (!sourceInfo.Exists)
            {
                Console.WriteLine($"Source directory '{source}' does not exist."); // TODO: Replace with logger
                return;
            }

            if (rootSave)
            {
                Transfering = true;
                FilesRemaining = sourceInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                SizeRemaining = (int) GetDirectorySize(sourceInfo);
                UpdateState();
            }

            // Create destination directory if necessary
            if (!destinationInfo.Exists)
            {
                destinationInfo.Create();
            }

            // Copy files
            foreach (FileInfo file in sourceInfo.GetFiles())
            {
                string destFilePath = Path.Combine(destination, file.Name);
                bool copyFile = true;
                if (!createSave && File.Exists(destFilePath)) // Loading save and file exists
                {
                    if (Type == SaveType.Differential) // Differential file load
                    {
                        // Do not overwrite files that haven't changed
                        if (File.GetLastWriteTime(destFilePath) <= file.LastWriteTime)
                        {
                            copyFile = false;
                        }
                    }
                }

                if (copyFile)
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    int cryptoTime = 0;

                    if (Cryptography.ShouldEncrypt(file.FullName))
                    {
                        cryptoTime = Cryptography.Encrypt(file.FullName, destFilePath);
                        if (cryptoTime < 0)
                        {
                            // Error occurred, copy the file without encryption
                            cryptoTime = -1;
                            file.CopyTo(destFilePath, true);
                        }
                        else if (cryptoTime == 0)
                        {
                            cryptoTime = 1; // Minimum encryption time
                        }
                    }
                    else
                    {
                        file.CopyTo(destFilePath, true);
                    }

                    stopwatch.Stop();
                    Log(Name, file.FullName, destFilePath, file.Length, (int) stopwatch.ElapsedMilliseconds, cryptoTime);
                }
                
                FilesRemaining--;
                SizeRemaining -= (int) file.Length;
                CurrentSource = file.FullName;
                CurrentDestination = destFilePath;
                UpdateState();
            }

            // Copy sub directories (recursive)
            foreach (DirectoryInfo subDir in sourceInfo.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destination, subDir.Name);
                Copy(subDir.FullName, newDestinationDir, createSave, false);
            }

            if (rootSave)
            {
                Transfering = false;
                FilesRemaining = 0;
                SizeRemaining = 0;
                CurrentSource = "";
                CurrentDestination = "";
                UpdateState();
            }
        }

        public bool DeleteSave()
        {
            if (Directory.Exists(CopyDirectoryPath))
            {
                if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
                {
                    Console.WriteLine("Banned software detected. Cannot use save.");
                    return false;
                }
                Directory.Delete(CopyDirectoryPath, true);
            }
            return true;
        }

        private long GetDirectorySize(DirectoryInfo directoryInfo)
        {
            long size = 0;
            // Add file sizes
            FileInfo[] files = directoryInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                size += file.Length;
            }
            // Add subdirectory sizes
            DirectoryInfo[] dirs = directoryInfo.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                size += GetDirectorySize(dir);
            }
            return size;
        }

        public void UpdateState()
        {
            Date = DateTime.Now;
            MainWindowViewModel.StateLogger.WriteState(MainWindowViewModel.Saves.ToList());
        }

        public bool IsRealDirectoryPathValid()
        {
            return Directory.Exists(RealDirectoryPath);
        }
    }
}
