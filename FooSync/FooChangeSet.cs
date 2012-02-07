using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FooSync
{
    public class FooChangeSet
    {
        public FooChangeSet(FooSyncEngine foo)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSync"),
                "Don't directly instantiate FooClasses");

            this.Foo = foo;
            this.Elems = new Dictionary<string, FooChangeSetElem>();
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
            }
        }

        public IEnumerable<string> Conflicts
        {
            get
            {
                return from e in Elems
                       where e.Value.ConflictStatus != ConflictStatus.NoConflict
                          || e.Value.ConflictStatus != ConflictStatus.Undetermined
                       select e.Key;
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

        public IEnumerator<string> GetEnumerator()
        {
            return Elems.Keys.GetEnumerator();
        }

        public FooChangeSetElem this[string filename]
        {
            get { return Elems[filename]; }
            set { Elems[filename] = value; }
        }

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
