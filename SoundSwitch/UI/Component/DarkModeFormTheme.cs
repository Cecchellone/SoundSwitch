using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using SoundSwitch.Framework.WinApi;

namespace SoundSwitch.UI.Component;

/// <summary>
/// Applies the app's dark palette to a plain <see cref="Form"/> and its children.
/// <see cref="Application.SetColorMode"/> (see <c>Program.cs</c>) only reaches controls that
/// pick up their colour through ambient inheritance from their parent (Label, CheckBox, Panel);
/// <see cref="TextBox"/>, <see cref="ComboBox"/>, <see cref="ListBox"/> and
/// <see cref="ListView"/> default their own <c>BackColor</c>/<c>ForeColor</c> to
/// <see cref="SystemColors.Window"/>/<see cref="SystemColors.WindowText"/> regardless of the
/// parent, and a <see cref="GroupBox"/>'s caption text ignores inherited <c>ForeColor</c>
/// entirely — both need to be set explicitly, same as the notifications panel and group
/// captions already handled in the Settings form.
/// </summary>
internal static class DarkModeFormTheme
{
    internal static readonly Color BackColor = Color.FromArgb(32, 32, 32);
    internal static readonly Color ForeColor = Color.FromArgb(240, 240, 240);

    internal static void Apply(Form form)
    {
        var dark = WindowsThemeHelper.IsDarkModeEnabled();
        form.BackColor = dark ? BackColor : SystemColors.Control;
        form.ForeColor = dark ? ForeColor : SystemColors.ControlText;

        foreach (var control in Enumerate(form))
        {
            switch (control)
            {
                case GroupBox groupBox:
                    groupBox.ForeColor = dark ? ForeColor : SystemColors.ControlText;
                    break;
                case TextBox or ComboBox or ListBox or System.Windows.Forms.ListView:
                    control.BackColor = dark ? BackColor : SystemColors.Window;
                    control.ForeColor = dark ? ForeColor : SystemColors.WindowText;
                    break;
            }
        }
    }

    private static IEnumerable<Control> Enumerate(Control root)
    {
        foreach (Control child in root.Controls)
        {
            yield return child;

            foreach (var grandchild in Enumerate(child))
            {
                yield return grandchild;
            }
        }
    }
}
