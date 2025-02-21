using EasySave.ViewModels;
using System.IO;
using static EasySave.Models.SaveProcess;

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
        private CountdownEvent CountdownEvent = new CountdownEvent(0);
        private Thread? transferFinishedThread; // Thread to check if transfer is finished

        // Transfer state
        public DateTime? Date { get; set; }
        public bool Transfering { get; set; } = false;
        public int FilesRemaining { get; set; } = 0;
        public long SizeRemaining { get; set; } = 0;
        public long TotalSize { get; set; } = 100;
        public string CurrentSource { get; set; } = "";
        public string CurrentDestination { get; set; } = "";
        public int Progress { get; set; } = 100;
        public SaveProcess.TransferType TransferType = SaveProcess.TransferType.Idle;

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
                transferFinishedThread?.Interrupt(); // Interrupt transfer finished thread
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
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress.");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected. Cannot use save.");
                return;
            }

            Copy(RealDirectoryPath, CopyDirectoryPath, SaveProcess.TransferType.Create);
        }

        public void UpdateSave()
        {
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected. Cannot use save.");
                return;
            }

            Copy(RealDirectoryPath, CopyDirectoryPath, SaveProcess.TransferType.Upload);
        }

        public void LoadSave()
        {
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                Console.Error.WriteLine("Save already in progress");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                Console.WriteLine("Banned software detected, cannot use save");
                return;
            }

            Copy(CopyDirectoryPath, RealDirectoryPath, SaveProcess.TransferType.Download);
        }

        private void Copy(string source, string destination, SaveProcess.TransferType transferType, bool isRootDirectory = true)
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
                TransferType = transferType;
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
                Copy(subDir.FullName, newDestinationDir, transferType, false);
            }

            // Copy files
            foreach (FileInfo file in sourceInfo.GetFiles())
            {
                string destFilePath = Path.Combine(destination, file.Name);
                bool copyFile = true;
                if (transferType == SaveProcess.TransferType.Download && File.Exists(destFilePath)) // Loading save and file exists
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
                    bool priorised = Settings.Instance.PriorisedExtensions.Any((extension) => file.Name.EndsWith(extension));
                    SaveProcess saveProcess = new SaveProcess(CountdownEvent, this, transferType, file, destFilePath, (int) file.Length, priorised);
                    saveProcesses.Add(saveProcess);
                }
            }

            // Start transfer processes
            if (isRootDirectory)
            {
                saveProcesses.ForEach(saveProcess => saveProcess.Start());

                // Wait for all transfers to finish in a separate Thread
                TransferFinishedTask();
            }
        }

        // Start a Thread and wait for the transfers to finish
        public void TransferFinishedTask()
        {
            transferFinishedThread = new Thread(() =>
            {
                // Wait for all transfers to finish
                CountdownEvent.Wait();

                // Print
                Console.ForegroundColor = ConsoleColor.Green;
                switch (TransferType)
                {
                    case SaveProcess.TransferType.Create:
                        Console.WriteLine($"Successfully created save {Name}.");
                        break;
                    case SaveProcess.TransferType.Upload:
                        Console.WriteLine($"Successfully updated save {Name}.");
                        break;
                    case SaveProcess.TransferType.Download:
                        Console.WriteLine($"Successfully downloaded save {Name}.");
                        break;
                }
                Console.ResetColor();

                // Update state
                saveProcesses.ForEach((saveProcess) => saveProcess.Thread?.Interrupt());
                saveProcesses.Clear();
                UpdateStateFinished(DateTime.Now);

            });

            transferFinishedThread.Start();
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
            MainWindowViewModel.StateLogger.WriteState(MainWindowViewModel.Saves.ToList());
            updateStateMutex.ReleaseMutex();
        }

        public void UpdateStateFinished(DateTime date)
        {
            updateStateMutex.WaitOne();
            Date = date;
            Progress = 100;
            Transfering = false;
            TransferType = TransferType.Idle;
            FilesRemaining = 0;
            SizeRemaining = 0;
            CurrentSource = "";
            CurrentDestination = "";
            PauseTransfer = false;
            MainWindowViewModel.StateLogger.WriteState(MainWindowViewModel.Saves.ToList());
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

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Paused save transfer");
            Console.ResetColor();
        }

        public void ResumeSaveTransfer()
        {
            if (!Transfering || saveProcesses.Count == 0)
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

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Resumed save transfer");
            Console.ResetColor();
        }

        public void AbortSaveTransfer()
        {
            saveProcesses.ForEach(saveProcess => saveProcess.Thread?.Interrupt());
            saveProcesses.Clear();
            transferFinishedThread?.Interrupt();
            TransferType = TransferType.Idle;

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Aborted save transfer");
            Console.ResetColor();

            UpdateStateFinished(DateTime.Now);
        }

        public bool IsRealDirectoryPathValid()
        {
            return Directory.Exists(RealDirectoryPath);
        }

        public bool CanProcess(SaveProcess saveProcess)
        {
            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                return false; // Banned software detected
            }

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
