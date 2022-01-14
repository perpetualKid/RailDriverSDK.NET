using System;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace RailDriver
{
    internal sealed class HidApiDeclarations
    {

        // API Declarations for communicating with HID-class devices. 

        // ******************************************************************************
        // API constants
        // ******************************************************************************

        // from hidpi.h
        // Typedef enum defines a set of integer constants for HidP_Report_Type
        public const short HidP_Input = 0;
        public const short HidP_Output = 1;
        public const short HidP_Feature = 2;

        // ******************************************************************************
        // Structures and classes for API calls, listed alphabetically
        // ******************************************************************************

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDD_ATTRIBUTES
        {
            public int Size;
            public ushort VendorID;
            public ushort ProductID;
            public ushort VersionNumber;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct HIDP_CAPS
        {
            public ushort Usage;
            public ushort UsagePage;
            public ushort InputReportByteLength;
            public ushort OutputReportByteLength;
            public ushort FeatureReportByteLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 17)]
            public ushort[] Reserved;
            public ushort NumberLinkCollectionNodes;
            public ushort NumberInputButtonCaps;
            public ushort NumberInputValueCaps;
            public ushort NumberInputDataIndices;
            public ushort NumberOutputButtonCaps;
            public ushort NumberOutputValueCaps;
            public ushort NumberOutputDataIndices;
            public ushort NumberFeatureButtonCaps;
            public ushort NumberFeatureValueCaps;
            public ushort NumberFeatureDataIndices;

        }

        // ******************************************************************************
        // API functions, listed alphabetically
        // ******************************************************************************

        [DllImport("hid.dll")]
        public static extern int HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes); //type changed by Onur for 64-bit compatability

        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(ref Guid HidGuid);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern int HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, StringBuilder buffer, int StringSize);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, ref IntPtr PreparsedData); //type changed by Onur for 64-bit compatability

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern int HidD_GetProductString(SafeFileHandle HidDeviceObject, StringBuilder buffer, int StringSize);

        [DllImport("hid.dll")]
        public static extern int HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);
    }

}
