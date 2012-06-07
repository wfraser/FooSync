///
/// Codewise/FooSync/WPFApp2/MainWindow.xaml.cs
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

namespace Codewise.FooSync.WPFApp2
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly string RepoListFilename = "syncgroups.xml";

        private string        _settingsPath;
        private SyncGroupList _syncGroupList;

        public static FooSyncEngine Foo { get; private set; }

        public MainWindow()
        {
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
        }

        private T GetAssemblyAttribute<T>()
        {
            object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);

            if (attrs == null || attrs.Length == 0)
                throw new InvalidOperationException();

            return (T)attrs[0];
        }

        private bool LoadSyncGroups()
        {
            try
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _syncGroupList = SyncGroupList.ReadFromFile(stream);
                }
            }
            catch (FileNotFoundException)
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    _syncGroupList = new SyncGroupList();
                    _syncGroupList.WriteToFile(stream);
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
                _syncGroupList.WriteToFile(stream);
            }
        }

        private void DeleteExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ServerRepositoryList)
            {
                var server = (ServerRepositoryList)e.Parameter;
                _syncGroupList.Servers.Remove(server);
            }
            else if (e.Parameter is ServerRepository)
            {
                var repo = (ServerRepository)e.Parameter;
                var server = _syncGroupList.Servers.Where((o) => o == repo.Server).FirstOrDefault();
                if (server != null)
                    server.Repositories.Remove(repo);
            }
            else if (e.Parameter is SyncGroup)
            {
                var pair = (SyncGroup)e.Parameter;
                _syncGroupList.SyncGroups.Remove(pair);
            }
        }

        private void CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null &&
                    (e.Parameter is ServerRepositoryList
                    || e.Parameter is ServerRepository
                    || e.Parameter is SyncGroup))
                e.CanExecute = true;
            else
                e.CanExecute = false;
        }

        private void CanNew(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter != null &&
                    (e.Parameter is ICollection<ServerRepositoryList>
                    || e.Parameter is ICollection<SyncGroup>
                    || e.Parameter is SyncGroup))
                e.CanExecute = true;
            else
                e.CanExecute = false;
        }

        private void OnNew(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ICollection<ServerRepositoryList>)
            {
                var servers = (ICollection<ServerRepositoryList>)e.Parameter;

                var serverEntryWindow = new ServerEntryWindow();
                serverEntryWindow.ShowInTaskbar = false;
                serverEntryWindow.ShowActivated = true;
                serverEntryWindow.Topmost = true;
                var result = serverEntryWindow.ShowDialog();
                if (result.HasValue && result.Value)
                {
                    if (servers.Count((server) =>
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

                    var newServer = new ServerRepositoryList()
                    {
                        Hostname = serverEntryWindow.ServerName,
                        Port     = serverEntryWindow.ServerPort,
                        Username = serverEntryWindow.Username,
                        Password = serverEntryWindow.Password
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

                    servers.Add(newServer);
                }
            }
            else if (e.Parameter is ICollection<SyncGroup>)
            {

            }
            else if (e.Parameter is SyncGroup)
            {

            }
        }
    }
}
