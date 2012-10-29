///
/// Codewise/FooSync/FooChangeSet.cs
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
using System.Linq;
using System.Text;

namespace Codewise.FooSync
{
    public class FooChangeSet : INotifyCollectionChanged
    {
        public FooChangeSet(IEnumerable<Guid> repositoryIds)
        {
            this.Elems = new Dictionary<string, FooChangeSetElem>();
            this.RepositoryIDs = repositoryIds;
        }

        private void SetConflictStatus(string filename, ConflictStatus status)
        {
            Elems[filename].ConflictStatus = status;
        }

        public void SetDefaultActions(Dictionary<Guid, FooTree> trees)
        {
            //
            // Detect Conflicts
            //
            foreach (string filename in Elems.Keys)
            {
                ChangeStatus change = ChangeStatus.Undetermined;
                DateTime? mTime = null;

                foreach (Guid repoId in RepositoryIDs)
                {
                    switch (Elems[filename].ChangeStatus[repoId])
                    {
                        case ChangeStatus.Changed:
                            {
                                DateTime thisMTime = trees[repoId].Files[filename].MTime;
                                if (mTime != null && mTime != thisMTime)
                                {
                                    SetConflictStatus(filename, ConflictStatus.MultipleChanges);
                                    goto nextFile;
                                }
                                mTime = thisMTime;
                            }
                            break;

                        case ChangeStatus.Deleted:
                            if (change == ChangeStatus.Undetermined)
                            {
                                change = ChangeStatus.Deleted;
                            }
                            else if (change != ChangeStatus.Deleted)
                            {
                                SetConflictStatus(filename, ConflictStatus.ChangedAndDeleted);
                                goto nextFile;
                            }
                            break;

                        case ChangeStatus.New:
                            {
                                if (change == ChangeStatus.Undetermined)
                                {
                                    change = ChangeStatus.New;
                                }
                                else if (change != ChangeStatus.New)
                                {
                                    SetConflictStatus(filename, ConflictStatus.ChangedAndDeleted);
                                    goto nextFile;
                                }

                                DateTime thisMTime = trees[repoId].Files[filename].MTime;
                                if (mTime != null && mTime != thisMTime)
                                {
                                    SetConflictStatus(filename, ConflictStatus.MultipleChanges);
                                    goto nextFile;
                                }
                                mTime = thisMTime;
                            }
                            break;

                        case ChangeStatus.Identical:
                            {
                                DateTime thisMTime = trees[repoId].Files[filename].MTime;
                                if (mTime != null && mTime != thisMTime)
                                {
                                    SetConflictStatus(filename, ConflictStatus.MultipleChanges);
                                    goto nextFile;
                                }
                                mTime = thisMTime;
                            }
                            break;
                    }
                }

                SetConflictStatus(filename, ConflictStatus.NoConflict);

            nextFile: ;
            }

            //
            // Set actions
            //
            foreach (string filename in Elems.Keys)
            {
                if (Elems[filename].ConflictStatus != ConflictStatus.NoConflict)
                {
                    continue;
                }
                    
                foreach (Guid repoId in RepositoryIDs)
                {
                    switch (Elems[filename].ChangeStatus[repoId])
                    {
                        case ChangeStatus.Changed:
                            {
                                if (Elems[filename].FileOperation[repoId] != FileOperation.NoOp)
                                {
                                    //
                                    // action already set
                                    //

                                    continue;
                                }

                                //
                                // Figure out the mtime of the newest copy of this file in all the repos
                                //

                                DateTime newestMTime = DateTime.MinValue;
                                foreach (Guid id in (from pair in trees
                                                        where pair.Value.Files.ContainsKey(filename)
                                                        select pair.Key))
                                {
                                    DateTime mtime = trees[id].Files[filename].MTime;
                                    if (mtime > newestMTime)
                                    {
                                        newestMTime = mtime;
                                    }
                                }

                                foreach (Guid id in trees.Keys)
                                {
                                    //
                                    // If it's the most recent, give, otherwise, take.
                                    //

                                    if (trees[id].Files.ContainsKey(filename)
                                        && trees[id].Files[filename].MTime == newestMTime)
                                    {
                                        Elems[filename].FileOperation[repoId] = FileOperation.Source;
                                    }
                                    else
                                    {
                                        Elems[filename].FileOperation[repoId] = FileOperation.Destination;
                                    }
                                }
                            }
                            break;

                        case ChangeStatus.New:
                            Elems[filename].FileOperation[repoId] = FileOperation.Source;
                            break;

                        case ChangeStatus.Missing:
                            Elems[filename].FileOperation[repoId] = FileOperation.Destination;

                            //
                            // Pick the newest copy of the file as the source.
                            //
                            Guid sourceRepo = RepositoryIDs.Where(id => 
                                id != repoId
                                && trees[id].Files.ContainsKey(filename)
                                && Elems[filename].ChangeStatus[id] == ChangeStatus.Identical
                                ).OrderByDescending(id => trees[id].Files[filename].MTime).FirstOrDefault();
                            if (sourceRepo != null)
                            {
                                Elems[filename].FileOperation[sourceRepo] = FileOperation.Source;
                            }
                            break;

                        case ChangeStatus.Deleted:
                            foreach (Guid id in Elems[filename].FileOperation.Keys)
                            {
                                if (id == repoId)
                                {
                                    Elems[filename].FileOperation[id] = FileOperation.NoOp;
                                }
                                else
                                {
                                    Elems[filename].FileOperation[id] = FileOperation.Delete;
                                }
                            }
                            break;

                        case ChangeStatus.Identical:
                            Elems[filename].FileOperation[repoId] = FileOperation.NoOp;
                            break;

                        default:
                            System.Diagnostics.Debug.Assert(false, "Invalid change status!");
                            break;
                    }
                } //foreach repoId
            }
        }

