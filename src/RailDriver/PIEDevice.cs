using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

namespace RailDriver
{
    /// <summary>
    /// PIE Device
    /// </summary>
    public class PIEDevice
    {
        private bool connected = false;
        private RingBuffer writeRing;
        private RingBuffer readRing;
        private SafeFileHandle readFileHandle;
        private SafeFileHandle writeFileHandle;
        private IDataHandler registeredDataHandler = null;
        private IErrorHandler registeredErrorHandler = null;
        private IntPtr readFileH;

        private int errCodeR = 0;
        private int errCodeReadError = 0;
        private int errCodeW = 0;
        private int errCodeWriteError = 0;
        private bool holdDataThreadOpen = false;
        private bool holdErrorThreadOpen = false;
        private readonly ManualResetEventSlim writeEvent = new ManualResetEventSlim(false);
        private readonly ManualResetEventSlim readEvent = new ManualResetEventSlim(false);

        private Thread dataThreadHandle;
        private Thread errorThreadHandle;
        private bool readThreadActive = false;
        private bool writeThreadActive = false;
        private bool dataThreadActive = false;
        private bool errorThreadActive = false;

        /// <summary>
        /// Device Path
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Vendor ID
        /// </summary>
        public int Vid { get; }

        /// <summary>
        /// Product ID
        /// </summary>
        public int Pid { get; }

        /// <summary>
        /// Version
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// HID Usage
        /// </summary>
        public int HidUsage { get; }

        /// <summary>
        /// HID Usage Page
        /// </summary>
        public int HidUsagePage { get; }

        /// <summary>
        /// Read Buffer Length
        /// </summary>
        public int ReadLength { get; }

        /// <summary>
        /// Write Buffer Length
        /// </summary>
        public int WriteLength { get; }

        /// <summary>
        /// Manufacturer Name
        /// </summary>
        public string ManufacturersString { get; }

        /// <summary>
        /// Product Name
        /// </summary>
        public string ProductString { get; }

        /// <summary>
        /// Suppresses duplicate/same reports, only reporting changes
        /// </summary>
        public bool SuppressDuplicateReports { get; set; }

        /// <summary>
        /// ???
        /// </summary>
        public bool CallNever { get; set; }

        /// <summary>
        /// public ctor
        /// </summary>
        /// <param name="path"></param>
        /// <param name="vid"></param>
        /// <param name="pid"></param>
        /// <param name="version"></param>
        /// <param name="hidUsage"></param>
        /// <param name="hidUsagePage"></param>
        /// <param name="readSize"></param>
        /// <param name="writeSize"></param>
        /// <param name="manufacturersString"></param>
        /// <param name="productString"></param>
        private PIEDevice(string path, int vid, int pid, int version, int hidUsage, int hidUsagePage, int readSize, int writeSize, string manufacturersString, string productString)
        {
            Path = path;
            Vid = vid;
            Pid = pid;
            Version = version;
            HidUsage = hidUsage;
            HidUsagePage = hidUsagePage;
            ReadLength = readSize;
            WriteLength = writeSize;
            ManufacturersString = manufacturersString;
            ProductString = productString;
        }

        /// <summary>
        /// Translating error codes into messages
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public static string GetErrorString(int error)
        {
            if (!ErrorMessages.Messages.TryGetValue(error, out string message))
                message = "Unknown Error" + error;
            return message;
        }

        private void ErrorThread()
        {
            while (errorThreadActive)
            {
                if (errCodeReadError != 0)
                {
                    holdDataThreadOpen = true;
                    registeredErrorHandler.HandleHidError(this, errCodeReadError);
                    holdDataThreadOpen = false;
                }
                if (errCodeWriteError != 0)
                {
                    holdErrorThreadOpen = true;
                    registeredErrorHandler.HandleHidError(this, errCodeWriteError);
                    holdErrorThreadOpen = false;

                }
                errCodeReadError = 0;
                errCodeWriteError = 0;
                Thread.Sleep(25);
            }
        }

