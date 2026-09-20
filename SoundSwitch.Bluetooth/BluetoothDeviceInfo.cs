namespace SoundSwitch.Bluetooth;

/// <summary>
/// Snapshot of a paired Bluetooth device, as reported by the OS at the time it was queried.
/// </summary>
/// <param name="Address">Device address formatted as "AA:BB:CC:DD:EE:FF".</param>
public sealed record BluetoothDeviceInfo(string Address, string Name, bool Connected, bool Remembered, bool Authenticated);
