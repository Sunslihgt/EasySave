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
        private Window? _currentWindow; // Current opened window (except MainWindow)

        // Allows to open a window of type T without breaking the MVVM pattern
        public void OpenWindow<T>() where T : Window, new()
        {
            // Checks if a window is already open
            if (_currentWindow != null)
            {
                _currentWindow.Focus(); // Gives focus to the already opened window
                return;
            }

            // creates a new window
            _currentWindow = new T();
            _currentWindow.Owner = Application.Current.MainWindow; // Sets the parent window
            _currentWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Center the window

            // Opens the window in modal mode
            _currentWindow.ShowDialog();
            _currentWindow = null;

            // Opens the window without modal mode
            //_currentWindow.Closed += (sender, e) => _currentWindow = null;
        }
    }
}

