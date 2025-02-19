using System.Diagnostics;
using System.IO;
using static EasySave.Logger.Logger;

namespace EasySave.Models
{
    public class SaveProcess
    {
        public enum TransferType
        {
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
        public int Size { get; set; }
        private bool Priorised { get; set; }

        public Thread? Thread { get; set; }

        public SaveProcess(CountdownEvent countdown, Save save, TransferType type, FileInfo fileSource, string fileDestinationPath, int size, bool priorised)
        {
            CountdownEvent = countdown;
            Save = save;
            Type = type;
            FileSource = fileSource;
            FileDestinationPath = fileDestinationPath;
            Size = size;
            Priorised = priorised;
        }

        public void Start()
        {
            Transfering = true;
            Thread = new Thread(Transfer);
            Thread.Start();
        }

        private void Transfer()
        {
            Transfering = true;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            int cryptoTime = 0;

            if (Cryptography.ShouldEncrypt(FileSource.FullName))
            {
                cryptoTime = Cryptography.Encrypt(FileSource.FullName, FileDestinationPath);
                if (cryptoTime < 0)
                {
                    // Error occurred, copy the file without encryption
                    cryptoTime = -1;
                    FileSource.CopyTo(FileDestinationPath, true);
                }
                else if (cryptoTime == 0)
                {
                    cryptoTime = 1; // Minimum encryption time
                }
            }
            else
            {
                FileSource.CopyTo(FileDestinationPath, true);
            }

            stopwatch.Stop();

            Log(Save.Name, FileSource.FullName, FileDestinationPath, Size, (int)stopwatch.ElapsedMilliseconds, cryptoTime);
            Save.UpdateState(DateTime.Now, Size, FileSource.FullName, FileDestinationPath);
            
            CountdownEvent.Signal();
        }
    }
}