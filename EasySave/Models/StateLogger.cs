using System.IO;
using System.Text.Json;

namespace EasySave.Models
{
    public class StateLogger
    {
        // Path to EasySave/State/state.json
        private string STATE_FILE_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Sources", "state.json").ToString();
        private FileInfo stateFile;

        private SaveManager saveManager;

        public StateLogger(SaveManager saveManager)
        {
            this.saveManager = saveManager;
            stateFile = new FileInfo(STATE_FILE_PATH);

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
                                        saveManager,
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
                                            save.Transfering = false;
                                            save.FilesRemaining = element.GetProperty("filesRemaining").GetInt32();
                                            save.SizeRemaining = element.GetProperty("filesRemaining").GetInt32();
                                            save.CurrentSource = element.GetProperty("filesRemaining").GetString() ?? "";
                                            save.CurrentDestination = element.GetProperty("filesRemaining").GetString() ?? "";
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
                                Console.WriteLine(e.Message); // TODO: replace with logger
                            }
                        }
                        return states;
                    }
                }
                return new List<Save>();
            }
        }

        public void WriteState(List<Save> saves)
        {
            var savesToStore = saves.Select<Save, object>(save =>
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
                        currentDestination = save.CurrentDestination
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

            using (var stream = stateFile.OpenWrite())
            {
                stream.SetLength(0); // Clear the file before writing
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(JsonSerializer.Serialize(savesToStore, new JsonSerializerOptions { WriteIndented = true }));
                }
            }
        }
    }
}
