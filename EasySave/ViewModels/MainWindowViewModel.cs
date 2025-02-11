using System.Collections.ObjectModel;
using EasySave.Models;

namespace EasySave.ViewModels
{
    public class MainWindowViewModel
    {
        SaveManager SaveManager { get; } = new SaveManager();

        public ObservableCollection<Save> Saves
        {
            get => new ObservableCollection<Save>(SaveManager.Saves);
        }

        public MainWindowViewModel()
        {
            SaveManager.Saves = SaveManager.StateLogger.ReadState();

            Console.WriteLine(SaveManager.GetSaveInfos());
            Console.WriteLine(SaveManager.Saves.Count());
            SaveManager.Saves.ForEach(save =>
            {
                Console.WriteLine($"{save.Name} {save.RealDirectoryPath} {save.CopyDirectoryPath} {save.Transfering}");
            });
        }
    }
}
