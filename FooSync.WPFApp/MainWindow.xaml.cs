using System;
using System.Collections.Generic;
using System.ComponentModel;
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
            EnableControls(FilesPanel, false);
            this.Show();

            if (System.Diagnostics.Debugger.IsAttached)
            {
                //
                // WRFDEV: for testing purposes
                //

                string error;
                _config = RepositoryConfigLoader.GetRepositoryConfig(@"W:\.FooSync_Repository.xml", out error);
                DirectorySelector.ItemsSource = _config.Directories;
                EnableControls(true);
                DirectorySelector.SelectedIndex = 0;
                InspectButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else
            {
                ShowStartWindow();
            }
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
                    if (obj != FilesPanel)
                    {
                        EnableControls(obj as DependencyObject, enabled);
                    }
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

                    if (_config.Directories.Count() == 1)
                    {
                        DirectorySelector.SelectedIndex = 0;
                        InspectButton.IsEnabled = true;
                    }
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
                dlg.Description = "Select the location for the new repository.";

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

            if (dir.Source == null)
            {
                var result = MessageBox.Show(
                    "There's no source configured for this repository directory that matches your computer.\n\nWould you like to configure one?",
                    "No Valid Source",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation,
                    MessageBoxResult.No);
                if (result == MessageBoxResult.Yes)
                {
                    MessageBox.Show("[[TODO: Configure Window]]");
                }
                else
                {
                    return;
                }
            }

            ProgressDialog dlg = new ProgressDialog();

            dlg.WindowTitle = "Inspecting Directory";
            dlg.Text = "Inspecting Directory...";
            dlg.ProgressBarStyle = ProgressBarStyle.ProgressBar;
            dlg.ShowCancelButton = false;

            dlg.DoWork += new DoWorkEventHandler(InspectDirectoryWorker);
            dlg.RunWorkerCompleted += new RunWorkerCompletedEventHandler(InspectDirectoryCompletedWorker);
            dlg.Show(dir);
        }

        void InspectDirectoryWorker(object sender, DoWorkEventArgs e)
        {
            var dir = e.Argument as RepositoryDirectory;
            var dlg = sender as ProgressDialog;

            var exceptions = FooSyncEngine.PrepareExceptions(dir);

            _repo = _foo.Tree(Path.Combine(_config.RepositoryPath, dir.Path), exceptions, 
            (Progress)delegate(int n, int total, string d)
            {
                if (n % 100 == 0)
                {
                    dlg.ReportProgress(0, "Enumerating Repository Files...", d);
                }
            });

            _source = _foo.Tree(dir.Source.Path, exceptions,
            (Progress)delegate(int n, int total, string d)
            {
                if (n % 100 == 0)
                {
                    dlg.ReportProgress(0, "Enumerating Source Files...", d);
                }
            });

            dlg.ReportProgress(0, "Loading Repository State...", null);

            try
            {
                _state = new RepositoryState(Path.Combine(_config.RepositoryPath, dir.Path, FooSyncEngine.RepoStateFileName));
            }
            catch (FileNotFoundException)
            {
                _state = new RepositoryState();
                _state.AddSource(_repo, RepositoryState.RepoSourceName);
                _state.AddSource(_source, Environment.MachineName.ToLower());
                _state.Write(Path.Combine(_config.RepositoryPath, dir.Path, FooSyncEngine.RepoStateFileName));
            }

            dlg.ProgressBarStyle = ProgressBarStyle.ProgressBar;

            _changeset = _foo.Inspect(_state, _repo, _source,
            (Progress)delegate(int n, int total, string d)
            {
                if (n % 100 == 0)
                {
                    dlg.ReportProgress((int)Math.Round((double)n / total * 100), "Comparing Files...", d);
                }
            });

            _foo.GetConflicts(_changeset, _state, _repo, _source);
            _foo.SetDefaultActions(_changeset);
        }

        void InspectDirectoryCompletedWorker(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_changeset.Count() == 0)
            {
                MessageBox.Show("No changes detected. Nothing to do.", "Change set empty", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                return;
            }

            EnableControls(FilesPanel, true);

            var newFiles = _changeset.Where(elem => 
                (!_state.Repository.MTimes.ContainsKey(elem.Filename) && !_repo.Files.ContainsKey(elem.Filename))
                    || (!_state.Source.MTimes.ContainsKey(elem.Filename) && !_source.Files.ContainsKey(elem.Filename)));
            NewFiles.DataContext = new BindableChangeSet(_changeset, newFiles, _repo, _source);
            if (newFiles.Count() > 0)
            {
                (NewFiles.Parent as Expander).IsExpanded = true;
            }

            var deletedFiles = _changeset.Where(elem =>
                (_state.Repository.MTimes.ContainsKey(elem.Filename) && !_repo.Files.ContainsKey(elem.Filename))
                    || (_state.Source.MTimes.ContainsKey(elem.Filename) && !_source.Files.ContainsKey(elem.Filename)));
            DeletedFiles.DataContext = new BindableChangeSet(_changeset, deletedFiles, _repo, _source);
            if (deletedFiles.Count() > 0)
            {
                (DeletedFiles.Parent as Expander).IsExpanded = true;
            }

            var changedFiles = _changeset.Where(elem => !deletedFiles.Contains(elem.Filename) && !newFiles.Contains(elem.Filename));
            ChangedFiles.DataContext = new BindableChangeSet(_changeset, changedFiles, _repo, _source);
            if (changedFiles.Count() > 0)
            {
                (ChangedFiles.Parent as Expander).IsExpanded = true;
            }
        }

        private void DirectorySelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 0)
            {
                var dir = e.AddedItems[0] as RepositoryDirectory;
                InspectButton.IsEnabled = (dir != null);
            }
        }

        private void ContextMenu_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var list = sender as ListView;

            if (list != null && list.ContextMenu != null && list.ContextMenu.Items != null)
            {
                for (int i = 0; i < list.ContextMenu.Items.Count; i++)
                {
                    var item = list.ContextMenu.Items[i] as MenuItem;
                    if (item != null)
                    {
                        //
                        // This is kinda hacky, but x:Name can't be used in a ControlTemplate :/
                        // The alternative is switching on the Header property...
                        //

                        if (i == 0 || i == 3) // Open file [location] (Repository)
                        {
                            item.IsEnabled = (list.SelectedItems.Count == 1
                                    && (list.SelectedItem as BindableChangeSetElem).RepositoryDate.HasValue);
                        }
                        else if (i == 1 || i == 4) // Open file [location] (Source)
                        {
                            item.IsEnabled = (list.SelectedItems.Count == 1
                                    && (list.SelectedItem as BindableChangeSetElem).SourceDate.HasValue);
                        }
                        else if (i == 6) // Change action to:
                        {
                            for (int j = 0; j < item.Items.Count; j++)
                            {
                                var subItem = item.Items[j] as MenuItem;
                                if (subItem != null)
                                {
                                    //WRFDEV TODO
                                    subItem.IsEnabled = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OpenExplorerAt(string filename)
        {
            System.Diagnostics.Process.Start("explorer.exe", "/select," + filename);
        }

        public void OpenWithDefaultApplication(string filename)
        {
            System.Diagnostics.Process.Start(filename);
        }

        #region Actions

        public static RoutedCommand ActionClick = new RoutedCommand("ActionClick", typeof(MainWindow));
        /// <summary>
        /// Responsible for updating the file operation on a file. Fired when the 'Action' buttons in the file grid are clicked.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e">The parameter contains the filename to be affected.</param>
        private void OnActionClick(object sender, ExecutedRoutedEventArgs e)
        {
            var filename = e.Parameter as string;
            if (filename == null)
            {
                throw new ArgumentException();
            }

            _changeset[filename].FileOperation++;

            //
            // Don't let nonsensical file operations be set.
            // (can't copy or delete a nonexistant file)
            //

            if (((_changeset[filename].ChangeStatus == ChangeStatus.RepoDeleted 
                        || _changeset[filename].ChangeStatus == ChangeStatus.RepoMissing)
                    && (_changeset[filename].FileOperation == FileOperation.UseRepo 
                        || _changeset[filename].FileOperation == FileOperation.DeleteRepo))
                || (((_changeset[filename].ChangeStatus == ChangeStatus.SourceDeleted 
                        || _changeset[filename].ChangeStatus == ChangeStatus.SourceMissing)
                    && (_changeset[filename].FileOperation == FileOperation.UseSource 
                        || _changeset[filename].FileOperation == FileOperation.DeleteSource))))
            {
                _changeset[filename].FileOperation++;
            }

            if (_changeset[filename].FileOperation == FileOperation.MaxFileOperation)
            {
                _changeset[filename].FileOperation = (FileOperation)0;
            }

            _changeset.AdviseChanged(filename);
        }

        public static RoutedCommand OpenLocationRepo = new RoutedCommand("OpenLocationRepo", typeof(MainWindow));
        private void OnOpenLocationRepo(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                return;

            var list = (e.Parameter as ListView);

            if (list != null)
            {
                if (list.SelectedItems.Count == 1)
                {
                    var item = list.SelectedItem as BindableChangeSetElem;
                    OpenExplorerAt(Path.Combine(_repo.Path, item.Filename));
                }
            }
        }

        public static RoutedCommand OpenLocationSource = new RoutedCommand("OpenLocationSource", typeof(MainWindow));
        private void OnOpenLocationSource(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                return;

            var list = (e.Parameter as ListView);

            if (list != null)
            {
                if (list.SelectedItems.Count == 1)
                {
                    var item = list.SelectedItem as BindableChangeSetElem;
                    OpenExplorerAt(Path.Combine(_source.Path, item.Filename));
                }
            }
        }

        public static RoutedCommand OpenFileRepo = new RoutedCommand("OpenFileRepo", typeof(MainWindow));
        private void OnOpenFileRepo(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                return;

            var list = (e.Parameter as ListView);

            if (list != null)
            {
                if (list.SelectedItems.Count == 1)
                {
                    var item = list.SelectedItem as BindableChangeSetElem;
                    OpenWithDefaultApplication(Path.Combine(_repo.Path, item.Filename));
                }
            }
        }

        public static RoutedCommand OpenFileSource = new RoutedCommand("OpenFileSource", typeof(MainWindow));
        private void OnOpenFileSource(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                return;

            var list = (e.Parameter as ListView);

            if (list != null)
            {
                if (list.SelectedItems.Count == 1)
                {
                    var item = list.SelectedItem as BindableChangeSetElem;
                    OpenWithDefaultApplication(Path.Combine(_source.Path, item.Filename));
                }
            }
        }

        public static RoutedCommand ChangeFileOperation = new RoutedCommand("ChangeFileOperation", typeof(MainWindow));
        private void OnChangeFileOperation(object sender, ExecutedRoutedEventArgs e)
        {
            if (e == null)
                return;

            var args = e.Parameter as object[];

            if (args != null)
            {
                var item = args[0] as BindableChangeSetElem;
                var newOp = (FooSync.FileOperation)Enum.Parse(typeof(FooSync.FileOperation), args[1] as string);

                item.Action = newOp;

                _changeset.AdviseChanged(item.Filename);
            }
        }

        #endregion
    }
}
