using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Text.Json;

namespace LockPaper.Ui.Services.Implementations;

public sealed class WallpaperRefreshService(
    IDeviceDisplayService deviceDisplayService,
    ILockScreenWallpaperService lockScreenWallpaperService,
    IOneDriveWallpaperSourceService oneDriveWallpaperSourceService,
    IRandomizer randomizer,
    IWallpaperSelectionService wallpaperSelectionService,
    ILogger<WallpaperRefreshService> logger) : IWallpaperRefreshService
{
    public async Task<WallpaperRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var attemptedAtLocal = DateTimeOffset.Now;
        var matchingAlbumCount = 0;

        try
        {
            var displays = deviceDisplayService.GetDisplays();
            var targetDisplay = GetTargetDisplay(displays);
            if (targetDisplay is null)
            {
                return WallpaperRefreshResult.Failed(attemptedAtLocal, 0, "LockPaper couldn't read the current display details.");
            }

            IReadOnlyList<OneDriveWallpaperAlbum> matchingAlbums;
            try
            {
                matchingAlbums = await oneDriveWallpaperSourceService
                    .GetMatchingAlbumsAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (InvalidOperationException exception)
            {
                return CreateReauthenticationRequiredResult(attemptedAtLocal, exception);
            }

            matchingAlbumCount = matchingAlbums.Count;
            if (matchingAlbums.Count == 0)
            {
                return WallpaperRefreshResult.NoMatchingAlbums(attemptedAtLocal);
            }

            foreach (var album in ShuffleAlbums(matchingAlbums))
            {
                IReadOnlyList<OneDriveWallpaperPhoto> photos;
                try
                {
                    photos = await oneDriveWallpaperSourceService
                        .GetAlbumPhotosAsync(album.Id, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (InvalidOperationException exception)
                {
                    return CreateReauthenticationRequiredResult(attemptedAtLocal, exception);
                }

                var selectedPhoto = wallpaperSelectionService.SelectBestPhoto(photos, targetDisplay);
                if (selectedPhoto is null)
                {
                    continue;
                }

                byte[] imageBytes;
                try
                {
                    imageBytes = await oneDriveWallpaperSourceService
                        .DownloadPhotoBytesAsync(selectedPhoto.Id, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (InvalidOperationException exception)
                {
                    return CreateReauthenticationRequiredResult(attemptedAtLocal, exception);
                }

                var localFilePath = await SaveWallpaperFileAsync(selectedPhoto, imageBytes, cancellationToken).ConfigureAwait(false);

                await lockScreenWallpaperService
                    .ApplyAsync(localFilePath, cancellationToken)
                    .ConfigureAwait(false);

                return WallpaperRefreshResult.Succeeded(attemptedAtLocal, matchingAlbumCount, album.Name, selectedPhoto.Name);
            }

            return WallpaperRefreshResult.NoEligiblePhotos(attemptedAtLocal, matchingAlbumCount);
        }
        catch (Exception exception) when (
            exception is HttpRequestException
            or JsonException
            or IOException
            or PlatformNotSupportedException
            or UnauthorizedAccessException
            or InvalidOperationException)
        {
            logger.LogWarning(exception, "Wallpaper refresh failed.");
            return WallpaperRefreshResult.Failed(attemptedAtLocal, matchingAlbumCount, exception.Message);
        }
    }

    private WallpaperRefreshResult CreateReauthenticationRequiredResult(
        DateTimeOffset attemptedAtLocal,
        InvalidOperationException exception)
    {
        logger.LogWarning(exception, "Wallpaper refresh requires the user to sign in again before LockPaper can read OneDrive.");
        return WallpaperRefreshResult.ReauthenticationRequired(attemptedAtLocal, exception.Message);
    }

    private IEnumerable<OneDriveWallpaperAlbum> ShuffleAlbums(IReadOnlyList<OneDriveWallpaperAlbum> albums)
    {
        var shuffledAlbums = albums.ToList();
        for (var index = shuffledAlbums.Count - 1; index > 0; index--)
        {
            var swapIndex = randomizer.Next(index + 1);
            (shuffledAlbums[index], shuffledAlbums[swapIndex]) = (shuffledAlbums[swapIndex], shuffledAlbums[index]);
        }

        return shuffledAlbums;
    }

    private static DeviceDisplayInfo? GetTargetDisplay(IReadOnlyList<DeviceDisplayInfo> displays) =>
        displays
            .OrderByDescending(display => display.IsPrimary)
            .ThenByDescending(display => (long)display.PixelWidth * display.PixelHeight)
            .FirstOrDefault();

    private static async Task<string> SaveWallpaperFileAsync(
        OneDriveWallpaperPhoto photo,
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        var wallpapersDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LockPaper",
            "Wallpapers");
        Directory.CreateDirectory(wallpapersDirectory);

        var fileExtension = Path.GetExtension(photo.Name);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".jpg";
        }

        var safeFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(photo.Name));
        var wallpaperFilePath = Path.Combine(
            wallpapersDirectory,
            $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}-{safeFileName}{fileExtension}");

        await File.WriteAllBytesAsync(wallpaperFilePath, imageBytes, cancellationToken).ConfigureAwait(false);
        DeleteOlderWallpaperFiles(wallpapersDirectory, wallpaperFilePath);

        return wallpaperFilePath;
    }

    private static void DeleteOlderWallpaperFiles(string wallpapersDirectory, string currentWallpaperFilePath)
    {
        var retainedFiles = Directory
            .EnumerateFiles(wallpapersDirectory)
            .OrderByDescending(File.GetCreationTimeUtc)
            .Take(5)
            .Append(currentWallpaperFilePath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var wallpaperFile in Directory.EnumerateFiles(wallpapersDirectory))
        {
            if (!retainedFiles.Contains(wallpaperFile))
            {
                File.Delete(wallpaperFile);
            }
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidCharacters = Path.GetInvalidFileNameChars();
        return string.IsNullOrWhiteSpace(fileName)
            ? "lockpaper-wallpaper"
            : new string(fileName.Select(character => invalidCharacters.Contains(character) ? '_' : character).ToArray());
    }
}
