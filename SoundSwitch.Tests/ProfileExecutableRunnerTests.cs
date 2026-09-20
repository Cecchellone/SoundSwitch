using FluentAssertions;

using NUnit.Framework;

using SoundSwitch.Framework.Profile;

namespace SoundSwitch.Tests;

[TestFixture]
public class ProfileExecutableRunnerTests
{
    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void BuildStartInfo_ReturnsNull_WhenPathIsEmpty(string? path)
    {
        ProfileExecutableRunner.BuildStartInfo(path, "--flag").Should().BeNull();
    }

    [Test]
    public void BuildStartInfo_RunsExeDirectly()
    {
        var startInfo = ProfileExecutableRunner.BuildStartInfo(@"C:\tools\notify.exe", "--profile foo");

        startInfo.Should().NotBeNull();
        startInfo!.FileName.Should().Be(@"C:\tools\notify.exe");
        startInfo.Arguments.Should().Be("--profile foo");
        startInfo.UseShellExecute.Should().BeFalse();
        startInfo.CreateNoWindow.Should().BeTrue();
    }

    [Test]
    public void BuildStartInfo_DefaultsNullArgumentsToEmptyString()
    {
        var startInfo = ProfileExecutableRunner.BuildStartInfo(@"C:\tools\notify.exe", null);

        startInfo!.Arguments.Should().Be("");
    }

    [Test]
    public void BuildStartInfo_RunsPowerShellScriptsThroughPowerShell()
    {
        var startInfo = ProfileExecutableRunner.BuildStartInfo(@"C:\scripts\mute.ps1", "-Silent");

        startInfo!.FileName.Should().Be("powershell.exe");
        startInfo.Arguments.Should().Be("-NoProfile -ExecutionPolicy Bypass -File \"C:\\scripts\\mute.ps1\" -Silent");
    }

    [TestCase(@"C:\scripts\mute.bat")]
    [TestCase(@"C:\scripts\mute.cmd")]
    [TestCase(@"C:\scripts\MUTE.BAT")]
    public void BuildStartInfo_RunsBatchScriptsThroughCmd(string path)
    {
        var startInfo = ProfileExecutableRunner.BuildStartInfo(path, "arg1 arg2");

        startInfo!.FileName.Should().Be("cmd.exe");
        startInfo.Arguments.Should().Be($"/c \"\"{path}\" arg1 arg2\"");
    }
}
