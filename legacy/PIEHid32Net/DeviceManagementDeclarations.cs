using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace PIEHid32Net
{
	
	sealed class DeviceManagementApiDeclarations
	{
		
		// API declarations relating to device management (SetupDixxx and
		// RegisterDeviceNotification functions). cimbom
		
		// ******************************************************************************
		// API constants
		// ******************************************************************************
		
		// from dbt.h
		public const int DBT_DEVICEARRIVAL = 0x8000;
		public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
		public const int DBT_DEVTYP_DEVICEINTERFACE = 5;
		public const int DBT_DEVTYP_HANDLE = 6;
		public const int DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
		public const int DEVICE_NOTIFY_SERVICE_HANDLE = 1;
		public const int DEVICE_NOTIFY_WINDOW_HANDLE = 0;
		public const int WM_DEVICECHANGE = 0x219;
		
		// from setupapi.h
		public const short DIGCF_PRESENT = 0x00000002;
		public const short DIGCF_DEVICEINTERFACE = 0x00000010;
		
		// ******************************************************************************
		// Structures and classes for API calls, listed alphabetically
		// ******************************************************************************
		
		// There are two declarations for the DEV_BROADCAST_DEVICEINTERFACE structure.
		
		// Use this in the call to RegisterDeviceNotification() and
		// in checking dbch_devicetype in a DEV_BROADCAST_HDR structure.
		[StructLayout(LayoutKind.Sequential)]
    public class DEV_BROADCAST_DEVICEINTERFACE
		{
			public int dbcc_size;
			public int dbcc_devicetype;
			public int dbcc_reserved;
			public Guid dbcc_classguid;
			public short dbcc_name;
		}
		
		// Use this to read the dbcc_name string and classguid.
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    public class DEV_BROADCAST_DEVICEINTERFACE_1
		{
			public int dbcc_size;
			public int dbcc_devicetype;
			public int dbcc_reserved;
			[MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.U1, SizeConst=16)]public byte[] dbcc_classguid;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=255)]public char[] dbcc_name;
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public class DEV_BROADCAST_HANDLE
		{
			public int dbch_size;
			public int dbch_devicetype;
			public int dbch_reserved;
			public int dbch_handle;
			public int dbch_hdevnotify;
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public class DEV_BROADCAST_HDR
		{
			public int dbch_size;
			public int dbch_devicetype;
			public int dbch_reserved;
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DATA
		{
			public int cbSize;
			public System.Guid InterfaceClassGuid;
			public int Flags;
            public IntPtr Reserved; //type modified by Onur for 64-bit
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVICE_INTERFACE_DETAIL_DATA
		{
			public int cbSize;
			public string DevicePath;
		}
		
		[StructLayout(LayoutKind.Sequential)]
    public struct SP_DEVINFO_DATA
		{
			public int cbSize;
			public System.Guid ClassGuid;
			public int DevInst;
            public IntPtr Reserved; //type modified by Onur for 64-bit
		}
		
		// ******************************************************************************
		// API functions, listed alphabetically
		// ******************************************************************************
		
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Auto)]
    static public extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, int Flags);

		[DllImport("setupapi.dll", SetLastError = true)]
        static public extern int SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, IntPtr hwndParent);

		[DllImport("setupapi.dll", SetLastError = true)]
    static public extern int SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

		[DllImport("setupapi.dll", SetLastError = true)]
    static public extern int SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, int DeviceInfoData, ref System.Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static public extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);//onur 0726

		[DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static public extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

		[DllImport("user32.dll", SetLastError = true)]
    static public extern bool UnregisterDeviceNotification(IntPtr Handle);

		[DllImport("kernel32.dll", SetLastError = true)]
		static public extern bool DeviceIoControl(SafeFileHandle handle, uint dwIoControlCode, ref uint lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, ref uint lpBytesReturned, ref FileIOApiDeclarations.OVERLAPPED lpOverlapped);

        //added by Onur for 64-bit
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool Wow64DisableWow64FsRedirection(ref IntPtr ptr);
	}
	
}
