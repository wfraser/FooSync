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
using System.Windows.Navigation;
using FooSync;

namespace FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            EnableControls(false);
            this.Show();
            ShowStartWindow();
        }

        public void ShowStartWindow()
        {
            start = new StartWindow();
            start.Left = this.Left + (this.Width / 2) - (start.Width / 2);
            start.Top = this.Top + (this.Height / 2) - (start.Height / 2);
            start.Topmost = true;
            start.WindowStyle = System.Windows.WindowStyle.None;
            start.NewButton.Click += new RoutedEventHandler(NewRepository);
            start.OpenButton.Click += new RoutedEventHandler(OpenRepository);
            start.Show();
        }

        private void EnableControls(DependencyObject parent, bool enabled)
        {
            foreach (var obj in LogicalTreeHelper.GetChildren(parent))
            {
                if (obj is Control)
                {
                    (obj as Control).IsEnabled = enabled ;
                }
                else if (obj is DependencyObject)
                {
                    EnableControls(obj as DependencyObject, enabled);
                }
            }
        }

        private void EnableControls(bool enabled)
        {
            EnableControls(this as DependencyObject, enabled);
        }

        private void OpenRepository(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "FooSync Repository Config|" + FooSyncEngine.ConfigFileName;
            dlg.FilterIndex = 1;
            dlg.Multiselect = false;

            bool? result = dlg.ShowDialog();
            var filename = dlg.FileName;

            if (!result.HasValue || result.Value == false)
            {
                ShowStartWindow();
            }
            else
            {
                EnableControls(true);
                MessageBox.Show(filename);
            }
        }

        private void NewRepository(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = FooSyncEngine.ConfigFileName;
            dlg.Filter = "FooSync Repository Config|" + FooSyncEngine.ConfigFileName;
            dlg.FilterIndex = 1;

            bool? result = dlg.ShowDialog();
            var filename = dlg.FileName;

            if (!result.HasValue || result.Value == false)
            {
                ShowStartWindow();
            }
            else
            {
                //
                // Discard whatever filename they chose;
                // Just use the path and append the standard config filename.
                //

                filename = 
                    Path.GetFullPath(
                        Path.Combine(
                            Path.GetDirectoryName(filename),
                            FooSyncEngine.ConfigFileName));

                MessageBox.Show(
                    string.Format(
                        "[TODO: New Repository dialog]\nPath={0}",
                        filename),
                    "TODO",
                    MessageBoxButton.OK, MessageBoxImage.Asterisk);

                EnableControls(true);
            }
        }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("[TODO: About Window]", "TODO", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (start != null)
            {
                start.Close();
            }
        }

        private StartWindow start = null;
    }
}
