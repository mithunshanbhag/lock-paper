#if WINDOWS
using Microsoft.UI.Windowing;
#endif
using Microsoft.Extensions.Logging;

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
        }
        catch (InvalidCastException exception)
        {
            logger.LogWarning(
                exception,
                "WinUI display enumeration failed. Falling back to the MAUI main display info for the current device.");
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
        return new DeviceDisplayInfo
        {
            PixelWidth = (int)Math.Round(displayInfo.Width),
            PixelHeight = (int)Math.Round(displayInfo.Height),
            ApproximateDiagonalInches = TryCalculateDiagonalInches(
                (int)Math.Round(displayInfo.Width),
                (int)Math.Round(displayInfo.Height),
                displayInfo.Density > 0 ? displayInfo.Density * 160d : null),
            IsPrimary = true,
        };
    }

#if WINDOWS
    private static IReadOnlyList<DeviceDisplayInfo> GetWindowsDisplays() =>
        DisplayArea.FindAll()
            .OrderByDescending(displayArea => displayArea.IsPrimary)
            .ThenBy(displayArea => displayArea.OuterBounds.X)
            .ThenBy(displayArea => displayArea.OuterBounds.Y)
            .Select(displayArea => new DeviceDisplayInfo
            {
                PixelWidth = displayArea.OuterBounds.Width,
                PixelHeight = displayArea.OuterBounds.Height,
                ApproximateDiagonalInches = TryCalculateDiagonalInches(
                    displayArea.OuterBounds.Width,
                    displayArea.OuterBounds.Height,
                    96d),
                IsPrimary = displayArea.IsPrimary,
            })
            .ToArray();
#endif

    private static double? TryCalculateDiagonalInches(int pixelWidth, int pixelHeight, double? pixelsPerInch)
    {
        if (pixelsPerInch is null or <= 0)
        {
            return null;
        }

        var widthInches = pixelWidth / pixelsPerInch.Value;
        var heightInches = pixelHeight / pixelsPerInch.Value;
        return Math.Sqrt((widthInches * widthInches) + (heightInches * heightInches));
    }
}
