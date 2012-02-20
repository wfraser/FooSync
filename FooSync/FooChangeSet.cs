using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace FooSync
{
    public class FooChangeSet : INotifyCollectionChanged
    {
        public FooChangeSet(FooSyncEngine foo)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSyncEngine"),
                "Don't directly instantiate FooClasses");

            this.Foo = foo;
            this.Elems = new Dictionary<string, FooChangeSetElem>();
        }

        public void SetDefaultActions()
        {
            foreach (var filename in Elems.Keys)
            {
                if (Elems[filename].ConflictStatus == ConflictStatus.NoConflict)
                {
                    switch (Elems[filename].ChangeStatus)
                    {
                        case ChangeStatus.Newer:
                        case ChangeStatus.RepoMissing:
                            Elems[filename].FileOperation = FileOperation.UseSource;
                            break;

                        case ChangeStatus.Older:
                        case ChangeStatus.SourceMissing:
                            Elems[filename].FileOperation = FileOperation.UseRepo;
                            break;

                        case ChangeStatus.RepoDeleted:
                            Elems[filename].FileOperation = FileOperation.DeleteSource;
                            break;

                        case ChangeStatus.SourceDeleted:
                            Elems[filename].FileOperation = FileOperation.DeleteRepo;
                            break;

                        default:
                            System.Diagnostics.Debug.Assert(false, "Invalid change status!");
                            break;
                    }
                }
            }
        }

        public void Add(string filename, ChangeStatus changeStatus)
        {
            lock (((System.Collections.ICollection)Elems).SyncRoot)
            {
                Elems.Add(filename, new FooChangeSetElem
                {
                    Filename = filename,
                    ChangeStatus = changeStatus,
                    ConflictStatus = ConflictStatus.Undetermined,
                    FileOperation = FileOperation.NoOp,
                });

                if (CollectionChanged != null)
                {
                    CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, filename));
                }
            }
        }

        public IEnumerable<string> WithFileOperation(FileOperation oper)
        {
            return from e in Elems
                   where e.Value.FileOperation == oper
                   select e.Key;
        }

        public IEnumerable<string> Where(Func<FooChangeSetElem, bool> predicate)
        {
            return from e in Elems
                   where predicate(e.Value)
                   select e.Key;
        }

        public int Count(Func<FooChangeSetElem, bool> predicate)
        {
            return Elems.Values.Count(predicate);
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

        public void AdviseChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private FooSyncEngine Foo;
        private Dictionary<string, FooChangeSetElem> Elems;
    }

    public class FooChangeSetElem
    {
        public string         Filename       { get; set; }
        public ChangeStatus   ChangeStatus   { get; set; }
        public ConflictStatus ConflictStatus { get; set; }
        public FileOperation  FileOperation  { get; set; }
    }
}
