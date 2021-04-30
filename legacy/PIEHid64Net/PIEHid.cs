using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;
using RngBuf2n;
namespace PIEHid64Net
{
	public interface PIEDataHandler
	{
		void HandlePIEHidData(Byte[] data, PIEDevice sourceDevice, int error);
	}

	public interface PIEErrorHandler
	{
		void HandlePIEHidError( PIEDevice sourceDevices, Int64 error);
	}
    //4/21/15 Patti replace Int32 with Int64 
	public class PIEDevice
	{
        private String path;
		private Int64 vid;
		private Int64 pid;
		private Int64 version;
		private Int64 hidUsage;
		private Int64 hidUsagePage;
		private bool connected = false;
		public bool suppressDuplicateReports = false;
        private int inputReportSize;
		private int outputReportSize;
        private RngBuf2 writeRing;
        private RngBuf2 readRing;
        private SafeFileHandle readFileHandle;
        private SafeFileHandle writeFileHandle;
		private PIEDataHandler registeredDataHandler = null;
        private PIEErrorHandler registeredErrorHandler = null;
        public bool callNever=false;
        private IntPtr readFileH;

		
        private int errCodeR = 0;
        private int errCodeRE = 0;
        private int errCodeW = 0;
        private int errCodeWE = 0;
        private bool holdDataThreadOpen=false;
        private bool holdErrorThreadOpen=false;
        FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnused = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
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

        private String manufacturersString;
        private String productString;
		
		private const int READ_BUFFER_COUNT = 512;
		private const int WRITE_BUFFER_COUNT = 512;

		protected static ushort[] convertToSplatModeSausages = { 7, 5, 4, 3, 2, 1 };
		protected static ushort[] ledSausages = { 7, 3, 1, 6, 4, 2 };

		public String Path
		{
			get { return path; }
		}
		public Int64 Vid
		{
			get { return vid; }
		}
		public Int64 Pid
		{
			get { return pid; }
		}
		public Int64 Version
		{
			get { return version; }
		}
		public Int64 HidUsage
		{
			get { return hidUsage; }
		}
		public Int64 HidUsagePage
		{
			get { return hidUsagePage; }
		}
		public Int64 ReadLength
		{
			get { return inputReportSize; }
		}
		public Int64 WriteLength
		{
			get { return outputReportSize; }
		}
        public String ManufacturersString
        {
            get { return manufacturersString; }
        }
        public String ProductString
        {
            get { return productString; }
        }
	/*	public bool Connected
		{
			get { return connected; }
		}
		public bool SuppressDuplicateReports
		{
			get { return suppressDuplicateReports; }
		}
*/
		public PIEDevice(String path, Int64 vid, Int64 pid, Int64 version,
			Int64 hidUsage, Int64 hidUsagePage, Int64 readSize, Int64 writeSize,
            String ManufacturersString,String ProductString)
		{
			this.path = path;
			this.vid = vid;
			this.pid = pid;
			this.version = version;
			this.hidUsage = hidUsage;
			this.hidUsagePage = hidUsagePage;
			this.inputReportSize = (int) readSize; //patti
			this.outputReportSize = (int) writeSize; //patti
            this.manufacturersString = ManufacturersString;
            this.productString = ProductString;
            this.securityAttrUnused.bInheritHandle = 1;
		}
//--------------------------------------------------------------------------------------------
       
