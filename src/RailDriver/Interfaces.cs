namespace RailDriver
{
    /// <summary>
    /// DataHandler Callback Interface
    /// </summary>
    public interface IDataHandler
    {
        /// <summary>
        /// Handle Input Data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sourceDevice"></param>
        /// <param name="error"></param>
        void HandleHidData(byte[] data, PIEDevice sourceDevice, int error);
    }

    /// <summary>
    /// ErrorHandler Callback Interface
    /// </summary>
    public interface IErrorHandler
    {
        /// <summary>
        /// Handle Error Data
        /// </summary>
        /// <param name="sourceDevices"></param>
        /// <param name="error"></param>
        void HandleHidError(PIEDevice sourceDevices, int error);
    }

}
