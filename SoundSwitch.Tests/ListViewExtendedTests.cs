using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

using NUnit.Framework;

using SoundSwitch.Framework.WinApi;
using SoundSwitch.UI.Component.ListView;

namespace SoundSwitch.Tests;

[TestFixture]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public class ListViewExtendedTests
{
    private const int WM_REFLECT_NOTIFY = 0x204E;
    private const int NM_CUSTOMDRAW = -12;

    private const uint CDDS_PREPAINT = 0x00000001;
    private const uint CDRF_SKIPDEFAULT = 0x00000004;
    private const uint CDRF_NOTIFYITEMDRAW = 0x00000020;
    private const uint LVCDI_GROUP = 0x00000001;

    [Test]
    public void DrawGroupHeader_ShouldPaintDarkBackgroundAndSeparator()
    {
        using var listView = new ListViewExtended
        {
            BackColor = Color.FromArgb(32, 32, 32)
        };
        var group = new ListViewGroup("Selected", HorizontalAlignment.Center);
        listView.Groups.Add(group);

        using var bitmap = new Bitmap(80, 24);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Red);

        listView.DrawGroupHeader(graphics, new Rectangle(0, 0, 80, 24), group);

        Assert.That(bitmap.GetPixel(1, 1), Is.EqualTo(Color.FromArgb(32, 32, 32)));
        Assert.That(bitmap.GetPixel(40, 23), Is.EqualTo(Color.FromArgb(80, 80, 80)));
    }

    [Test]
    public void WndProc_CustomDraw_ControlLevelPrepaint_RequestsItemDraw()
    {
        try
        {
            WindowsThemeHelper.DarkModeProvider = () => true;

            using var listView = new TestableListViewExtended
            {
                BackColor = Color.FromArgb(32, 32, 32)
            };
            _ = listView.Handle;

            using var bitmap = new Bitmap(80, 24);
            using var bitmapGraphics = Graphics.FromImage(bitmap);
            var hdc = bitmapGraphics.GetHdc();
            try
            {
                var prepaintPointer = Marshal.AllocHGlobal(Marshal.SizeOf<TestNMLVCUSTOMDRAW>());
                try
                {
                    Marshal.StructureToPtr(CreateCustomDraw(CDDS_PREPAINT, hdc, null, 0), prepaintPointer, false);
                    var prepaintMessage = CreateNotifyMessage(listView, prepaintPointer);
                    listView.SendWndProc(ref prepaintMessage);
                    Assert.That(prepaintMessage.Result, Is.EqualTo((IntPtr)CDRF_NOTIFYITEMDRAW));
                }
                finally
                {
                    Marshal.FreeHGlobal(prepaintPointer);
                }
            }
            finally
            {
                bitmapGraphics.ReleaseHdc(hdc);
            }
        }
        finally
        {
            // The provider seam is reset even on assertion failure so no test state leaks.
            WindowsThemeHelper.DarkModeProvider = null;
        }
    }

    /// <summary>
    /// comctl32 reports group headers through their own CDDS_PREPAINT (ItemType == LVCDI_GROUP),
    /// not through CDDS_ITEMPREPAINT — see ListViewExtended's comment. Painting them needs
    /// LVM_GETGROUPRECT, which only returns real geometry once the native control has gone
    /// through an actual layout/paint pass; a synthetic message alone can't reliably force that
    /// (confirmed manually against a real running window). So this only pins down the safe,
    /// deterministic half of the contract — no crash, no default-paint suppressed on a miss —
    /// while the visual output itself is covered by <see cref="DrawGroupHeader_ShouldPaintDarkBackgroundAndSeparator"/>.
    /// </summary>
    [Test]
    public void WndProc_CustomDraw_GroupPrepaintWithoutLaidOutGeometry_FallsThroughSafely()
    {
        try
        {
            WindowsThemeHelper.DarkModeProvider = () => true;

            using var listView = new TestableListViewExtended
            {
                BackColor = Color.FromArgb(32, 32, 32)
            };
            var group = new ListViewGroup("Selected", HorizontalAlignment.Center);
            listView.Groups.Add(group);
            _ = listView.Handle;

            var groupId = GetGroupID(group);
            Assert.That(groupId, Is.Not.Null);

            using var bitmap = new Bitmap(80, 24);
            using var bitmapGraphics = Graphics.FromImage(bitmap);
            var hdc = bitmapGraphics.GetHdc();
            try
            {
                var groupPaintPointer = Marshal.AllocHGlobal(Marshal.SizeOf<TestNMLVCUSTOMDRAW>());
                try
                {
                    Marshal.StructureToPtr(CreateCustomDraw(CDDS_PREPAINT, hdc, groupId, LVCDI_GROUP),
                        groupPaintPointer, false);
                    var groupPaintMessage = CreateNotifyMessage(listView, groupPaintPointer);

                    Assert.DoesNotThrow(() => listView.SendWndProc(ref groupPaintMessage));
                }
                finally
                {
                    Marshal.FreeHGlobal(groupPaintPointer);
                }
            }
            finally
            {
                bitmapGraphics.ReleaseHdc(hdc);
            }
        }
        finally
        {
            // The provider seam is reset even on assertion failure so no test state leaks.
            WindowsThemeHelper.DarkModeProvider = null;
        }
    }

    private static TestNMLVCUSTOMDRAW CreateCustomDraw(uint drawStage, IntPtr hdc, int? groupId, uint itemType)
    {
        return new TestNMLVCUSTOMDRAW
        {
            Nmcd = new TestNMCUSTOMDRAW
            {
                Header = new TestNMHDR
                {
                    Code = NM_CUSTOMDRAW
                },
                DrawStage = drawStage,
                HDC = hdc,
                Rect = new TestRECT
                {
                    Left = 0,
                    Top = 0,
                    Right = 80,
                    Bottom = 24
                },
                ItemSpec = groupId.HasValue ? (IntPtr)groupId.Value : IntPtr.Zero
            },
            ItemType = itemType
        };
    }

    private static Message CreateNotifyMessage(TestableListViewExtended listView, IntPtr lParam)
    {
        var message = Message.Create(listView.Handle, WM_REFLECT_NOTIFY, IntPtr.Zero, lParam);
        message.Result = IntPtr.Zero;
        return message;
    }

    private static int? GetGroupID(ListViewGroup group)
    {
        var property = group.GetType().GetProperty("ID", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (int?)property?.GetValue(group);
    }

    private sealed class TestableListViewExtended : ListViewExtended
    {
        public void SendWndProc(ref Message message) => WndProc(ref message);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestNMHDR
    {
        public IntPtr HwndFrom;
        public IntPtr IdFrom;
        public int Code;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestNMCUSTOMDRAW
    {
        public TestNMHDR Header;
        public uint DrawStage;
        public IntPtr HDC;
        public TestRECT Rect;
        public IntPtr ItemSpec;
        public uint ItemState;
        public IntPtr ItemParam;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestNMLVCUSTOMDRAW
    {
        public TestNMCUSTOMDRAW Nmcd;
        public uint TextColor;
        public uint TextBackColor;
        public int SubItem;
        public uint ItemType;
        public uint FaceColor;
        public int IconEffect;
        public int IconPhase;
        public int PartId;
        public int StateId;
        public TestRECT TextRect;
        public uint Align;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TestRECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
