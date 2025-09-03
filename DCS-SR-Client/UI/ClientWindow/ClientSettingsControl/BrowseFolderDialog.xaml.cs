using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    public partial class BrowseFolderDialog : Window
    {
        public string SelectedPath { get; private set; }

        // Generic properties for reuse
        public string DialogTitle { get; set; } = "Select Directory";
        public string PromptText { get; set; } = "Select a directory:";
        public string BrowseButtonText { get; set; } = "Browse...";
        public string OkButtonText { get; set; } = "OK";
        public string CancelButtonText { get; set; } = "Cancel";
        public List<string> DirectoryNotExistMessages { get; set; } = new()
        {
            "The selected directory does not exist.",
            "Directory not found."
        };

        public BrowseFolderDialog(
            string initialPath = "",
            string dialogTitle = null,
            string promptText = null,
            string browseButtonText = null,
            string okButtonText = null,
            string cancelButtonText = null)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(dialogTitle)) DialogTitle = dialogTitle;
            if (!string.IsNullOrEmpty(promptText)) PromptText = promptText;
            if (!string.IsNullOrEmpty(browseButtonText)) BrowseButtonText = browseButtonText;
            if (!string.IsNullOrEmpty(okButtonText)) OkButtonText = okButtonText;
            if (!string.IsNullOrEmpty(cancelButtonText)) CancelButtonText = cancelButtonText;

            DataContext = this;
            PathTextBox.Text = initialPath ?? string.Empty;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var initialPath = PathTextBox.Text;
                if (!string.IsNullOrWhiteSpace(initialPath) && System.IO.Directory.Exists(initialPath))
                    dialog.SelectedPath = initialPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    PathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var path = PathTextBox.Text;
            if (!System.IO.Directory.Exists(path))
            {
                try
                {
                    System.IO.Directory.CreateDirectory(path); //create if not exist
                }
                catch (System.Exception ex)
                {
                    System.Windows.MessageBox.Show(this,
                        DirectoryNotExistMessages[0] + "\n" + ex.Message,
                        DirectoryNotExistMessages[1],
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            SelectedPath = path;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}