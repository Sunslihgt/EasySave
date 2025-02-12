using EasySave.ViewModels;
using EasySave.Views;
using System.Configuration;
using System.Data;
using System.Windows;

namespace EasySave
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var navigationService = new NavigationService();
            var mainViewModel = new MainWindowViewModel(navigationService);

            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }

}
