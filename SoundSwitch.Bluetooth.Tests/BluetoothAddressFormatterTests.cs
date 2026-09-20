using FluentAssertions;

using NUnit.Framework;

namespace SoundSwitch.Bluetooth.Tests;

[TestFixture]
public class BluetoothAddressFormatterTests
{
    [Test]
    public void Format_RendersBytesMostSignificantFirst()
    {
        // 0x0100000000AA -> byte0(LSB)=AA, byte5(MSB)=01
        BluetoothAddressFormatter.Format(0x0100000000AAUL).Should().Be("01:00:00:00:00:AA");
    }

    [Test]
    public void Format_RendersZeroAddress()
    {
        BluetoothAddressFormatter.Format(0UL).Should().Be("00:00:00:00:00:00");
    }

    [TestCase("AA:BB:CC:DD:EE:FF")]
    [TestCase("aa:bb:cc:dd:ee:ff")]
    [TestCase("AA-BB-CC-DD-EE-FF")]
    public void TryParse_AcceptsColonAndDashSeparators(string address)
    {
        BluetoothAddressFormatter.TryParse(address, out var value).Should().BeTrue();
        value.Should().Be(0xAABBCCDDEEFFUL);
    }

    [Test]
    public void FormatThenParse_RoundTrips()
    {
        const ulong original = 0x0123456789ABUL;

        var formatted = BluetoothAddressFormatter.Format(original);
        BluetoothAddressFormatter.TryParse(formatted, out var parsed).Should().BeTrue();

        parsed.Should().Be(original);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("not-an-address")]
    [TestCase("AA:BB:CC:DD:EE")]
    [TestCase("AA:BB:CC:DD:EE:FF:00")]
    public void TryParse_RejectsInvalidInput(string? address)
    {
        BluetoothAddressFormatter.TryParse(address, out var value).Should().BeFalse();
        value.Should().Be(0UL);
    }
}
