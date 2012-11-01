///
/// Codewise/FooSync/RepositoryState.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.IO;

namespace Codewise.FooSync
{
    public class RepositoryStateCollection
    {
        public RepositoryStateCollection()
        {
            Modified = DateTime.Now;
            Dirty = true;
            Repositories = new Dictionary<Guid, RepositoryState>();
            Origin  = new Dictionary<string, Guid>();
            RepositoryID = Guid.NewGuid();
            SourcePath = "???";
        }

        public RepositoryStateCollection(string stateFilename)
        {
            Repositories = new Dictionary<Guid, RepositoryState>();
            Origin = new Dictionary<string, Guid>();

            using (var r = new StreamReader(stateFilename, System.Text.Encoding.UTF8))
            {
                Read(r);
            }

            SourcePath = stateFilename;
        }

        public RepositoryStateCollection(Stream stream)
        {
            Repositories = new Dictionary<Guid, RepositoryState>();
            Origin = new Dictionary<string, Guid>();

            using (StreamReader r = new StreamReader(stream, System.Text.Encoding.UTF8))
            {
                Read(r);
            }

            SourcePath = "???";
        }

        #region public methods

        public void AddRepository(FooTree tree, Guid ID)
        {
            if (tree == null)
                throw new ArgumentNullException("tree");
            if (ID == null)
                throw new ArgumentException("ID");

            Dirty = true;
            Modified = DateTime.Now;

            RepositoryState repository = new RepositoryState();
            repository.ID = ID;

            foreach (var file in tree.Files)
            {
                string filename = file.Key;
                
                repository.MTimes.Add(filename, file.Value.MTime);

                if (ID == RepositoryID)
                {
                    Origin.Add(filename, RepositoryID);
                }
            }

            Repositories.Add(ID, repository);
        }

        public void Read(StreamReader r)
        {
            var current = new RepositoryState();
            Guid otherRepoId;
            Guid? origin = null;
            string filename;
            DateTime mtime;

            string guidString = ReadString(r);
            RepositoryID = new Guid(guidString);
            
            Modified = new DateTime(long.Parse(ReadString(r)), DateTimeKind.Utc);

            while (!r.EndOfStream)
            {
                otherRepoId = new Guid(ReadString(r));
                while (r.Peek() != 0)
                {
                    filename = ReadString(r);

                    //
                    // Replace forward-slashes with the system directory separator character.
                    //
                    if (Path.DirectorySeparatorChar != '/')
                    {
                        filename = filename.Replace('/', Path.DirectorySeparatorChar);
                    }

                    if (otherRepoId == RepositoryID)
                    {
                        origin = new Guid(ReadString(r));
                    }

                    mtime = DateTime.FromFileTimeUtc(long.Parse(ReadString(r)));

                    current.MTimes.Add(filename, mtime);

                    if (origin.HasValue)
                    {
                        Origin.Add(filename, origin.Value);
                        origin = null;
                    }
                }

                current.ID = otherRepoId;
                Repositories.Add(otherRepoId, current);
                current = new RepositoryState();

                r.Read();
            }

            Dirty = false;
        }

        public void Write(string filename)
        {
            using (var w = new StreamWriter(filename, false, System.Text.Encoding.UTF8))
            {
                Write(w);
            }
        }

        public void Write(StreamWriter w)
        {
            w.Write(RepositoryID.ToString());
            w.Write('\0');

            w.Write(Modified.ToUniversalTime().Ticks);
            w.Write('\0');

            foreach (var repo in Repositories.Values)
            {
                w.Write(repo.ID.ToString());
                w.Write('\0');

                foreach (var mtime in repo.MTimes)
                {
                    string filename = mtime.Key;

                    //
                    // Normalize the path to use forward slashes instead of whatever
                    //  the system normally uses.
                    // Rationale: Unix filenames can contain backslashes, but
                    //  Windows filenames can't contain forward slashes, so
                    //  forward slashes win.
                    //
                    if (Path.DirectorySeparatorChar != '/')
                    {
                        filename = filename.Replace(Path.DirectorySeparatorChar, '/');
                    }

                    w.Write(filename);
                    w.Write('\0');

                    if (repo.ID == RepositoryID)
                    {
                        w.Write(Origin.ContainsKey(mtime.Key) ? Origin[mtime.Key] : RepositoryID);
                        w.Write('\0');
                    }

                    w.Write(mtime.Value.ToFileTimeUtc());
                    w.Write('\0');
                }

                w.Write('\0');
            }

            Dirty = false;
        }

        private static string ReadString(StreamReader r)
        {
            var buf = new List<char>();
            int c;
            while (-1 != (c = r.Read()))
            {
                if (c == 0)
                {
                    break;
                }
                else
                {
                    buf.Add((char)c);
                }
            }

            return new string(buf.ToArray());
        }

        #endregion

        #region public properties

        public Guid RepositoryID { get; set; }
        public DateTime Modified { get; set; }

        //
        // These are not serialized into the state file.
        //
        public bool Dirty { get; set; }
        public string SourcePath { get; set; }

        public string ToString()
        {
            return string.Format("{0} from {1}", this.GetType().Name, SourcePath);
        }

        /// <summary>
        /// Gets the RepositoryState for the repository itself.
        /// </summary>
        public RepositoryState Repository
        {
            get
            {
                if (Repositories == null || !Repositories.ContainsKey(RepositoryID))
                {
                    return null;
                }
                else
                {
                    return Repositories[RepositoryID];
                }
            }
        }

        /// <summary>
        /// Maps repository IDs to their RepositoryState.
        /// </summary>
        public Dictionary<Guid, RepositoryState> Repositories { get; private set; }

        /// <summary>
        /// Maps the files in the repository to their origin repository ID.
        /// </summary>
        public Dictionary<string, Guid> Origin { get; private set; }

        #endregion
    }

    /// <summary>
    /// Describes the state of an individual repository.
    /// </summary>
    public class RepositoryState
    {
        public RepositoryState()
        {
            MTimes = new Dictionary<string, DateTime>();
        }

        public Guid ID { get; set; }
        public Dictionary<string, DateTime> MTimes { get; private set; }
    }
}
