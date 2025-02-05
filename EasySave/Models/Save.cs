using System.Data;

namespace EasySave.Models
{
    public class Save
    {
        public enum SaveType
        {
            Complete,
            Differential
        }


        public SaveType Type;
        public string Name { get; private set; }
        public string RealDirectoryPath, CopyDirectoryPath;

        public DateTime? Date;
        public bool Transfering = false;
        public int FilesRemaining = 0;
        public int SizeRemaining = 0;
        public string CurrentSource = "";
        public string CurrentDestination = "";

        public SaveManager SaveManager;


        public Save(SaveManager saveManager, SaveType saveType, string name, string realDirectoryPath, string copyDirectoryPath, DateTime? date = null, bool transfering = false, int filesRemaining = 0, int sizeRemaining = 0, string currentSource = "", string currentDestination = "")
        {
            this.SaveManager = saveManager;
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

        public void CreateSave()
        {
            Copy(RealDirectoryPath, CopyDirectoryPath, true);
        }

        public void UpdateSave()
        {
            CreateSave();
        }

        public void LoadSave()
        {
            Copy(CopyDirectoryPath, RealDirectoryPath, false);
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
                    file.CopyTo(destFilePath, true);
                    

                    Console.WriteLine($"{DateTime.Now}: {file.FullName} -> {destFilePath}"); // TODO: Replace with logger
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
            SaveManager.SaveState();
        }

        public bool IsRealDirectoryPathValid()
        {
            return Directory.Exists(RealDirectoryPath);
        }
    }
}
