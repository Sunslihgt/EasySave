namespace EasySave.Models
{
    public class Save
    {
        public enum SaveType
        {
            Complete,
            Differential
        }

        public SaveType saveType;
        public string realDirectoryPath, copyDirectoryPath;

        public Save(SaveType saveType, string realDirectoryPath, string copyDirectoryPath)
        {
            this.saveType = saveType;
            this.realDirectoryPath = realDirectoryPath;
            this.copyDirectoryPath = copyDirectoryPath;
        }

        public void CreateSave()
        {
            Copy(realDirectoryPath, copyDirectoryPath, true);
        }

        public void LoadSave()
        {
            Copy(copyDirectoryPath, realDirectoryPath, false);
        }

        private void Copy(string source, string destination, bool createSave)
        {
            DirectoryInfo sourceInfo = new DirectoryInfo(source);
            DirectoryInfo destinationInfo = new DirectoryInfo(destination);
            
            if (!sourceInfo.Exists)
            {
                Console.WriteLine($"Source directory '{source}' does not exist."); // TODO: Replace with logger
                return;
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
                    if (saveType == SaveType.Differential) // Differential file load
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
            }

            // Copy sub directories (recursive)
            foreach (DirectoryInfo subDir in sourceInfo.GetDirectories())
            {
                string newDestinationDir = Path.Combine(destination, subDir.Name);
                Copy(subDir.FullName, newDestinationDir, createSave);
            }
        }
    }
}
