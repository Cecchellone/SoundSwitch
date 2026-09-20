using System;
using System.Runtime.InteropServices;

namespace SoundSwitch.Bluetooth.Interop;

[StructLayout(LayoutKind.Sequential)]
internal struct SystemTime
{
    public ushort Year;
    public ushort Month;
    public ushort DayOfWeek;
    public ushort Day;
    public ushort Hour;
    public ushort Minute;
    public ushort Second;
    public ushort Milliseconds;
}

/// <summary>
/// Mirrors the native BLUETOOTH_DEVICE_INFO struct. <see cref="Address"/> holds the 48-bit device
/// address in its lower 6 bytes (the native struct stores it as a union of a ULONGLONG and a 6-byte
/// array, byte 0 being the least significant byte).
/// </summary>
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct BluetoothDeviceInfo
{
    private const int MaxNameLength = 248;

    public int Size;
    public ulong Address;
    public uint ClassOfDevice;
    [MarshalAs(UnmanagedType.Bool)] public bool Connected;
    [MarshalAs(UnmanagedType.Bool)] public bool Remembered;
    [MarshalAs(UnmanagedType.Bool)] public bool Authenticated;
    public SystemTime LastSeen;
    public SystemTime LastUsed;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MaxNameLength)] public string Name;

    public static BluetoothDeviceInfo Create() => new() { Size = Marshal.SizeOf<BluetoothDeviceInfo>(), Name = "" };
}

[StructLayout(LayoutKind.Sequential)]
internal struct BluetoothDeviceSearchParams
{
    public int Size;
    [MarshalAs(UnmanagedType.Bool)] public bool ReturnAuthenticated;
    [MarshalAs(UnmanagedType.Bool)] public bool ReturnRemembered;
    [MarshalAs(UnmanagedType.Bool)] public bool ReturnUnknown;
    [MarshalAs(UnmanagedType.Bool)] public bool ReturnConnected;
    [MarshalAs(UnmanagedType.Bool)] public bool IssueInquiry;
    public byte TimeoutMultiplier;
    public IntPtr RadioHandle;

    public static BluetoothDeviceSearchParams ForPairedDevices() => new()
    {
        Size = Marshal.SizeOf<BluetoothDeviceSearchParams>(),
        ReturnAuthenticated = true,
        ReturnRemembered = true,
        ReturnConnected = true,
        ReturnUnknown = false,
        IssueInquiry = false
    };
}

[StructLayout(LayoutKind.Sequential)]
internal struct BluetoothFindRadioParams
{
    public int Size;

    public static BluetoothFindRadioParams Default => new() { Size = Marshal.SizeOf<BluetoothFindRadioParams>() };
}
