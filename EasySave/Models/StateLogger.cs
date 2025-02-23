using EasySave.ViewModels;
using System.IO;
using System.Text.Json;

namespace EasySave.Models
{
    public class StateLogger
    {
        private string stateFilePath = Settings.Instance.StateFilePath;
        public string StateFilePath
        {
            get => stateFilePath;
            set
            {
                stateFilePath = value;
                stateFile = new FileInfo(stateFilePath);
            }
        }

        private FileInfo stateFile;
        private object writeLock = new object();

        private MainWindowViewModel mainWindowViewModel;

        public StateLogger(MainWindowViewModel mainWindowViewModel)
        {
            this.mainWindowViewModel = mainWindowViewModel;
            stateFile = new FileInfo(StateFilePath);

            // Create default empty file if necessary
            if (!stateFile.Exists || stateFile.Length == 0)
            {
                using (var stream = stateFile.Create())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("[]");
                }
            }
        }

        public List<Save> ReadState()
        {
            using (var stream = stateFile.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                if (string.IsNullOrEmpty(json))
                {
                    return new List<Save>();
                }

                using (var jsonDocument = JsonDocument.Parse(json))
                {
                    var root = jsonDocument.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var states = new List<Save>();
                        foreach (var element in root.EnumerateArray())
                        {
                            try
                            {
                                Save.SaveType saveType = element.GetProperty("type").GetString() == "Complete" ? Save.SaveType.Complete : Save.SaveType.Differential;
                                string? saveName = element.GetProperty("name").GetString();
                                string? sourcePath = element.GetProperty("realDirectoryPath").GetString();
                                string? destinationPath = element.GetProperty("copyDirectoryPath").GetString();

                                if (saveName != null && !string.IsNullOrEmpty(sourcePath) && !string.IsNullOrEmpty(destinationPath))
                                {
                                    Save save = new(
                                        mainWindowViewModel,
                                        saveType,
                                        saveName,
                                        sourcePath,
                                        destinationPath
                                    );

                                    if (element.TryGetProperty("date", out JsonElement dateElement) && dateElement.TryGetDateTime(out DateTime saveDate) && saveDate > DateTime.UnixEpoch)
                                    {
                                        save.Date = saveDate;
                                    }

                                    //bool transfering = false;

                                    if (element.TryGetProperty("transfering", out JsonElement transferingElement) && transferingElement.TryGetInt32(out int transfering))
                                    {
                                        if (transfering == 1)
                                        {
                                            save.Transfering = true;
                                            save.FilesRemaining = element.GetProperty("filesRemaining").GetInt32();
                                            save.SizeRemaining = element.GetProperty("sizeRemaining").GetInt64();
                                            save.CurrentSource = element.GetProperty("currentSource").GetString() ?? "";
                                            save.CurrentDestination = element.GetProperty("currentDestination").GetString() ?? "";
                                        }
                                        else
                                        {
                                            save.Transfering = false;
                                        }
                                    }

                                    states.Add(save);
                                }
                            }
                            catch (Exception e)
                            {
                                ConsoleLogger.LogError(e.Message);
                            }
                        }
                        return states;
                    }
                }
                return new List<Save>();
            }
        }

        private List<object> GetStateObjects(List<Save> saves)
        {
            return saves.Select<Save, object>(save =>
            {
                if (save.Transfering)
                {
                    return new
                    {
                        name = save.Name,
                        type = save.Type.ToString(),
                        realDirectoryPath = save.RealDirectoryPath,
                        copyDirectoryPath = save.CopyDirectoryPath,
                        date = save.Date ?? DateTime.UnixEpoch,
                        transfering = 1,
                        filesRemaining = save.FilesRemaining,
                        sizeRemaining = save.SizeRemaining,
                        currentSource = save.CurrentSource,
                        currentDestination = save.CurrentDestination,
                        progress = save.Progress,
                        pauseTransfer = save.PauseTransfer ? 1 : 0
                    };
                }
                else
                {
                    return new
                    {
                        name = save.Name,
                        type = save.Type.ToString(),
                        realDirectoryPath = save.RealDirectoryPath,
                        copyDirectoryPath = save.CopyDirectoryPath,
                        date = save.Date ?? DateTime.UnixEpoch,
                        transfering = 0,
                    };
                }
            }).ToList();
        }

        public string GetStateString(List<Save> saves, bool indent = false)
        {
            if (indent)
            {
                return JsonSerializer.Serialize(GetStateObjects(saves), new JsonSerializerOptions { WriteIndented = true });
            }
            else
            {
                return JsonSerializer.Serialize(GetStateObjects(saves));
            }
        }

        public void WriteState(List<Save> saves, bool sendServerUpdate = true)
        {
            var savesToStore = GetStateObjects(saves);

            if (sendServerUpdate)
            {
                mainWindowViewModel.Server.SendState(GetStateString(saves, false));
            }

            lock (writeLock)
            {
                using (var stream = stateFile.OpenWrite())
                {
                    stream.SetLength(0); // Clear the file before writing
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(GetStateString(saves, true));
                    }
                }
            }
        }
    }
}
