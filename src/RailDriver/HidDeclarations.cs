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

        // If IsRange is false, UsageMin is the Usage and UsageMax is unused.
        // If IsStringRange is false, StringMin is the string index and StringMax is unused.
        // If IsDesignatorRange is false, DesignatorMin is the designator index and DesignatorMax is unused.

        [StructLayout(LayoutKind.Sequential)]
        public struct HidP_Value_Caps
        {
            public short UsagePage;
            public byte ReportID;
            public int IsAlias;
            public short BitField;
            public short LinkCollection;
            public short LinkUsage;
            public short LinkUsagePage;
            public int IsRange;
            public int IsStringRange;
            public int IsDesignatorRange;
            public int IsAbsolute;
            public int HasNull;
            public byte Reserved;
            public short BitSize;
            public short ReportCount;
            public short Reserved2;
            public short Reserved3;
            public short Reserved4;
            public short Reserved5;
            public short Reserved6;
            public int LogicalMin;
            public int LogicalMax;
            public int PhysicalMin;
            public int PhysicalMax;
            public short UsageMin;
            public short UsageMax;
            public short StringMin;
            public short StringMax;
            public short DesignatorMin;
            public short DesignatorMax;
            public short DataIndexMin;
            public short DataIndexMax;
        }

        // ******************************************************************************
        // API functions, listed alphabetically
        // ******************************************************************************

        [DllImport("hid.dll")]
        public static extern bool HidD_FlushQueue(SafeFileHandle HidDeviceObject);

        [DllImport("hid.dll")]
        public static extern bool HidD_FreePreparsedData(IntPtr PreparsedData);

        [DllImport("hid.dll")]
        public static extern int HidD_GetAttributes(SafeFileHandle HidDeviceObject, ref HIDD_ATTRIBUTES Attributes); //type changed by Onur for 64-bit compatability

        [DllImport("hid.dll")]
        public static extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, ref byte lpReportBuffer, int ReportBufferLength);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetInputReport(SafeFileHandle HidDeviceObject, ref byte lpReportBuffer, int ReportBufferLength);

        [DllImport("hid.dll")]
        public static extern void HidD_GetHidGuid(ref Guid HidGuid);

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern int HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, StringBuilder buffer, int StringSize);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetNumInputBuffers(SafeFileHandle HidDeviceObject, ref int NumberBuffers);

        [DllImport("hid.dll")]
        public static extern bool HidD_GetPreparsedData(SafeFileHandle HidDeviceObject, ref IntPtr PreparsedData); //type changed by Onur for 64-bit compatability

        [DllImport("hid.dll", CharSet = CharSet.Auto)]
        public static extern int HidD_GetProductString(SafeFileHandle HidDeviceObject, StringBuilder buffer, int StringSize);

        [DllImport("hid.dll")]
        public static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, ref byte lpReportBuffer, int ReportBufferLength);

        [DllImport("hid.dll")]
        public static extern bool HidD_SetNumInputBuffers(SafeFileHandle HidDeviceObject, int NumberBuffers);

        [DllImport("hid.dll")]
        public static extern bool HidD_SetOutputReport(SafeFileHandle HidDeviceObject, ref byte lpReportBuffer, int ReportBufferLength);

        [DllImport("hid.dll")]
        public static extern int HidP_GetCaps(IntPtr PreparsedData, ref HIDP_CAPS Capabilities);

        [DllImport("hid.dll")]
        public static extern int HidP_GetValueCaps(short ReportType, ref byte ValueCaps, ref short ValueCapsLength, IntPtr PreparsedData);

    }

}
