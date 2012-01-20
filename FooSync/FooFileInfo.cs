using System;
using System.IO;
using System.Security.Cryptography;

namespace FooSync
{
    public class FooFileInfo : IComparable<FooFileInfo>
    {
        internal FooFileInfo(FooSync foo, string path)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSync"),
                "Don't directly instantiate FooClasses");
            
            this.Foo = foo;
            this.Path = path;
            this.Identical = false;
        }

        public int CompareTo(FooFileInfo other)
        {
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

        public bool Identical { get; set; }

        public string Path { get; private set; }

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
                return Info.LastWriteTimeUtc;
            }
        }

        public long Size
        {
            get
            {
                return Info.Length;
            }
        }

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

        static MD5 Hasher = MD5.Create();

        private FooSync Foo { get; set; }

        private FileInfo _info;
        private string _hash;
    }
}
