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
            // Vérifie si une fenêtre est déjà ouverte
            if (_currentWindow != null)
            {
                _currentWindow.Focus(); // Donne le focus à la fenêtre déjà ouverte
                return;
            }

            // Crée une nouvelle fenêtre
            _currentWindow = new T();
            _currentWindow.Owner = Application.Current.MainWindow; // Définit la fenêtre parent
            _currentWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner; // Centre la fenêtre

            // Ouvre la fenêtre en mode modal
            _currentWindow.ShowDialog();
            _currentWindow = null;

            // Ouvre la fenêtre sans le mode modal
            //_currentWindow.Closed += (sender, e) => _currentWindow = null;
        }
    }
}

