///
/// Codewise/FooSync/NativeMethods.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Codewise.FooSync
{
    public static class NativeMethods
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201")]
        public static bool CopyOperation(ICollection<string> sourceFiles, ICollection<string> destFiles, IntPtr hwnd)
        {
            bool allCopied = true;

            if (sourceFiles == null)
                throw new ArgumentNullException("sourceFiles");
            if (destFiles == null)
                throw new ArgumentNullException("destFiles");
            if (sourceFiles.Count != destFiles.Count)
                throw new ArgumentException("Unequal count of source and destination files.");
            if (sourceFiles.Count == 0)
                return allCopied;

            var fromStr = string.Empty;
            var toStr = string.Empty;

            foreach (var f in sourceFiles)
            {
                System.Diagnostics.Debug.Assert(System.IO.File.Exists(f), string.Format("Trying to copy nonexistant file {0}", f));
                fromStr += f + '\0';
            }
            fromStr += '\0';

            foreach (var f in destFiles)
            {
                toStr += f + '\0';
            }
            toStr += '\0';

            var fromPtr = Marshal.StringToHGlobalUni(fromStr);
            var toPtr = Marshal.StringToHGlobalUni(toStr);

            var op = new SHFILEOPSTRUCT();

            op.hwnd = hwnd;
            op.wFunc = FILEOP_FUNC.FO_COPY;
            op.pFrom = fromPtr;
            op.pTo = toPtr;
            op.fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO
                            | FILEOP_FLAGS.FOF_FILESONLY
                            | FILEOP_FLAGS.FOF_MULTIDESTFILES
                            | FILEOP_FLAGS.FOF_NOCONFIRMMKDIR
                            | FILEOP_FLAGS.FOF_NOCONFIRMATION
                            | FILEOP_FLAGS.FOF_NORECURSION;
            op.fAnyOperationsAborted = false;
            op.hNameMappings = IntPtr.Zero;
            op.lpszProgressTitle = string.Empty;

            int result = SHFileOperation(ref op);

            if (0 != result)
            {
                if (result == 1223) // ERROR_CANCELLED; "The operation was canceled by the user."
                {
                    op.fAnyOperationsAborted = true;
                }
                else
                {
                    string shError = SHFileOpError(result);
                    if (shError == null)
                    {
                        throw Marshal.GetExceptionForHR(HResultFromWin32(result));
                    }
                    else
                    {
                        throw new ApplicationException(shError);
                    }
                }
            }

            allCopied = !op.fAnyOperationsAborted;

            Marshal.FreeHGlobal(fromPtr);
            Marshal.FreeHGlobal(toPtr);

            return allCopied;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2201")]
        public static bool DeleteOperation(ICollection<string> delFiles, IntPtr hwnd)
        {
            bool allDeleted = true;

            if (delFiles == null)
                throw new ArgumentNullException("delFiles");
            if (delFiles.Count == 0)
                return allDeleted;

            var delStr = string.Empty;

            foreach (var f in delFiles)
            {
                System.Diagnostics.Debug.Assert(System.IO.File.Exists(f), string.Format("Trying to delete nonexistant file {0}", f));
                delStr += f + '\0';
            }
            delStr += '\0';

            var delPtr = Marshal.StringToHGlobalUni(delStr);

            var op = new SHFILEOPSTRUCT();

            op.hwnd = hwnd;
            op.wFunc = FILEOP_FUNC.FO_DELETE;
            op.pFrom = delPtr;
            op.pTo = IntPtr.Zero;
            op.fFlags = FILEOP_FLAGS.FOF_ALLOWUNDO
                            | FILEOP_FLAGS.FOF_FILESONLY
                            | FILEOP_FLAGS.FOF_NOCONFIRMATION
                            | FILEOP_FLAGS.FOF_NORECURSION;
            op.fAnyOperationsAborted = false;
            op.hNameMappings = IntPtr.Zero;
            op.lpszProgressTitle = string.Empty;

            int result = SHFileOperation(ref op);

            if (0 != result)
            {
                if (result == 0x4C7) // ERROR_CANCELLED; "The operation was canceled by the user."
                {
                    op.fAnyOperationsAborted = true;
                }
                else
                {
                    string shError = SHFileOpError(result);
                    if (shError == null)
                    {
                        throw Marshal.GetExceptionForHR(HResultFromWin32(result));
                    }
                    else
                    {
                        throw new ApplicationException(shError);
                    }
                }
            }

            allDeleted = !op.fAnyOperationsAborted;

            Marshal.FreeHGlobal(delPtr);

            return allDeleted;
        }

        [DllImport("Shell32.dll", CharSet=CharSet.Unicode)]
        private static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public FILEOP_FUNC wFunc;
            public IntPtr pFrom;
            public IntPtr pTo;
            public FILEOP_FLAGS fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszProgressTitle;
        }

        private enum FILEOP_FUNC : uint
        {
            FO_MOVE = 1,
            FO_COPY = 2,
            FO_DELETE = 3,
            FO_RENAME = 4,
        }

        [Flags]
        private enum FILEOP_FLAGS : ushort
        {
            FOF_MULTIDESTFILES          = 0x0001,
            FOF_CONFIRMMOUSE            = 0x0002,
            FOF_SILENT                  = 0x0004,
            FOF_RENAMEONCOLLISION       = 0x0008,
            FOF_NOCONFIRMATION          = 0x0010,
            FOF_WANTMAPPINGHANDLE       = 0x0020,
            FOF_ALLOWUNDO               = 0x0040,
            FOF_FILESONLY               = 0x0080,
            FOF_SIMPLEPROGRESS          = 0x0100,
            FOF_NOCONFIRMMKDIR          = 0x0200,
            FOF_NOERRORUI               = 0x0400,
            FOF_NOCOPYSECURITYATTRIBS   = 0x0800,
            FOF_NORECURSION             = 0x1000,
            FOF_NO_CONNECTED_ELEMENTS   = 0x2000,
            FOF_WANTNUKEWARNING         = 0x4000,
            FOF_NORECURSEREPARSE        = 0x8000,
        }

        private static int HResultFromWin32(int result)
        {
            const int FACILITY_WIN32 = 7;
            UInt32 hresult = (result <= 0)
                ? (UInt32)(result)
                : (((UInt32)(result & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000));
            return (int)hresult;
        }

        private static string SHFileOpError(int errorCode)
        {
            switch (errorCode)
            {
                case 0x71:
                    return "The source and destination files are the same file.";
                case 0x72:
                    return "Multiple file paths were specified in the source buffer, but only one destination file path.";
                case 0x73:
                    return "Rename operation was specified but the destination path is a different directory. Use the move operation instead.";
                case 0x74:
                    return "The source is a root directory, which cannot be moved or renamed.";
                case 0x75:
                    return "The operation was canceled by the user, or silently canceled if the appropriate flags were supplied to SHFileOperation.";
                case 0x76:
                    return "The destination is a subtree of the source.";
                case 0x77:
                    break;
                case 0x78:
                    return "Security settings denied access to the source.";
                case 0x79:
                    return "The source or destination path exceeded or would exceed MAX_PATH.";
                case 0x7A:
                    return "The operation involved multiple destination paths, which can fail in the case of a move operation.";
                case 0x7B:
                    break;
                case 0x7C:
                    return "The path in the source or destination or both was invalid.";
                case 0x7D:
                    return "The source and destination have the same parent folder.";
                case 0x7E:
                    return "The destination path is an existing file.";
                case 0x7F:
                    break;
                case 0x80:
                    return "The destination path is an existing folder.";
                case 0x81:
                    return "The name of the file exceeds MAX_PATH.";
                case 0x82:
                    return "The destination is a read-only CD-ROM, possibly unformatted.";
                case 0x83:
                    return "The destination is a read-only DVD, possibly unformatted.";
                case 0x84:
                    return "The destination is a writable CD-ROM, possibly unformatted.";
                case 0x85:
                    return "The file involved in the operation is too large for the destination media or file system.";
                case 0x86:
                    return "The source is a read-only CD-ROM, possibly unformatted.";
                case 0x87:
                    return "The source is a read-only DVD, possibly unformatted.";
                case 0x88:
                    return "The source is a writable CD-ROM, possibly unformatted.";

                case 0xB7:
                    return "MAX_PATH was exceeded during the operation.";

                case 0x402:
                    return "An unknown error occurred. This is typically due to an invalid path in the source or destination.";

                case 0x10000:
                    return "An unspecified error occurred on the destination.";

                case 0x10074:
                    return "Destination is a root directory and cannot be renamed.";
            }

            return null;
        }
    }
}
