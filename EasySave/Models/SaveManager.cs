
namespace EasySave.Models
{
    public class SaveManager
    {
        public List<Save> Saves = new();
        public StateLogger StateLogger;

        public SaveManager(StateLogger stateLogger)
        {
            this.StateLogger = stateLogger;

            //Saves = StateLogger.ReadState();

            //Save testSave = new Save(this, Save.SaveType.Differential, "save1", "C:/TEST/A", "C:/TEST/SAVE");
            //Saves.Add(testSave);

            //SaveState();
        }

        // Create a save by loading the user's directory into the save location
        // Returns true if the process goes right
        public bool CreateSave(int saveId)
        {
            throw new NotImplementedException("This feature hasn't been implemented yet");
        }

        // Get a save and load it into the user's directory
        // Returns true if the process goes right
        public bool LoadSave(int saveId)
        {
            throw new NotImplementedException("This feature hasn't been implemented yet");
        }

        public void SaveState()
        {
            StateLogger.WriteState(Saves);
        }

        public string[] GetSaveNames()
        {
            return Saves.Select(save => save.Name).ToArray();
        }

        public string[] GetSaveInfos()
        {
            return Saves.Select(save => $"{save.Name} -> {save.RealDirectoryPath} <> {save.CopyDirectoryPath} ({save.Date})" ).ToArray();
        }
    }
}
