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

            var dataGrid = this.FindName("DataGrid") as DataGrid;
            if (dataGrid != null)
            {
                dataGrid.Columns[0].Header = LanguageManager.GetText("name_column");
                dataGrid.Columns[1].Header = LanguageManager.GetText("source_column");
                dataGrid.Columns[2].Header = LanguageManager.GetText("destination_column");
                dataGrid.Columns[3].Header = LanguageManager.GetText("type_column");
                dataGrid.Columns[4].Header = LanguageManager.GetText("transfering_column");
                
                //dataGrid.Columns[5].Header = LanguageManager.GetText("load_column");
                //dataGrid.Columns[6].Header = LanguageManager.GetText("update_column");
                //dataGrid.Columns[7].Header = LanguageManager.GetText("delete_column");
            }

            var languageButton = this.FindName("LanguageButton") as Button;
            if (languageButton != null)
            {
                languageButton.Content = LanguageManager.GetText("language_button");
            }
            var SettingsButton = this.FindName("SettingsButton") as Button;
            if (SettingsButton != null)
            {
                SettingsButton.Content = LanguageManager.GetText("settings_button");
            }
            var AddSaveButton = this.FindName("AddSaveButton") as Button;
            if (AddSaveButton != null)
            {
                AddSaveButton.Content = LanguageManager.GetText("create_save");
            }

        }

        private void Window_Closed(object sender, EventArgs e)
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }
    }
}