        private async Task ReadFromHid()
        {
            byte[] buffer = new byte[ReadLength];
            if (ReadLength == 0)
            {
                errCodeR = errCodeReadError = 302;
                return;
            }
            if (readRing == null)
            {
                errCodeW = errCodeWriteError = 307;
                return;
            }

            using (FileStream deviceRead = new FileStream(readFileHandle, FileAccess.Read, ReadLength, true))
            {
                while (readThreadActive)
                {
                    if (readFileHandle.IsInvalid)
                    {
                        errCodeReadError = errCodeR = 301;
                        break;
                    }
                    try
                    {
                        if (await deviceRead.ReadAsync(buffer, 0, ReadLength).ConfigureAwait(false) == ReadLength)
                        {
                            if (SuppressDuplicateReports)
                            {
                                if (readRing.TryPutChanged(buffer))
                                    readEvent.Set();
                            }
                            else
                            {
                                readRing.Put(buffer);
                                readEvent.Set();
                            }
                        }
                        else
                        {
                            errCodeR = errCodeReadError = 310;
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        errCodeR = errCodeReadError = 309;
                        break;
                    }
                }
            }
        }

        private async Task WriteToHid()
        {
            byte[] buffer = new byte[WriteLength];
            if (WriteLength == 0)
            {
                errCodeR = errCodeReadError = 402;
                return;
            }
            if (writeRing == null)
            {
                errCodeW = errCodeWriteError = 407;
                return;
            }

            using (FileStream deviceWrite = new FileStream(writeFileHandle, FileAccess.Write, WriteLength, true))
            {
                while (writeThreadActive)
                {
                    while (writeRing.Get(buffer))
                    {
                        if (writeFileHandle.IsInvalid)
                        {
                            errCodeReadError = errCodeR = 401;
                            return;
                        }
                        await deviceWrite.WriteAsync(buffer, 0, WriteLength).ConfigureAwait(false);
                    }
                    writeEvent.Wait(100);
                    writeEvent.Reset();
                }
            }
        }

        private void DataEventThread()
        {
            byte[] currBuff = new byte[ReadLength];

            while (dataThreadActive)
            {
                if (readRing == null)
                    return;
                if (!CallNever)
                {
                    if (errCodeR != 0)
                    {
                        Array.Clear(currBuff, 0, ReadLength);
                        holdDataThreadOpen = true;
                        registeredDataHandler.HandleHidData(currBuff, this, errCodeR);
                        holdDataThreadOpen = false;
                        dataThreadActive = false;
                    }
                    else if (readRing.Get(currBuff))
                    {
                        holdDataThreadOpen = true;
                        registeredDataHandler.HandleHidData(currBuff, this, 0);
                        holdDataThreadOpen = false;
                    }
                    if (readRing.IsEmpty())
                        readEvent.Reset();
                }
                readEvent.Wait(100);
            }
            return;
        }

        //-----------------------------------------------------------------------------
        /// <summary>
        /// Sets connection to the enumerated device. 
        /// If inputReportSize greater than zero it generates a read handle.
        /// If outputReportSize greater than zero it generates a write handle.
        /// </summary>
        /// <returns></returns>
        public int SetupInterface()
        {
            int retin = 0;
            int retout = 0;

            if (connected) return 203;
            if (ReadLength > 0)
            {
                readFileH = FileIOApiDeclarations.CreateFile(Path, FileIOApiDeclarations.GENERIC_READ,
                    FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                    IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, FileIOApiDeclarations.FILE_FLAG_OVERLAPPED, 0);

                readFileHandle = new SafeFileHandle(readFileH, true);
                if (readFileHandle.IsInvalid)
                {
                    readRing = null;
                    retin = 207;
                }
                else
                {
                    readRing = new RingBuffer(128, ReadLength);
                    readThreadActive = true;
                    _ = Task.Run(() => ReadFromHid());
                }
            }

            if (WriteLength > 0)
            {
                IntPtr writeFileH = FileIOApiDeclarations.CreateFile(Path, FileIOApiDeclarations.GENERIC_WRITE,
                      FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                       IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING,
                      FileIOApiDeclarations.FILE_FLAG_OVERLAPPED,
                      0);
                writeFileHandle = new SafeFileHandle(writeFileH, true);
                if (writeFileHandle.IsInvalid)
                {
                    writeRing = null;
                    retout = 208;
                }
                else
                {
                    writeRing = new RingBuffer(128, WriteLength);
                    writeThreadActive = true;
                    _ = Task.Run(() => WriteToHid());
                }
            }

            if ((retin == 0) && (retout == 0))
            {
                connected = true;
                return 0;
            }
            else if ((retin == 207) && (retout == 208))
                return 209;
            else
                return retin + retout;
        }

        /// <summary>
        /// CLoses any open handles and shut down the active interface
        /// </summary>
        public void CloseInterface()
        {
            if ((holdErrorThreadOpen) || (holdDataThreadOpen)) return;

            // Shut down event thread
            if (dataThreadActive)
            {
                dataThreadActive = false;
                readEvent.Set();
            }

            // Shut down read thread
            if (readThreadActive)
            {
                readThreadActive = false;
            }
            if (writeThreadActive)
            {
                writeThreadActive = false;
                writeEvent.Set();
            }
            if (errorThreadActive)
            {
                errorThreadActive = false;
                if (errorThreadHandle != null)
                {
                    int n = 0;
                    while (errorThreadHandle.IsAlive)
                    {
                        Thread.Sleep(10);
                        n++;
                        if (n == 10) { errorThreadHandle.Abort(); break; }
                    }
                    errorThreadHandle = null;
                }
            }

            writeRing = null;
            readRing = null;

            if ((0x00FF != Pid && 0x00FE != Pid && 0x00FD != Pid && 0x00FC != Pid && 0x00FB != Pid) || Version > 272)
            {
                // it's not an old VEC foot pedal (those hang when closing the handle)
                if (readFileHandle != null && !readFileHandle.IsInvalid)
                    readFileHandle.Close();
                if (writeFileHandle != null && !writeFileHandle.IsInvalid)
                    writeFileHandle.Close();
            }
            connected = false;

        }

        /// <summary>
        /// IDataHandler Setup
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public int SetDataCallback(IDataHandler handler)
        {
            if (!connected)
                return 702;
            if (ReadLength == 0)
                return 703;

            if (registeredDataHandler == null)
            {//registeredDataHandler is not defined so define it and create thread. 
                registeredDataHandler = handler;
                dataThreadHandle = new Thread(new ThreadStart(DataEventThread))
                {
                    IsBackground = true,
                    Name = $"PIEHidEventThread for {Pid}"
                };
                dataThreadActive = true;
                dataThreadHandle.Start();
            }
            else
            {
                return 704;//Only the eventType has been changed.
            }
            return 0;
        }

        /// <summary>
        /// IErrorHandler Setup
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public int SetErrorCallback(IErrorHandler handler)
        {
            if (!connected)
                return 802;

            if (registeredErrorHandler == null)
            {
                //registeredErrorHandler is not defined so define it and create thread. 
                registeredErrorHandler = handler;
                errorThreadHandle = new Thread(new ThreadStart(ErrorThread))
                {
                    IsBackground = true,
                    Name = $"PIEHidErrorThread for {Pid}"
                };
                errorThreadActive = true;
                errorThreadHandle.Start();
            }
            else
            {
                return 804;//Error Handler Already Exists.
            }
            return 0;
        }

        /// <summary>
        /// Reading last n bytes from buffer
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public int ReadLast(ref byte[] dest)
        {
            if (ReadLength == 0)
                return 502;
            if (!connected)
                return 507;
            if (dest == null)
                dest = new byte[ReadLength];
            if (dest.Length < ReadLength)
                return 503;
            if (readRing.GetLast(dest) != 0)
                return 504;
            return 0;
        }

        /// <summary>
        /// Reading n bytes from buffer
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public int ReadData(ref byte[] dest)
        {
            if (!connected)
                return 303;
            if (dest == null)
                dest = new byte[ReadLength];
            if (dest.Length < ReadLength)
                return 311;
            if (!readRing.Get(dest))
                return 304;
            return 0;
        }

        /// <summary>
        /// Blocking Read, waiting for data
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="maxMillis"></param>
        /// <returns></returns>
        public int BlockingReadData(ref byte[] dest, int maxMillis)
        {
            long startTicks = DateTime.UtcNow.Ticks;
            int ret = 304;
            int mills = maxMillis;
            while ((mills > 0) && (ret == 304))
            {
                if ((ret = ReadData(ref dest)) == 0) break;
                long nowTicks = DateTime.UtcNow.Ticks;
                mills = maxMillis - ((int)(nowTicks - startTicks) / 10000);
                Thread.Sleep(10);
            }
            return ret;
        }

        /// <summary>
        /// Writing to the device
        /// </summary>
        /// <param name="wData"></param>
        /// <returns></returns>
        public int WriteData(byte[] wData)
        {
            if (WriteLength == 0)
                return 402;
            if (!connected)
                return 406;
            if (wData.Length < WriteLength)
                return 403;
            if (writeRing == null)
                return 405;
            if (errCodeW != 0)
                return errCodeW;
            if (writeRing.TryPut(wData) == 3)
            {
                Thread.Sleep(1);
                return 404;
            }
            writeEvent.Set();
            return 0;
        }


        /// <summary>
        /// Enumerates all valid PIE USB devics.
        /// </summary>
        /// <returns>list of all devices found, ordered by USB port connection</returns>
        public static IList<PIEDevice> EnumeratePIE()
        {
            return EnumeratePIE(0x05F3);
        }

        /// <summary>
        /// Enumerates all valid USB devics of the specified Vid.
        /// </summary>
        /// <returns>list of all devices found, ordered by USB port connection</returns>
        public static IList<PIEDevice> EnumeratePIE(int vid)
        {
            // FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnusedE = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();  
            List<PIEDevice> devices = new List<PIEDevice>();

            // Get all device paths
            Guid guid = Guid.Empty;
            HidApiDeclarations.HidD_GetHidGuid(ref guid);
            IntPtr deviceInfoSet = DeviceManagementApiDeclarations.SetupDiGetClassDevs(ref guid, null, IntPtr.Zero,
                DeviceManagementApiDeclarations.DIGCF_PRESENT | DeviceManagementApiDeclarations.DIGCF_DEVICEINTERFACE);

            DeviceManagementApiDeclarations.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new DeviceManagementApiDeclarations.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData); //28;

            List<string> paths = new List<string>();

            for (int i = 0; 0 != DeviceManagementApiDeclarations.SetupDiEnumDeviceInterfaces(deviceInfoSet, 0, ref guid, i, ref deviceInterfaceData); i++)
            {
                int buffSize = 0;
                DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref buffSize, IntPtr.Zero);
                // Use IntPtr to simulate detail data structure
                IntPtr detailBuffer = Marshal.AllocHGlobal(buffSize);

                // sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA) depends on the process bitness,
                // it's 6 with an X86 process (byte packing + 1 char, auto -> unicode -> 4 + 2*1)
                // and 8 with an X64 process (8 bytes packing anyway).
                Marshal.WriteInt32(detailBuffer, Environment.Is64BitProcess ? 8 : 6);

                if (DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailBuffer, buffSize, ref buffSize, IntPtr.Zero))
                {
                    // convert buffer (starting past the cbsize variable) to string path
                    paths.Add(Marshal.PtrToStringAuto(detailBuffer + 4));
                }
            }
            _ = DeviceManagementApiDeclarations.SetupDiDestroyDeviceInfoList(deviceInfoSet);

