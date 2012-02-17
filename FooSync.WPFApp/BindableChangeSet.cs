using System;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace FooSync.WPFApp
{
    public class BindableChangeSet : IEnumerable<BindableChangeSetElem>, INotifyCollectionChanged
    {
        public BindableChangeSet(FooChangeSet changeset, IEnumerable<string> keys, FooTree repository, FooTree source)
        {
            _changeset = changeset;
            _keys = keys;
            _repo = repository;
            _source = source;

            _changeset.CollectionChanged += ChangesetChanged;
        }

        public IEnumerator<BindableChangeSetElem> GetEnumerator()
        {
            return new Enumerator(_changeset, _keys.GetEnumerator(), _repo, _source);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void ChangesetChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        private FooChangeSet        _changeset;
        private IEnumerable<string> _keys;
        private FooTree _repo;
        private FooTree _source;

        private class Enumerator : IEnumerator<BindableChangeSetElem>
        {
            public Enumerator(FooChangeSet changeset, IEnumerator<string> keyEnumerator, FooTree repository, FooTree source)
            {
                _changeset = changeset;
                _key = keyEnumerator;
                _repo = repository;
                _source = source;
            }

            public BindableChangeSetElem Current
            {
                get
                {
                    return new BindableChangeSetElem
                    { 
                        Filename = _changeset[_key.Current].Filename,
                        RepositoryDate = (_repo.Files.ContainsKey(_key.Current)) ? (DateTime?)_repo.Files[_key.Current].MTime : null,
                        SourceDate = (_source.Files.ContainsKey(_key.Current)) ? (DateTime?)_source.Files[_key.Current].MTime : null,
                        Action = _changeset[_key.Current].FileOperation,
                    };
                }
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _key.MoveNext();
            }

            public void Reset()
            {
                _key.Reset();
            }

            public void Dispose()
            {
                _key.Dispose();
            }

            private FooChangeSet _changeset;
            private IEnumerator<string> _key;
            private FooTree _repo;
            private FooTree _source;
        }
    }

    public class BindableChangeSetElem
    {
        public string Filename { get; set; }
        public DateTime? RepositoryDate { get; set; }
        public DateTime? SourceDate { get; set; }
        public FileOperation Action { get; set; }
    }
}
