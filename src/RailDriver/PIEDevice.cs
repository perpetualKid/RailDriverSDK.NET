using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Win32.SafeHandles;

using static RailDriver.ResultCode;

namespace RailDriver
{
    /// <summary>
    /// PIE Device
    /// </summary>
    public sealed class PIEDevice : IDisposable
    {
        private const int RingBufferCapacity = 128;
        private const int HidStringBufferLength = 128;
        private const int PieVendorId = 0x05F3;
        private const int ErrorPollIntervalMilliseconds = 25;
        private const int BlockingReadPollIntervalMilliseconds = 10;
        private const int DeviceInterfaceDetailDataOffset = 4;

        private volatile bool connected;
        private volatile RingBuffer writeRing;
        private volatile RingBuffer readRing;
        private SafeFileHandle readFileHandle;
        private SafeFileHandle writeFileHandle;
        private IDataHandler registeredDataHandler;
        private IErrorHandler registeredErrorHandler;

        private volatile int errCodeReadError;
        private volatile int errCodeWriteError;
        private readonly AsyncManualResetEvent writeEvent = new AsyncManualResetEvent();
        private readonly AsyncManualResetEvent readEvent = new AsyncManualResetEvent();
        private CancellationTokenSource cts;
        private Task readTask;
        private Task writeTask;
        private Task dataTask;
        private Task errorTask;
        private bool disposedValue;

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
        /// When set to <see langword="true"/>, the registered data handler is not invoked
        /// for incoming reports. Used to temporarily pause data callbacks without unregistering.
        /// </summary>
        public bool CallNever { get; set; }

        /// <summary>
        /// Initializes a new <see cref="PIEDevice"/> from values discovered during enumeration.
        /// </summary>
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

        private async Task ErrorEvent()
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    if (errCodeReadError != Success)
                    {
                        registeredErrorHandler.HandleHidError(this, errCodeReadError);
                    }
                    if (errCodeWriteError != Success)
                    {
                        registeredErrorHandler.HandleHidError(this, errCodeWriteError);
                    }
                    errCodeReadError = Success;
                    errCodeWriteError = Success;

                    await Task.Delay(ErrorPollIntervalMilliseconds, cts.Token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async Task ReadFromHid()
        {
            byte[] buffer = new byte[ReadLength];
            if (ReadLength == 0)
            {
                errCodeReadError = ReadLengthZero;
                return;
            }
            if (readRing == null)
            {
                errCodeReadError = NoReadBuffer;
                return;
            }

            using (FileStream deviceRead = new FileStream(readFileHandle, FileAccess.Read, ReadLength, true))
            {
                while (!cts.IsCancellationRequested)
                {
                    if (readFileHandle.IsInvalid)
                    {
                        errCodeReadError = BadReadInterfaceHandle;
                        break;
                    }
                    try
                    {
                        if (await deviceRead.ReadAsync(buffer, 0, ReadLength, cts.Token).ConfigureAwait(false) == ReadLength)
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
                            errCodeReadError = BytesReadNotEqualReadSize;
                            break;
                        }
                    }
                    catch (IOException)
                    {
                        errCodeReadError = ReadError;
                        connected = false;
                        break;
                    }
                    catch (OperationCanceledException)
                    { }
                }
            }
        }

