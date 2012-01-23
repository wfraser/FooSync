using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace FooSync
{
    public class FooTree
    {
        public string Path { get; private set; }

        public Dictionary<string, FooFileInfo> Files;

        private FooSync Foo { get; set; }

        internal FooTree(FooSync foo, string path, IEnumerable<string> exceptions)
        {
            System.Diagnostics.Debug.Assert(
                (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("FooSync.FooSync"),
                "Don't directly instantiate FooClasses");

            this.Foo   = foo;
            this.Path  = path;
            this.Files = new Dictionary<string, FooFileInfo>();

            Walk(path, path, exceptions);
        }

        private void Walk(string path, string basePath, IEnumerable<string> exceptions)
        {
            foreach (string file in Directory.EnumerateFiles(path))
            {
                System.Diagnostics.Debug.Assert(file.StartsWith(basePath), "file is supposed to start with basePath");

                string trimmedName = file.Substring(basePath.Length + 1);

                if (trimmedName == FooSync.ConfigFileName || trimmedName == FooSync.RepoStateFileName)
                {
                    continue;
                }

                bool failsRegex = false;
                foreach (string ex in exceptions)
                {
                    if (Regex.Match(System.IO.Path.GetFileName(file), ex).Success)
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

            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                Walk(dir, basePath, exceptions);
            }
        }
    }
}
