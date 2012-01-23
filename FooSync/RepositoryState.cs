using System;
using System.Collections.Generic;
using System.IO;

namespace FooSync
{
    public class RepositoryState
    {
        public RepositoryState()
        {
            Sources = new Dictionary<string,RepositorySourceState>();
        }

        public void AddSource(FooTree tree, string name)
        {
            var source = new RepositorySourceState();
            source.Name = name;

            foreach (var file in tree.Files)
            {
                string filename = file.Key;

                //
                // Convert normalized path back to system directory separators.
                //
                if (Path.DirectorySeparatorChar != '/')
                {
                    filename = filename.Replace(Path.DirectorySeparatorChar, '/');
                }
                
                source.MTimes.Add(filename, file.Value.MTime);
            }

            Sources.Add(name, source);
        }

        public RepositoryState(string stateFilename)
        {
            Sources = new Dictionary<string, RepositorySourceState>();

            using (var r = new StreamReader(stateFilename, System.Text.Encoding.UTF8))
            {
                var current = new RepositorySourceState();
                var buf = new List<char>();
                string source = null, filename = null, mtime = null;

                bool lastWasNull = false;
                int c;
                while (-1 != (c = r.Read()))
                {
                    if (c == 0)
                    {
                        if (lastWasNull)
                        {
                            //
                            // double null; end of source
                            //

                            current.Name = source;
                            Sources.Add(source, current);
                            current = new RepositorySourceState();
                            source = null;
                        }
                        else if (source == null)
                        {
                            source = new string(buf.ToArray());
                            buf.Clear();
                        }
                        else if (filename == null)
                        {
                            filename = new string(buf.ToArray());
                            buf.Clear();

                            //
                            // Normalize the path to use forward slashes instead of whatever
                            //  the system normally uses.
                            // Rationale: Unix filenames can contain backslashes, but
                            //  Windows filenames can't contain forward slashes, so
                            //  forward slashes win.
                            //
                            if (Path.DirectorySeparatorChar != '/')
                            {
                                filename = filename.Replace('/', Path.DirectorySeparatorChar);
                            }
                        }
                        else if (mtime == null)
                        {
                            mtime = new string(buf.ToArray());
                            buf.Clear();

                            current.MTimes.Add(filename, DateTime.Parse(mtime));

                            filename = null;
                            mtime = null;
                        }

                        lastWasNull = true;
                    }
                    else
                    {
                        lastWasNull = false;
                        buf.Add((char)c);
                    }
                }
            }
        }

        public void Write(string sourceFilename)
        {
            using (var w = new StreamWriter(sourceFilename, false, System.Text.Encoding.UTF8))
            {
                foreach (var source in Sources.Values)
                {
                    w.Write(source.Name);
                    w.Write('\0');

                    foreach (var mtime in source.MTimes)
                    {
                        w.Write(mtime.Key);
                        w.Write('\0');
                        w.Write(mtime.Value.ToUniversalTime().ToString() + " Z");
                        w.Write('\0');
                    }

                    w.Write('\0');
                }
            }
        }

        public const string RepoSourceName = ".";

        public RepositorySourceState Repository
        {
            get
            {
                if (Sources == null || !Sources.ContainsKey(RepoSourceName))
                {
                    return null;
                }
                else
                {
                    return Sources[RepoSourceName];
                }
            }
        }

        public Dictionary<string, RepositorySourceState> Sources { get; private set; }
    }

    public class RepositorySourceState
    {
        public RepositorySourceState()
        {
            MTimes = new Dictionary<string, DateTime>();
        }

        public string Name { get; set; }
        public Dictionary<string, DateTime> MTimes { get; private set; }
    }
}
