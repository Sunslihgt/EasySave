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

        private List<SaveProcess> saveProcesses = new List<SaveProcess>();

        public SaveType Type { get; set; }
        public string Name { get; set; }
        public string RealDirectoryPath { get; set; }
        public string CopyDirectoryPath { get; set; }

        private Mutex updateStateMutex = new Mutex();
        private CountdownEvent CountdownEvent;

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
                saveProcesses.ForEach(saveProcess => saveProcess.Thread?.Interrupt()); // Interrupt all threads
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
            if (saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress.");
                return;
            }

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
            if (saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress.");
                return;
            }

            CreateSave();
        }

        public void LoadSave()
        {
            if (saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress.");
                return;
            }

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
                CountdownEvent = new CountdownEvent(FilesRemaining);
                UpdateState(DateTime.Now);
            }

            // Create destination directory if necessary
            if (!destinationInfo.Exists)
            {
                destinationInfo.Create();
            }

            // Copy sub directories (recursive)
            foreach (DirectoryInfo subDir in sourceInfo.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destination, subDir.Name);
                Copy(subDir.FullName, newDestinationDir, createSave, false);
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
                    // Create TransferProcess objects
                    SaveProcess.TransferType transferType = SaveProcess.TransferType.Create;
                    bool priorised = false;
                    SaveProcess saveProcess = new SaveProcess(CountdownEvent, this, transferType, file, destFilePath, (int) file.Length, priorised);
                    saveProcesses.Add(saveProcess);
                }
            }

            if (rootSave)
            {
                // Start transfer processes
                foreach (SaveProcess saveProcess in saveProcesses)
                {
                    saveProcess.Start();
                }

                // Wait for all transfers to finish
                CountdownEvent.Wait();

                // Tranfer finished
                saveProcesses.Clear();
                Transfering = false;
                FilesRemaining = 0;
                SizeRemaining = 0;
                CurrentSource = "";
                CurrentDestination = "";
                UpdateState(DateTime.Now);
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

        public void UpdateState(DateTime date)
        {
            updateStateMutex.WaitOne();
            Date = date;
            MainWindowViewModel.StateLogger.WriteState(MainWindowViewModel.Saves.ToList());
            updateStateMutex.ReleaseMutex();
        }

        public void UpdateState(DateTime date, int size, string fileSourcePath, string fileDestinationPath)
        {
            updateStateMutex.WaitOne();
            Date = date;
            FilesRemaining--;
            SizeRemaining = int.Max(SizeRemaining - size, 0); // Prevents negative size remaining if close to 0
            CurrentSource = fileSourcePath;
            CurrentDestination = fileDestinationPath;
            updateStateMutex.ReleaseMutex();
        }

        public bool IsRealDirectoryPathValid()
        {
            return Directory.Exists(RealDirectoryPath);
        }
    }
}
