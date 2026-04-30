using LockPaper.Ui.Models;

namespace LockPaper.Ui.Misc.Utilities;

internal static class DeviceDisplayInfoFactory
{
    public static DeviceDisplayInfo Create(DisplaySnapshot snapshot) =>
        new()
        {
            PixelWidth = snapshot.PixelWidth,
            PixelHeight = snapshot.PixelHeight,
            ApproximateDiagonalInches = TryCalculateDiagonalInches(
                snapshot.PixelWidth,
                snapshot.PixelHeight,
                snapshot.PixelsPerInch),
            IsPrimary = snapshot.IsPrimary,
        };

    public static IReadOnlyList<DeviceDisplayInfo> CreateOrdered(IEnumerable<DisplaySnapshot> snapshots) =>
        snapshots
            .OrderByDescending(snapshot => snapshot.IsPrimary)
            .ThenBy(snapshot => snapshot.PositionX)
            .ThenBy(snapshot => snapshot.PositionY)
            .Select(Create)
            .ToArray();

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
