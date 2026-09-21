#nullable enable
using System.Diagnostics;
using System.IO;

namespace SoundSwitch.Framework.Profile;

/// <summary>
/// Builds the <see cref="ProcessStartInfo"/> used to run a profile's start/stop command.
/// Scripts (.ps1/.bat/.cmd) aren't native executables, so Windows can't <c>CreateProcess</c> them
/// directly with <see cref="ProcessStartInfo.UseShellExecute"/> set to <c>false</c> — they need to
/// be handed to their interpreter instead.
/// </summary>
public static class ProfileExecutableRunner
{
    /// <param name="command">
    /// A single command line: the executable/script path, optionally quoted (needed if it contains
    /// spaces), followed by its arguments exactly as they should reach the process.
    /// </param>
    public static ProcessStartInfo? BuildStartInfo(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return null;
        }

        var (path, arguments) = SplitCommand(command.Trim());
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

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

    /// <summary>
    /// Splits a command line into its leading executable/script path and the remaining arguments,
    /// following the same convention Windows uses for CreateProcess's image name: the path is
    /// delimited by the next quote if it starts with one (allowing spaces), otherwise by whitespace.
    /// </summary>
    public static (string Path, string Arguments) SplitCommand(string command)
    {
        if (command.Length == 0)
        {
            return ("", "");
        }

        if (command[0] == '"')
        {
            var closingQuote = command.IndexOf('"', 1);
            if (closingQuote > 0)
            {
                return (command[1..closingQuote], command[(closingQuote + 1)..].TrimStart());
            }
        }

        var spaceIndex = command.IndexOf(' ');
        return spaceIndex < 0
            ? (command, "")
            : (command[..spaceIndex], command[(spaceIndex + 1)..].TrimStart());
    }
}
