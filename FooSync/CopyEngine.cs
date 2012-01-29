using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FooSync
{
    /// <summary>
    /// Copies files using the system's native file copy dialog, if possible.
    /// </summary>
    public class CopyEngine
    {
        public static void Copy(List<string> copyFrom, List<string> copyTo)
        {
            Copy(copyFrom, copyTo, IntPtr.Zero);
        }

        public static void Copy(List<string> copyFrom, List<string> copyTo, IntPtr hwnd)
        {
            if (copyFrom.Count != copyTo.Count)
            {
                throw new ArgumentException("Unequal count of source and destination files.");
            }

            if (copyFrom.Count == 0)
            {
                return;
            }

            if (Type.GetType("Mono.Runtime") != null)
            {
                //
                // TODO
                //
                throw new NotImplementedException();
            }

            var fromStr = string.Empty;
            var toStr = string.Empty;

            foreach (var f in copyFrom)
            {
                System.Diagnostics.Debug.Assert(System.IO.File.Exists(f), string.Format("Trying to copy nonexistant file {0}", f));
                fromStr += f + '\0';
            }
            fromStr += '\0';

            foreach (var f in copyTo)
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
            op.lpszProgressTitle = "FooSync File Copy";

            int result = SHFileOperation(ref op);

            if (0 != result)
            {
                if ((result >= 0x70 && result <= 0x88) || result == 0xB7 || result == 0x402 || result == 0x10000 || result == 0x10074)
                {
                    throw new ApplicationException(string.Format("Copy operation failed: return code {0}", result));
                }
                else
                {
                    const int FACILITY_WIN32 = 7;
                    UInt32 hresult = (result <= 0)
                        ? (UInt32)(result)
                        : (((UInt32)(result & 0x0000FFFF) | (FACILITY_WIN32 << 16) | 0x80000000));
                    throw Marshal.GetExceptionForHR((int)hresult);
                }
            }

            Marshal.FreeHGlobal(fromPtr);
            Marshal.FreeHGlobal(toPtr);
        }

        public static void Delete(List<string> files)
        {
            Delete(files, IntPtr.Zero);
        }

        public static void Delete(List<string> files, IntPtr hwnd)
        {
            if (Type.GetType("Mono.Runtime") != null)
            {
                //
                // TODO
                //
                throw new NotImplementedException();
            }

            var delStr = string.Empty;

            foreach (var f in files)
            {
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
            op.lpszProgressTitle = "FooSync File Delete";

            SHFileOperation(ref op);

            Marshal.FreeHGlobal(delPtr);
        }

        [DllImport("Shell32.dll", CharSet=CharSet.Unicode)]
        private static extern int SHFileOperation([In] ref SHFILEOPSTRUCT lpFileOp);

        private struct SHFILEOPSTRUCT
        {
            public IntPtr       hwnd;
            public FILEOP_FUNC  wFunc;
            public IntPtr       pFrom;
            public IntPtr       pTo;
            public FILEOP_FLAGS fFlags;
            public bool         fAnyOperationsAborted;
            public IntPtr       hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string       lpszProgressTitle;
        }

        private enum FILEOP_FUNC : uint
        {
            FO_MOVE   = 1,
            FO_COPY   = 2,
            FO_DELETE = 3,
            FO_RENAME = 4,
        }

        private enum FILEOP_FLAGS : uint
        {
            FOF_MULTIDESTFILES        = 0x0001,
            FOF_CONFIRMMOUSE          = 0x0002,
            FOF_SILENT                = 0x0004,
            FOF_RENAMEONCOLLISION     = 0x0008,
            FOF_NOCONFIRMATION        = 0x0010,
            FOF_WANTMAPPINGHANDLE     = 0x0020,
            FOF_ALLOWUNDO             = 0x0040,
            FOF_FILESONLY             = 0x0080,
            FOF_SIMPLEPROGRESS        = 0x0100,
            FOF_NOCONFIRMMKDIR        = 0x0200,
            FOF_NOERRORUI             = 0x0400,
            FOF_NOCOPYSECURITYATTRIBS = 0x0800,
            FOF_NORECURSION           = 0x1000,
            FOF_NO_CONNECTED_ELEMENTS = 0x2000,
            FOF_WANTNUKEWARNING       = 0x4000,
            FOF_NORECURSEREPARSE      = 0x8000,
        }
    }
}