            foreach (string devicePath in paths)
            {
                IntPtr fileH = FileIOApiDeclarations.CreateFile(devicePath, FileIOApiDeclarations.GENERIC_WRITE,
                    FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                    IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, 0, 0);
                SafeFileHandle fileHandle = new SafeFileHandle(fileH, true);
                if (fileHandle.IsInvalid)
                {
                    // Bad handle, try next path
                    continue;
                }
                try
                {
                    HidApiDeclarations.HIDD_ATTRIBUTES hidAttributes = new HidApiDeclarations.HIDD_ATTRIBUTES();
                    hidAttributes.Size = Marshal.SizeOf(hidAttributes);
                    if (0 != HidApiDeclarations.HidD_GetAttributes(fileHandle, ref hidAttributes) && hidAttributes.VendorID == vid)
                    {
                        // Good attributes and right vid, try to get caps
                        IntPtr pPerparsedData = new IntPtr();
                        if (HidApiDeclarations.HidD_GetPreparsedData(fileHandle, ref pPerparsedData))
                        {
                            HidApiDeclarations.HIDP_CAPS hidCaps = new HidApiDeclarations.HIDP_CAPS();
                            if (0 != HidApiDeclarations.HidP_GetCaps(pPerparsedData, ref hidCaps))
                            {
                                // Got Capabilities, add device to list
                                char[] Mstring = new char[128];
                                StringBuilder manufacturer = new StringBuilder();
                                StringBuilder productName = new StringBuilder();
                                _ = HidApiDeclarations.HidD_GetManufacturerString(fileHandle, manufacturer, 128);
                                _ = HidApiDeclarations.HidD_GetProductString(fileHandle, productName, 128);

                                devices.Add(new PIEDevice(devicePath, hidAttributes.VendorID, hidAttributes.ProductID, hidAttributes.VersionNumber, hidCaps.Usage,
                                    hidCaps.UsagePage, hidCaps.InputReportByteLength, hidCaps.OutputReportByteLength, manufacturer.ToString(), productName.ToString()));
                            }

                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
                finally
                {
                    fileHandle.Close();
                }
            }
            return devices;
        }
    }
}
