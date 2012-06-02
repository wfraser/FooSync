using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ookii.Dialogs.Wpf;

namespace Codewise.FooSync.WPFApp2
{
    /// <summary>
    /// Interaction logic for LocalPairEntryWindow.xaml
    /// </summary>
    public partial class LocalPairEntryWindow : Window
    {
        public LocalPairEntryWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles a click on a Browse button.
        /// The Button that sends this should have a Tag attribute populated with a reference to
        /// the TextBox which is to receive the result of the browsing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FolderBrowse(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                var target = ((FrameworkElement)sender).Tag as TextBox;

                if (target ==  null)
                    throw new InvalidCastException("FolderBrowse sender needs to have a tag set to a TextBox instance.");

                var folder = ShowFolderBrowser("All Files|*.*");
                if (folder != null)
                {
                    target.Text = folder;
                }
            }
        }

        /// <summary>
        /// Presents the user with a folder browser dialog.
        /// </summary>
        /// <param name="filter">Filename filter for if the OS doesn't support a folder browser.</param>
        /// <returns>Full path the user selected.</returns>
        private string ShowFolderBrowser(string filter)
        {
            bool cancelled = false;
            string path = null;

            if (VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                var dlg = new VistaFolderBrowserDialog();

                cancelled = !(dlg.ShowDialog() ?? false);

                if (!cancelled)
                {
                    path = Path.GetFullPath(dlg.SelectedPath);
                }
            }
            else
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.Filter = filter;
                dlg.FilterIndex = 1;

                cancelled = !(dlg.ShowDialog() ?? false);

                if (!cancelled)
                {
                    path = Path.GetFullPath(Path.GetDirectoryName(dlg.FileName)); // Discard whatever filename they chose
                }
            }

            return path;
        }

        private void Submit(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(RepositoryPath.Text))
            {
                MessageBox.Show("Repository path is invalid.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (!Directory.Exists(SourcePath.Text))
            {
                MessageBox.Show("Source path is invalid.", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
        }

        private void Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
