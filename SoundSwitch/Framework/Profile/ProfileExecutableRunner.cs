#nullable enable
using System.Diagnostics;
using System.IO;

namespace SoundSwitch.Framework.Profile;

/// <summary>
/// Builds the <see cref="ProcessStartInfo"/> used to run a profile's start/stop executable.
/// Scripts (.ps1/.bat/.cmd) aren't native executables, so Windows can't <c>CreateProcess</c> them
/// directly with <see cref="ProcessStartInfo.UseShellExecute"/> set to <c>false</c> — they need to
/// be handed to their interpreter instead.
/// </summary>
public static class ProfileExecutableRunner
{
    public static ProcessStartInfo? BuildStartInfo(string? path, string? arguments)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        arguments ??= "";

        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden
        };

        switch (Path.GetExtension(path).ToLowerInvariant())
        {
            case ".ps1":
                startInfo.FileName = "powershell.exe";
                startInfo.Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{path}\" {arguments}";
                break;
            case ".bat":
            case ".cmd":
                startInfo.FileName = "cmd.exe";
                startInfo.Arguments = $"/c \"\"{path}\" {arguments}\"";
                break;
            default:
                startInfo.FileName = path;
                startInfo.Arguments = arguments;
                break;
        }

        return startInfo;
    }
}
