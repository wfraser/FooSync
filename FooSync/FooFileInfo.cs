///
/// Codewise/FooSync/FooFileInfo.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.IO;
using System.Security.Cryptography;

namespace Codewise.FooSync
{
    public class FooFileInfo : FooFileInfoBase
    {
        static MD5 Hasher = MD5.Create();

        internal FooFileInfo()
        {
        }

        public FooFileInfo(FooSyncEngine foo, string path)
        {
            this.Foo = foo;
            this.Path = path;
            this.Source = string.Empty;
        }

        #region public properties

        public override string Hash
        {
            get
            {
                if (_hash == null)
                {
                    using (FileStream f = Info.OpenRead())
                    {
                        byte[] bytes = Hasher.ComputeHash(f);
                        _hash = string.Empty;
                        foreach (byte b in bytes)
                        {
                            _hash += string.Format("{0:X2}", b);
                        }
                    }
                }

                return _hash;
            }
        }

        public override DateTime MTime
        {
            get
            {
                Info.Refresh();
                return Info.LastWriteTimeUtc;
            }
        }

        public override long Size
        {
            get
            {
                Info.Refresh();
                return Info.Length;
            }
        }

        public string FullPath
        {
            get
            {
                return Info.FullName;
            }
        }

        #endregion

        #region private properties

        private FileInfo Info
        {
            get
            {
                if (_info == null)
                {
                    _info = new FileInfo(Path);
                }

                return _info;
            }
        }

        #endregion

        #region private members

        private FileInfo _info;
        private string _hash;

        #endregion
    }

    /// <summary>
    /// A FooFileInfo that is not backed by a file, but instead by explicitly set properties.
    /// For use by FooTree.Unserialize()
    /// </summary>
    public class FooFileInfoBase
    {
        public FooFileInfoBase()
        {
            Foo = null;
        }

        public FooFileInfoBase(FooSyncEngine foo)
        {
            Foo = foo;
        }

        public int CompareTo(FooFileInfoBase other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            if (this.MTime != other.MTime)
            {
                if (Foo != null && Foo.Options.ComputeHashes && (this.Hash == other.Hash))
                {
                    return 0;
                }
                else
                {
                    return this.MTime.CompareTo(other.MTime);
                }
            }
            else
            {
                return 0;
            }
        }

        public string Path { get; internal set; }
        public string Source { get; internal set; }
        public virtual DateTime MTime { get; internal set; }
        public virtual long Size { get; internal set; }
        public virtual string Hash { get { throw new NotImplementedException("Can't get hash from a FooFileInfoBase"); } }

        protected FooSyncEngine Foo { get; set; }
    }
}
