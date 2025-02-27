using System.Windows;
using System.Windows.Controls;
using EasySave.Models;

namespace EasySave.Views
{
    public partial class LanguageWindow : Window
    {
        public LanguageWindow()
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
            Title = LanguageManager.GetText("language_window_title");
            SubmitButton.Content = LanguageManager.GetText("modify_language_button");
        }

        private void SubmitButton_Click(object sender, RoutedEventArgs e)
        {
            if (LanguageComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedLanguage = selectedItem.Tag.ToString() ?? "";
                MessageBox.Show($"Current Language : {selectedLanguage}");

                switch (selectedLanguage)
                {
                    case "FR":
                        LanguageManager.SetLanguage(LanguageManager.Language.FR);
                        break;
                    case "EN":
                        LanguageManager.SetLanguage(LanguageManager.Language.EN);
                        break;
                    case "ES":
                        LanguageManager.SetLanguage(LanguageManager.Language.ES);
                        break;
                    default:
                        LanguageManager.SetLanguage(LanguageManager.Language.EN);
                        break;
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}
