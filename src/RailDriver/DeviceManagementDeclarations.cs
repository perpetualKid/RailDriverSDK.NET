using System;
using System.Runtime.InteropServices;

namespace RailDriver
{
    internal static class DeviceManagementApiDeclarations
    {

        // API declarations relating to device management (SetupDixxx functions).

        // ******************************************************************************
        // API constants
        // ******************************************************************************

        // from setupapi.h
        public const short DIGCF_PRESENT = 0x00000002;
        public const short DIGCF_DEVICEINTERFACE = 0x00000010;

        // ******************************************************************************
        // Structures and classes for API calls, listed alphabetically
        // ******************************************************************************

        [StructLayout(LayoutKind.Sequential)]
        public struct SP_DEVICE_INTERFACE_DATA
        {
            public int Size;
            public Guid InterfaceClassGuid;
            public int Flags;
            public IntPtr Reserved; //type modified by Onur for 64-bit
        }

        // ******************************************************************************
        // API functions, listed alphabetically
        // ******************************************************************************

        [DllImport("setupapi.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, int DeviceInfoData, ref Guid InterfaceClassGuid, int MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, int Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, int DeviceInterfaceDetailDataSize, ref int RequiredSize, IntPtr DeviceInfoData);

    }

}
