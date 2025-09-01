using System.Windows;
using System.Windows.Forms;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow.ClientSettingsControl
{
    public partial class BrowseFolderDialog : Window
    {
        public string SelectedPath { get; private set; }

        public BrowseFolderDialog(string initialPath = "")
        {
            InitializeComponent();
            PathTextBox.Text = initialPath ?? string.Empty;
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                var initialPath = PathTextBox.Text;
                if (!string.IsNullOrWhiteSpace(initialPath) && System.IO.Directory.Exists(initialPath))
                    dialog.SelectedPath = initialPath;
                // else: do not set SelectedPath, dialog will open at default location

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    PathTextBox.Text = dialog.SelectedPath;
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var path = PathTextBox.Text;
            if (!System.IO.Directory.Exists(path))
            {
                System.Windows.MessageBox.Show(this,
                    "The selected directory does not exist.",
                    "Directory Not Found",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
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