        public void Add(string filename, ChangeStatus changeStatus, Guid where)
        {
            lock (((System.Collections.ICollection)Elems).SyncRoot)
            {
                if (!Elems.ContainsKey(filename))
                {
                    Elems.Add(filename, new FooChangeSetElem(filename, RepositoryIDs));
                }

                if (Elems[filename].ChangeStatus[where] != changeStatus)
                {
                    Elems[filename].ChangeStatus[where] = changeStatus;

                    if (CollectionChanged != null)
                    {
                        CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, filename));
                    }
                }
            }
        }

        /// <summary>
        /// Gets filenames where the given predicate (applied to FooChangeSetElem objects) returns
        /// true at least once.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns>Enumerable of filenames</returns>
        public IEnumerable<string> Where(Func<FooChangeSetElem, bool> predicate)
        {
            return from e in Elems
                   where predicate(e.Value)
                   select e.Key;
        }

        public int Count()
        {
            return Elems.Values.Count();
        }

        public IEnumerable<string> Conflicts
        {
            get
            {
                return from e in Elems
                       where e.Value.ConflictStatus != ConflictStatus.NoConflict
                       select e.Key;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Elems.Keys.GetEnumerator();
        }

        public FooChangeSetElem this[string filename]
        {
            get { return Elems[filename]; }
            set { Elems[filename] = value; }
        }

        public IEnumerable<string> Filenames
        {
            get { return Elems.Keys; }
        }

        public void AdviseChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public IEnumerable<Guid> RepositoryIDs { get; private set; }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private Dictionary<string, FooChangeSetElem> Elems;
    }

    public class FooChangeSetElem
    {
        internal FooChangeSetElem(string filename, IEnumerable<Guid> repositoryIds)
        {
            Filename = filename;
            ConflictStatus = FooSync.ConflictStatus.Undetermined;
            ChangeStatus = new Dictionary<Guid, ChangeStatus>();
            FileOperation = new Dictionary<Guid, FileOperation>();

            foreach (Guid repoId in repositoryIds)
            {
                ChangeStatus.Add(repoId, FooSync.ChangeStatus.Undetermined);
                FileOperation.Add(repoId, FooSync.FileOperation.NoOp);
            }
        }

        public string                          Filename       { get; set; }
        public ConflictStatus                  ConflictStatus { get; set; }
        public Dictionary<Guid, ChangeStatus>  ChangeStatus   { get; private set; }
        public Dictionary<Guid, FileOperation> FileOperation  { get; private set; }
    }
}
