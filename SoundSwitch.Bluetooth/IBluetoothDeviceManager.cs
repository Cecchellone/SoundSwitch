using System.Collections.Generic;

namespace SoundSwitch.Bluetooth;

public interface IBluetoothDeviceManager
{
    /// <summary>
    /// List every device paired with the machine, whether currently connected or not.
    /// </summary>
    IReadOnlyList<BluetoothDeviceInfo> GetPairedDevices();

    /// <summary>
    /// Look up a single paired device by its address. Returns <c>null</c> when the address isn't
    /// paired (e.g. it was removed from Windows since the profile was configured).
    /// </summary>
    BluetoothDeviceInfo? FindDevice(string address);

    /// <summary>
    /// Ask Windows to (re)connect the given paired device. Returns <c>false</c> when the device
    /// couldn't be found or none of the attempted services could be enabled.
    /// </summary>
    bool Connect(string address);

    /// <summary>
    /// Ask Windows to disconnect the given paired device. Returns <c>false</c> when the device
    /// couldn't be found or none of the attempted services could be disabled.
    /// </summary>
    bool Disconnect(string address);
}
