using System.Diagnostics;
using System.IO;
using static EasySave.Logger.Logger;

namespace EasySave.Models
{
    public class SaveProcess
    {
        public enum TransferType
        {
            Idle,
            Create,
            Upload,
            Download,
        }

        private CountdownEvent CountdownEvent { get; set; }
        private Save Save { get; set; }
        public TransferType Type { get; set; }
        public FileInfo FileSource { get; set; }
        public string FileDestinationPath { get; set; }
        public bool Transfering { get; set; } = false;
        public bool Finished { get; set; } = false;
        public long Size { get; set; }
        public bool Priorised { get; set; }

        public Thread? Thread { get; set; }

        public SaveProcess(CountdownEvent countdown, Save save, TransferType type, FileInfo fileSource, string fileDestinationPath, long size, bool priorised)
        {
            CountdownEvent = countdown;
            Save = save;
            Type = type;
            FileSource = fileSource;
            FileDestinationPath = fileDestinationPath;
            Size = size;
            Priorised = priorised;

            if (Type == TransferType.Idle)
            {
                throw new ArgumentException("Transfer type cannot be idle.");
            }
        }

        public void Start()
        {
            Transfering = true;
            Thread = new Thread(Transfer);
            Thread.Name = FileSource.FullName;
            if (Priorised)
            {
                Thread.Priority = ThreadPriority.Highest;
            }
            Thread.Start();
        }

        private void Transfer()
        {
            try
            {
                Transfering = true;

                // Wait for the process to be ready
                while (Save.PauseTransfer || Save.CanProcess(this) == false)
                {
                    if (!Thread.Yield()) // Lets the OS know that the current thread is willing to yield execution to another thread
                    {
                        Thread.Sleep(100); // Sleep otherwise
                    }
                }

                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                int cryptoTime = 0;
                if (Size >= Settings.Instance.MaxFileSize * 1024 * 1024) // Only one thread can process large files
                {
                    lock (Save.MainWindowViewModel.LargeFileTransferLock)
                    {
                        //Wait for the process to be ready since large files might have been waiting and not listening to pause
                        while (Save.PauseTransfer || Save.CanProcess(this) == false)
                        {
                            if (!Thread.Yield()) // Lets the OS know that the current thread is willing to yield execution to another thread
                            {
                                Thread.Sleep(100); // Sleep otherwise
                            }
                        }
                        cryptoTime = CopyFile();
                    }
                }
                else
                {
                    cryptoTime = CopyFile();
                }

                stopwatch.Stop();

                if (cryptoTime < 0) // Failed
                {
                    Transfering = false;
                    Finished = true;

                    CountdownEvent.Signal();

                    return;
                }

                Log(Save.Name, FileSource.FullName, FileDestinationPath, Size, (int)stopwatch.ElapsedMilliseconds, cryptoTime);
                Save.UpdateState(DateTime.Now, Size, FileSource.FullName, FileDestinationPath);

                ConsoleLogger.Log($"{Save.Progress}% - File transfered: {FileSource.FullName} -> {FileDestinationPath} in {stopwatch.ElapsedMilliseconds} ms (encryption in {cryptoTime} ms).", ConsoleColor.Magenta);

                Transfering = false;
                Finished = true;

                CountdownEvent.Signal();
            }
            catch (ThreadInterruptedException) { }
            catch (Exception e)
            {
                ConsoleLogger.LogError(e.Message);
            }
        }

        private int CopyFile()
        {
            int cryptoTime = 0;

            try
            {
                if (Cryptography.ShouldEncrypt(FileSource.FullName))
                {
                    cryptoTime = Cryptography.Encrypt(FileSource.FullName, FileDestinationPath);
                    if (cryptoTime < 0)
                    {
                        // Error occurred, copy the file without encryption
                        cryptoTime = -1;
                        ConsoleLogger.LogError($"Couldn't encrypt file {FileSource.FullName}, file won't be transferred");
                    }
                    else if (cryptoTime == 0)
                    {
                        cryptoTime = int.Min(cryptoTime, 1); // Minimum encryption time
                    }
                }
                else
                {
                    FileSource.CopyTo(FileDestinationPath, true);
                }
            }
            catch
            {
                cryptoTime = -1;
            }

            return cryptoTime;
        }
    }
}