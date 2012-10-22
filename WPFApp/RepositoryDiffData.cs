///
/// Codewise/FooSync/WPFApp/RepositoryDiffData.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Codewise.FooSync.WPFApp
{
    public class RepositoryDiffData : List<RepositoryDiffDataItem>
    {
    }

    public class RepositoryDiffDataItem : INotifyPropertyChanged
    {
        //maybe make these an enum with descriptions or something?
        public static readonly string ConflictState         = "Conflict";
        public static readonly string ResolvedState         = "Resolved";
        public static readonly string InvalidActionsState   = "Invalid Actions";
        public static readonly string AddedState            = "Added";
        public static readonly string ChangedState          = "Changed";
        public static readonly string DeletedState          = "Deleted";

        public event PropertyChangedEventHandler PropertyChanged;

        public RepositoryDiffDataItem()
        {
            ChangeStatus = new ObservableDictionary<Guid, ChangeStatus>();
            FileOperation = new ObservableDictionary<Guid, FileOperation>();
            CombinedStatus = new Dictionary<Guid, string>();

            ChangeStatus.CollectionChanged += new NotifyCollectionChangedEventHandler(ChangeStatus_CollectionChanged);
            FileOperation.CollectionChanged += new NotifyCollectionChangedEventHandler(FileOperation_CollectionChanged);

            _actionsAreValid = false;
            _state = string.Empty;
        }

        private string _state;
        private bool _actionsAreValid;

        public string State
        {
            get
            {
                if (_actionsAreValid)
                {
                    return _state;
                }
                else
                {
                    return InvalidActionsState;
                }
            }

            set
            {
                _state = value;
            }
        }

        public string Filename { get; set; }
        public ObservableDictionary<Guid, ChangeStatus> ChangeStatus { get; private set; }
        public ObservableDictionary<Guid, FileOperation> FileOperation { get; private set; }
        public Dictionary<Guid, string> CombinedStatus { get; private set; }

        void FileOperation_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    CombinedStatus.Clear();
                    break;

                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        Guid id = (Guid)item;
                        CombinedStatus.Add(id, ChangeStatus[id].GetDescription() + " / " + FileOperation[id].GetDescription());
                    }
                    break;
                    
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.NewItems)
                    {
                        Guid id = ((KeyValuePair<Guid, FileOperation>)item).Key;
                        CombinedStatus[id] = ChangeStatus[id].GetDescription() + " / " + FileOperation[id].GetDescription();
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.NewItems)
                    {
                        Guid id = (Guid)item;
                        CombinedStatus.Remove(id);
                    }
                    break;
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("FileOperation"));
                PropertyChanged(this, new PropertyChangedEventArgs("CombinedStatus"));
            }

            bool valid = ActionsAreValid();
            if (valid != _actionsAreValid)
            {
                if (e.Action != NotifyCollectionChangedAction.Add && valid && _state == ConflictState)
                {
                    //
                    // Was in conflict, was incomplete, is now no longer in conflict. :)
                    //
                    _state = ResolvedState;
                }

                _actionsAreValid = valid;

                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("State"));
                }
            }
        }

        void ChangeStatus_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs("ChangeStatus"));
                PropertyChanged(this, new PropertyChangedEventArgs("CombinedStatus"));
            }
        }

        bool ActionsAreValid()
        {
            bool hasSource = false;
            bool hasDest = false;

            foreach (FileOperation fileOp in FileOperation.Values)
            {
                if (fileOp == FooSync.FileOperation.Source)
                {
                    if (hasSource)
                    {
                        // can only have one source
                        return false;
                    }
                    hasSource = true;
                }
                else if (fileOp == FooSync.FileOperation.Destination)
                {
                    hasDest = true;
                }
            }

            return (!(hasSource ^ hasDest));
        }
    }
}