        private async Task WriteToHid()
        {
            byte[] buffer = new byte[WriteLength];
            if (WriteLength == 0)
            {
                errCodeWriteError = WriteLengthZero;
                return;
            }
            if (writeRing == null)
            {
                errCodeWriteError = NoWriteBufferForWorker;
                return;
            }

            using (FileStream deviceWrite = new FileStream(writeFileHandle, FileAccess.Write, WriteLength, true))
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        await writeEvent.WaitAsync(cts.Token).ConfigureAwait(false);
                        while (writeRing.Get(buffer))
                        {
                            if (writeFileHandle.IsInvalid)
                            {
                                errCodeWriteError = BadWriteInterfaceHandle;
                                return;
                            }
                            await deviceWrite.WriteAsync(buffer, 0, WriteLength, cts.Token).ConfigureAwait(false);
                        }
                        writeEvent.Reset();
                    }
                    catch (IOException)
                    {
                        errCodeWriteError = WriteError;
                        connected = false;
                        break;
                    }
                    catch (OperationCanceledException)
                    { }
                }
            }
        }

        private async Task DataEvent()
        {
            byte[] buffer = new byte[ReadLength];

            if (readRing == null)
                return;

            while (!cts.IsCancellationRequested)
            {
                await readEvent.WaitAsync(cts.Token).ConfigureAwait(false);
                if (!CallNever)
                {
                    if (errCodeReadError != Success)
                    {
                        Array.Clear(buffer, 0, ReadLength);
                        registeredDataHandler.HandleHidData(buffer, this, errCodeReadError);
                    }
                    else if (readRing.Get(buffer))
                    {
                        registeredDataHandler.HandleHidData(buffer, this, Success);
                    }
                    if (readRing.IsEmpty())
                        readEvent.Reset();
                }
            }
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

            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }
            cts = new CancellationTokenSource();

            if (connected) return AlreadyConnected;
            if (ReadLength > 0)
            {
                IntPtr readFileH = FileIOApiDeclarations.CreateFile(Path, FileIOApiDeclarations.GENERIC_READ,
                    FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                    IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, FileIOApiDeclarations.FILE_FLAG_OVERLAPPED, 0);

                readFileHandle = new SafeFileHandle(readFileH, true);
                if (readFileHandle.IsInvalid)
                {
                    readRing = null;
                    retin = CannotOpenReadHandle;
                }
                else
                {
                    readRing = new RingBuffer(RingBufferCapacity, ReadLength);
                    readTask = ReadFromHid();
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
                    retout = CannotOpenWriteHandle;
                }
                else
                {
                    writeRing = new RingBuffer(RingBufferCapacity, WriteLength);
                    writeTask = WriteToHid();
                }
            }

            if ((retin == 0) && (retout == 0))
            {
                connected = true;
                return Success;
            }
            else if ((retin == CannotOpenReadHandle) && (retout == CannotOpenWriteHandle))
                return CannotOpenEitherHandle;
            else
                return retin + retout;
        }

        /// <summary>
        /// CLoses any open handles and shut down the active interface
        /// </summary>
        public void CloseInterface()
        {
            cts?.Cancel();
            writeEvent.Set();
            readEvent.Set();
            WaitForBackgroundTasks();

            writeRing = null;
            readRing = null;

            if ((Pid != 0x00FF && Pid != 0x00FE && Pid != 0x00FD && Pid != 0x00FC && Pid != 0x00FB) || Version > 272)
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
                return DataCallbackInterfaceNotValid;
            if (ReadLength == 0)
                return InputReportSizeZero;

            if (registeredDataHandler == null)
            {
                //registeredDataHandler is not defined so define it and create thread. 
                registeredDataHandler = handler;
                dataTask = DataEvent();
            }
            else
            {
                return DataHandlerAlreadyExists;//Only the eventType has been changed.
            }
            return Success;
        }

        /// <summary>
        /// IErrorHandler Setup
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public int SetErrorCallback(IErrorHandler handler)
        {
            if (!connected)
                return ErrorCallbackInterfaceNotValid;

            if (registeredErrorHandler == null)
            {
                //registeredErrorHandler is not defined so define it and create thread. 
                registeredErrorHandler = handler;
                errorTask = ErrorEvent();
            }
            else
            {
                return ErrorHandlerAlreadyExists;//Error Handler Already Exists.
            }
            return Success;
        }

        /// <summary>
        /// Reading last n bytes from buffer
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public int ReadLast(ref byte[] dest)
        {
            if (ReadLength == 0)
                return ReadLastLengthZero;
            if (!connected)
                return ReadLastInterfaceNotValid;
            if (dest == null)
                dest = new byte[ReadLength];
            if (dest.Length < ReadLength)
                return ReadLastDestinationTooSmall;
            if (readRing.GetLast(dest) != Success)
                return NoDataYet;
            return Success;
        }

        /// <summary>
        /// Reading n bytes from buffer
        /// </summary>
        /// <param name="dest"></param>
        /// <returns></returns>
        public int ReadData(ref byte[] dest)
        {
            if (!connected)
                return ReadInterfaceNotValid;
            if (dest == null)
                dest = new byte[ReadLength];
            if (dest.Length < ReadLength)
                return ReadDestinationTooSmall;
            if (!readRing.Get(dest))
                return RingBufferEmpty;
            return Success;
        }

        /// <summary>
        /// Blocking Read, waiting for data
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="maxMillis"></param>
        /// <returns></returns>
        public int BlockingReadData(ref byte[] dest, int maxMillis)
        {
            DateTime timeout = DateTime.UtcNow.AddMilliseconds(maxMillis);
            int result = RingBufferEmpty;
            while ((timeout > DateTime.UtcNow) && (result == RingBufferEmpty))
            {
                if ((result = ReadData(ref dest)) == Success) 
                    break;
                Thread.Sleep(BlockingReadPollIntervalMilliseconds);
            }
            return result;
        }

        /// <summary>
        /// Writing to the device
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public int WriteData(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            if (WriteLength == 0)
                return WriteLengthZero;
            if (!connected)
                return WriteInterfaceNotValid;
            if (buffer.Length < WriteLength)
                return WriteDestinationTooSmall;
            if (writeRing == null)
                return NoWriteBuffer;
            if (errCodeWriteError != Success)
                return errCodeWriteError;
            if (!writeRing.TryPut(buffer))
            {
                return WriteBufferFull;
            }
            writeEvent.Set();
            return Success;
        }


        /// <summary>
        /// Enumerates all valid PIE USB devics.
        /// </summary>
        /// <returns>list of all devices found, ordered by USB port connection</returns>
        public static IList<PIEDevice> EnumeratePIE()
        {
            return EnumeratePIE(PieVendorId);
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
                try
                {
                    // sizeof(SP_DEVICE_INTERFACE_DETAIL_DATA) depends on the process bitness,
                    // it's 6 with an X86 process (byte packing + 1 char, auto -> unicode -> 4 + 2*1)
                    // and 8 with an X64 process (8 bytes packing anyway).
                    Marshal.WriteInt32(detailBuffer, Environment.Is64BitProcess ? 8 : 6);

                    if (DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailBuffer, buffSize, ref buffSize, IntPtr.Zero))
                    {
                        // convert buffer (starting past the cbsize variable) to string path
                        paths.Add(Marshal.PtrToStringAuto(detailBuffer + DeviceInterfaceDetailDataOffset));
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(detailBuffer);
                }
            }
            _ = DeviceManagementApiDeclarations.SetupDiDestroyDeviceInfoList(deviceInfoSet);

            foreach (string devicePath in paths)
            {
                IntPtr fileH = FileIOApiDeclarations.CreateFile(devicePath, FileIOApiDeclarations.GENERIC_WRITE,
                    FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                    IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, 0, 0);
                using (SafeFileHandle fileHandle = new SafeFileHandle(fileH, true))
                {
                    if (fileHandle.IsInvalid)
                    {
                        // Bad handle, try next path
                        continue;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
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
                                    StringBuilder manufacturer = new StringBuilder();
                                    StringBuilder productName = new StringBuilder();
                                    _ = HidApiDeclarations.HidD_GetManufacturerString(fileHandle, manufacturer, HidStringBufferLength);
                                    _ = HidApiDeclarations.HidD_GetProductString(fileHandle, productName, HidStringBufferLength);

                                    devices.Add(new PIEDevice(devicePath, hidAttributes.VendorID, hidAttributes.ProductID, hidAttributes.VersionNumber, hidCaps.Usage,
                                        hidCaps.UsagePage, hidCaps.InputReportByteLength, hidCaps.OutputReportByteLength, manufacturer.ToString(), productName.ToString()));
                                }

                            }
                        }
                    }
                    // The device is probed independently; any failure here just skips this path.
                    catch (Exception) { }
#pragma warning restore CA1031 // Do not catch general exception types
                }
            }
            return devices;
        }

        private void WaitForBackgroundTasks()
        {
            List<Task> tasks = new List<Task>(4);
            if (readTask != null)
                tasks.Add(readTask);
            if (writeTask != null)
                tasks.Add(writeTask);
            if (dataTask != null)
                tasks.Add(dataTask);
            if (errorTask != null)
                tasks.Add(errorTask);

            if (tasks.Count == 0)
                return;

            try
            {
                Task.WaitAll(tasks.ToArray(), TimeSpan.FromSeconds(1));
            }
            catch (AggregateException ex)
            {
                ex.Handle(e => e is OperationCanceledException);
            }

            readTask = null;
            writeTask = null;
            dataTask = null;
            errorTask = null;
        }

        /// <summary>
        /// Releases the cancellation source and device handles held by this instance.
        /// </summary>
        public void Dispose()
        {
            if (disposedValue)
                return;

            disposedValue = true;
            if (cts != null && !cts.IsCancellationRequested)
                cts.Cancel();
            cts?.Dispose();
            readFileHandle?.Dispose();
            writeFileHandle?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
