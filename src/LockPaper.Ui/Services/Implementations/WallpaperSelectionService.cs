using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;

namespace LockPaper.Ui.Services.Implementations;

public sealed class WallpaperSelectionService(IRandomizer randomizer) : IWallpaperSelectionService
{
    public OneDriveWallpaperPhoto? SelectBestPhoto(IReadOnlyList<OneDriveWallpaperPhoto> photos, DeviceDisplayInfo display)
    {
        ArgumentNullException.ThrowIfNull(photos);
        ArgumentNullException.ThrowIfNull(display);

        if (photos.Count == 0)
        {
            return null;
        }

        // Android re-validates orientation from downloaded EXIF data, so Graph width/height metadata
        // should not pre-filter the random pool there.
        var selectionPool = GetSelectionPool(
            photos,
            display,
            preferMatchingOrientation: !OperatingSystem.IsAndroid());
        return selectionPool[randomizer.Next(selectionPool.Count)];
    }

    internal static IReadOnlyList<OneDriveWallpaperPhoto> GetSelectionPool(
        IReadOnlyList<OneDriveWallpaperPhoto> photos,
        DeviceDisplayInfo display,
        bool preferMatchingOrientation)
    {
        if (!preferMatchingOrientation)
        {
            return photos;
        }

        var displayOrientation = GetOrientation(display.PixelWidth, display.PixelHeight);
        if (displayOrientation == WallpaperOrientation.Square)
        {
            return photos;
        }

        var matchingOrientationPhotos = photos
            .Where(photo => GetOrientation(photo.PixelWidth, photo.PixelHeight) == displayOrientation)
            .ToArray();

        return matchingOrientationPhotos.Length == 0
            ? photos
            : matchingOrientationPhotos;
    }

    private static WallpaperOrientation GetOrientation(int width, int height)
    {
        if (width == height)
        {
            return WallpaperOrientation.Square;
        }

        return height > width
            ? WallpaperOrientation.Portrait
            : WallpaperOrientation.Landscape;
    }

    private enum WallpaperOrientation
    {
        Portrait,
        Landscape,
        Square,
    }
}
