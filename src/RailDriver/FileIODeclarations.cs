using System;
using System.Runtime.InteropServices;

namespace RailDriver
{
    internal static class FileIOApiDeclarations
    {
        // API declarations relating to file I/O.

        // ******************************************************************************
        // API constants
        // ******************************************************************************

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const short OPEN_EXISTING = 3;

        // ******************************************************************************
        // API functions, listed alphabetically
        // ******************************************************************************

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, uint dwFlagsAndAttributes, int hTemplateFile); 

    }

}
