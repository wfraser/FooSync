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
        public FooChangeSet()
        {
            this.Elems = new Dictionary<string, Dictionary<Guid, FooChangeSetElem>>();
        }

        public void SetDefaultActions(Dictionary<Guid, FooTree> trees)
        {
            foreach (string filename in Elems.Keys)
            {
                foreach (Guid repoId in Elems[filename].Keys)
                {
                    if (Elems[filename][repoId].ConflictStatus == ConflictStatus.NoConflict)
                    {
                        switch (Elems[filename][repoId].ChangeStatus)
                        {
                            case ChangeStatus.Changed:
                                {
                                    if (Elems[filename][repoId].FileOperation != FileOperation.NoOp)
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
                                            Elems[filename][repoId].FileOperation = FileOperation.Give;
                                        }
                                        else
                                        {
                                            Elems[filename][repoId].FileOperation = FileOperation.Take;
                                        }
                                    }
                                }
                                break;

                            case ChangeStatus.New:
                                Elems[filename][repoId].FileOperation = FileOperation.Give;
                                break;

                            case ChangeStatus.Missing:
                                Elems[filename][repoId].FileOperation = FileOperation.Take;
                                break;

                            case ChangeStatus.Deleted:
                                Elems[filename][repoId].FileOperation = FileOperation.DeleteOthers;
                                break;

                            case ChangeStatus.Identical:
                                Elems[filename][repoId].FileOperation = FileOperation.NoOp;
                                break;

                            default:
                                System.Diagnostics.Debug.Assert(false, "Invalid change status!");
                                break;
                        }
                    }
                }
            }
        }

        public void Add(string filename, ChangeStatus changeStatus, Guid where)
        {
            lock (((System.Collections.ICollection)Elems).SyncRoot)
            {
                if (!Elems.ContainsKey(filename))
                {
                    Elems.Add(filename, new Dictionary<Guid, FooChangeSetElem>());
                }

                if (!Elems[filename].ContainsKey(where))
                {
                    Elems[filename].Add(where, new FooChangeSetElem
                    {
                        Filename = filename,
                        ChangeStatus = changeStatus,
                        RepositoryId = where,
                        ConflictStatus = ConflictStatus.Undetermined,
                        FileOperation = FileOperation.NoOp,
                    });

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
                   where e.Value.Count(x => predicate(x.Value)) > 0
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
                       where e.Value.Values.Count(elem => elem.ConflictStatus != ConflictStatus.NoConflict) > 0
                       select e.Key;
            }
        }

        public IEnumerator<string> GetEnumerator()
        {
            return Elems.Keys.GetEnumerator();
        }

        public Dictionary<Guid, FooChangeSetElem> this[string filename]
        {
            get { return Elems[filename]; }
            set { Elems[filename] = value; }
        }

        public FooChangeSetElem this[string filename, Guid repoId]
        {
            get { return Elems[filename][repoId]; }
            set { Elems[filename][repoId] = value; }
        }

        public IEnumerable<string> Filenames
        {
            get { return Elems.Keys; }
        }

        public IEnumerable<Guid> RepoIDs
        {
            get
            {
                return Elems.Values
                       .SelectMany(elem => elem.Keys)
                       .Distinct();
            }
        }

        public void AdviseChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private Dictionary<string, Dictionary<Guid, FooChangeSetElem>> Elems;
    }

    public class FooChangeSetElem
    {
        public string         Filename       { get; set; }
        public Guid           RepositoryId   { get; set; }
        public ChangeStatus   ChangeStatus   { get; set; }
        public ConflictStatus ConflictStatus { get; set; }
        public FileOperation  FileOperation  { get; set; }
    }
}
