using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;

namespace Ciribob.DCS.SimpleRadio.Standalone.Client.UI.ClientWindow
{
    public partial class BrowseFolderControl : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty PathProperty =
            DependencyProperty.Register(nameof(Path), typeof(string), typeof(BrowseFolderControl), new PropertyMetadata(""));

        public static readonly DependencyProperty DialogTitleProperty =
            DependencyProperty.Register(nameof(DialogTitle), typeof(string), typeof(BrowseFolderControl), new PropertyMetadata("Select Directory"));

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set => SetValue(PathProperty, value);
        }

        public string DialogTitle
        {
            get => (string)GetValue(DialogTitleProperty);
            set => SetValue(DialogTitleProperty, value);
        }

        public BrowseFolderControl()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = DialogTitle;

                // Resolve initial path: if not rooted, combine with executable directory
                var initialPath = Path;
                if (!string.IsNullOrWhiteSpace(initialPath) && !System.IO.Path.IsPathRooted(initialPath))
                {
                    string exeDir = System.AppDomain.CurrentDomain.BaseDirectory;
                    initialPath = System.IO.Path.Combine(exeDir, initialPath);
                }

                if (!string.IsNullOrWhiteSpace(initialPath) && System.IO.Directory.Exists(initialPath))
                    dialog.SelectedPath = initialPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    Path = dialog.SelectedPath;
            }
        }
    }
}