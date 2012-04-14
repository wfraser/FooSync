///
/// Codewise/FooSync/FooTree.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using IOPath = System.IO.Path;

namespace Codewise.FooSync
{
    public class FooTree
    {
        public string Path { get; private set; }

        public Dictionary<string, FooFileInfoBase> Files { get; private set; }

        private FooSyncEngine Foo { get; set; }

        private FooTree()
        {
            Files = new Dictionary<string, FooFileInfoBase>();
        }

        internal FooTree(FooSyncEngine foo, string path, IEnumerable<string> exceptions, Progress callback = null)
        {
            Debug.Assert(
                (new StackTrace()).GetFrame(1).GetMethod().DeclaringType.FullName.Equals("Codewise.FooSync.FooSyncEngine"),
                "Don't directly instantiate FooClasses");

            this.Foo   = foo;
            this.Path  = path;
            this.Files = new Dictionary<string, FooFileInfoBase>();

            Walk(path, path, exceptions, callback);
        }

        public void Serialize(Stream output)
        {
            NetUtil.WriteInt(output, Files.Count);

            foreach (FooFileInfo info in Files.Values)
            {
                NetUtil.WriteString(output, info.Path);
                NetUtil.WriteString(output, info.Source);
                NetUtil.WriteLong(output, info.MTime.Ticks);
                NetUtil.WriteLong(output, info.Size);
            }
        }

        public static FooTree Unserialize(Stream input)
        {
            var tree = new FooTree();

            int numFiles = NetUtil.GetInt(input);

            for (int i = 0; i < numFiles; i++)
            {
                var path = NetUtil.GetString(input);
                var source = NetUtil.GetString(input);
                var mTimeTicks = NetUtil.GetLong(input);
                var size = NetUtil.GetLong(input);

                var info = new FooFileInfoBase();
                info.Path = path;
                info.Source = source;
                info.MTime = new DateTime(mTimeTicks);
                info.Size = size;

                tree.Files.Add(path, info);
            }

            return tree;
        }

        private void Walk(string path, string basePath, IEnumerable<string> exceptions, Progress callback)
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(path))
            {
                Debug.Assert(entry.StartsWith(basePath), "file is supposed to start with basePath");

                string trimmedName = entry.Substring(basePath.Length + 1);
                if (trimmedName == FooSyncEngine.ConfigFileName || trimmedName == FooSyncEngine.RepoStateFileName)
                {
                    continue;
                }

                bool isDirectory = (File.GetAttributes(entry) & FileAttributes.Directory) == FileAttributes.Directory;

                if (callback != null)
                {
                    callback(Files.Count + 1, -1, path);
                }

                bool failsRegex = false;
                foreach (string ex in exceptions ?? Enumerable.Empty<string>())
                {
                    string regex = ex;
                    string searchAgainst = IOPath.GetFileName(entry);

                    if (ex.EndsWith("/$"))
                    {
                        if (!isDirectory)
                        {
                            continue;
                        }

                        //
                        // Don't use Path.DirectorySeparatorChar here because the regex ends with slash, not system dependent.
                        //
                        searchAgainst += "/";
                    }
                    else if (isDirectory)
                    {
                        continue;
                    }
                    
                    if (Regex.Match(searchAgainst, regex, Foo.Options.CaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None).Success)
                    {
                        failsRegex = true;
                        break;
                    }
                }

                if (!failsRegex)
                {
                    if (isDirectory)
                    {
                        Walk(entry, basePath, exceptions, callback);
                    }
                    else
                    {
                        Files[trimmedName] = Foo.FileInfo(entry);
                    }
                }
            }
        }
    }
}