        public String GetErrorString(int errNumb)
        {
            int[] EDS = new int[100];
            String[] EDA = new String[100];
            String st;           

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
        for(int i=0;i<59;i++){
	       if(EDS[i] ==errNumb){
		   st=EDA[i];
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
                    registeredErrorHandler.HandlePIEHidError( this, errCodeRE);
                    holdDataThreadOpen = false;                    
                }
                if (errCodeWE != 0)
                {
                    holdErrorThreadOpen = true;
                    registeredErrorHandler.HandlePIEHidError(this, errCodeWE);
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
            FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED();

            overlapped.Offset = 0;// IntPtr.Zero;
            overlapped.OffsetHigh = 0;// IntPtr.Zero;
            overlapped.hEvent = overlapEvent;
            overlapped.Internal = IntPtr.Zero;
            overlapped.InternalHigh = IntPtr.Zero;
            if (outputReportSize == 0) return;

            byte[] buffer = new byte[outputReportSize];
            GCHandle wgch = GCHandle.Alloc(buffer, GCHandleType.Pinned); //onur March 2009 - pinning is reuired

            int byteCount = 0; ;
          //  int loopCount = 0;

            errCodeW = 0;
            errCodeWE = 0;
            while (writeThreadActive) 
            {
                if (writeRing == null) { errCodeW = 407; errCodeWE = 407; goto Error; }
                   while (writeRing.get((byte[])buffer) == 0)
                   {
                       if (0==FileIOApiDeclarations.WriteFile(
                            writeFileHandle,
                            wgch.AddrOfPinnedObject(),
                            outputReportSize,
                            ref byteCount,
                            ref overlapped))
                       {
                          int result = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                           if (result != FileIOApiDeclarations.ERROR_IO_PENDING)
                         //if ((result == FileIOApiDeclarations.ERROR_INVALID_HANDLE) ||
                          //    (result == FileIOApiDeclarations.ERROR_DEVICE_NOT_CONNECTED))
                           {
                               if (result == 87) { errCodeW = 412; errCodeWE = 412; }
                               else { errCodeW = result; errCodeWE = 408; }
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
                       }else{
                           if ((long)byteCount != outputReportSize)
                           {
                               errCodeW = 410;
                               errCodeWE = 410;
                           }
                           //iface->errCodeWE=1399;
                       }
                   WriteCompleted:;                       
                   }//while(get==
                   FileIOApiDeclarations.WaitForSingleObject(writeEvent, 100);
                   FileIOApiDeclarations.ResetEvent(writeEvent);		
               // System.Threading.Thread.Sleep(100);
            }
            Error:
            wgch.Free(); //onur
           
            return;
        }
        protected void ReadThread()
        {
 
    //        FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttrUnused = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
            IntPtr overlapEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
             FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED();

             overlapped.Offset = 0;// IntPtr.Zero;
             overlapped.OffsetHigh = 0;// IntPtr.Zero;
            overlapped.hEvent = (IntPtr)overlapEvent;
            overlapped.Internal = IntPtr.Zero;
            overlapped.InternalHigh = IntPtr.Zero;
            if (inputReportSize == 0)
            {
                errCodeR = 302;
                errCodeRE = 302;
                return;
            }
            errCodeR = 0;
            errCodeRE = 0;

            byte[] buffer = new byte[inputReportSize];
            GCHandle gch = GCHandle.Alloc(buffer, GCHandleType.Pinned); //onur March 2009 - pinning is reuired
            
                while (readThreadActive)
                {
                   
                    int dataRead = 0;//FileIOApiDeclarations.
                    if (readFileHandle.IsInvalid) { errCodeRE = errCodeR = 320; goto EXit; }

                    if (0 == FileIOApiDeclarations.ReadFile(readFileHandle, gch.AddrOfPinnedObject(), inputReportSize, ref dataRead, ref overlapped)) //ref readFileBuffer[0]
                    {
                        int result = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
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
                               
                                result=FileIOApiDeclarations.WaitForSingleObject(overlapEvent, 50);
                               if (FileIOApiDeclarations.WAIT_OBJECT_0 == result)
                               {
                                   if (0 == FileIOApiDeclarations.GetOverlappedResult(readFileHandle, ref overlapped, ref dataRead, 0))
                                   {
                                       result = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
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
                    if (dataRead != inputReportSize) { errCodeR = 310; errCodeRE = 310; goto EXit; }
                    
                    if (suppressDuplicateReports)
                    {
                         int r=readRing.putIfDiff(buffer);
                         if (r == 0) FileIOApiDeclarations.SetEvent(readEvent);
                    }
                     else
                     {
                            readRing.put(buffer);
                            FileIOApiDeclarations.SetEvent(readEvent);
                     }
                     
                }//while
             
        EXit:
            FileIOApiDeclarations.CancelIo(readFileHandle);
                readFileHandle = null;
                gch.Free(); //onur
            return;
        }
//------------------------------------------------------------------------------------------
    
//----------------------------------------------------------------------------------------
       protected void DataEventThread()
       {
           Byte[] currBuff = new Byte[inputReportSize];
           
			
			while (dataThreadActive)
            {
                if (readRing == null) return;
                if (callNever==false)
                {
                    if (errCodeR!=0)
                    {
                        Array.Clear(currBuff, 0, inputReportSize);
                        holdDataThreadOpen = true;
                        registeredDataHandler.HandlePIEHidData(currBuff, this, errCodeR);
                        holdDataThreadOpen = false;
                        dataThreadActive = false;
                    }
                    else if (readRing.get(currBuff) == 0)
                    {
                        holdDataThreadOpen = true;
                        registeredDataHandler.HandlePIEHidData(currBuff, this, 0);
                        holdDataThreadOpen = false;
                    }
                    if (readRing.IsEmpty()) FileIOApiDeclarations.ResetEvent(readEvent);
                }
              // System.Threading.Thread.Sleep(10);
                FileIOApiDeclarations.WaitForSingleObject(readEvent, 100);
			 }//while	
            return;
		}//DataEventThread()
//----------------------------------------------------------------------------------------

//-----------------------------------------------------------------------------
		/// <summary>
		/// Sets connection to the enumerated device. 
        /// If inputReportSize greater than zero it generates a read handle.
        /// If outputReportSize greater than zero it generates a write handle.
		/// </summary>
		/// <returns></returns>
        public Int64 SetupInterface()
       {
           int retin=0;
           int retout=0;

            if (connected) return 203;
	                if (inputReportSize > 0)
                {
                     readFileH = FileIOApiDeclarations.CreateFile(path, FileIOApiDeclarations.GENERIC_READ,
                      FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                      IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, FileIOApiDeclarations.FILE_FLAG_OVERLAPPED, 0);

                    readFileHandle = new SafeFileHandle(readFileH, true);
                    if (readFileHandle.IsInvalid)
                    {
                        //readEvent = null;
                        //readFileHandle = null;
                        readRing = null;
                        //CloseInterface();
                        retin= 207;
                        goto outputinit;
                    }
                    readEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
                    readRing = new RngBuf2(128, inputReportSize);
                    readThreadHandle = new Thread(new ThreadStart(ReadThread));
                    readThreadHandle.IsBackground = true;
                    readThreadHandle.Name = "PIEHidReadThread for " + pid;
                    readThreadActive = true;
                    readThreadHandle.Start();                    
                }
                   
            outputinit:
                if (outputReportSize > 0)
                {
                  IntPtr  writeFileH = FileIOApiDeclarations.CreateFile(path, FileIOApiDeclarations.GENERIC_WRITE,
                        FileIOApiDeclarations.FILE_SHARE_READ | FileIOApiDeclarations.FILE_SHARE_WRITE,
                         IntPtr.Zero, FileIOApiDeclarations.OPEN_EXISTING, 
                        FileIOApiDeclarations.FILE_FLAG_OVERLAPPED, 
                        0);
                    writeFileHandle = new SafeFileHandle(writeFileH, true);
                    if (writeFileHandle.IsInvalid)
                    {
                       // writeEvent = null;
                       // writeFileHandle = null;
                        writeRing =null;
                        //CloseInterface();
                        retout=208;
                        goto ErrorOut;
                    }
                    writeEvent = FileIOApiDeclarations.CreateEvent(ref securityAttrUnused, 1, 0, "");
                    writeRing = new RngBuf2(128, outputReportSize);
                    writeThreadHandle = new Thread(new ThreadStart(WriteThread));
                    writeThreadHandle.IsBackground = true;
                    writeThreadHandle.Name = "PIEHidWriteThread for " + pid;
                    writeThreadActive = true;
                    writeThreadHandle.Start();

                }
				connected = true;		
            ErrorOut:
            if((retin==0)&&(retout==0))return 0;
            if((retin==207)&&(retout==208))return 209;
            else return retin+retout; 
			
		}

		public void CloseInterface()
		{
            if ((holdErrorThreadOpen) || (holdDataThreadOpen)) return;
			
				// Shut down event thread
				if (dataThreadActive)
				{
					dataThreadActive = false;
                    FileIOApiDeclarations.SetEvent(readEvent);
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
                    FileIOApiDeclarations.SetEvent(writeEvent);
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

                if (writeRing != null) {writeRing=null ; }
                if (readRing != null) { readRing = null; }

              //  if (readEvent != null) {readEvent = null;}
              //  if (writeEvent != null) { writeEvent = null; }
				
				if ((0x00FF != pid && 0x00FE != pid && 0x00FD != pid && 0x00FC != pid && 0x00FB != pid) || version > 272)
				{
					// it's not an old VEC foot pedal (those hang when closing the handle)
                    if (readFileHandle != null) // 9/1/09 - readFileHandle != null ||added by Onur to avoid null reference exception
					{
                        if (!readFileHandle.IsInvalid)readFileHandle.Close();
					}
                    if (writeFileHandle != null)
                    {
                        if (!writeFileHandle.IsInvalid) writeFileHandle.Close();
                    }
				}
                connected = false;
			
		}
        public Int64 SetDataCallback(PIEDataHandler handler)
        {
            if (!connected) return 702;
            if (inputReportSize == 0) return 703;
            
            if (registeredDataHandler == null)
            {//registeredDataHandler is not defined so define it and create thread. 
                registeredDataHandler = handler;
                dataThreadHandle = new Thread(new ThreadStart(DataEventThread));
                dataThreadHandle.IsBackground = true;
                dataThreadHandle.Name = "PIEHidEventThread for " + pid;
                dataThreadActive = true;
                dataThreadHandle.Start();
            }
            else
            {
                return 704;//Only the eventType has been changed.
            }
            return 0;
        }

		public Int64 SetErrorCallback(PIEErrorHandler handler)
		{
            if (!connected) return 802;       
            
            if (registeredErrorHandler == null)
            {//registeredErrorHandler is not defined so define it and create thread. 
                registeredErrorHandler = handler;
                errorThreadHandle = new Thread(new ThreadStart(ErrorThread));
                errorThreadHandle.IsBackground = true;
                errorThreadHandle.Name = "PIEHidErrorThread for " + pid;
                errorThreadActive = true;
                errorThreadHandle.Start();
            }
            else
            {
                return 804;//Error Handler Already Exists.
            }
            return 0;
		}

	/*	public int ClearBuffer()
		{			
			return 0;
		}
     */

		public int ReadLast(ref byte[] dest)
        {
            if (inputReportSize == 0) return 502;
			if (false == connected)return 507;
            if (dest == null) dest = new byte[inputReportSize]; 
			if ( dest.Length < inputReportSize)return 503;
			
            if (readRing.getlast(dest) != 0) return 504;
            return 0;					
		}

		public int ReadData(ref byte[] dest)
		{
			if (false == connected)return 303;
            if (dest == null) dest = new byte[inputReportSize]; 
			if ( dest.Length < inputReportSize)return 311;
			
             if(readRing.get(dest)!=0)return 304;					
			 return 0;		
		}

		public int BlockingReadData(ref byte[] dest, int maxMillis)
		{			
			long startTicks = System.DateTime.UtcNow.Ticks;	
			int ret=304;		
                        int mills = maxMillis;
                        while ((mills > 0)&&(ret==304))
                        {
                            if ((ret = ReadData(ref dest)) == 0) break; 
                            long nowTicks = System.DateTime.UtcNow.Ticks;
                            mills = maxMillis - ((int)(nowTicks - startTicks) / 10000);
                            Thread.Sleep(10);
                        }					
					return ret;			
		}
        public int WriteData(byte[] wData)
        {
            if (outputReportSize == 0) return 402;
            if (false == connected) return 406;
            
            if (wData.Length < outputReportSize) return 403;
            if (writeRing == null) return 405;
            if (errCodeW != 0) return errCodeW;
            if (writeRing.putIfCan(wData) == 3)
            {
                System.Threading.Thread.Sleep(1);
                return 404;
            }
            FileIOApiDeclarations.SetEvent(writeEvent);
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
		public static PIEDevice[] EnumeratePIE(Int64 vid)
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
            deviceInterfaceData.cbSize = Marshal.SizeOf(deviceInterfaceData); //28;

			LinkedList<String> paths = new LinkedList<String>();
             
			for (int i = 0; 0 != DeviceManagementApiDeclarations.SetupDiEnumDeviceInterfaces(
				deviceInfoSet, 0, ref guid, i, ref deviceInterfaceData); i++)
			{
				int buffSize = 0;
				DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet,
					ref deviceInterfaceData, IntPtr.Zero, 0, ref buffSize, IntPtr.Zero);
				// Use IntPtr to simulate detail data structure
				IntPtr detailBuffer = Marshal.AllocHGlobal(buffSize);
				// Simulate setting cbSize to 4 bytes + one character (seems to be what everyone has always done, even though it makes no sense)
                //onur modified for 64-bit compatibility - March 2009
                if(IntPtr.Size == 8) //64-bit
                    Marshal.WriteInt64(detailBuffer, Marshal.SizeOf(typeof(IntPtr))); //????patti
                else //32-bit
                    Marshal.WriteInt64(detailBuffer, 4 + Marshal.SystemDefaultCharSize); //patti

				if (DeviceManagementApiDeclarations.SetupDiGetDeviceInterfaceDetail(deviceInfoSet,
					ref deviceInterfaceData, detailBuffer, buffSize, ref buffSize, IntPtr.Zero))
				{
					// convert buffer (starting past the cbsize variable) to string path
					paths.AddLast(Marshal.PtrToStringAuto(new IntPtr(detailBuffer.ToInt64() + 4)));
				}
			}
			DeviceManagementApiDeclarations.SetupDiDestroyDeviceInfoList(deviceInfoSet);
            //Security attributes not used anymore - not necessary Onur March 2009
			// Open each device file and test for vid
			FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttributes = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
            securityAttributes.lpSecurityDescriptor = IntPtr.Zero;
			securityAttributes.bInheritHandle = System.Convert.ToInt32(true); //patti keep Int32 here
			securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

			for (LinkedList<String>.Enumerator en = paths.GetEnumerator(); en.MoveNext(); )
			{
				String path = en.Current;

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
                                String ssss = ""; ;
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
                                String psss = ""; 
                                if (0 != HidApiDeclarations.HidD_GetProductString(fileHandle, ref Pstring[0], 128))
                                {
                                   // Pstring[0] = 0xa0;  Test unicode
                                   // Pstring[1] = 0x03;
                                    for (int i = 0; i < 64; i++) {
                                        byte[] t = new byte[2];
                                        t[0]= Pstring[2 * i];
                                        t[1] = Pstring[2 * i+1];
                                        if (t[0] == 0)break;                                      
                                        psss += System.Text.Encoding.Unicode.GetString(t);
                                    }
                                    // psss=(System.Text.Encoding.Unicode.GetString(Pstring)).Trim(new char[]{' '});
                                    
                                }

								devices.AddLast(new PIEDevice(path, hidAttributes.VendorID, hidAttributes.ProductID, hidAttributes.VersionNumber,
									hidCaps.Usage, hidCaps.UsagePage, hidCaps.InputReportByteLength, hidCaps.OutputReportByteLength,
                                    ssss,psss));
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

		protected Int64 SendSausageCommands(ushort[] commandSequence)
		{
			if (outputReportSize != 2 || hidUsagePage != 1 || hidUsage != 6) // hid page 1, usage 6 is keyboard
			{
				return 1302;
			}

			FileIOApiDeclarations.SECURITY_ATTRIBUTES securityAttributes = new FileIOApiDeclarations.SECURITY_ATTRIBUTES();
            securityAttributes.lpSecurityDescriptor = IntPtr.Zero;
			securityAttributes.bInheritHandle = System.Convert.ToInt32(true);  //patti keep int32 here
			securityAttributes.nLength = Marshal.SizeOf(securityAttributes);

			IntPtr hF = FileIOApiDeclarations.CreateFile(path,
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
			FileIOApiDeclarations.OVERLAPPED overlapped = new FileIOApiDeclarations.OVERLAPPED();
            overlapped.hEvent =  IntPtr.Zero;
            overlapped.Offset = 0;  // IntPtr.Zero;
            overlapped.OffsetHigh = 0;// IntPtr.Zero;
			foreach (ushort command in commandSequence)
			{
				uint cmd = ((uint)command) << 16;
				uint bytesReturned = 0;

				if (!DeviceManagementApiDeclarations.DeviceIoControl(hFile, (uint)0x000b0008, ref cmd, 4, IntPtr.Zero, 0, ref bytesReturned, ref overlapped))
				{
					int result = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    return result;
				}
			}
			hFile.Close();
            return 0;
		}
		public Int64 ConvertToSplatMode()
		{
			return SendSausageCommands(convertToSplatModeSausages);
		}
		public Int64 SendLEDSausage()
		{
			return SendSausageCommands(ledSausages);
		}

        public static void DongleCheck2(int k0, int k1, int k2, int k3, int a0, int a1, int a2, int a3, out int r0, out int r1, out int r2, out int r3)
        {
            //patti removed code
            r0 = 0;
            r1 = 0;
            r2 = 0;
            r3 = 0;

        }

		
	}
}
