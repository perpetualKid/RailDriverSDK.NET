using System;
using System.Runtime.InteropServices;

using Microsoft.Win32.SafeHandles;

namespace RailDriver
{
    internal class FileIOApiDeclarations
    {
        // API declarations relating to file I/O.

        // ******************************************************************************
        // API constants
        // ******************************************************************************

        public const int ERROR_INVALID_HANDLE = 6;
        public const int ERROR_DEVICE_NOT_CONNECTED = 1167;
        public const int ERROR_IO_INCOMPLETE = 996;
        public const int ERROR_IO_PENDING = 997;

        public const uint GENERIC_READ = 0x80000000;
        public const uint GENERIC_WRITE = 0x40000000;
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
        public const int INVALID_HANDLE_VALUE = -1;
        public const short OPEN_EXISTING = 3;
        public const int WAIT_TIMEOUT = 0x102;
        public const short WAIT_OBJECT_0 = 0;

        // ******************************************************************************
        // API functions, listed alphabetically
        // ******************************************************************************

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, uint dwFlagsAndAttributes, int hTemplateFile); 

    }

}
