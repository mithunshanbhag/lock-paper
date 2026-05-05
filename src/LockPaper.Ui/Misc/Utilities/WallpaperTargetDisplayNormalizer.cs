using LockPaper.Ui.Models;

namespace LockPaper.Ui.Misc.Utilities;

internal static class WallpaperTargetDisplayNormalizer
{
    public static DeviceDisplayInfo NormalizeForPortraitLockScreen(DeviceDisplayInfo display)
    {
        ArgumentNullException.ThrowIfNull(display);

        return display.PixelWidth <= display.PixelHeight
            ? display
            : new DeviceDisplayInfo
            {
                PixelWidth = display.PixelHeight,
                PixelHeight = display.PixelWidth,
                ApproximateDiagonalInches = display.ApproximateDiagonalInches,
                IsPrimary = display.IsPrimary,
            };
    }
}
