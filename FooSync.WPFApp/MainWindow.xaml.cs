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
using Ookii.Dialogs.Wpf;

namespace FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            _foo = new FooSyncEngine();

            InitializeComponent();
            EnableControls(false);
            this.Show();
            ShowStartWindow();
        }

        public void ShowStartWindow()
        {
            _start = new StartWindow();
            _start.Left = this.Left + (this.Width / 2) - (_start.Width / 2);
            _start.Top = this.Top + (this.Height / 2) - (_start.Height / 2);
            _start.Topmost = true;
            _start.WindowStyle = System.Windows.WindowStyle.None;
            _start.NewButton.Click += new RoutedEventHandler(NewRepository);
            _start.OpenButton.Click += new RoutedEventHandler(OpenRepository);
            _start.Show();
        }

        private void EnableControls(DependencyObject parent, bool enabled)
        {
            foreach (var obj in LogicalTreeHelper.GetChildren(parent))
            {
                if (obj is Control)
                {
                    if (obj != InspectButton && obj != DoActionsButton)
                    {
                        (obj as Control).IsEnabled = enabled;
                    }
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
            string filename = null;
            bool cancelled = false;

            var dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.Filter = "FooSync Repository Config|" + FooSyncEngine.ConfigFileName;
            dlg.FilterIndex = 1;
            dlg.Multiselect = false;

            cancelled = !(dlg.ShowDialog() ?? false);
            filename = dlg.FileName;

            if (cancelled)
            {
                ShowStartWindow();
            }
            else
            {
                string errStr = string.Empty;
                try
                {
                    _config = RepositoryConfigLoader.GetRepositoryConfig(filename, out errStr);
                }
                catch (Exception ex)
                {
                    errStr = ex.Message;
                }
                finally
                {
                    if (errStr != string.Empty)
                    {
                        MessageBox.Show("Loading config failed: " + errStr, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                if (_config == null)
                {
                    ShowStartWindow();
                }
                else
                {
                    DirectorySelector.ItemsSource = _config.Directories;
                }

                EnableControls(true);
            }
        }

        private void NewRepository(object sender, RoutedEventArgs e)
        {
            string filename = null;
            bool cancelled = false;

            if (VistaFolderBrowserDialog.IsVistaFolderDialogSupported)
            {
                var dlg = new VistaFolderBrowserDialog();
                dlg.Description = "Select the location for the new repository:";

                cancelled = !(dlg.ShowDialog() ?? false);

                filename =
                    Path.GetFullPath(
                        Path.Combine(
                            dlg.SelectedPath,
                            FooSyncEngine.ConfigFileName));
            }
            else
            {
                var dlg = new Microsoft.Win32.SaveFileDialog();
                dlg.FileName = FooSyncEngine.ConfigFileName;
                dlg.Filter = "FooSync Repository Config|" + FooSyncEngine.ConfigFileName;
                dlg.FilterIndex = 1;

                cancelled = !(dlg.ShowDialog() ?? false);

                filename =
                    Path.GetFullPath(
                        Path.Combine(
                            Path.GetDirectoryName(dlg.FileName), // Discard whatever filename they chose
                            FooSyncEngine.ConfigFileName));
            }

            if (cancelled)
            {
                ShowStartWindow();
            }
            else
            {
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
            if (_start != null)
            {
                _start.Close();
            }
        }

        public static NullStringConverter<DateTime> NullDateTimeConverter = new NullStringConverter<DateTime>();

        private StartWindow         _start      = null;
        private RepositoryConfig    _config     = null;
        private FooSyncEngine       _foo        = null;
        private FooTree             _repo       = null;
        private FooTree             _source     = null;
        private RepositoryState     _state      = null;
        private FooChangeSet        _changeset  = null;

        private void Inspect(object sender, RoutedEventArgs e)
        {
            RepositoryDirectory dir = DirectorySelector.SelectedItem as RepositoryDirectory;

            if (dir == null)
                return;

            //WRFDEV TODO: add progress callbacks to all these

            var exceptions = FooSyncEngine.PrepareExceptions(dir);
            _repo = _foo.Tree(Path.Combine(_config.RepositoryPath, dir.Path), exceptions);
            _source = _foo.Tree(dir.Source.Path, exceptions);
            _state = new RepositoryState(Path.Combine(_config.RepositoryPath, dir.Path, FooSyncEngine.RepoStateFileName));

            _changeset = _foo.Inspect(_repo, _source, _state);

            var newFiles = _changeset.Where(elem => !_state.Repository.MTimes.ContainsKey(elem.Filename) && !_state.Source.MTimes.ContainsKey(elem.Filename));

            NewFiles.ItemsSource = new BindableChangeSet(_changeset, newFiles, _repo, _source);
            if (newFiles.Count() > 0)
            {
                (NewFiles.Parent as Expander).IsExpanded = true;
            }

            //(NewFiles.View as GridView).Columns[0].DisplayMemberBinding = new Binding("Filename");

            //WRFDEV TODO
        }

        private void DirectorySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RepositoryDirectory dir = e.AddedItems[0] as RepositoryDirectory;

            InspectButton.IsEnabled = (dir != null);
        }
    }

    public class NullStringConverter<T> : IValueConverter where T : struct
    {
        public object Convert(object value, Type target, object parameter, System.Globalization.CultureInfo culture)
        {
            var v = value as Nullable<T>;

            if (v == null)
            {
                return "none";
            }
            else if (!v.HasValue)
            {
                return "none";
            }
            else
            {
                return v.Value;
            }
        }

        public object ConvertBack(object value, Type target, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
