///
/// Codewise/FooSync/WPFApp/MainWindow.xaml.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Ookii.Dialogs.Wpf;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly string RepoListFilename = "syncgroups.xml";
        public static FooSyncEngine Foo { get; private set; }

        private static MainWindow _instance = null;

        public bool IsClosed { get; private set; }

        private string        _settingsPath;
        private SyncGroupList _syncGroupList;
        private Dictionary<SyncGroup, RepositoryDiff> _repositoryDiffControls;

        internal MainWindow()
        {
            System.Diagnostics.Debug.Assert(_instance == null,
                "duplicate Codewise.FooSync.WPFApp.MainWindow constructed! It's supposed to be a singleton!");

            _instance = this;

            IsClosed = false;

            InitializeComponent();

            if (Properties.Settings.Default.IsFirstRun)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsFirstRun = false;
                Properties.Settings.Default.Save();
            }

            var options = new Options();
            options.CaseInsensitive = true;
            Foo = new FooSyncEngine(options);

            _settingsPath = Path.Combine(
                                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                                GetAssemblyAttribute<AssemblyCompanyAttribute>().Company,
                                this.GetType().Assembly.GetName().Name);

            if (!Directory.Exists(_settingsPath))
                Directory.CreateDirectory(_settingsPath);

            if (!LoadSyncGroups())
                Application.Current.Shutdown();

            _repositoryDiffControls = new Dictionary<SyncGroup, RepositoryDiff>();
        }

        public static MainWindow Instance
        {
            get { return _instance; }
        }

        private static T GetAssemblyAttribute<T>()
        {
            object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);

            if (attrs == null || attrs.Length == 0)
                throw new InvalidOperationException();

            return (T)attrs[0];
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        private bool LoadSyncGroups()
        {
            try
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _syncGroupList = SyncGroupList.Deserialize(stream);
                }
            }
            catch (FileNotFoundException)
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    _syncGroupList = new SyncGroupList();
                    _syncGroupList.Serialize(stream);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format("Your repository list settings file is corrupt! You must either fix it or delete it.\n\nFilename: {0}\n\nError on deserializing: {1}{2}",
                        Path.Combine(_settingsPath, RepoListFilename),
                        ex.Message,
#if DEBUG
                        "\n\n" + ex.StackTrace
#else
                        string.Empty
#endif
                    ),
                    "Error reading repository list",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                return false;
            }

            TreePane.DataContext = _syncGroupList;

            return true;
        }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.ShowActivated = true;
            about.ShowDialog();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.Truncate, FileAccess.Write, FileShare.None))
            {
                _syncGroupList.Serialize(stream);
            }

            IsClosed = true;
        }

        private void OnDelete(object sender, ExecutedRoutedEventArgs e)
        {
            object param = e.Parameter;
            FooServer paramFooServer;
            SyncGroup paramSyncGroup;

            if (param == null)
            {
                var elem = Keyboard.FocusedElement as FrameworkElement;
                if (elem != null)
                {
                    param = elem.DataContext;
                }
            }

            if ((paramFooServer = param as FooServer) != null)
            {
                foreach (SyncGroup syncGroup in _syncGroupList.SyncGroups)
                {
                    foreach (ServerRepository repo in paramFooServer.Repositories)
                    {
                        if (syncGroup.URLs.Contains(repo.URL))
                        {
                            syncGroup.URLs.Remove(repo.URL);
                        }
                    }
                }

                _syncGroupList.Servers.Remove(paramFooServer);
            }
            else if ((paramSyncGroup = param as SyncGroup) != null)
            {
                foreach (FooServer server in _syncGroupList.Servers)
                {
                    foreach (ServerRepository repo in server.Repositories)
                    {
                        repo.MemberOfSyncGroups.Remove(paramSyncGroup.Name);
                    }
                }

                _syncGroupList.SyncGroups.Remove(paramSyncGroup);
            }
        }

        private void CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            object param = e.Parameter;

            if (param == null)
            {
                var elem = Keyboard.FocusedElement as FrameworkElement;
                if (elem != null)
                {
                    param = elem.DataContext;
                }
            }

            if (param != null &&
                    (param is FooServer
                    || param is SyncGroup))
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void CanNew(object sender, CanExecuteRoutedEventArgs e)
        {
            object param = e.Parameter;

            if (param == null)
            {
                var elem = Keyboard.FocusedElement as FrameworkElement;
                if (elem != null)
                {
                    param = elem.DataContext;
                }
            }

            if (param != null &&
                    (param is ICollection<FooServer>
                    || param is ICollection<SyncGroup>
                    || param is SyncGroup))
            {
                e.CanExecute = true;
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            object param = e.Parameter;

            if (param == null)
            {
                var elem = Keyboard.FocusedElement as FrameworkElement;
                if (elem != null)
                {
                    param = elem.DataContext;
                }
            }

            if (param is ICollection<FooServer>)
            {
                NewRemoteServer();
            }
            else if (param is ICollection<SyncGroup>)
            {
                NewSyncGroup();
            }
            else if (param is SyncGroup)
            {

            }
        }

        private void NewRemoteServer_Click(object sender, RoutedEventArgs e)
        {
            NewRemoteServer();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void NewRemoteServer()
        {
            var serverEntryWindow = new ServerEntryWindow();
            serverEntryWindow.ShowInTaskbar = false;
            serverEntryWindow.ShowActivated = true;
            serverEntryWindow.Topmost = true;
            var result = serverEntryWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (_syncGroupList.Servers.Count((server) =>
                        (server.Hostname.Equals(serverEntryWindow.ServerName)
                            && server.Port == serverEntryWindow.ServerPort)) > 0)
                {
                    //
                    // Duplicate.
                    //

                    MessageBox.Show(
                        string.Format("That server ({0}) is already in your Saved Servers list.",
                            serverEntryWindow.ServerNameEntry.Text),
                        "Duplicate Server",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                    return;
                }

                var newServer = new FooServer()
                {
                    Hostname = serverEntryWindow.ServerName,
                    Port = serverEntryWindow.ServerPort,
                    Username = serverEntryWindow.Username,
                    Password = serverEntryWindow.Password,
                    Description = serverEntryWindow.ServerDescription
                };

                foreach (var repoName in serverEntryWindow.Repositories)
                {
                    var repo = new ServerRepository() { Server = newServer, Name = repoName };
                    newServer.Repositories.Add(repo);
                    foreach (var syncGroup in _syncGroupList.SyncGroups.Where(
                        (syncGroup) => syncGroup.URLs.Contains(repo.URL)))
                    {
                        repo.MemberOfSyncGroups.Add(syncGroup.Name);
                    }
                }

                _syncGroupList.Servers.Add(newServer);

                TreePane.SelectPath(new Predicate<object>[] {
                    (object o) => (o is TreeViewItem && string.Equals(((TreeViewItem)o).Header, "Saved Servers")),
                    (object o) => (o == newServer)
                });
            }
        }

        private void NewSyncGroup_Click(object sender, RoutedEventArgs e)
        {
            NewSyncGroup();
        }

        private void NewSyncGroup()
        {
            var syncGroupEntryWindow = new SyncGroupEntryWindow();
            syncGroupEntryWindow.ShowInTaskbar = false;
            syncGroupEntryWindow.ShowActivated = true;
            syncGroupEntryWindow.Topmost = true;
            var result = syncGroupEntryWindow.ShowDialog();
            if (result.HasValue && result.Value)
            {
                if (_syncGroupList.SyncGroups.Count((group) => group.Name.Equals(syncGroupEntryWindow.SyncGroupNameEntry.Text)) > 0)
                {
                    //
                    // Duplicate.
                    //

                    MessageBox.Show(
                        string.Format("A sync group with that name ({0}) already exists.",
                            syncGroupEntryWindow.SyncGroupNameEntry.Text),
                        "Duplicate Server",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                        );
                    return;
                }

                var newSyncGroup = new SyncGroup()
                {
                    Name = syncGroupEntryWindow.SyncGroupNameEntry.Text
                };
                newSyncGroup.URLs.Add(new FooSyncUrl("file:///" + syncGroupEntryWindow.LocationEntry.Text));

                _syncGroupList.SyncGroups.Add(newSyncGroup);
            }
        }

        private void DragDropDataContext_MouseMove(object sender, MouseEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(element, element.DataContext, DragDropEffects.Link);
            }
        }

        private void SyncGroup_DragEnter(object sender, DragEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (elem == null || !(elem.DataContext is SyncGroup))
            {
                return;
            }

            var formats = e.Data.GetFormats(true);
            if (formats.Contains(typeof(ServerRepository).FullName))
            {
                e.Effects = DragDropEffects.Link;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void SyncGroup_Drop(object sender, DragEventArgs e)
        {
            var elem = sender as FrameworkElement;
            if (elem == null || !(elem.DataContext is SyncGroup))
            {
                return;
            }

            var formats = e.Data.GetFormats(true);
            if (!formats.Contains(typeof(ServerRepository).FullName))
            {
                e.Effects = DragDropEffects.None;
                return;
            }

            e.Effects = DragDropEffects.Link;
            var repo = (ServerRepository)e.Data.GetData(typeof(ServerRepository));
            var syncGroup = (SyncGroup)elem.DataContext;

            if (!syncGroup.URLs.Contains(repo.URL))
            {
                syncGroup.URLs.Add(repo.URL);
                repo.MemberOfSyncGroups.Add(syncGroup.Name);
            }
        }

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            var syncGroup = TreePane.SelectedItem as SyncGroup;

            if (syncGroup == null)
            {
                return;
            }

            if (!_repositoryDiffControls.ContainsKey(syncGroup))
            {
                _repositoryDiffControls.Add(syncGroup, new RepositoryDiff(syncGroup));
            }

            RepositoryDiff diffPanel = _repositoryDiffControls[syncGroup];

            SyncGroupView.Children.Add(diffPanel);
            SyncGroupOverView.Visibility = Visibility.Collapsed;

            diffPanel.Cancelled += new EventHandler(delegate(object panel, EventArgs x)
            {
                _repositoryDiffControls.Remove(syncGroup);
                SyncGroupView.Children.Remove(diffPanel);
                SyncGroupOverView.Visibility = Visibility.Visible;
            }
            );

            diffPanel.Start();
        }

        private void Synchronize_Click(object sender, RoutedEventArgs e)
        {
            var syncGroup = TreePane.SelectedItem as SyncGroup;

            if (syncGroup == null)
            {
                return;
            }

            //TODO
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        private void SyncGroupLocationShow_Click(object sender, RoutedEventArgs e)
        {
            var syncGroup = TreePane.SelectedItem as SyncGroup;
            var index = SyncGroupLocation.SelectedIndex;

            if (syncGroup == null || index == -1)
            {
                return;
            }

            FooSyncUrl url;

            try
            {
                url = syncGroup.URLs[index];
            }
            catch (IndexOutOfRangeException)
            {
                return;
            }

            if (url.IsLocal)
            {
                //
                // open explorer
                //

                OpenExplorerIn(url.LocalPath);
            }
            else
            {
                //
                // Highlight server repository in the tree pane
                //

                TreePane.SelectPath(new Predicate<object>[] {
                    (object o) => (o is TreeViewItem && string.Equals(((TreeViewItem)o).Header, "Saved Servers")),
                    (object o) => (o is FooServer && ((FooServer)o).Hostname == url.Host),
                    (object o) => (o is ServerRepository && ((ServerRepository)o).Name == url.AbsolutePath.Substring(1))
                });
            }
        }

        private void SyncGroupLocationRemove_Click(object sender, RoutedEventArgs e)
        {
            var syncGroup = TreePane.SelectedItem as SyncGroup;
            var index = SyncGroupLocation.SelectedIndex;

            if (syncGroup == null || index == -1)
            {
                return;
            }

            try
            {
                syncGroup.URLs.RemoveAt(index);
                SyncGroupLocation.GetBindingExpression(ListBox.ItemsSourceProperty).UpdateTarget();
            }
            catch (ArgumentOutOfRangeException)
            {
            }
        }

        private void SyncGroup_AddFolder_Click(object sender, RoutedEventArgs e)
        {
            var syncGroup = TreePane.SelectedItem as SyncGroup;

            if (syncGroup == null)
            {
                return;
            }

            try
            {
                VistaFolderBrowserDialog folderDialog = new VistaFolderBrowserDialog();
                folderDialog.Description = "Select a folder to add to the sync group.";
                bool? result = folderDialog.ShowDialog();

                if (result.HasValue && result.Value)
                {
                    syncGroup.URLs.Add(new FooSyncUrl(folderDialog.SelectedPath));
                }
            }
            catch (Exception)
            {
            }
        }

        private static void OpenExplorerIn(string path)
        {
            System.Diagnostics.Process.Start("explorer.exe", path);
        }

        /*
        private void ServerDescriptionDisplay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ServerDescriptionDisplay.Visibility = Visibility.Collapsed;
            ServerDescriptionEdit.Visibility = Visibility.Visible;
        }

        private void ServerDescriptionEdit_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                e.Handled = true;
                ServerDescriptionEdit.Visibility = Visibility.Collapsed;
                ServerDescriptionDisplay.Visibility = Visibility.Visible;
                ServerDescriptionEdit.GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }
         */

        private void TreePane_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.OldValue is FooServer)
            {
                //
                // Make sure description edit box is closed.
                //
                ServerDescription.CancelEdit();
            }

            if (e.NewValue is SyncGroup)
            {
                SyncGroup syncGroup = (SyncGroup)e.NewValue;

                SyncGroupView.Visibility = Visibility.Visible;
                ServerView.Visibility = Visibility.Collapsed;

                bool found = false;
                foreach (KeyValuePair<SyncGroup, RepositoryDiff> pair in _repositoryDiffControls)
                {
                    if (pair.Key == syncGroup)
                    {
                        SyncGroupOverView.Visibility = Visibility.Collapsed;
                        pair.Value.Visibility = Visibility.Visible;
                        found = true;
                    }
                    else
                    {
                        pair.Value.Visibility = Visibility.Collapsed;
                    }
                }

                if (!found)
                {
                    SyncGroupOverView.Visibility = Visibility.Visible;
                }
            }
            else if (e.NewValue is FooServer)
            {
                SyncGroupView.Visibility = Visibility.Collapsed;
                ServerView.Visibility = Visibility.Visible;
            }
            else
            {
                SyncGroupView.Visibility = Visibility.Collapsed;
                ServerView.Visibility = Visibility.Collapsed;
            }
        }
    }
}
