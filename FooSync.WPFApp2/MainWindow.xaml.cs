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
        public static readonly string RepoListFilename = "repolist.xml";

        private string         _settingsPath;
        private RepositoryList _repoList;

        public MainWindow()
        {
            InitializeComponent();

            if (Properties.Settings.Default.IsFirstRun)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.IsFirstRun = false;
                Properties.Settings.Default.Save();
            }

            _settingsPath = Path.Combine(
                                Environment.GetEnvironmentVariable("LOCALAPPDATA"),
                                GetAssemblyAttribute<AssemblyCompanyAttribute>().Company,
                                this.GetType().Assembly.GetName().Name);

            if (!Directory.Exists(_settingsPath))
                Directory.CreateDirectory(_settingsPath);

            if (!LoadRepoList())
                Application.Current.Shutdown();
        }

        private T GetAssemblyAttribute<T>()
        {
            object[] attrs = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false);

            if (attrs == null || attrs.Length == 0)
                throw new InvalidOperationException();

            return (T)attrs[0];
        }

        private bool LoadRepoList()
        {
            try
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    _repoList = RepositoryList.ReadFromFile(stream);
                }
            }
            catch (FileNotFoundException)
            {
                using (var stream = new FileStream(Path.Combine(_settingsPath, RepoListFilename), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    _repoList = new RepositoryList();
                    _repoList.WriteToFile(stream);
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

            RepoListTree.DataContext = _repoList;

            return true;
        }

        private void ShowAboutWindow(object sender, RoutedEventArgs e)
        {
            var about = new AboutWindow();
            about.ShowActivated = true;
            about.ShowDialog();
        }

        private void NewLocalPair(object sender, RoutedEventArgs e)
        {
        }

        private void NewRemoteServer(object sender, RoutedEventArgs e)
        {
        }

        private void NewRemotePair(object sender, RoutedEventArgs e)
        {
        }
    }
}
