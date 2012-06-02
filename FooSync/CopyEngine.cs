///
/// Codewise/FooSync/CopyEngine.cs
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
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Runtime.InteropServices;

namespace Codewise.FooSync
{
    /// <summary>
    /// Copies files using the system's native file copy dialog, if possible.
    /// </summary>
    public static class CopyEngine
    {
        public static bool Copy(ICollection<string> copyFrom, ICollection<string> copyTo)
        {
            return Copy(copyFrom, copyTo, null);
        }

        public static bool Copy(ICollection<string> copyFrom, ICollection<string> copyTo, Progress callback)
        {
            if (copyFrom == null)
                throw new ArgumentNullException("copyFrom");
            if (copyTo == null)
                throw new ArgumentNullException("copyTo");
            if (copyFrom.Count != copyTo.Count)
                throw new ArgumentException("Unequal count of source and destination files.");
            if (copyFrom.Count == 0)
                return true;

            if (Type.GetType("Mono.Runtime") != null)
            {
                int i = 0;
                IEnumerator<string> enumFrom = copyFrom.GetEnumerator();
                IEnumerator<string> enumTo = copyTo.GetEnumerator();

                while (enumFrom.MoveNext() && enumTo.MoveNext())
                {
                    //
                    // check that all components of the path to the destination exists
                    // if any do not, create them.
                    //
                    var path = string.Empty;
                    var parts = enumTo.Current.Split(Path.DirectorySeparatorChar);
                    for (var p = 0; p < parts.Length - 1; p++)
                    {
                        path += parts[p] + Path.DirectorySeparatorChar;

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                    }

                    if (callback != null)
                    {
                        callback(++i, copyFrom.Count, enumFrom.Current);
                    }

                    /*
                    Stream source = null, dest = null;

                    if (enumFrom.Current.StartsWith("fs://"))
                    {
                    }

                    if (enumTo.Current.StartsWith("fs://"))
                    {
                    }

                    if (source == null && dest == null)
                    {
                        //
                        // Simple local file-to-file copy.
                        //
                        File.Copy(enumFrom.Current, enumTo.Current);
                    }
                    else
                    {
                        //
                        // Either the source or destination (or both) are foreign streams.
                        //

                        if (source == null)
                        {
                            source = new FileStream(enumFrom.Current, FileMode.Open, FileAccess.Read, FileShare.Read);
                        }

                        if (dest == null)
                        {
                            dest = new FileStream(enumTo.Current, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
                        }

                        source.CopyTo(dest);
                    }
                     */

                    File.Copy(enumFrom.Current, enumTo.Current);
                }

                callback(i, copyFrom.Count, string.Empty);

                return true;
            }
            else
            {
                return NativeMethods.CopyOperation(copyFrom, copyTo, IntPtr.Zero);
            }
        }

        static Tuple<string, int, string> SplitUrl(string url)
        {
            if (!url.StartsWith("fs://"))
                return null;

            string hostname = null, port = null, path = null;

            var trimmed = url.Substring("fs://".Length);
            var parts = trimmed.Split(new char[] { ':' }, 2);

            hostname = parts[0];

            parts = parts[1].Split(new char[] { '/' }, 2);

            port = parts[0];
            path = parts[1];

            return new Tuple<string, int, string>(hostname, int.Parse(port), path);
        }

        public static bool Delete(ICollection<string> files)
        {
            return Delete(files, null);
        }

        public static bool Delete(ICollection<string> files, Progress callback)
        {
            if (files == null)
                throw new ArgumentNullException("files");

            if (files.Count == 0)
                return true;

            if (Type.GetType("Mono.Runtime") != null)
            {
                int i = 0;

                foreach (var file in files)
                {
                    File.Delete(file);

                    if (callback != null)
                    {
                        callback(++i, files.Count, file);
                    }
                }

                return true;
            }
            else
            {
                return NativeMethods.DeleteOperation(files, IntPtr.Zero);
            }
        }

        public static bool RemoveEmptyDirectories(ICollection<string> deletedFiles)
        {
            return RemoveEmptyDirectories(deletedFiles, null);
        }

        public static bool RemoveEmptyDirectories(ICollection<string> deletedFiles, Progress callback)
        {
            if (deletedFiles == null)
                throw new ArgumentNullException("deletedFiles");

            if (deletedFiles.Count == 0)
                return true;

            bool allSuccessful = true;
            var directories = new List<string>();

            foreach (var file in deletedFiles)
            {
                var dir = Path.GetDirectoryName(file);

                if (!directories.Contains(dir))
                {
                    directories.Add(dir);
                }
            }

            directories.Sort();
            directories.Reverse();

            int n = 0;
            foreach (var dir in directories)
            {
                if (Directory.GetFileSystemEntries(dir).Length == 0)
                {
                    try
                    {
                        if (callback != null)
                        {
                            callback(++n, -1, dir);
                        }

                        Directory.Delete(dir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        allSuccessful = false;
                    }
                }
            }

            return allSuccessful;
        }
    }
}