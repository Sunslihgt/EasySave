using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using EasySave.Models;
using EasySave.ViewModels;

namespace EasySave.Views
{
    /// <summary>
    /// Logique d'interaction pour SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateTexts();
            DataContext = new SettingsWindowViewModel(new NavigationService());
        }

        private void OnLanguageChanged()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            Title = LanguageManager.GetText("settings_window_title");
            var languageButton = this.FindName("LanguageButton") as Button;
            if (languageButton != null)
            {
                languageButton.Content = LanguageManager.GetText("language_button");
            }
            LanguageLabel.Text = LanguageManager.GetText("modify_language_button");
            LogFormatLabel.Text = LanguageManager.GetText("modify_log_format");
            BanListLabel.Text = LanguageManager.GetText("ban_list");
            AddSoftware.Content = LanguageManager.GetText("add_software");
            BackToMenu.Content = LanguageManager.GetText("to_menu");
            HeaderName.Header = LanguageManager.GetText("name_column");
            HeaderSoftware.Header = LanguageManager.GetText("header_software");
            HeaderActions.Header = LanguageManager.GetText("header_actions");
            AddSoftwareExe.Text = LanguageManager.GetText("modify_banned_software");
            AddBannedSoftware.Content = LanguageManager.GetText("add_banned_software");
            CancelBannedSoftware.Content = LanguageManager.GetText("cancel_banned_software");
        }
        private void OpenAddSoftwarePopup(object sender, RoutedEventArgs e)
        {
            AddSoftwarePopup.IsOpen = true;
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            AddSoftwarePopup.IsOpen = false;
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
