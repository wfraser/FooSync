///
/// Codewise/FooSync/WPFApp/RepositoryDiff.xaml.cs
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
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for RepositoryDiff.xaml
    /// </summary>
    public partial class RepositoryDiff : UserControl
    {
        private SyncGroup _syncGroup;
        private bool _cancel;

        public event EventHandler Cancelled;

        public RepositoryDiff(SyncGroup syncGroup)
        {
            InitializeComponent();

            _syncGroup = syncGroup;
        }

        private MainWindow _mainWindow = null;
        public MainWindow GetMainWindow()
        {
            DependencyObject obj = Parent;
            while (!(obj is MainWindow))
            {
                FrameworkElement fw = obj as FrameworkElement;
                if (fw == null)
                {
                    return null;
                }
                else
                {
                    obj = fw.Parent;
                }
            }

            return (MainWindow)obj;
        }

        public void Start()
        {
            _mainWindow = GetMainWindow();

            Thread workingThread = new Thread(
                delegate ()
                {
                    Dictionary<Guid, RepositoryStateCollection> repoStates = new Dictionary<Guid, RepositoryStateCollection>();
                    Dictionary<Guid, FooTree> trees = new Dictionary<Guid, FooTree>();
                    
                    Progress.ValueChanged += new RoutedPropertyChangedEventHandler<double>((object sender, RoutedPropertyChangedEventArgs<double> args) =>
                        {
                            _mainWindow.TaskbarItemInfo.ProgressValue = args.NewValue / 100;
                        }
                    );

                    //
                    // Get RepositoryStateCollection and FooTree for each URL in the sync group.
                    //

                    foreach (FooSyncUrl url in _syncGroup.URLs)
                    {
                        NetClient client;
                        RepositoryStateCollection repoState;
                        FooTree tree;

                        if (url.IsLocal)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                                Progress.IsIndeterminate = true;
                                ProgressText.Text = "Getting Repository State...";
                                DetailText1.Text = url.LocalPath;
                                DetailText2.Text = string.Empty;
                            }
                            ));

                            try
                            {
                                repoState = new RepositoryStateCollection(Path.Combine(url.LocalPath, FooSyncEngine.RepoStateFileName));
                            }
                            catch (FileNotFoundException)
                            {
                                repoState = new RepositoryStateCollection();
                            }

                            DateTime last = DateTime.Now;
                            tree = new FooTree(MainWindow.Foo, url.LocalPath, (IEnumerable<string>)null, new Progress((int current, int total, string path) =>
                                {
                                    if ((DateTime.Now - last).Milliseconds > 50)
                                    {
                                        last = DateTime.Now;
                                        Dispatcher.Invoke(new Action(() =>
                                            {
                                                _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                                                Progress.IsIndeterminate = true;
                                                ProgressText.Text = "Enumerating Files...";
                                                DetailText1.Text = string.Format("{0} files so far", current);
                                                DetailText2.Text = Path.Combine(url.LocalPath, Path.GetDirectoryName(path));
                                            }
                                        ));
                                    }
                                }
                            ));

                            if (repoState.Repositories.Count == 0)
                            {
                                repoState.AddRepository(tree, repoState.RepositoryID);
                                repoState.Write(Path.Combine(url.LocalPath, FooSyncEngine.RepoStateFileName));
                            }
                        }
                        else
                        {
                            Dispatcher.Invoke(new Action(() =>
                                {
                                    _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                                    Progress.IsIndeterminate = true;
                                    ProgressText.Text = "Getting Repository State...";
                                    DetailText1.Text = url.ToString();
                                    DetailText2.Text = string.Empty;
                                }
                            ));

                            try
                            {
                                client = new NetClient(MainWindow.Foo, url.Host, url.Port, "WRFDEV", "WRFDEV", url.AbsolutePath.Substring(1));
                                repoState = client.GetState();

                                DateTime last = DateTime.Now;
                                tree = client.GetTree(new Progress((int current, int total, string path) =>
                                    {
                                        if ((DateTime.Now - last).Milliseconds > 50)
                                        {
                                            last = DateTime.Now;
                                            Dispatcher.Invoke(new Action(() =>
                                                {
                                                    _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                                                    Progress.IsIndeterminate = true;
                                                    ProgressText.Text = "Enumerating Files...";
                                                    DetailText1.Text = string.Format("{0} files so far", current);
                                                    DetailText2.Text = Path.GetDirectoryName(path);
                                                }
                                            ));
                                        }
                                    }
                                ));
                            }
                            catch (Exception ex)
                            {
                                Dispatcher.Invoke(new Action(() =>
                                    {
                                        _mainWindow.TaskbarItemInfo.ProgressValue = 1.0;
                                        _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                                    }
                                ));
                                MessageBox.Show(string.Format("Error: {0} ({1})", ex.Message, ex.GetType().Name), "Error Getting Remote Repository", MessageBoxButton.OK, MessageBoxImage.Error);
                                repoState = null;
                                tree = null;
                            }
                        }

                        if (repoState != null && tree != null)
                        {
                            repoStates.Add(repoState.RepositoryID, repoState);
                            trees.Add(repoState.RepositoryID, tree);
                        }
                    }
                    
                    //
                    // TODO: Diff!
                    //

                    for (int i = 0; i <= 100; i++)
                    {
                        System.Threading.Thread.Sleep(10);
                        Dispatcher.Invoke(new Action(() => 
                            {
                                _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                                Progress.IsIndeterminate = false;
                                Progress.Value = i;
                                ProgressText.Text = "Not Doing Anything...";
                                DetailText1.Text = string.Format("{0}%", i);
                                DetailText2.Text = string.Empty;
                            }
                        ));

                        if (MainWindow.Instance.IsClosed || _cancel)
                        {
                            break;
                        }
                    }

                    Guid fakeRepo1 = Guid.NewGuid();
                    Guid fakeRepo2 = Guid.NewGuid();
                    Guid fakeRepo3 = Guid.NewGuid();

                    //
                    // Fake files, for UI demo purposes.
                    //
                    // file1 is new in Repo 1, and needs to be pushed to Repo 2 and 3.
                    //
                    // heyo/file2 is new in fakeRepo1.
                    //

                    FooChangeSet fakeChangeSet = new FooChangeSet();
                    fakeChangeSet.Add("file1", ChangeStatus.Newer, fakeRepo1);
                    fakeChangeSet.Add("file1", ChangeStatus.Identical, fakeRepo2);
                    fakeChangeSet.Add("file1", ChangeStatus.Identical, fakeRepo3);
                    fakeChangeSet.Add("heyo/file2", ChangeStatus.New, fakeRepo1);

                    // just set all to no conflict; this would normally be done in the diff stage
                    foreach (string filename in fakeChangeSet)
                    {
                        foreach (var x in fakeChangeSet[filename])
                        {
                            x.Value.ConflictStatus = ConflictStatus.NoConflict;
                        }
                    }

                    fakeChangeSet.SetDefaultActions();

                    RepositoryDiffData diffData = new RepositoryDiffData();
                    foreach (string filename in fakeChangeSet)
                    {
                        diffData.Add(new RepositoryDiffDataItem()
                        {
                            Filename = filename,
                        });
                    }

                    Dispatcher.Invoke(new Action(() =>
                        {
                            _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                            ProgressView.Visibility = Visibility.Collapsed;
                            Grid.ItemsSource = diffData;
                            Grid.Visibility = Visibility.Visible;
                        }
                    ));
                }
            );

            workingThread.Name = "worker thread for sync group " + _syncGroup.Name;
            
            workingThread.Start();
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // TODO
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancel = true;

            if (Cancelled != null)
            {
                Cancelled(this, new EventArgs());
            }
        }

        private void Synchronize_Click(object sender, RoutedEventArgs e)
        {
            // TODO
        }
    }
}
