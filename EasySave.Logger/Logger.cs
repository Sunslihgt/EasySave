namespace EasySave.Logger
{
    public static class Logger
    {
        public static void Log(string backupName, string sourcePath, string destinationPath, long fileSize, long transferTime)
        {
            Console.WriteLine($"Backup: {backupName}, Source: {sourcePath}, Destination: {destinationPath}, Size: {fileSize} bytes, Time: {transferTime} ms");
        }
    }
}
