
namespace EasySave.Models
{
    public class SaveManager
    {
        public List<Save> Saves = new();
        public StateLogger StateLogger;

        public SaveManager()
        {
            StateLogger = new StateLogger(this);

            //Saves = StateLogger.ReadState();

            //Save testSave = new Save(this, Save.SaveType.Differential, "save1", "C:/TEST/A", "C:/TEST/SAVE");
            //Saves.Add(testSave);

            //SaveState();
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
