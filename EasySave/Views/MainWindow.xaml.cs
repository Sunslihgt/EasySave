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

            var languageButton = this.FindName("LanguageButton") as Button;
            if (languageButton != null)
            {
                languageButton.Content = LanguageManager.GetText("language_button");
            }
            var settingsButton = this.FindName("SettingsButton") as Button;
            if (settingsButton != null)
            {
                settingsButton.Content = LanguageManager.GetText("settings_button");
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}
