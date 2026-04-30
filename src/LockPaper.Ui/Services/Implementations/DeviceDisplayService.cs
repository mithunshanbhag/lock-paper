using Microsoft.Extensions.Logging;
#if WINDOWS
using System.ComponentModel;
using System.Runtime.InteropServices;
#endif
using LockPaper.Ui.Misc.Utilities;

namespace LockPaper.Ui.Services.Implementations;

public sealed class DeviceDisplayService(ILogger<DeviceDisplayService> logger) : IDeviceDisplayService
{
    public IReadOnlyList<DeviceDisplayInfo> GetDisplays()
    {
        logger.LogInformation("Collecting device display details.");

#if WINDOWS
        IReadOnlyList<DeviceDisplayInfo> displays;
        try
        {
            displays = GetWindowsDisplays();
            if (displays.Count == 0)
            {
                logger.LogWarning(
                    "Win32 monitor enumeration returned no monitors. Falling back to the MAUI main display info for the current device.");
                displays =
                [
                    GetMainDisplayInfo(),
                ];
            }
        }
        catch (Win32Exception exception)
        {
            logger.LogWarning(
                exception,
                "Win32 monitor enumeration failed. Falling back to the MAUI main display info for the current device.");
            displays =
            [
                GetMainDisplayInfo(),
            ];
        }
#elif ANDROID
        IReadOnlyList<DeviceDisplayInfo> displays =
        [
            GetMainDisplayInfo(),
        ];
#else
        IReadOnlyList<DeviceDisplayInfo> displays = Array.Empty<DeviceDisplayInfo>();
#endif

        logger.LogInformation("Collected {DisplayCount} display(s).", displays.Count);
        foreach (var display in displays)
        {
            logger.LogInformation(
                "Display collected: {Width}x{Height}, primary={IsPrimary}, approximate diagonal={Diagonal}.",
                display.PixelWidth,
                display.PixelHeight,
                display.IsPrimary,
                display.ApproximateDiagonalInches);
        }

        return displays;
    }

    private static DeviceDisplayInfo GetMainDisplayInfo()
    {
        var displayInfo = DeviceDisplay.Current.MainDisplayInfo;
        return DeviceDisplayInfoFactory.Create(
            new DisplaySnapshot
            {
                PixelWidth = (int)Math.Round(displayInfo.Width),
                PixelHeight = (int)Math.Round(displayInfo.Height),
                PositionX = 0,
                PositionY = 0,
                PixelsPerInch = displayInfo.Density > 0 ? displayInfo.Density * 160d : null,
                IsPrimary = true,
            });
    }

#if WINDOWS
    private static IReadOnlyList<DeviceDisplayInfo> GetWindowsDisplays()
    {
        List<DisplaySnapshot> displaySnapshots = [];
        Win32Exception? callbackException = null;

        MonitorEnumProc callback = (monitorHandle, _, _, _) =>
        {
            var monitorInfo = MonitorInfoEx.Create();
            if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                callbackException = new Win32Exception(Marshal.GetLastWin32Error());
                return false;
            }

            var width = monitorInfo.MonitorArea.Right - monitorInfo.MonitorArea.Left;
            var height = monitorInfo.MonitorArea.Bottom - monitorInfo.MonitorArea.Top;
            displaySnapshots.Add(
                new DisplaySnapshot
                {
                    PixelWidth = width,
                    PixelHeight = height,
                    PositionX = monitorInfo.MonitorArea.Left,
                    PositionY = monitorInfo.MonitorArea.Top,
                    PixelsPerInch = 96d,
                    IsPrimary = (monitorInfo.Flags & MonitorInfoPrimaryFlag) != 0,
                });

            return true;
        };

        if (!EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero))
        {
            throw callbackException ?? new Win32Exception(Marshal.GetLastWin32Error());
        }

        return DeviceDisplayInfoFactory.CreateOrdered(displaySnapshots);
    }

    private const uint MonitorInfoPrimaryFlag = 0x1;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(
        IntPtr hdc,
        IntPtr clipRectangle,
        MonitorEnumProc callback,
        IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitorHandle, ref MonitorInfoEx monitorInfo);

    private delegate bool MonitorEnumProc(
        IntPtr monitorHandle,
        IntPtr deviceContextHandle,
        IntPtr monitorRectangle,
        IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;

        public int Top;

        public int Right;

        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct MonitorInfoEx
    {
        public uint Size;

        public NativeRectangle MonitorArea;

        public NativeRectangle WorkArea;

        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string DeviceName;

        public static MonitorInfoEx Create() =>
            new()
            {
                Size = (uint)Marshal.SizeOf<MonitorInfoEx>(),
                DeviceName = string.Empty,
            };
    }
#endif
}
