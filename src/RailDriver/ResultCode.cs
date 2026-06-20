namespace RailDriver
{
    /// <summary>
    /// Numeric result/error codes returned by <see cref="PIEDevice"/> operations.
    /// These values are the single source of truth and are also used to map
    /// human readable messages in <see cref="ErrorMessages"/>.
    /// </summary>
    internal static class ResultCode
    {
        public const int Success = 0;

        public const int AlreadyConnected = 203;
        public const int CannotOpenReadHandle = 207;
        public const int CannotOpenWriteHandle = 208;
        public const int CannotOpenEitherHandle = 209;

        public const int BadReadInterfaceHandle = 301;
        public const int ReadLengthZero = 302;
        public const int ReadInterfaceNotValid = 303;
        public const int RingBufferEmpty = 304;
        public const int NoReadBuffer = 307;
        public const int DeviceDisconnectedRead = 308;
        public const int ReadError = 309;
        public const int BytesReadNotEqualReadSize = 310;
        public const int ReadDestinationTooSmall = 311;

        public const int BadWriteInterfaceHandle = 401;
        public const int WriteLengthZero = 402;
        public const int WriteDestinationTooSmall = 403;
        public const int WriteBufferFull = 404;
        public const int NoWriteBuffer = 405;
        public const int WriteInterfaceNotValid = 406;
        public const int NoWriteBufferForWorker = 407;
        public const int DeviceDisconnectedWrite = 408;
        public const int WriteError = 409;
        public const int ByteCountNotEqualWriteSize = 410;
        public const int WriteTimedOut = 411;
        public const int ReportIdError = 412;

        public const int ReadLastLengthZero = 502;
        public const int ReadLastDestinationTooSmall = 503;
        public const int NoDataYet = 504;
        public const int ReadLastInterfaceNotValid = 507;

        public const int DataCallbackInterfaceNotValid = 702;
        public const int InputReportSizeZero = 703;
        public const int DataHandlerAlreadyExists = 704;

        public const int ErrorCallbackInterfaceNotValid = 802;
        public const int ErrorHandlerAlreadyExists = 804;
    }
}
