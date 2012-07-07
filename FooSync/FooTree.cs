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
using System.Net.Sockets;
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

        /// <summary>
        /// Creates a new FooTree, fully populated with files.
        /// </summary>
        /// <param name="foo">FooSyncEngine instance; its configuration is used</param>
        /// <param name="path">Directory the tree is rooted at.</param>
        /// <param name="exceptions">Regular expressions to exclude from the tree.
        ///     If the regex ends with '/$', it applies to directories, otherwise it applies to files.
        ///     Pass 'null' to not use any exceptions.</param>
        /// <param name="callback">Progress callback, invoked once per file found.
        ///     The 'total' parameter passed in is always -1, and the 'item' is the directory currently being enumerated.
        ///     Pass 'null' if no callback is desired.</param>
        public FooTree(FooSyncEngine foo, string path, IEnumerable<string> exceptions = null, Progress callback = null)
        {
            this.Foo   = foo;
            this.Path  = path;
            this.Files = new Dictionary<string, FooFileInfoBase>();

            Walk(Foo, path, path, exceptions,
                (trimmedPath, info) =>
                    {
                        if (info != null)
                        {
                            Files.Add(trimmedPath, info);
                        }

                        if (callback != null)
                        {
                            callback(Files.Count, -1, trimmedPath);
                        }
                    }
            );
        }

        /// <summary>
        /// Creates a new FooTree from a stream.
        /// </summary>
        /// <param name="foo">FooSyncEngine instance; its configuration is used</param>
        /// <param name="url">fs:// URL the stream is from</param>
        /// <param name="input">Stream to load data from</param>
        /// <param name="callback">Progress callback, invoked once per file found.
        ///     The 'total' parameter passed in is always -1, and the 'item' is the directory currently being enumerated.
        ///     Pass 'null' if no callback is desired.</param>
        public FooTree(FooSyncEngine foo, string url, Stream input, Progress callback = null)
        {
            this.Foo   = foo;
            this.Path  = url;
            this.Files = new Dictionary<string, FooFileInfoBase>();

            string path = string.Empty;
            string source = string.Empty;
            long mTime = 0;
            long size = 0;

            var reader = new BinaryReader(input);

            while (true)
            {
                path   = reader.ReadString();
                source = reader.ReadString();
                mTime  = reader.ReadInt64();
                size   = reader.ReadInt64();

                if (path == string.Empty && source == string.Empty && mTime == 0 && size == 0)
                {
                    return;
                }

                var info = new FooFileInfoBase();
                info.Path   = path;
                info.Source = source;
                info.MTime  = new DateTime(mTime);
                info.Size   = size;

                if (callback != null)
                {
                    callback(Files.Count + 1, -1, path);
                }

                Files.Add(path, info);
            }
        }

        /// <summary>
        /// Create a FooTree, writing the data to a stream.
        /// </summary>
        /// <param name="foo">FooSyncEngine instance; its configuration is used</param>
        /// <param name="path">Directory the tree is rooted at</param>
        /// <param name="exceptions">Regular expressions to exclude from the tree.
        ///     If the regex ends with '/$', it applies to directories, otherwise it applies to files.
        ///     Pass 'null' to not use any exceptions.</param>
        /// <param name="output">Stream to write the data to</param>
        public static void ToStream(FooSyncEngine foo, string path, IEnumerable<string> exceptions, Stream output)
        {
            var writer = new BinaryWriter(output);
            Walk(foo, path, path, exceptions,
                (trimmedPath, info) =>
                    {
                        if (info != null)
                        {
                            writer.Write(trimmedPath);
                            writer.Write(info.Source);
                            writer.Write(info.MTime.Ticks);
                            writer.Write(info.Size);
                        }
                    }
            );

            writer.Write(string.Empty);
            writer.Write(string.Empty);
            writer.Write(0L);
            writer.Write(0L);
        }

        private static void Walk(FooSyncEngine foo, string path, string basePath, IEnumerable<string> exceptions, Action<string, FooFileInfoBase> OnItem)
        {
            foreach (string entry in Directory.EnumerateFileSystemEntries(path))
            {
                Debug.Assert(entry.StartsWith(basePath), "file is supposed to start with basePath");

                string trimmedName = entry.Substring(basePath.Length + 1);
                if (trimmedName == FooSyncEngine.RepoStateFileName)
                {
                    continue;
                }

                bool isDirectory = (File.GetAttributes(entry) & FileAttributes.Directory) == FileAttributes.Directory;

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
                    
                    if (Regex.Match(searchAgainst, regex, foo.Options.CaseInsensitive ? RegexOptions.IgnoreCase : RegexOptions.None).Success)
                    {
                        failsRegex = true;
                        break;
                    }
                }

                if (!failsRegex)
                {
                    if (isDirectory)
                    {
                        OnItem(trimmedName, null);
                        Walk(foo, entry, basePath, exceptions, OnItem);
                    }
                    else
                    {
                        var info = new FooFileInfo(foo, entry);
                        OnItem(trimmedName, info);
                    }
                }
            }
        }
    }
}
