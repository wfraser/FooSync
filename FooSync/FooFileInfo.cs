using System;
using System.IO;
using System.Security.Cryptography;

namespace FooSync
{
    public class FooFileInfo
    {
        static MD5 Hasher = MD5.Create();

        internal FooFileInfo(FooSyncEngine foo, string path)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSync"),
                "Don't directly instantiate FooClasses");
            
            this.Foo = foo;
            this.Path = path;
            this.Source = "";
        }

        #region public methods 

        public int CompareTo(FooFileInfo other)
        {
            if (other == null)
                throw new ArgumentNullException("other");

            if (this.MTime != other.MTime)
            {
                if (Foo.Options.ComputeHashes && (this.Hash == other.Hash))
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

        #endregion

        #region public properties

        public string Hash
        {
            get
            {
                if (_hash == null)
                {
                    using (FileStream f = Info.OpenRead())
                    {
                        byte[] bytes = Hasher.ComputeHash(f);
                        _hash = "";
                        foreach (byte b in bytes)
                        {
                            _hash += string.Format("{0:X2}", b);
                        }
                    }
                }

                return _hash;
            }
        }

        public DateTime MTime
        {
            get
            {
                Info.Refresh();
                return Info.LastWriteTimeUtc;
            }
        }

        public long Size
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

        public string         Source         { get; set; }
        public string         Path           { get; private set; }

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

        private FooSyncEngine Foo { get; set; }

        #endregion

        #region private members

        private FileInfo _info;
        private string _hash;

        #endregion
    }
}
