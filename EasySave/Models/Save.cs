using EasySave.ViewModels;
using System.IO;

namespace EasySave.Models
{
    public class Save : IDisposable
    {
        public enum SaveType
        {
            Complete,
            Differential
        }

        public readonly int MAX_CONCURRENT_FILE_SIZE = 4 * 1024 * 1024; // 4 MB

        // Dispose
        private bool _disposed = false;

        // Save properties
        public SaveType Type { get; set; }
        public string Name { get; set; }
        public string RealDirectoryPath { get; set; }
        public string CopyDirectoryPath { get; set; }

        // Transfer threads
        private List<SaveProcess> saveProcesses = new List<SaveProcess>();
        private Mutex updateStateMutex = new Mutex();
        public Mutex LargeFileMutex = new Mutex();
        private CountdownEvent CountdownEvent = new CountdownEvent(0);

        // Transfer state
        public DateTime? Date { get; set; }
        public bool Transfering { get; set; } = false;
        public int FilesRemaining { get; set; } = 0;
        public long SizeRemaining { get; set; } = 0;
        public long TotalSize { get; set; } = 100;
        public string CurrentSource { get; set; } = "";
        public string CurrentDestination { get; set; } = "";
        public int Progress { get; set; } = 100;

        public bool PauseTransfer { get; set; } = false;

        public MainWindowViewModel MainWindowViewModel { get; }

        public Save(MainWindowViewModel mainWindowViewModel, SaveType saveType, string name, string realDirectoryPath, string copyDirectoryPath, DateTime? date = null, bool transfering = false, int filesRemaining = 0, long sizeRemaining = 0, string currentSource = "", string currentDestination = "")
        {
            this.MainWindowViewModel = mainWindowViewModel;
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

        public void CreateSave(bool upload = false)
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

            Console.ForegroundColor = ConsoleColor.Green;
            if (upload)
            {
                Console.WriteLine($"Uploaded save {Name}.");
            }
            else
            {
                Console.WriteLine($"Created save {Name}.");
            }
            Console.ResetColor();
        }

        public void UpdateSave()
        {
            if (saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress");
                return;
            }

            CreateSave(true);
        }

        public void LoadSave()
        {
            if (saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected, cannot use save");
                return;
            }

            Copy(CopyDirectoryPath, RealDirectoryPath, false);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Downloaded save {Name}");
            Console.ResetColor();
        }

        private void Copy(string source, string destination, bool createSave, bool isRootDirectory = true)
        {
            DirectoryInfo sourceInfo = new DirectoryInfo(source);
            DirectoryInfo destinationInfo = new DirectoryInfo(destination);

            if (!sourceInfo.Exists)
            {
                Console.WriteLine($"Source directory '{source}' does not exist."); // TODO: Replace with logger
                return;
            }

            // Initialize transfer state parameters
            if (isRootDirectory)
            {
                Transfering = true;
                PauseTransfer = false;
                FilesRemaining = sourceInfo.GetFiles("*", SearchOption.AllDirectories).Length;
                SizeRemaining = TotalSize = GetDirectorySize(sourceInfo);
                CountdownEvent.Reset(FilesRemaining);
                Progress = 0;
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
                    bool priorised = Settings.Instance.PriorisedExtensions.Any((extension) => file.Name.EndsWith(extension));
                    SaveProcess saveProcess = new SaveProcess(CountdownEvent, this, transferType, file, destFilePath, (int) file.Length, priorised);
                    saveProcesses.Add(saveProcess);
                }
            }

            // Start transfer processes
            if (isRootDirectory)
            {
                saveProcesses.ForEach(saveProcess => saveProcess.Start());

                // Wait for all transfers to finish
                CountdownEvent.Wait(); // TODO: Do not wait in main thread

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

        private void UpdateState(DateTime date)
        {
            updateStateMutex.WaitOne();
            Date = date;
            MainWindowViewModel.StateLogger.WriteState(MainWindowViewModel.Saves.ToList());
            updateStateMutex.ReleaseMutex();
        }

        public void UpdateState(DateTime date, long size, string fileSourcePath, string fileDestinationPath)
        {
            updateStateMutex.WaitOne();
            Progress = (int) (100 - (SizeRemaining - size) * 100 / TotalSize);
            Date = date;
            FilesRemaining--;
            SizeRemaining = long.Max(SizeRemaining - size, 0); // Prevents negative size remaining if close to 0
            CurrentSource = fileSourcePath;
            CurrentDestination = fileDestinationPath;
            updateStateMutex.ReleaseMutex();
        }

        public void PauseSaveTransfer()
        {
            if (!Transfering)
            {
                Console.Error.WriteLine("No transfer in progress.");
                return;
            }
            else if (PauseTransfer)
            {
                Console.Error.WriteLine("Transfer is already paused.");
                return;
            }

            PauseTransfer = true;
        }

        public void ResumeSaveTransfer()
        {
            if (!Transfering)
            {
                Console.Error.WriteLine("No transfer in progress.");
                return;
            }
            else if (!PauseTransfer)
            {
                Console.Error.WriteLine("Transfer is not paused.");
                return;
            }

            PauseTransfer = false;
        }

        public void AbortSaveTransfer()
        {
            saveProcesses.ForEach(saveProcess => saveProcess.Thread?.Interrupt());
        }

        public bool IsRealDirectoryPathValid()
        {
            return Directory.Exists(RealDirectoryPath);
        }

        public bool CanProcess(SaveProcess saveProcess)
        {
            // Check if save process is priorised or if there are no other priorised processes in all saves
            if (saveProcess.Priorised || !MainWindowViewModel.Saves.Any((save) => save.HasPriorisedProcessesRemaining()))
            {
                return true;
            }
            return false;
        }

        public bool HasPriorisedProcessesRemaining()
        {
            return saveProcesses.Any((process) => process.Priorised && !process.Finished);
        }
    }
}
