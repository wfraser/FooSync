﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace FooSync
{
    /// <summary>
    /// Copies files using the system's native file copy dialog, if possible.
    /// </summary>
    public static class CopyEngine
    {
        public delegate void Progress(int completed, int total, string file);

        public static void Copy(ICollection<string> copyFrom, ICollection<string> copyTo, Progress callback)
        {
            Copy(copyFrom, copyTo, IntPtr.Zero, callback);
        }

        public static void Copy(ICollection<string> copyFrom, ICollection<string> copyTo, IntPtr hwnd, Progress callback)
        {
            if (copyFrom == null)
                throw new ArgumentNullException("copyFrom");
            if (copyTo == null)
                throw new ArgumentNullException("copyTo");
            if (copyFrom.Count != copyTo.Count)
                throw new ArgumentException("Unequal count of source and destination files.");
            if (copyFrom.Count == 0)
                return;

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

                    callback(++i, copyFrom.Count, enumFrom.Current);
                    File.Copy(enumFrom.Current, enumTo.Current);
                    
                }

                callback(i, copyFrom.Count, string.Empty);
            }
            else
            {
                NativeMethods.CopyOperation(copyFrom, copyTo, hwnd);
            }
        }

        public static void Delete(ICollection<string> files, Progress callback)
        {
            Delete(files, IntPtr.Zero, callback);
        }

        public static void Delete(ICollection<string> files, IntPtr hwnd, Progress callback)
        {
            if (files == null)
                throw new ArgumentNullException("files");

            if (files.Count == 0)
                return;

            if (Type.GetType("Mono.Runtime") != null)
            {
                int i = 0;

                foreach (var file in files)
                {
                    File.Delete(file);

                    callback(++i, files.Count, file);
                }
            }
            else
            {
                NativeMethods.DeleteOperation(files, hwnd);
            }
        }
    }
}