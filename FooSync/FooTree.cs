using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FooSync
{
    public class FooTree
    {
        public string Path { get; private set; }

        public Dictionary<string, FooFileInfo> Files { get; private set; }

        private FooSyncEngine Foo { get; set; }

        internal FooTree(FooSyncEngine foo, string path, IEnumerable<string> exceptions, Progress callback = null)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSyncEngine"),
                "Don't directly instantiate FooClasses");

            this.Foo   = foo;
            this.Path  = path;
            this.Files = new Dictionary<string, FooFileInfo>();

            Walk(path, path, exceptions, callback);
        }

        private void Walk(string path, string basePath, IEnumerable<string> exceptions, Progress callback)
        {
            int n = 0;
            foreach (string file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                System.Diagnostics.Debug.Assert(file.StartsWith(basePath), "file is supposed to start with basePath");

                string trimmedName = file.Substring(basePath.Length + 1);

                if (trimmedName == FooSyncEngine.ConfigFileName || trimmedName == FooSyncEngine.RepoStateFileName)
                {
                    continue;
                }

                if (callback != null)
                {
                    callback(++n, -1, System.IO.Path.GetDirectoryName(file));
                }

                bool failsRegex = false;
                foreach (string ex in exceptions)
                {
                    string regex;
                    string searchAgainst;

                    if (ex.EndsWith("/$"))
                    {
                        searchAgainst = System.IO.Path.GetDirectoryName(file) + System.IO.Path.DirectorySeparatorChar;
                        regex = ex.Substring(0, ex.Length - 2) + System.IO.Path.DirectorySeparatorChar;

                        if (regex.EndsWith(@"\"))   // can't end with a single backslash
                            regex += @"\";
                    }
                    else
                    {
                        searchAgainst = System.IO.Path.GetFileName(file);
                        regex = ex;
                    }
                    
                    if (Regex.Match(searchAgainst, regex, Foo.Options.CaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None).Success)
                    {
                        failsRegex = true;
                        break;
                    }
                }

                if (!failsRegex)
                {
                    Files[trimmedName] = Foo.FileInfo(file);
                }
            }
        }
    }
}
