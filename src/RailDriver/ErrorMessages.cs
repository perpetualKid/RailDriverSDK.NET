using System.Collections.Generic;

namespace RailDriver
{
    internal static class ErrorMessages
    {
        internal static readonly Dictionary<int, string> Messages = new Dictionary<int, string>
        {
            { ResultCode.Success, "000 Success" },

            { ResultCode.AlreadyConnected, "203 Already Connected" },
            { ResultCode.CannotOpenReadHandle, "207 Cannot open read handle" },
            { ResultCode.CannotOpenWriteHandle, "208 Cannot open write handle" },
            { ResultCode.CannotOpenEitherHandle, "209 Cannot open either handle" },

            { ResultCode.BadReadInterfaceHandle, "301 Bad interface handle" },
            { ResultCode.ReadLengthZero, "302 readSize is zero" },
            { ResultCode.ReadInterfaceNotValid, "303 Interface not valid" },
            { ResultCode.RingBufferEmpty, "304 Ring buffer empty." },
            { ResultCode.NoReadBuffer, "307 No readBuffer" },
            { ResultCode.DeviceDisconnectedRead, "308 Device disconnected" },
            { ResultCode.ReadError, "309 Read error. ( unplugged )" },
            { ResultCode.BytesReadNotEqualReadSize, "310 Bytes read not equal readSize" },
            { ResultCode.ReadDestinationTooSmall, "311 dest.Length<ReportSize" },

            { ResultCode.BadWriteInterfaceHandle, "401 Bad interface handle" },
            { ResultCode.WriteLengthZero, "402 Write length is zero" },
            { ResultCode.WriteDestinationTooSmall, "403 wData.Length<ReportSize" },
            { ResultCode.WriteBufferFull, "404 WriteBuffer full--retry" },
            { ResultCode.NoWriteBuffer, "405 No write buffer" },
            { ResultCode.WriteInterfaceNotValid, "406 Interface not valid" },
            { ResultCode.NoWriteBufferForWorker, "407 No writeBuffer" },
            { ResultCode.DeviceDisconnectedWrite, "408 Device disconnected" },
            { ResultCode.WriteError, "409 Write error. ( unplugged )" },
            { ResultCode.ByteCountNotEqualWriteSize, "410 byteCount != writeSize" },
            { ResultCode.WriteTimedOut, "411 Timed out in write." },
            { ResultCode.ReportIdError, "412 Report ID error" },

            { ResultCode.ReadLastLengthZero, "502 Read length is zero" },
            { ResultCode.ReadLastDestinationTooSmall, "503 dest.Length<ReportSize" },
            { ResultCode.NoDataYet, "504 No data yet." },
            { ResultCode.ReadLastInterfaceNotValid, "507 Interface not valid." },

            { ResultCode.DataCallbackInterfaceNotValid, "702 Interface not valid" },
            { ResultCode.InputReportSizeZero, "703 Input ReportSize Zero" },
            { ResultCode.DataHandlerAlreadyExists, "704 Data Handler Already Exists" },

            { ResultCode.ErrorCallbackInterfaceNotValid, "802 Interface not valid" },
            { ResultCode.ErrorHandlerAlreadyExists, "804 Error Handler Already Exists" },
        };
    }
}
