﻿using EasySave.ViewModels;
using System.ComponentModel; // Ajout de la directive using
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static EasySave.Logger.Logger;
using static EasySave.Models.SaveProcess;

namespace EasySave.Models
{
    public class Save : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

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
        public TransferType TransferType { get; set; } = TransferType.Idle;
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

        public bool CreateSave(bool upload = false)
        {
            Progress = 0.0; // Réinitialiser la progression
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                ConsoleLogger.LogWarning("Save already in progress.");
                return false;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                ConsoleLogger.Log("Banned software detected. Cannot use save.");
                return false;
            }

            Copy(RealDirectoryPath, CopyDirectoryPath, SaveProcess.TransferType.Create);
            return true;
        }

        public void UpdateSave()
        {
            Progress = 0.0; // Réinitialiser la progression
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                ConsoleLogger.LogWarning("Save already in progress");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                ConsoleLogger.Log("Banned software detected. Cannot use save.");
                return;
            }

            Copy(RealDirectoryPath, CopyDirectoryPath, SaveProcess.TransferType.Upload);
        }

        public void LoadSave()
        {
            Progress = 0.0; // Réinitialiser la progression
            if (TransferType != SaveProcess.TransferType.Idle || saveProcesses.Count > 0)
            {
                ConsoleLogger.Log("Save already in progress");
                return;
            }

            if (ProcessChecker.AreProcessesRunning(Settings.Instance.BannedSoftwares))
            {
                ConsoleLogger.Log("Banned software detected, cannot use save");
                return;
            }

            Copy(CopyDirectoryPath, RealDirectoryPath, SaveProcess.TransferType.Download);
        }

        private async void Copy(string source, string destination, SaveProcess.TransferType transferType, bool isRootDirectory = true)
        {
            DirectoryInfo sourceInfo = new DirectoryInfo(source);
            DirectoryInfo destinationInfo = new DirectoryInfo(destination);

            if (!sourceInfo.Exists)
            {
                ConsoleLogger.LogWarning($"Source directory '{source}' does not exist.", true); // TODO: Replace with logger
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
                    SaveProcess saveProcess = new SaveProcess(CountdownEvent, this, transferType, file, destFilePath, (int)file.Length, priorised);
                    saveProcesses.Add(saveProcess);
                }

                // Ajouter un délai pour simuler une vitesse de progression plus lente
                await Task.Delay(100); // Délai de 100 ms entre chaque mise à jour
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
                switch (TransferType)
                {
                    case SaveProcess.TransferType.Create:
                        ConsoleLogger.Log($"Successfully created save {Name}.", ConsoleColor.Green);
                        break;
                    case SaveProcess.TransferType.Upload:
                        ConsoleLogger.Log($"Successfully updated save {Name}.", ConsoleColor.Green);
                        break;
                    case SaveProcess.TransferType.Download:
                        ConsoleLogger.Log($"Successfully downloaded save {Name}.", ConsoleColor.Green);
                        break;
                }

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
                    ConsoleLogger.Log("Banned software detected. Cannot use save.", ConsoleColor.Blue);
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
            Progress = (100 - (SizeRemaining - size) * 100 / TotalSize);
            Date = date;
            FilesRemaining--;
            SizeRemaining = Math.Max(SizeRemaining - size, 0); // Prevents negative size remaining if close to 0
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
            TransferType = SaveProcess.TransferType.Idle;
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
                ConsoleLogger.LogWarning("No transfer in progress.");
                return;
            }
            else if (PauseTransfer)
            {
                ConsoleLogger.LogWarning("Transfer is already paused.");
                return;
            }

            PauseTransfer = true;

            ConsoleLogger.Log("Paused save transfer", ConsoleColor.Blue);
        }

        public void ResumeSaveTransfer()
        {
            if (!Transfering || saveProcesses.Count == 0)
            {
                ConsoleLogger.LogWarning("No transfer in progress.");
                return;
            }
            else if (!PauseTransfer)
            {
                ConsoleLogger.LogWarning("Transfer is not paused.");
                return;
            }

            PauseTransfer = false;

            ConsoleLogger.Log("Resumed save transfer", ConsoleColor.Yellow);
        }

        public void AbortSaveTransfer()
        {
            saveProcesses.ForEach(saveProcess => saveProcess.Thread?.Interrupt());
            saveProcesses.Clear();
            transferFinishedThread?.Interrupt();
            TransferType = SaveProcess.TransferType.Idle;

            ConsoleLogger.Log("Aborted save transfer");

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

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}


