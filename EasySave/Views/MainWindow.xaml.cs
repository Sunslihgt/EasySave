using System.Windows;
using System.Windows.Controls;
using EasySave.Models;

namespace EasySave.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LanguageManager.LanguageChanged += OnLanguageChanged;
            UpdateTexts();
        }

        private void OnLanguageChanged()
        {
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            Title = LanguageManager.GetText("main_window_title");

            LanguageButton.Content = LanguageManager.GetText("language_button");
            SettingsButton.Content = LanguageManager.GetText("settings_button");

            NameSave.Text = LanguageManager.GetText("modify_name_save");
            SourcePath.Text = LanguageManager.GetText("modify_source_path");
            DestPath.Text = LanguageManager.GetText("modify_dest_path");
            TypeSave.Text = LanguageManager.GetText("modify_type_save");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}
