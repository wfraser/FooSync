///
/// Codewise/FooSync/WPFApp/NativeMethods.cs
/// 
/// by William R. Fraser:
///     http://www.codewise.org/
///     https://github.com/wfraser/FooSync
///     
/// Copyright (c) 2012
/// 

using System;
using System.Runtime.InteropServices;

namespace Codewise.FooSync.WPFApp
{
    internal static class NativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool ShellExecuteEx(ref SHELLEXECUTEINFO lpExecInfo);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHELLEXECUTEINFO
        {
            public int cbSize;
            public SEE_MASK fMask;
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpVerb;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpFile;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpParameters;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpDirectory;
            public NShowCommand nShow;
            public IntPtr hInstApp;
            public IntPtr lpIDList;
            [MarshalAs(UnmanagedType.LPTStr)]
            public string lpClass;
            public IntPtr hkeyClass;
            public uint dwHotKey;
            public IntPtr hIcon;
            public IntPtr hProcess;
        }

        public enum NShowCommand
        {
            SW_HIDE             =  0,
            SW_SHOWNORMAL       =  1,
            SW_SHOWMINIMIZED    =  2,
            SW_SHOWMAXIMIZED    =  3,
            SW_SHOWNOACTIVATE   =  4,
            SW_SHOW             =  5,
            SW_MINIMIZE         =  6,
            SW_SHOWMINNOACTIVE  =  7,
            SW_SHOWNA           =  8,
            SW_RESTORE          =  9,
            SW_SHOWDEFAULT      = 10,
        }

        [Flags]
        public enum SEE_MASK
        {
            SEE_MASK_DEFAULT            = 0x00000000,
            SEE_MASK_CLASSNAME          = 0x00000001,
            SEE_MASK_CLASSKEY           = 0x00000003,
            SEE_MASK_IDLIST             = 0x00000004,
            SEE_MASK_INVOKEIDLIST       = 0x0000000C,
            SEE_MASK_ICON               = 0x00000010,
            SEE_MASK_HOTKEY             = 0x00000020,
            SEE_MASK_NOCLOSEPROCESS     = 0x00000040,
            SEE_MASK_CONNECTNETDRV      = 0x00000080,
            SEE_MASK_NOASYNC            = 0x00000100,
            SEE_MASK_DOENVSUBST         = 0x00000200,
            SEE_MASK_FLAG_NO_UI         = 0x00000400,
            SEE_MASK_UNICODE            = 0x00004000,
            SEE_MASK_NO_CONSOLE         = 0x00008000,
            SEE_MASK_ASYNCOK            = 0x00100000,
            SEE_MASK_HMONITOR           = 0x00200000,
            SEE_MASK_NOZONECHECKS       = 0x00800000,
          //SEE_MASK_NOQUERYCLASSSTORE  = 0x01000000,   // "Not used."
            SEE_MASK_WAITFORINPUTIDLE   = 0x02000000,
            SEE_MASK_FLAG_LOG_USAGE     = 0x04000000,
        }

        public enum SE_ERR
        {
            SE_ERR_FNF              =  2, // "File not found."
            SE_ERR_PNF              =  3, // "Path not found."
            SE_ERR_ACCESSDENIED     =  5, // "Access Denied."
            SE_ERR_OOM              =  8, // "Out of memory.",
            SE_ERR_SHARE            = 26, // "Cannot share an open file."
            SE_ERR_ASSOCINCOMPLETE  = 27, // "File association information not complete."
            SE_ERR_DDETIMEOUT       = 28, // "DDE operation timed out."
            SE_ERR_DDEFAIL          = 29, // "DDE operation failed."
            SE_ERR_DDEBUSY          = 30, // "DDE operation is busy."
            SE_ERR_NOASSOC          = 31, // "File association not available."
            SE_ERR_DLLNOTFOUND      = 32, // "Dynamic-link library not found."
        }
    }
}
