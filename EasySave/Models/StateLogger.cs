using System.Text.Json;

namespace EasySave.Models
{
    public class StateLogger
    {
        private const string STATE_FILE_PATH = "C:/Users/samsa/Documents/Programmation/C#/EasySave/EasySave/State/state.json";
        private FileInfo stateFile;

        public StateLogger()
        {
            stateFile = new FileInfo(STATE_FILE_PATH);
            if (!stateFile.Exists)
            {
                using (var stream = stateFile.Create())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write("{}");
                }
            }

            stateFile.OpenRead();
        }

        public struct saveState
        {
            public DateTime date;
            public string sourcePath;
            public string destinationPath;
        }


        public object[] ReadState()
        {
            using (var stream = stateFile.OpenRead())
            using (var reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                if (string.IsNullOrEmpty(json))
                {
                    return Array.Empty<object>();
                }

                using (var jsonDocument = JsonDocument.Parse(json))
                {
                    var root = jsonDocument.RootElement;
                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        var states = new List<saveState>();
                        foreach (var element in root.EnumerateArray())
                        {
                            var state = new saveState
                            {
                                date = element.GetProperty("date").GetDateTime(),
                                sourcePath = element.GetProperty("sourcePath").GetString(),
                                destinationPath = element.GetProperty("destinationPath").GetString()
                            };
                            states.Add(state);
                        }
                        return states.Cast<object>().ToArray();
                    }
                }
            }
            return Array.Empty<object>();
        }


    public void WriteState(object myObject)
        {
            // Implementation for writing state
        }
    }
}
