///
/// Codewise/FooSync/WPFApp/BindableChangeSet.cs
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

namespace Codewise.FooSync.WPFApp
{
    /// <summary>
    /// BindableChangeSet
    /// 
    /// Allows binding to a sub-group of elements in a FooChangeSet, and being notified when they change.
    /// </summary>
    public class BindableChangeSet : IEnumerable<BindableChangeSetElem>, INotifyCollectionChanged, INotifyPropertyChanged
    {
        /// <summary>
        /// Create a BindableChangeSet
        /// </summary>
        /// <param name="changeset">The full FooChangeSet backing store.</param>
        /// <param name="predicate">Filtering predicate of which elements to present.</param>
        /// <param name="repository">FooTree of the repository</param>
        /// <param name="source">FooTree of the source</param>
        public BindableChangeSet(FooChangeSet changeset, Func<FooChangeSetElem, bool> predicate, FooTree repository, FooTree source)
        {
            _changeset = changeset;
            _predicate = predicate;
            _repo = repository;
            _source = source;

            _changeset.CollectionChanged += ChangesetChanged;
        }

        /// <summary>
        /// Get a new enumerator over the changeset.
        /// Re-evaluates the filtering predicate given in the constructor.
        /// </summary>
        /// <returns>a new IEnumerator over BindableChangeSetElem objects</returns>
        public IEnumerator<BindableChangeSetElem> GetEnumerator()
        {
            return new Enumerator(_changeset, _changeset.Where(_predicate).GetEnumerator(), _repo, _source);
        }

        /// <summary>
        /// Get a new enumerator over the changeset.
        /// Re-evaluates the filtering predicate given in the constructor.
        /// </summary>
        /// <returns>a new IEnumerator over BindableChangeSetElem objects</returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Used to notify our observers when the underlying FooChangeSet has been altered.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangesetChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, e);
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(string.Empty));
            }
        }

        /// <summary>
        /// Get the number of elements in the changeset that satisfy the filtering predicate.
        /// </summary>
        public int Count
        {
            get
            {
                int count = 0;
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Return a string summarizing the sub-group of the changeset satisfying the filtering predicate.
        /// 
        /// The format is "# files; #.## (K/M/G/T)B", with the size omitted if there are no files, or they all have NoOp as an action.
        /// </summary>
        public string Summary
        {
            get
            {
                int count = 0;
                double bytes = 0.0;
                bool haveBytes = false; // if all files have file operation NoOp, this will be left false.

                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        var file = enumerator.Current;
                        count++;

                        switch (file.Action)
                        {
                            case FileOperation.DeleteRepo:
                            case FileOperation.UseRepo:
                                bytes += _repo.Files[file.Filename].Size;
                                haveBytes = true;
                                break;

                            case FileOperation.DeleteSource:
                            case FileOperation.UseSource:
                                bytes += _source.Files[file.Filename].Size;
                                haveBytes = true;
                                break;
                        }
                    }
                }
                
                var ret = string.Format("{0} files", count);
                if (count > 0 && haveBytes)
                {
                    var unit = "B";
                    var prefixes = new string[] { "K", "M", "G", "T" };
                    for (var i = prefixes.Length - 1; i >= 0; i--)
                    {
                        var unitSize = (long)Math.Pow(10, 3 * (i + 1));

                        if (bytes >= unitSize)
                        {
                            unit = prefixes[i] + unit;
                            bytes /= unitSize;
                            break;
                        }
                    }

                    ret += string.Format("; {0:#.##} {1}", bytes, unit);
                }

                return ret;
            }
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler         PropertyChanged;
        
        private FooChangeSet                 _changeset;
        private Func<FooChangeSetElem, bool> _predicate;
        private FooTree                      _repo;
        private FooTree                      _source;

        /// <summary>
        /// Internal enumerator class.
        /// </summary>
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
                    return new BindableChangeSetElem(
                        _changeset,
                        _changeset[_key.Current].Filename,
                        _repo.Files.ContainsKey(_key.Current) ? (DateTime?)_repo.Files[_key.Current].MTime : null,
                        _source.Files.ContainsKey(_key.Current) ? (DateTime?)_source.Files[_key.Current].MTime : null
                        );
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

    /// <summary>
    /// An element of the changeset.
    /// </summary>
    public class BindableChangeSetElem
    {
        /// <summary>
        /// Create a new bindable changeset element.
        /// </summary>
        /// <param name="changeset">The full FooChangeSet</param>
        /// <param name="filename">Filename of the element</param>
        /// <param name="repoDate">Modification date/time of the file in the repository, or null if one doesn't exist</param>
        /// <param name="sourceDate">Modification date/time of the file in the source, or null if one doesn't exist</param>
        public BindableChangeSetElem(FooChangeSet changeset, string filename, DateTime? repoDate, DateTime? sourceDate)
        {
            _changeset  = changeset;
            _filename   = filename;
            _repoDate   = repoDate;
            _sourceDate = sourceDate;
        }

        public string    Filename       { get { return _filename; } }
        public DateTime? RepositoryDate { get { return _repoDate; } }
        public DateTime? SourceDate     { get { return _sourceDate; } }

        /// <summary>
        /// Get/Set the FileOperation on the element.
        /// Setting it causes the changeset to advise its observers that it has been completely reset,
        /// causing any enumerators to be invalid.
        /// (This is needed because changing the file operation could cause filtering predicates on the BindableChangeSet to select
        /// different elements, so a refresh of the enumerators is needed.)
        /// </summary>
        public FileOperation Action
        {
            get
            {
                return _changeset[_filename].FileOperation;
            }
            set
            {
                _changeset[_filename].FileOperation = value;
                _changeset.AdviseChanged();
            }
        }

        /// <summary>
        /// Get/Set the ConflictStatus on the element.
        /// Setting it causes the changeset to advise its observers that it has been completely reset,
        /// causing any enumerators to be invalid.
        /// (This is needed because changing the conflict status could cause filtering predicates on the BindableChangeSet to select
        /// different elements, so a refresh of the enumerators is needed.)
        /// </summary>
        public ConflictStatus ConflictStatus
        {
            get
            {
                return _changeset[_filename].ConflictStatus;
            }
            set
            {
                _changeset[_filename].ConflictStatus = value;
                _changeset.AdviseChanged();
            }
        }

        private FooChangeSet _changeset;
        private string       _filename;
        private DateTime?    _repoDate;
        private DateTime?    _sourceDate;
    }
}
