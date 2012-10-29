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
        public static bool PerformActions(FooChangeSet changeSet, Dictionary<Guid, FooSyncUrl> basePaths, Progress copyCallback = null, Progress deleteCallback = null)
        {
            List<FooSyncUrl> copyFrom = new List<FooSyncUrl>();
            List<FooSyncUrl> copyTo = new List<FooSyncUrl>();
            List<FooSyncUrl> delete = new List<FooSyncUrl>();

            foreach (string filename in changeSet.Filenames)
            {
                FooSyncUrl copySourceUrl = null;
                List<FooSyncUrl> copyDestUrls = new List<FooSyncUrl>();
                List<FooSyncUrl> deleteUrls = new List<FooSyncUrl>();

                foreach (Guid repoId in changeSet.RepositoryIDs)
                {
                    FooSyncUrl fullUrl = new FooSyncUrl(basePaths[repoId], filename);

                    switch (changeSet[filename].FileOperation[repoId])
                    {
                        case FileOperation.Source:
                            copySourceUrl = fullUrl;
                            break;

                        case FileOperation.Destination:
                            copyDestUrls.Add(fullUrl);
                            break;

                        case FileOperation.Delete:
                            deleteUrls.Add(fullUrl);
                            break;
                    }
                }

                if (copySourceUrl == null && copyDestUrls.Count > 0)
                {
                    throw new InvalidOperationException(
                        string.Format("No copy source given for {0}", filename));
                }

                foreach (FooSyncUrl destUrl in copyDestUrls)
                {
                    copyFrom.Add(copySourceUrl);
                    copyTo.Add(destUrl);
                }

                delete.AddRange(deleteUrls);
            }

            bool result = Copy(copyFrom, copyTo, copyCallback);

            if (!result)
            {
                return result;
            }

            result = Delete(delete, deleteCallback);

            return result;
        }

        public static bool Copy(ICollection<FooSyncUrl> copyFrom, ICollection<FooSyncUrl> copyTo)
        {
            return Copy(copyFrom, copyTo, null);
        }

        public static bool Copy(ICollection<FooSyncUrl> copyFrom, ICollection<FooSyncUrl> copyTo, Progress callback)
        {
            if (copyFrom == null)
                throw new ArgumentNullException("copyFrom");
            if (copyTo == null)
                throw new ArgumentNullException("copyTo");
            if (copyFrom.Count != copyTo.Count)
                throw new ArgumentException("Unequal count of source and destination files.");
            if (copyFrom.Count == 0)
                return true;

            if (Type.GetType("Mono.Runtime") == null && copyFrom.All(f => f.IsLocal) && copyTo.All(f => f.IsLocal))
            {
                //
                // Special case: on Windows, with all source and dest local, use the native copy operation.
                //

                List<string> copyFromStr = new List<string>(copyFrom.Select(f => f.LocalPath));
                List<string> copyToStr   = new List<string>(copyTo.Select(f => f.LocalPath));

                return NativeMethods.CopyOperation(copyFromStr, copyToStr, IntPtr.Zero);
            }

            int i = 0;
            IEnumerator<FooSyncUrl> enumFrom = copyFrom.GetEnumerator();
            IEnumerator<FooSyncUrl> enumTo = copyTo.GetEnumerator();

            while (enumFrom.MoveNext() && enumTo.MoveNext())
            {
                //
                // check that all components of the path to the destination exists
                // if any do not, create them.
                //

                if (enumTo.Current.IsLocal)
                {
                    var path = string.Empty;
                    var parts = enumTo.Current.LocalPath.Split(Path.DirectorySeparatorChar);
                    for (var p = 0; p < parts.Length - 1; p++)
                    {
                        path += parts[p] + Path.DirectorySeparatorChar;

                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }
                    }
                }

                if (callback != null)
                {
                    callback(++i, copyFrom.Count, enumFrom.Current.NaturalFormat);
                }

                if (enumFrom.Current.IsLocal && enumTo.Current.IsLocal)
                {
                    File.Copy(enumFrom.Current.LocalPath, enumTo.Current.LocalPath);
                }
                else
                {
                    throw new NotImplementedException("Copying to/from fs:// URLs is not yet implemented. Sorry!");
                }
            }

            return true;
        }

        public static bool Delete(ICollection<FooSyncUrl> files)
        {
            return Delete(files, null);
        }

        public static bool Delete(ICollection<FooSyncUrl> files, Progress callback)
        {
            if (files == null)
                throw new ArgumentNullException("files");

            if (files.Count == 0)
                return true;

            if (Type.GetType("Mono.Runtime") == null && files.All(x => x.IsLocal))
            {
                //
                // Special case: on Windows, with all files local, use the native delete operation.
                //

                List<string> filesStr = new List<string>(files.Select(f => f.LocalPath));

                return NativeMethods.DeleteOperation(filesStr, IntPtr.Zero);
            }

            int i = 0;

            foreach (FooSyncUrl file in files)
            {
                if (file.IsLocal)
                {
                    File.Delete(file.LocalPath);
                }
                else
                {
                    throw new NotImplementedException("Deleting files with fs:// URLs is not yet implemented. Sorry!");
                }

                if (callback != null)
                {
                    callback(++i, files.Count, file.NaturalFormat);
                }
            }

            RemoveEmptyDirectories(files.Where(f => f.IsLocal).Select(f => f.LocalPath), callback);

            return true;
        }

        public static bool RemoveEmptyDirectories(ICollection<string> deletedFiles)
        {
            return RemoveEmptyDirectories(deletedFiles, null);
        }

        public static bool RemoveEmptyDirectories(IEnumerable<string> deletedFiles, Progress callback)
        {
            if (deletedFiles == null)
                throw new ArgumentNullException("deletedFiles");

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

            if (directories.Count == 0)
            {
                return true;
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