using System;
using System.Runtime.InteropServices;

namespace SoundSwitch.Bluetooth.Interop;

/// <summary>
/// P/Invoke declarations for the classic Win32 Bluetooth API (bthprops.cpl). There is no managed
/// or WinRT equivalent for enumerating paired devices and toggling their connection state, so this
/// is the only way to reconnect a paired-but-idle audio device from user code.
/// </summary>
internal static class NativeMethods
{
    private const string BluetoothApiDll = "bthprops.cpl";

    [DllImport(BluetoothApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern IntPtr BluetoothFindFirstDevice(ref BluetoothDeviceSearchParams searchParams, ref BluetoothDeviceInfo deviceInfo);

    [DllImport(BluetoothApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindNextDevice(IntPtr findHandle, ref BluetoothDeviceInfo deviceInfo);

    [DllImport(BluetoothApiDll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindDeviceClose(IntPtr findHandle);

    [DllImport(BluetoothApiDll, SetLastError = true)]
    public static extern IntPtr BluetoothFindFirstRadio(ref BluetoothFindRadioParams radioParams, out IntPtr radioHandle);

    [DllImport(BluetoothApiDll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindNextRadio(IntPtr findHandle, out IntPtr radioHandle);

    [DllImport(BluetoothApiDll, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool BluetoothFindRadioClose(IntPtr findHandle);

    [DllImport(BluetoothApiDll, SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int BluetoothSetServiceState(IntPtr radioHandle, ref BluetoothDeviceInfo deviceInfo, ref Guid serviceGuid, int flags);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr handle);
}
