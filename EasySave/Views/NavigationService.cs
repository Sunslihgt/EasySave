using System.Windows;

namespace EasySave.Views
{
    public interface INavigationService
    {
        // Opens a window of type T
        void OpenWindow<T>() where T : Window, new();
    }


    public class NavigationService : INavigationService
    {
        // Allows to open a window of type T without breaking the MVVM pattern
        public void OpenWindow<T>() where T : Window, new()
        {
            var window = new T();
            window.Show();
        }
    }
}

