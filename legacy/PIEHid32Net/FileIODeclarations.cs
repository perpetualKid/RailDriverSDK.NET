using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PIEHid32Net
{
	
	class FileIOApiDeclarations
	{
		
		// API declarations relating to file I/O. cimbom
		
		// ******************************************************************************
		// API constants
		// ******************************************************************************

		public const int ERROR_INVALID_HANDLE = 6;
		public const int ERROR_DEVICE_NOT_CONNECTED = 1167;
        public const int ERROR_IO_INCOMPLETE = 996;
		public const int ERROR_IO_PENDING = 997;

    public const uint GENERIC_READ				= 0x80000000;
    public const uint GENERIC_WRITE				= 0x40000000;
    public const uint FILE_SHARE_READ			= 0x00000001;
    public const uint FILE_SHARE_WRITE			= 0x00000002;
    public const uint FILE_FLAG_OVERLAPPED = 0x40000000;
		public const int INVALID_HANDLE_VALUE = -1;
		public const short OPEN_EXISTING = 3;
		public const int WAIT_TIMEOUT = 0x102;
		public const short WAIT_OBJECT_0 = 0;
		
		// ******************************************************************************
		// Structures and classes for API calls, listed alphabetically
		// ******************************************************************************
		
		[StructLayout(LayoutKind.Sequential)]
    public struct OVERLAPPED
		{
             public Int32 Internal; //type changed by Onur for 64-bit compatability
             public Int32 InternalHigh; //type changed by Dan Simkin for 64-bit compatability
			 public Int32 Offset;
             public Int32 OffsetHigh;//type changed by Dan Simkin for 64-bit compatability
             public Int32 hEvent; //type changed by Onur for 64-bit compatability
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
		{
			public int nLength;
            public IntPtr lpSecurityDescriptor;//onur 07/27/09
			public int bInheritHandle;
		}
		
		// ******************************************************************************
		// API functions, listed alphabetically
		// ******************************************************************************

		[DllImport("kernel32.dll", SetLastError = true)]
        static public extern int CancelIo(SafeFileHandle hFile);

		[DllImport("kernel32.dll", SetLastError = true)]
    static public extern int CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static public extern IntPtr CreateEvent(ref SECURITY_ATTRIBUTES SecurityAttributes, int bManualReset, int bInitialState, string lpName); //type changed by Onur for 64-bit compatability

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static public extern int SetEvent(IntPtr eEvent);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static public extern int ResetEvent(IntPtr eEvent);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static public extern SafeFileHandle
      CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, int dwCreationDisposition, uint dwFlagsAndAttributes, int hTemplateFile); //type changed by Onur for 64-bit compatability
		
		[DllImport("kernel32.dll", SetLastError = true)]
        static public extern int ReadFile(SafeFileHandle hFile, IntPtr lpBuffer, int nNumberOfBytesToRead, ref int lpNumberOfBytesRead, ref OVERLAPPED lpOverlapped); //type changed by Onur for 64-bit compatability

		[DllImport("kernel32.dll", SetLastError = true)]
        static public extern int WaitForSingleObject(IntPtr hHandle, int dwMilliseconds); //type changed by Onur for 64-bit compatability

		[DllImport("kernel32.dll", SetLastError = true)]
        static public extern int WriteFile(SafeFileHandle hFile, IntPtr lpBuffer, int nNumberOfBytesToWrite, ref int lpNumberOfBytesWritten, ref OVERLAPPED lpOverlapped); //type changed by Onur for 64-bit compatability
		
		[DllImport("kernel32.dll", SetLastError = true)]
        static public extern int GetOverlappedResult(SafeFileHandle hFile, ref OVERLAPPED lpOverlapped, ref int lpNumberOfBytesTransferred, int bWait);//type changed by Onur for 64-bit compatability

	}
	
}
