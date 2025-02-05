
namespace EasySave.Models
{
    public class SaveManager
    {
        public List<Save> Saves = new();

        public SaveManager()
        {
            Save testSave = new Save(Save.SaveType.Differential, "C:/TEST/A", "C:/TEST/SAVE");
            Saves.Add(testSave);
            testSave.CreateSave();
            //testSave.LoadSave();
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
    }
}
