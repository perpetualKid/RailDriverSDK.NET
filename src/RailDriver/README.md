# RailDriver

RailDriver is a managed .NET SDK library for the PIE Engineering RailDriver USB HID Desktop Train Cab Controller.

The package targets .NET Standard 2.0 so it can be used from supported .NET implementations, including .NET Framework 4.6.2 and later, .NET Core, and modern .NET.

## Getting started

Install the package from NuGet:

```pwsh
dotnet add package RailDriver
```

Enumerate connected PIE HID devices:

```csharp
using RailDriver;

IList<PIEDevice> devices = PIEDevice.EnumeratePIE();
```

Open a device interface before reading or writing data:

```csharp
PIEDevice device = devices[0];
int result = device.SetupInterface();

if (result == 0)
{
	byte[] data = null;
	result = device.ReadData(ref data);
}
```

Dispose devices or close interfaces when finished:

```csharp
device.CloseInterface();
device.Dispose();
```

## Notes

- This library communicates with Windows HID APIs.
- A physical RailDriver-compatible device is required for end-to-end device I/O.
- Methods preserve the PIE SDK-style numeric error codes. Use `PIEDevice.GetErrorString(errorCode)` to convert codes to messages.

## License

This package is licensed under the MIT license.
