
namespace EasySave.Models
{
    public class SaveManager
    {
        public List<SaveType> Saves = new();

        public SaveManager()
        {

        }

        // Create a save by loading the user's directory into the save location
        // Returns true if the process goes right
        public bool createSave(int saveId)
        {
            throw new NotImplementedException("This feature hasn't been implemented yet");
        }

        // Get a save and load it into the user's directory
        // Returns true if the process goes right
        public bool loadSave(int saveId)
        {
            throw new NotImplementedException("This feature hasn't been implemented yet");
        }
    }
}
