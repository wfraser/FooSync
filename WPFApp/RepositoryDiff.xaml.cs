﻿///
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
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Shell;

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// Interaction logic for RepositoryDiff.xaml
    /// </summary>
    public partial class RepositoryDiff : UserControl
    {
        private MainWindow    _mainWindow;
        private FooSyncEngine _foo;
        private SyncGroup     _syncGroup;
        private Thread        _inspectWorkingThread;
        private bool _cancel;

        private static readonly int ProgressUpdateRateMsecs = 100;

        public event EventHandler Cancelled;

        public RepositoryDiff(MainWindow mainWindow, FooSyncEngine foo, SyncGroup syncGroup)
        {
            InitializeComponent();

            _mainWindow = mainWindow;
            _foo = foo;
            _syncGroup = syncGroup;
        }

        public void Start()
        {
            _inspectWorkingThread = new Thread(
                delegate ()
                {
                    RepositoryStateCollection repoState;
                    Dictionary<Guid, FooTree> trees = new Dictionary<Guid, FooTree>();
                    List<RepositoryStateCollection> repoStates = new List<RepositoryStateCollection>();
                    
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
                                repoState.Modified = DateTime.Now;
                            }

                            DateTime last = DateTime.Now;
                            tree = new FooTree(MainWindow.Foo, url.LocalPath, FooSyncEngine.PrepareExceptions(_syncGroup.IgnorePatterns.OfType<IIgnorePattern>().ToList()), new Progress((current, total, path) =>
                                {
                                    if ((DateTime.Now - last).Milliseconds > ProgressUpdateRateMsecs)
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
                                tree = client.GetTree(new Progress((current, total, path) =>
                                    {
                                        if ((DateTime.Now - last).Milliseconds > ProgressUpdateRateMsecs)
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
                            foreach (RepositoryStateCollection otherState in repoStates)
                            {
                                if (otherState.Repositories.Count(pair => pair.Key == repoState.RepositoryID) < 1)
                                {
                                    otherState.AddRepository(tree, repoState.RepositoryID);

                                    if (trees[otherState.RepositoryID].Base.IsLocal)
                                    {
                                        otherState.Write(Path.Combine(trees[otherState.RepositoryID].Base.LocalPath, FooSyncEngine.RepoStateFileName));
                                    }
                                    else
                                    {
                                        // client.UpdateState(otherState)
                                        throw new NotImplementedException();
                                    }
                                }
                            }

                            repoStates.Add(repoState);
                            trees.Add(repoState.RepositoryID, tree);
                        }
                    }

                    repoState = (from state in repoStates
                                 orderby state.Modified descending
                                 select state).First();
                    
                    //
                    // Diff!
                    //

                    Dispatcher.Invoke(new Action(() => 
                        {
                            _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                            Progress.IsIndeterminate = false;
                            ProgressText.Text = "Inspecting Files...";
                        }
                    ));

                    DateTime lastUpdate = DateTime.Now;
                    FooChangeSet changeSet = _foo.Inspect(repoState, trees, new Progress((current, total, name) =>
                        {
                            if ((DateTime.Now - lastUpdate).Milliseconds > ProgressUpdateRateMsecs)
                            {
                                lastUpdate = DateTime.Now;
                                Dispatcher.Invoke(new Action(() =>
                                    {
                                        Progress.Maximum = total;
                                        Progress.Value = current;
                                        DetailText1.Text = string.Format("{0:##0.00}%", (double)current / total * 100);
                                        DetailText2.Text = name;
                                    }
                                ));
                            }
                        }
                    ));

                    changeSet.SetDefaultActions(trees);

                    //
                    // Convert the changeset into data structures for display.
                    //

                    DictionaryItemPickerConverter converter = new DictionaryItemPickerConverter();

                    foreach (KeyValuePair<Guid, FooTree> pair in trees)
                    {
                        FooSyncUrl url = pair.Value.Base;
                        Guid repoId = pair.Key;

                        Dispatcher.Invoke(new Action(() =>
                            {
                                ((GridView)Grid.View).Columns.Add(new GridViewColumn()
                                    {
                                        Header = string.Format("  {0}  ", url.IsLocal ? url.LocalPath : url.ToString()),
                                        DisplayMemberBinding = new Binding()
                                            {
                                                Converter = converter,
                                                ConverterParameter = repoId,
                                                Path = new PropertyPath("ChangeStatus"),
                                            },
                                    });

                                FrameworkElementFactory comboBoxFactory = new FrameworkElementFactory(typeof(ComboBox));
                                comboBoxFactory.Name = "actions combobox";
                                comboBoxFactory.SetValue(ComboBox.ItemsSourceProperty, Enum.GetValues(typeof(FileOperation)));
                                comboBoxFactory.SetBinding(ComboBox.SelectedItemProperty, new Binding()
                                    {
                                        Converter = converter,
                                        ConverterParameter = repoId,
                                        Path = new PropertyPath("FileOperation"),
                                    });

                                ((GridView)Actions.View).Columns.Add(new GridViewColumn()
                                    {
                                        Header = string.Format("  {0}  ", url.IsLocal ? url.LocalPath : url.ToString()),
                                        CellTemplate = new DataTemplate()
                                            {
                                                VisualTree = comboBoxFactory
                                            },
                                    });
                            }
                        ));
                    }

                    RepositoryDiffData diffData = new RepositoryDiffData();
                    foreach (string filename in changeSet)
                    {
                        RepositoryDiffDataItem item = new RepositoryDiffDataItem();
                        item.Filename = filename;
                        if (changeSet[filename].Values.Any(x => x.ConflictStatus != ConflictStatus.NoConflict))
                        {
                            item.State = "Conflict";
                        }
                        else
                        {
                            foreach (FooChangeSetElem changeElem in changeSet[filename].Values)
                            {
                                if (changeElem.ChangeStatus == ChangeStatus.New)
                                {
                                    item.State = "Added";
                                    break;
                                }
                                else if (changeElem.ChangeStatus == ChangeStatus.Deleted)
                                {
                                    item.State = "Deleted";
                                    break;
                                }
                                else if (changeElem.ChangeStatus == ChangeStatus.Changed)
                                {
                                    item.State = "Changed";
                                    break;
                                }
                            }
                        }

                        foreach (Guid repoId in changeSet[filename].Keys)
                        {
                            item.ChangeStatus.Add(repoId, changeSet[filename][repoId].ChangeStatus);
                            item.FileOperation.Add(repoId, changeSet[filename][repoId].FileOperation);
                        }

                        diffData.Add(item);
                    }

                    //
                    // Display
                    //

                    Dispatcher.Invoke(new Action(() =>
                        {
                            _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                            ProgressView.Visibility = Visibility.Collapsed;
                            Grid.ItemsSource = diffData;
                            Grid.Visibility = Visibility.Visible;
                            ActionsPanel.Visibility = Visibility.Visible;
                            Actions.ItemsSource = diffData;
                        }
                    ));
                }
            );

            _inspectWorkingThread.Name = "worker thread for sync group " + _syncGroup.Name;
            
            _inspectWorkingThread.Start();
        }

        private void Grid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // TODO
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _cancel = true;

            _inspectWorkingThread.Abort();

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
