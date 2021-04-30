namespace RailDriver
{
    public interface IDataHandler
    {
        void HandleHidData(byte[] data, PIEDevice sourceDevice, int error);
    }

    public interface IErrorHandler
    {
        void HandleHidError(PIEDevice sourceDevices, int error);
    }

}
