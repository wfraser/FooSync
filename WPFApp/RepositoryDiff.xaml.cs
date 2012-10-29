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
        private bool          _cancel;

        private Dictionary<Guid, FooTree> _trees;
        private FooChangeSet  _changeSet;
        private RepositoryDiffData _diffData;

        private Dictionary<Guid, ComboBox> _actionBoxes;
        private Dictionary<Guid, bool> _updatingActionBox;

        private static readonly int ProgressUpdateRateMsecs = 100;

        public event EventHandler Cancelled;

        public RepositoryDiff(MainWindow mainWindow, FooSyncEngine foo, SyncGroup syncGroup)
        {
            InitializeComponent();

            _actionBoxes = new Dictionary<Guid, ComboBox>();
            _mainWindow = mainWindow;
            _foo = foo;
            _syncGroup = syncGroup;
            _trees = null;
            _changeSet = null;
            _diffData = null;

            _updatingActionBox = new Dictionary<Guid, bool>();
        }

        public void Start()
        {
            _inspectWorkingThread = new Thread(
                delegate ()
                {
                    RepositoryStateCollection repoState;
                    _trees = new Dictionary<Guid, FooTree>();
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
                            tree = new FooTree(
                                MainWindow.Foo,
                                url.LocalPath,
                                FooSyncEngine.PrepareExceptions(_syncGroup.IgnorePatterns.OfType<IIgnorePattern>().ToList()),
                                new Progress((current, total, path) =>
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

                                    if (_trees[otherState.RepositoryID].Base.IsLocal)
                                    {
                                        otherState.Write(Path.Combine(_trees[otherState.RepositoryID].Base.LocalPath, FooSyncEngine.RepoStateFileName));
                                    }
                                    else
                                    {
                                        // client.UpdateState(otherState)
                                        throw new NotImplementedException();
                                    }
                                }
                            }

                            repoStates.Add(repoState);
                            _trees.Add(repoState.RepositoryID, tree);
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
                    _changeSet = _foo.Inspect(repoState, _trees, new Progress((current, total, name) =>
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

                    _changeSet.SetDefaultActions(_trees);

                    //
                    // Convert the changeset into data structures for display.
                    //

                    DictionaryItemPickerConverter converter = new DictionaryItemPickerConverter();

                    IEnumerable<string> fileOperations = EnumMethods.GetEnumDescriptions(typeof(FileOperation));

                    //foreach (KeyValuePair<Guid, FooTree> pair in trees)
                    IEnumerator<KeyValuePair<Guid, FooTree>> treeEnum = _trees.GetEnumerator();
                    for (int i = 0; treeEnum.MoveNext(); i++)
                    {
                        FooSyncUrl url = treeEnum.Current.Value.Base;
                        Guid repoId = treeEnum.Current.Key;

                        Dispatcher.Invoke(new Action(() =>
                            {
                                ((GridView)DiffGrid.View).Columns.Add(new GridViewColumn()
                                    {
                                        Header = string.Format("  {0}  ", url.IsLocal ? url.LocalPath : url.ToString()),
                                        DisplayMemberBinding = new Binding()
                                            {
                                                Converter = converter,
                                                ConverterParameter = repoId,
                                                Path = new PropertyPath("CombinedStatus"),
                                            },
                                    });

                                ActionsPanel.ColumnDefinitions.Add(new ColumnDefinition() { Width = GridLength.Auto });

                                Label header = new Label();
                                header.Content = string.Format("  {0}  ", url.IsLocal ? url.LocalPath : url.ToString());
                                ActionsPanel.Children.Add(header);
                                Grid.SetRow(header, 0);
                                Grid.SetColumn(header, i + 1);

                                ComboBox actionBox = new ComboBox();
                                actionBox.ItemsSource = fileOperations;
                                actionBox.SelectionChanged += new SelectionChangedEventHandler(actionBox_SelectionChanged);
                                _updatingActionBox.Add(repoId, false);
                                actionBox.Tag = repoId;
                                ActionsPanel.Children.Add(actionBox);
                                Grid.SetRow(actionBox, 1);
                                Grid.SetColumn(actionBox, i + 1);
                                _actionBoxes.Add(repoId, actionBox);
                            }
                        ));
                    }

                    _diffData = new RepositoryDiffData();
                    foreach (string filename in _changeSet)
                    {
                        RepositoryDiffDataItem item = new RepositoryDiffDataItem();
                        item.Filename = filename;
                        if (_changeSet[filename].ConflictStatus != ConflictStatus.NoConflict)
                        {
                            item.State = RepositoryDiffDataItem.ConflictState;
                        }
                        else
                        {
                            FooChangeSetElem changeElem = _changeSet[filename];

                            if (changeElem.ChangeStatus.Any(e => e.Value == ChangeStatus.New))
                            {
                                item.State = RepositoryDiffDataItem.AddedState;
                            }
                            else if (changeElem.ChangeStatus.Any(e => e.Value == ChangeStatus.Deleted))
                            {
                                item.State = RepositoryDiffDataItem.DeletedState;
                            }
                            else if (changeElem.ChangeStatus.Any(e => e.Value == ChangeStatus.Changed))
                            {
                                item.State = RepositoryDiffDataItem.ChangedState;
                            }
                        }

                        foreach (Guid repoId in _changeSet.RepositoryIDs)
                        {
                            item.ChangeStatus.Add(repoId, _changeSet[filename].ChangeStatus[repoId]);
                            item.FileOperation.Add(repoId, _changeSet[filename].FileOperation[repoId]);
                        }

                        _diffData.Add(item);
                    }

                    //
                    // Display
                    //

                    Dispatcher.Invoke(new Action(() =>
                        {
                            _mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                            ProgressView.Visibility = Visibility.Collapsed;
                            DiffGrid.ItemsSource = _diffData;
                            DiffGrid.Visibility = Visibility.Visible;
                            ActionsPanel.Visibility = Visibility.Visible;
                            //Actions.ItemsSource = diffData;
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
            foreach (RepositoryDiffDataItem file in _diffData)
            {
                string reason;
                if (!file.ActionsAreValid(out reason))
                {
                    MessageBox.Show(
                        string.Format("The highlighted file ({0}) has invalid actions selected:\n\n    {1}", file.Filename, reason),
                        "Invalid Actions",
                        MessageBoxButton.OK,
                        MessageBoxImage.Stop);
                    
                    DiffGrid.SelectedItem = file;
                    return;
                }
            }

            //
            // Update the FooChangeSet with the selected actions.
            //
            foreach (RepositoryDiffDataItem file in _diffData)
            {
                foreach (Guid repoId in _changeSet.RepositoryIDs)
                {
                    _changeSet[file.Filename].FileOperation[repoId] = file.FileOperation[repoId];
                }
            }

            Dictionary<Guid, FooSyncUrl> basePaths = new Dictionary<Guid, FooSyncUrl>();

            foreach (Guid repoId in _trees.Keys)
            {
                if (_trees[repoId].Base.AbsoluteUri.EndsWith("/"))
                {
                    basePaths.Add(repoId, _trees[repoId].Base);
                }
                else
                {
                    basePaths.Add(repoId, new FooSyncUrl(_trees[repoId].Base.ToString() + "/"));
                }
            }

            CopyEngine.PerformActions(_changeSet, basePaths, null, null);

            //
            // Need to update RepoState here
            //
        }

        private void DiffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Dictionary<Guid, FileOperation?> selectedOperation = new Dictionary<Guid, FileOperation?>();

            if (DiffGrid.SelectedItems.Count == 0)
            {
                foreach (Guid id in _actionBoxes.Keys)
                {
                    _updatingActionBox[id] = true;
                    _actionBoxes[id].SelectedIndex = -1;
                }

                return;
            }

            foreach (object item in DiffGrid.SelectedItems)
            {
                RepositoryDiffDataItem dataItem = item as RepositoryDiffDataItem;

                if (dataItem == null)
                {
                    System.Diagnostics.Debug.Assert(false, "this shouldn't happen");
                    continue;
                }

                foreach (Guid id in dataItem.FileOperation.Keys)
                {
                    if (!selectedOperation.ContainsKey(id))
                    {
                        selectedOperation.Add(id, dataItem.FileOperation[id]);
                    }
                    else if (selectedOperation[id] != dataItem.FileOperation[id])
                    {
                        selectedOperation[id] = null;
                    }
                }
            }

            foreach (Guid id in selectedOperation.Keys)
            {
                if (!_actionBoxes.ContainsKey(id))
                {
                    System.Diagnostics.Debug.Assert(false, "missing actionbox for a repoId!");
                    continue;
                }

                FileOperation? currentOp;
                if (_actionBoxes[id].SelectedIndex == -1)
                {
                    currentOp = null;
                }
                else
                {
                    currentOp = EnumMethods.GetEnumFromDescription<FileOperation>((string)_actionBoxes[id].SelectedItem);
                }

                if (currentOp != selectedOperation[id])
                {
                    _updatingActionBox[id] = true;
                    if (selectedOperation[id] == null)
                    {
                        _actionBoxes[id].SelectedIndex = -1;
                    }
                    else
                    {
                        _actionBoxes[id].SelectedItem = selectedOperation[id].GetDescription();
                    }
                }
            }
        }

        private void actionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Guid repoId = (Guid)((ComboBox)sender).Tag;

            if (_updatingActionBox[repoId])
            {
                _updatingActionBox[repoId] = false;
                return;
            }

            FileOperation? newOp = EnumMethods.GetEnumFromDescription<FileOperation>((string)((ComboBox)sender).SelectedItem);

            if (!newOp.HasValue)
            {
                return;
            }

            foreach (object item in DiffGrid.SelectedItems)
            {
                RepositoryDiffDataItem dataItem = item as RepositoryDiffDataItem;

                if (dataItem == null)
                {
                    continue;
                }

                dataItem.FileOperation[repoId] = newOp.Value;
            }
        }
    }
}
