using System.Windows;
using EasySave.Monitoring.Views;
using EasySave.Monitoring.ViewModels;

namespace EasySave.Monitoring
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var mainViewModel = new MainWindowViewModel();

            var mainWindow = new MainWindow { DataContext = mainViewModel };
            mainWindow.Show();
        }
    }

}
