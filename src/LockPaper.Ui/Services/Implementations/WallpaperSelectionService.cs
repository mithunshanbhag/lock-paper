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

        var orientationPool = FilterPool(
            photos,
            photo => MatchesOrientation(photo, display));
        var coveragePool = FilterPool(
            orientationPool,
            photo => CanCoverTarget(photo, display));
        var aspectPool = FilterByScore(
            coveragePool,
            photo => GetAspectRatioDelta(photo, display),
            0.08d);
        var resolutionPool = FilterByScore(
            aspectPool,
            photo => GetResolutionPenalty(photo, display),
            0.2d);

        return resolutionPool[randomizer.Next(resolutionPool.Count)];
    }

    private static IReadOnlyList<OneDriveWallpaperPhoto> FilterPool(
        IReadOnlyList<OneDriveWallpaperPhoto> photos,
        Func<OneDriveWallpaperPhoto, bool> predicate)
    {
        var filteredPhotos = photos.Where(predicate).ToArray();
        return filteredPhotos.Length == 0
            ? photos
            : filteredPhotos;
    }

    private static IReadOnlyList<OneDriveWallpaperPhoto> FilterByScore(
        IReadOnlyList<OneDriveWallpaperPhoto> photos,
        Func<OneDriveWallpaperPhoto, double> scoreSelector,
        double tolerance)
    {
        if (photos.Count == 0)
        {
            return photos;
        }

        var bestScore = photos.Min(scoreSelector);
        var filteredPhotos = photos
            .Where(photo => scoreSelector(photo) <= bestScore + tolerance)
            .ToArray();

        return filteredPhotos.Length == 0
            ? photos
            : filteredPhotos;
    }

    private static bool MatchesOrientation(OneDriveWallpaperPhoto photo, DeviceDisplayInfo display)
    {
        var displayOrientation = GetOrientation(display.PixelWidth, display.PixelHeight);
        var photoOrientation = GetOrientation(photo.PixelWidth, photo.PixelHeight);

        return displayOrientation == WallpaperOrientation.Square
            || photoOrientation == WallpaperOrientation.Square
            || displayOrientation == photoOrientation;
    }

    private static bool CanCoverTarget(OneDriveWallpaperPhoto photo, DeviceDisplayInfo display) =>
        photo.PixelWidth >= display.PixelWidth
        && photo.PixelHeight >= display.PixelHeight;

    private static double GetAspectRatioDelta(OneDriveWallpaperPhoto photo, DeviceDisplayInfo display)
    {
        var photoRatio = GetAspectRatio(photo.PixelWidth, photo.PixelHeight);
        var displayRatio = GetAspectRatio(display.PixelWidth, display.PixelHeight);
        return Math.Abs(photoRatio - displayRatio);
    }

    private static double GetResolutionPenalty(OneDriveWallpaperPhoto photo, DeviceDisplayInfo display)
    {
        if (CanCoverTarget(photo, display))
        {
            var photoArea = (double)photo.PixelWidth * photo.PixelHeight;
            var displayArea = Math.Max((double)display.PixelWidth * display.PixelHeight, 1d);
            return Math.Abs(Math.Log(photoArea / displayArea));
        }

        var widthShortfall = Math.Max(display.PixelWidth - photo.PixelWidth, 0) / (double)Math.Max(display.PixelWidth, 1);
        var heightShortfall = Math.Max(display.PixelHeight - photo.PixelHeight, 0) / (double)Math.Max(display.PixelHeight, 1);
        return widthShortfall + heightShortfall;
    }

    private static double GetAspectRatio(int width, int height) =>
        width <= 0 || height <= 0
            ? 1d
            : width / (double)height;

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
