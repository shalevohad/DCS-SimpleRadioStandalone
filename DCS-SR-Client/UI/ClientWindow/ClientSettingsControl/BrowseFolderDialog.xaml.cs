using System.Collections.Generic;
using System.Windows;
using System.Windows.Forms;
using NLog; // Add this

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    public partial class BrowseFolderDialog : Window
    {
        public string SelectedPath { get; private set; }

        // Add a static logger
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        // Generic properties for reuse
        public string DialogTitle { get; set; } = "Select Directory";
        public string PromptText { get; set; } = "Select a directory:";
        public string BrowseButtonText { get; set; } = "Browse...";
        public string OkButtonText { get; set; } = "OK";
        public string CancelButtonText { get; set; } = "Cancel";
        public BrowseErrorMessages BrowseDialogErrorMessages { get; set; } = new BrowseErrorMessages();

        // Error messages for directory operations
        public class BrowseErrorMessages
        {
            public string not_exist { get; } = "The selected directory does not exist";
            public string not_found { get; } = "Directory Not Found";

            // Constructor to allow custom messages
            public BrowseErrorMessages(string xi_not_exist = null, string xi_not_found = null)
            {
                if (!string.IsNullOrEmpty(xi_not_exist))
                    not_exist = xi_not_exist;

                if (!string.IsNullOrEmpty(xi_not_found))
                    not_found = xi_not_found;
            }
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
                Logger.Info($"Directory not exist: {path}");
                try
                {
                    System.IO.Directory.CreateDirectory(path); //create if not exist
                    Logger.Info($"Directory created successfuly!");
                }
                catch (System.Exception ex)
                {
                    Logger.Error(ex, $"Failed to create the directory!");
                    System.Windows.MessageBox.Show(this,
                        BrowseDialogErrorMessages.not_exist + "\n" + ex.Message,
                        BrowseDialogErrorMessages.not_found,
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