using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

using Microsoft.Win32.SafeHandles;

namespace RailDriver
{
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
        private int errCodeRE = 0;
        private int errCodeW = 0;
        private int errCodeWE = 0;
        private bool holdDataThreadOpen = false;
        private bool holdErrorThreadOpen = false;
        private FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnused = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
        private IntPtr readEvent;
        private IntPtr writeEvent;

        private Thread readThreadHandle;
        private Thread dataThreadHandle;
        private Thread writeThreadHandle;
        private Thread errorThreadHandle;
        private bool readThreadActive = false;
        private bool writeThreadActive = false;
        private bool dataThreadActive = false;
        private bool errorThreadActive = false;
        private static readonly ushort[] convertToSplatModeSausages = { 7, 5, 4, 3, 2, 1 };
        private static readonly ushort[] ledSausages = { 7, 3, 1, 6, 4, 2 };

        public string Path { get; }

        public int Vid { get; }

        public int Pid { get; }

        public int Version { get; }

        public int HidUsage { get; }

        public int HidUsagePage { get; }

        public int ReadLength { get; }

        public int WriteLength { get; }

        public string ManufacturersString { get; }

        public string ProductString { get; }

        public bool SuppressDuplicateReports { get; set; }

        public bool CallNever { get; set; }

        public PIEDevice(string path, int vid, int pid, int version, int hidUsage, int hidUsagePage, int readSize, int writeSize, string manufacturersString, string productString)
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
            securityAttrUnused.bInheritHandle = 1;
        }
        //--------------------------------------------------------------------------------------------

        public static string GetErrorString(int errNumb)
        {
            int[] EDS = new int[100];
            string[] EDA = new string[100];
            string st;

            EDS[0] = 0; EDA[0] = "000 Success";

            EDS[1] = 101; EDA[1] = "101 ";
            EDS[2] = 102; EDA[2] = "102 ";
            EDS[4] = 104; EDA[4] = "104 ";
            EDS[5] = 105; EDA[5] = "105 ";
            EDS[6] = 106; EDA[6] = "106 ";
            EDS[7] = 107; EDA[7] = "107 ";
            EDS[8] = 108; EDA[8] = "108 ";
            EDS[9] = 109; EDA[9] = "109 ";
            EDS[10] = 110; EDA[10] = "110 ";
            EDS[11] = 111; EDA[11] = "111 ";
            EDS[12] = 112; EDA[12] = "112 ";


            EDS[13] = 201; EDA[13] = "201 ";
            EDS[14] = 202; EDA[14] = "202 ";
            EDS[53] = 203; EDA[53] = "203 Already Connected";
            EDS[15] = 207; EDA[15] = "207 Cannot open read handle";
            EDS[16] = 204; EDA[16] = "204 ";
            EDS[17] = 205; EDA[17] = "205 ";
            EDS[18] = 208; EDA[18] = "208 Cannot open write handle";
            EDS[19] = 209; EDA[19] = "209 Cannot open either handle";
            EDS[20] = 210; EDA[20] = "210 ";


            EDS[21] = 301; EDA[21] = "301 Bad interface handle";
            EDS[22] = 302; EDA[22] = "302 readSize is zero";
            EDS[23] = 303; EDA[23] = "303 Interface not valid";
            EDS[24] = 304; EDA[24] = "304 Ring buffer empty.";
            EDS[25] = 305; EDA[25] = "305 ";
            EDS[26] = 307; EDA[26] = "307 ";
            EDS[27] = 308; EDA[27] = "308 Device disconnected";
            EDS[28] = 309; EDA[28] = "309 Read error. ( unplugged )";
            EDS[29] = 310; EDA[29] = "310 Bytes read not equal readSize";
            EDS[30] = 311; EDA[30] = "311 dest.Length<ReportSize";


            EDS[31] = 401; EDA[31] = "401 ";
            EDS[32] = 402; EDA[32] = "402 Write length is zero";
            EDS[33] = 403; EDA[33] = "403 wData.Length<ReportSize";
            EDS[34] = 404; EDA[34] = "404 WriteBuffer full--retry";
            EDS[35] = 405; EDA[35] = "405 No write buffer";
            EDS[36] = 406; EDA[36] = "406 Interface not valid";
            EDS[37] = 407; EDA[37] = "407 No writeBuffer";
            EDS[38] = 408; EDA[38] = "408 Device disconnected";
            EDS[55] = 409; EDA[55] = "409 Unknown write error";
            EDS[56] = 410; EDA[56] = "410 byteCount != writeSize";
            EDS[57] = 411; EDA[57] = "411 Timed out in write.";
            EDS[58] = 412; EDA[58] = "412 Report ID error";
            EDS[39] = 501; EDA[39] = "501 ";
            EDS[40] = 502; EDA[40] = "502 Read length is zero";
            EDS[41] = 503; EDA[41] = "503 dest.Length<ReportSize";
            EDS[42] = 504; EDA[42] = "504 No data yet.";
            EDS[43] = 507; EDA[43] = "507 Interface not valid.";

            EDS[44] = 601; EDA[44] = "601 ";
            EDS[45] = 602; EDA[45] = "602 ";


            EDS[46] = 701; EDA[46] = "701 ";
            EDS[47] = 702; EDA[47] = "702 Interface not valid";
            EDS[48] = 703; EDA[48] = "703 Input ReportSize Zero";
            EDS[49] = 704; EDA[49] = "704 Data Handler Already Exists";

            EDS[50] = 801; EDA[50] = "801 ";
            EDS[51] = 802; EDA[51] = "802 Interface not valid";
            EDS[52] = 803; EDA[52] = "803 ";
            EDS[54] = 804; EDA[54] = "804 Error Handler Already Exists";
            st = "Unknown Error" + errNumb;
            for (int i = 0; i < 59; i++)
            {
                if (EDS[i] == errNumb)
                {
                    st = EDA[i];
                    break;
                }
            }
            // Error = st;
            return st;
        }

        //-----------------------------------------------------------------------------------------
        protected void ErrorThread()
        {
            while (errorThreadActive)
            {
                if (errCodeRE != 0)
                {
                    holdDataThreadOpen = true;
                    registeredErrorHandler.HandleHidError(this, errCodeRE);
                    holdDataThreadOpen = false;
                }
                if (errCodeWE != 0)
                {
                    holdErrorThreadOpen = true;
                    registeredErrorHandler.HandleHidError(this, errCodeWE);
                    holdErrorThreadOpen = false;

                }
                errCodeRE = 0;
                errCodeWE = 0;
                Thread.Sleep(25);
            }
        }
        //-------------------------------------------------------------------------------------------
        /// <summary>
        /// Write Thread
        /// </summary>
        protected void WriteThread()
        {
            //   FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnused = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
            IntPtr overlapEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
            FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED
            {
                Offset = 0,
                OffsetHigh = 0,
                hEvent = overlapEvent,
                Internal = IntPtr.Zero,
                InternalHigh = IntPtr.Zero
            };
            if (WriteLength == 0) 
                return;

            byte[] buffer = new byte[WriteLength];
            GCHandle wgch = GCHandle.Alloc(buffer, GCHandleType.Pinned); //onur March 2009 - pinning is reuired

            int byteCount = 0; ;
            //  int loopCount = 0;

            errCodeW = 0;
            errCodeWE = 0;
            while (writeThreadActive)
            {
                if (writeRing == null) { errCodeW = 407; errCodeWE = 407; goto Error; }
                while (writeRing.Get((byte[])buffer) == 0)
                {
                    if (0 == FileIOApiDeclarations.WriteFile(writeFileHandle, wgch.AddrOfPinnedObject(), WriteLength, ref byteCount, ref overlapped))
                    {
                        int result = Marshal.GetLastWin32Error();
                        if (result != FileIOApiDeclarations.ERROR_IO_PENDING)
                        //if ((result == FileIOApiDeclarations.ERROR_INVALID_HANDLE) ||
                        //    (result == FileIOApiDeclarations.ERROR_DEVICE_NOT_CONNECTED))
                        {
                            if (result == 87) 
                            { 
                                errCodeW = 412; 
                                errCodeWE = 412; 
                            }
                            else 
                            { 
                                errCodeW = result; 
                                errCodeWE = 408; 
                            }
                            goto Error;
                        }//if (result ==
                        else
                        {
                            // loopCount = 0;
                            // while (writeThreadActive)
                            // {

                            result = FileIOApiDeclarations.WaitForSingleObject(overlapEvent, 1000);
                            if (result == FileIOApiDeclarations.WAIT_OBJECT_0)
                            {
                                // errCodeWE=1400+loopCount;
                                goto WriteCompleted;
                            }
                            //  loopCount++;
                            //  if (loopCount > 10)
                            // {
                            errCodeW = 411;
                            errCodeWE = 411;

                            goto Error;

                            //  }
                            /*    if (0 == FileIOApiDeclarations.GetOverlappedResult(readFileHandle, ref overlapped, ref byteCount, 0))
                                {
                                    result = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                                    // if (result == ERROR_INVALID_HANDLE || result == ERROR_DEVICE_NOT_CONNECTED)
                                    if (result != FileIOApiDeclarations.ERROR_IO_INCOMPLETE)
                                    {
                                        errCodeW = 409;
                                        errCodeWE = 409;

                                        goto Error;
                                    }//if(result == ERROR_INVALID_HANDLE
                                }//if(!GetOverlappedResult*/
                            //  }//while
                            //   goto Error;
                        }//else if(result==ERROR_IO_PENDING){
                         // continue;
                    }
                    else
                    {
                        if ((long)byteCount != WriteLength)
                        {
                            errCodeW = 410;
                            errCodeWE = 410;
                        }
                        //iface->errCodeWE=1399;
                    }
                WriteCompleted:;
                }//while(get==
                _ = FileIOApiDeclarations.WaitForSingleObject(writeEvent, 100);
                _ = FileIOApiDeclarations.ResetEvent(writeEvent);
                // System.Threading.Thread.Sleep(100);
            }
        Error:
            wgch.Free(); //onur

            return;
        }

        protected void ReadThread()
        {

            IntPtr overlapEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
            FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED
            {
                Offset = 0,
                OffsetHigh = 0,
                hEvent = overlapEvent,
                Internal = IntPtr.Zero,
                InternalHigh = IntPtr.Zero
            };
            if (ReadLength == 0)
            {
                errCodeR = 302;
                errCodeRE = 302;
                return;
            }
            errCodeR = 0;
            errCodeRE = 0;

            byte[] buffer = new byte[ReadLength];
            GCHandle gch = GCHandle.Alloc(buffer, GCHandleType.Pinned); //onur March 2009 - pinning is required

            while (readThreadActive)
            {

                int dataRead = 0;//FileIOApiDeclarations.
                if (readFileHandle.IsInvalid) 
                { 
                    errCodeRE = errCodeR = 320; 
                    goto EXit; 
                }

                if (0 == FileIOApiDeclarations.ReadFile(readFileHandle, gch.AddrOfPinnedObject(), ReadLength, ref dataRead, ref overlapped)) //ref readFileBuffer[0]
                {
                    int result = Marshal.GetLastWin32Error();
                    if (result != FileIOApiDeclarations.ERROR_IO_PENDING) //|| result == FileIOApiDeclarations.ERROR_DEVICE_NOT_CONNECTED)
                    {
                        if (readFileHandle.IsInvalid) { errCodeRE = errCodeR = 321; goto EXit; }
                        errCodeR = result;
                        errCodeRE = 308;
                        goto EXit;
                    }
                    else //if (result != .ERROR_IO_PENDING)
                    {
                        // gch.Free(); //onur
                        while (readThreadActive)
                        {

                            result = FileIOApiDeclarations.WaitForSingleObject(overlapEvent, 50);
                            if (FileIOApiDeclarations.WAIT_OBJECT_0 == result)
                            {
                                if (0 == FileIOApiDeclarations.GetOverlappedResult(readFileHandle, ref overlapped, ref dataRead, 0))
                                {
                                    result = Marshal.GetLastWin32Error();
                                    if (result == FileIOApiDeclarations.ERROR_INVALID_HANDLE || result == FileIOApiDeclarations.ERROR_DEVICE_NOT_CONNECTED)
                                    {

                                        errCodeR = 309;
                                        errCodeRE = 309;
                                        goto EXit;

                                    }
                                }
                                // buffer[0] = 89;
                                goto ReadCompleted;
                            }
                        }//while

                    }//if (result != .ERROR_IO_PENDING)...else 
                    continue;
                }
            //buffer[0] = 90;
            ReadCompleted:
                if (dataRead != ReadLength) { errCodeR = 310; errCodeRE = 310; goto EXit; }

                if (SuppressDuplicateReports)
                {
                    int r = readRing.PutIfDiff(buffer);
                    if (r == 0)
                        _ = FileIOApiDeclarations.SetEvent(readEvent);
                }
                else
                {
                    readRing.Put(buffer);
                    _ = FileIOApiDeclarations.SetEvent(readEvent);
                }

            }//while

        EXit:
            _ = FileIOApiDeclarations.CancelIo(readFileHandle);
            readFileHandle = null;
            gch.Free();
            return;
        }
        //------------------------------------------------------------------------------------------

        //----------------------------------------------------------------------------------------
        protected void DataEventThread()
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
                    else if (readRing.Get(currBuff) == 0)
                    {
                        holdDataThreadOpen = true;
                        registeredDataHandler.HandleHidData(currBuff, this, 0);
                        holdDataThreadOpen = false;
                    }
                    if (readRing.IsEmpty())
                        _ = FileIOApiDeclarations.ResetEvent(readEvent);
                }
                // System.Threading.Thread.Sleep(10);
                _ = FileIOApiDeclarations.WaitForSingleObject(readEvent, 100);
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
                    //readEvent = null;
                    //readFileHandle = null;
                    readRing = null;
                    //CloseInterface();
                    retin = 207;
                    goto outputinit;
                }
                readEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
                readRing = new RingBuffer(128, ReadLength);
                readThreadHandle = new Thread(new ThreadStart(ReadThread))
                {
                    IsBackground = true,
                    Name = $"PIEHidReadThread for {Pid}"
                };
                readThreadActive = true;
                readThreadHandle.Start();
            }

        outputinit:
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
                    // writeEvent = null;
                    // writeFileHandle = null;
                    writeRing = null;
                    //CloseInterface();
                    retout = 208;
                    goto ErrorOut;
                }
                writeEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
                writeRing = new RingBuffer(128, WriteLength);
                writeThreadHandle = new Thread(new ThreadStart(WriteThread))
                {
                    IsBackground = true,
                    Name = $"PIEHidWriteThread for {Pid}"
                };
                writeThreadActive = true;
                writeThreadHandle.Start();

            }
            connected = true;
        ErrorOut:
            if ((retin == 0) && (retout == 0)) 
                return 0;
            if ((retin == 207) && (retout == 208)) 
                return 209;
            else 
                return retin + retout;

        }

        public void CloseInterface()
        {
            if ((holdErrorThreadOpen) || (holdDataThreadOpen)) return;

            // Shut down event thread
            if (dataThreadActive)
            {
                dataThreadActive = false;
                _ = FileIOApiDeclarations.SetEvent(readEvent);
                int n = 0;
                if (dataThreadHandle != null)
                {
                    while (dataThreadHandle.IsAlive)
                    {
                        Thread.Sleep(10);
                        n++;
                        if (n == 10) { dataThreadHandle.Abort(); break; }
                    }
                    dataThreadHandle = null;
                }
            }

            // Shut down read thread
            if (readThreadActive)
            {
                readThreadActive = false;
                // Wait for thread to exit
                if (readThreadHandle != null)
                {
                    int n = 0;
                    while (readThreadHandle.IsAlive)
                    {
                        Thread.Sleep(10);
                        n++;
                        if (n == 10) { readThreadHandle.Abort(); break; }
                    }
                    readThreadHandle = null;
                }
            }
            if (writeThreadActive)
            {
                writeThreadActive = false;
                _ = FileIOApiDeclarations.SetEvent(writeEvent);
                if (writeThreadHandle != null)
                {
                    int n = 0;
                    while (writeThreadHandle.IsAlive)
                    {
                        Thread.Sleep(10);
                        n++;
                        if (n == 10) { writeThreadHandle.Abort(); break; }
                    }
                    writeThreadHandle = null;
                }
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

            if (writeRing != null) { writeRing = null; }
            if (readRing != null) { readRing = null; }

            //  if (readEvent != null) {readEvent = null;}
            //  if (writeEvent != null) { writeEvent = null; }

            if ((0x00FF != Pid && 0x00FE != Pid && 0x00FD != Pid && 0x00FC != Pid && 0x00FB != Pid) || Version > 272)
            {
                // it's not an old VEC foot pedal (those hang when closing the handle)
                if (readFileHandle != null) // 9/1/09 - readFileHandle != null ||added by Onur to avoid null reference exception
                {
                    if (!readFileHandle.IsInvalid) readFileHandle.Close();
                }
                if (writeFileHandle != null)
                {
                    if (!writeFileHandle.IsInvalid) writeFileHandle.Close();
                }
            }
            connected = false;

        }
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

        public int SetErrorCallback(IErrorHandler handler)
        {
            if (!connected) 
                return 802;

            if (registeredErrorHandler == null)
            {//registeredErrorHandler is not defined so define it and create thread. 
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
            if (readRing.Getlast(dest) != 0) 
                return 504;
            return 0;
        }

        public int ReadData(ref byte[] dest)
        {
            if (!connected) 
                return 303;
            if (dest == null) 
                dest = new byte[ReadLength];
            if (dest.Length < ReadLength) 
                return 311;
            if (readRing.Get(dest) != 0) 
                return 304;
            return 0;
        }

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
            if (writeRing.PutIfCan(wData) == 3)
            {
                Thread.Sleep(1);
                return 404;
            }
            _ = FileIOApiDeclarations.SetEvent(writeEvent);
            return 0;
        }

        //--------------------------------------------------------------------------------------------
        /// <summary>
        /// Enumerates all valid PIE USB devics.
        /// </summary>
        /// <returns>list of all devices found, ordered by USB port connection</returns>
        public static PIEDevice[] EnumeratePIE()
        {
            return EnumeratePIE(0x05F3);
        }

        /// <summary>
        /// Enumerates all valid USB devics of the specified Vid.
        /// </summary>
        /// <returns>list of all devices found, ordered by USB port connection</returns>
        public static PIEDevice[] EnumeratePIE(int vid)
        {

            // FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnusedE = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();  
            LinkedList<PIEDevice> devices = new LinkedList<PIEDevice>();

            // Get all device paths
            Guid guid = Guid.Empty;
            HidApiDeclarations.HidD_GetHidGuid(ref guid);
            IntPtr deviceInfoSet = DeviceManagementApiDeclarations.SetupDiGetClassDevs(ref guid, null, IntPtr.Zero,
                DeviceManagementApiDeclarations.DIGCF_PRESENT
                | DeviceManagementApiDeclarations.DIGCF_DEVICEINTERFACE);

            DeviceManagementApiDeclarations.SP_DEVICE_INTERFACE_DATA deviceInterfaceData = new DeviceManagementApiDeclarations.SP_DEVICE_INTERFACE_DATA();
            deviceInterfaceData.Size = Marshal.SizeOf(deviceInterfaceData); //28;

            LinkedList<string> paths = new LinkedList<string>();

            for (int i = 0; 0 != DeviceManagementApiDeclarations.SetupDiEnumDeviceInterfaces(deviceInfoSet, 0, ref guid, i, ref deviceInterfaceData); i++)
            {
                int buffSize = 0;
                DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, IntPtr.Zero, 0, ref buffSize, IntPtr.Zero);
                // Use IntPtr to simulate detail data structure
                IntPtr detailBuffer = Marshal.AllocHGlobal(buffSize);

                // Simulate setting cbSize to 4 bytes + one character (seems to be what everyone has always done, even though it makes no sense)
                //onur modified for 64-bit compatibility - March 2009
                if (IntPtr.Size == 8) //64-bit
                    Marshal.WriteInt64(detailBuffer, Marshal.SizeOf(typeof(IntPtr))); //????patti
                else //32-bit
                    Marshal.WriteInt64(detailBuffer, 4 + Marshal.SystemDefaultCharSize); //patti

                if (DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet, ref deviceInterfaceData, detailBuffer, buffSize, ref buffSize, IntPtr.Zero))
                {
                    // convert buffer (starting past the cbsize variable) to string path
                    paths.AddLast(Marshal.PtrToStringAuto(new IntPtr(detailBuffer.ToInt64() + 4)));
                }
            }
            _ = DeviceManagementApiDeclarations.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            //Security attributes not used anymore - not necessary Onur March 2009
            // Open each device file and test for vid
            FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttributes = new FileIOApiDeclarations.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = Convert.ToInt32(true) //patti keep Int32 here
            };
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

            for (LinkedList<string>.Enumerator en = paths.GetEnumerator(); en.MoveNext();)
            {
                string path = en.Current;

                IntPtr fileH = FileIOApiDeclarations.CreateFile(path, FileIOApiDeclarations.GENERIC_WRITE,
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
                    if (0 != HidApiDeclarations.HidD_GetAttributes(fileHandle, ref hidAttributes)
                        && hidAttributes.VendorID == vid)
                    {
                        // Good attributes and right vid, try to get caps
                        IntPtr pPerparsedData = new IntPtr();
                        if (HidApiDeclarations.HidD_GetPreparsedData(fileHandle, ref pPerparsedData))
                        {
                            HidApiDeclarations.HIDP_CAPS hidCaps = new HidApiDeclarations.HIDP_CAPS();
                            if (0 != HidApiDeclarations.HidP_GetCaps(pPerparsedData, ref hidCaps))
                            {
                                // Got Capabilities, add device to list
                                byte[] Mstring = new byte[128];
                                string ssss = ""; ;
                                if (0 != HidApiDeclarations.HidD_GetManufacturerString(fileHandle, ref Mstring[0], 128))
                                {
                                    for (int i = 0; i < 64; i++)
                                    {
                                        byte[] t = new byte[2];
                                        t[0] = Mstring[2 * i];
                                        t[1] = Mstring[2 * i + 1];
                                        if (t[0] == 0) break;
                                        ssss += System.Text.Encoding.Unicode.GetString(t);
                                    }
                                }
                                byte[] Pstring = new byte[128];
                                string psss = "";
                                if (0 != HidApiDeclarations.HidD_GetProductString(fileHandle, ref Pstring[0], 128))
                                {
                                    for (int i = 0; i < 64; i++)
                                    {
                                        byte[] t = new byte[2];
                                        t[0] = Pstring[2 * i];
                                        t[1] = Pstring[2 * i + 1];
                                        if (t[0] == 0) break;
                                        psss += System.Text.Encoding.Unicode.GetString(t);
                                    }
                                }

                                devices.AddLast(new PIEDevice(path, hidAttributes.VendorID, hidAttributes.ProductID, hidAttributes.VersionNumber, hidCaps.Usage, 
                                    hidCaps.UsagePage, hidCaps.InputReportByteLength, hidCaps.OutputReportByteLength, ssss, psss));
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
            PIEDevice[] ret = new PIEDevice[devices.Count];
            devices.CopyTo(ret, 0);
            return ret;
        }

        protected int SendSausageCommands(ushort[] commandSequence)
        {
            if (WriteLength != 2 || HidUsagePage != 1 || HidUsage != 6) // hid page 1, usage 6 is keyboard
            {
                return 1302;
            }

            FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttributes = new FileIOApiDeclarations.SECURITY_ATTRIBUTES
            {
                lpSecurityDescriptor = IntPtr.Zero,
                bInheritHandle = Convert.ToInt32(true)
            };
            securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

            IntPtr hF = FileIOApiDeclarations.CreateFile(Path,
                FileIOApiDeclarations.GENERIC_WRITE,
                FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                 IntPtr.Zero,
                FileIOApiDeclarations.OPEN_EXISTING,
                0,
                0);
            SafeFileHandle hFile = new SafeFileHandle(hF, true);
            if (hFile.IsInvalid)
            {
                return 1301; ;
            }
            FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED
            {
                hEvent = IntPtr.Zero,
                Offset = 0,  // IntPtr.Zero;
                OffsetHigh = 0// IntPtr.Zero;
            };
            foreach (ushort command in commandSequence)
            {
                uint cmd = ((uint)command) << 16;
                uint bytesReturned = 0;

                if (!DeviceManagementApiDeclarations.DeviceIoControl(hFile, (uint)0x000b0008, ref cmd, 4, IntPtr.Zero, 0, ref bytesReturned, ref overlapped))
                {
                    return Marshal.GetLastWin32Error();
                }
            }
            hFile.Close();
            return 0;
        }

        public int ConvertToSplatMode()
        {
            return SendSausageCommands(convertToSplatModeSausages);
        }

        public int SendLEDSausage()
        {
            return SendSausageCommands(ledSausages);
        }

    }
}
