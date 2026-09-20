using System;
using System.Collections.Generic;
using System.Linq;

using NativeMethods = SoundSwitch.Bluetooth.Interop.NativeMethods;
using NativeDeviceInfo = SoundSwitch.Bluetooth.Interop.BluetoothDeviceInfo;
using NativeSearchParams = SoundSwitch.Bluetooth.Interop.BluetoothDeviceSearchParams;
using NativeRadioParams = SoundSwitch.Bluetooth.Interop.BluetoothFindRadioParams;

namespace SoundSwitch.Bluetooth;

/// <summary>
/// Enumerates paired Bluetooth devices and toggles their connection state through the classic
/// Win32 Bluetooth API. <see cref="BluetoothSetServiceState"/>-style toggling (via
/// <c>BluetoothSetServiceState</c>) is the only public mechanism Windows exposes to nudge a
/// paired-but-idle device into reconnecting; it only works for the classic (BR/EDR) audio profiles
/// below, not for BLE-only "LE Audio" devices.
/// </summary>
public sealed class BluetoothDeviceManager : IBluetoothDeviceManager
{
    private const int BluetoothServiceDisable = 0x00;
    private const int BluetoothServiceEnable = 0x01;

    /// <summary>
    /// Bluetooth SIG service class UUIDs (base 0000xxxx-0000-1000-8000-00805F9B34FB) covering the
    /// audio profiles Windows uses for headsets/speakers. Every candidate is attempted since a
    /// device usually only implements a subset of them.
    /// </summary>
    private static readonly Guid[] AudioServiceClassIds =
    [
        new("0000110D-0000-1000-8000-00805F9B34FB"), // Advanced Audio Distribution (A2DP)
        new("0000110B-0000-1000-8000-00805F9B34FB"), // A2DP Sink role
        new("0000111E-0000-1000-8000-00805F9B34FB"), // Handsfree
        new("00001108-0000-1000-8000-00805F9B34FB") // Headset
    ];

    public IReadOnlyList<BluetoothDeviceInfo> GetPairedDevices()
    {
        return EnumerateNativeDevices().Select(ToPublicInfo).ToArray();
    }

    public BluetoothDeviceInfo? FindDevice(string address)
    {
        if (!BluetoothAddressFormatter.TryParse(address, out var parsed))
        {
            return null;
        }

        foreach (var candidate in EnumerateNativeDevices())
        {
            if (candidate.Address == parsed)
            {
                return ToPublicInfo(candidate);
            }
        }

        return null;
    }

    public bool Connect(string address) => SetServiceState(address, BluetoothServiceEnable);

    public bool Disconnect(string address) => SetServiceState(address, BluetoothServiceDisable);

    private static bool SetServiceState(string address, int flag)
    {
        if (!BluetoothAddressFormatter.TryParse(address, out var parsedAddress))
        {
            return false;
        }

        var found = false;
        var deviceInfo = default(NativeDeviceInfo);
        foreach (var candidate in EnumerateNativeDevices())
        {
            if (candidate.Address != parsedAddress)
            {
                continue;
            }

            deviceInfo = candidate;
            found = true;
            break;
        }

        if (!found)
        {
            return false;
        }

        var succeeded = false;
        foreach (var radioHandle in EnumerateRadios())
        {
            foreach (var serviceGuid in AudioServiceClassIds)
            {
                var guid = serviceGuid;
                var result = NativeMethods.BluetoothSetServiceState(radioHandle, ref deviceInfo, ref guid, flag);
                succeeded |= result == 0;
            }
        }

        return succeeded;
    }

    private static IEnumerable<NativeDeviceInfo> EnumerateNativeDevices()
    {
        var searchParams = NativeSearchParams.ForPairedDevices();
        var deviceInfo = NativeDeviceInfo.Create();

        var findHandle = NativeMethods.BluetoothFindFirstDevice(ref searchParams, ref deviceInfo);
        if (findHandle == IntPtr.Zero)
        {
            yield break;
        }

        try
        {
            do
            {
                yield return deviceInfo;
                deviceInfo = NativeDeviceInfo.Create();
            } while (NativeMethods.BluetoothFindNextDevice(findHandle, ref deviceInfo));
        }
        finally
        {
            NativeMethods.BluetoothFindDeviceClose(findHandle);
        }
    }

    private static IEnumerable<IntPtr> EnumerateRadios()
    {
        var radioParams = NativeRadioParams.Default;
        var findHandle = NativeMethods.BluetoothFindFirstRadio(ref radioParams, out var radioHandle);
        if (findHandle == IntPtr.Zero)
        {
            yield break;
        }

        try
        {
            do
            {
                yield return radioHandle;
                NativeMethods.CloseHandle(radioHandle);
            } while (NativeMethods.BluetoothFindNextRadio(findHandle, out radioHandle));
        }
        finally
        {
            NativeMethods.BluetoothFindRadioClose(findHandle);
        }
    }

    private static BluetoothDeviceInfo ToPublicInfo(NativeDeviceInfo native) =>
        new(BluetoothAddressFormatter.Format(native.Address), native.Name, native.Connected, native.Remembered, native.Authenticated);
}
