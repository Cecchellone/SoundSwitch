using System;
using System.Globalization;
using System.Linq;

namespace SoundSwitch.Bluetooth;

/// <summary>
/// Converts between the native 48-bit Bluetooth address (carried in a ulong, byte 0 = least
/// significant) and the "AA:BB:CC:DD:EE:FF" form used everywhere else (profiles, UI, logs).
/// </summary>
public static class BluetoothAddressFormatter
{
    public static string Format(ulong address)
    {
        var bytes = new byte[6];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(address >> (8 * i));
        }

        return string.Join(":", bytes.Reverse().Select(b => b.ToString("X2", CultureInfo.InvariantCulture)));
    }

    public static bool TryParse(string? address, out ulong value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        var parts = address.Split([':', '-'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6)
        {
            return false;
        }

        ulong result = 0;
        foreach (var part in parts)
        {
            if (!byte.TryParse(part, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var component))
            {
                return false;
            }

            result = (result << 8) | component;
        }

        value = result;
        return true;
    }
}
