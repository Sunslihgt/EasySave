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
            BackToMenu.Content = LanguageManager.GetText("to_menu");

            // Language :
            LanguageLabel.Text = LanguageManager.GetText("modify_language_button");
            LanguageButton.Content = LanguageManager.GetText("language_button");

            // Format
            LogFormatLabel.Text = LanguageManager.GetText("modify_log_format");

            // MaxSize
            SizeLabel.Text = LanguageManager.GetText("modify_max_size");

            // Ban Software :
            BanListLabel.Text = LanguageManager.GetText("ban_list");
            AddSoftwareExe.Text = LanguageManager.GetText("modify_banned_software");
            AddBannedSoftware.Content = LanguageManager.GetText("add");
            CancelBannedSoftware.Content = LanguageManager.GetText("cancel");
            AddSoftware.Content = LanguageManager.GetText("add_software");
            HeaderName.Header = LanguageManager.GetText("name_column");
            HeaderSoftware.Header = LanguageManager.GetText("header_software");
            HeaderActions.Header = LanguageManager.GetText("header_actions");

            // Ban Extension :
            ExtensionList.Text = LanguageManager.GetText("extension_list");
            HeaderExtension.Header = LanguageManager.GetText("header_extension");
            HeaderActions2.Header = LanguageManager.GetText("header_actions");
            AddExtension.Content = LanguageManager.GetText("add_extension");
            AddExtensionDot.Text = LanguageManager.GetText("modify_extension");
            AddExtensionToEncrypt.Content = LanguageManager.GetText("add");
            CancelExtensionToEncrypt.Content = LanguageManager.GetText("cancel");

            // Priority Extension :
            PriorityList.Text = LanguageManager.GetText("priority_list");
            HeaderExtension2.Header = LanguageManager.GetText("header_extension");
            HeaderActions3.Header = LanguageManager.GetText("header_actions");
            AddPriority.Content = LanguageManager.GetText("add_extension");
            AddPriorityDot.Text = LanguageManager.GetText("modify_extension");
            AddExtensionPriority.Content = LanguageManager.GetText("add");
            CancelExtensionPriority.Content = LanguageManager.GetText("cancel");
        }

        private void OpenAddSoftwarePopup(object sender, RoutedEventArgs e)
        {
            AddSoftwarePopup.IsOpen = true;
        }

        private void OpenAddExtensionPopup(object sender, RoutedEventArgs e)
        {
            AddExtensionPopup.IsOpen = true;
        }

        private void OpenAddPriorityPopup(object sender, RoutedEventArgs e)
        {
            AddPriorityPopup.IsOpen = true;
        }

        private void ClosePopup(object sender, RoutedEventArgs e)
        {
            AddSoftwarePopup.IsOpen = false;
            AddExtensionPopup.IsOpen = false;
            AddPriorityPopup.IsOpen = false;
        }
    }
}