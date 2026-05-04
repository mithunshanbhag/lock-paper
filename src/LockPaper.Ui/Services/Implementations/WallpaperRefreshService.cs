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
    public Task<string?> GetCurrentWallpaperPreviewFilePathAsync(CancellationToken cancellationToken = default) =>
        lockScreenWallpaperService.GetCurrentWallpaperPreviewFilePathAsync(cancellationToken);

    public async Task<WallpaperRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
    {
        var attemptedAtLocal = DateTimeOffset.Now;
        var matchingAlbumCount = 0;
        logger.LogInformation("Starting wallpaper refresh attempt at {AttemptedAtLocal}.", attemptedAtLocal);

        try
        {
            var displays = deviceDisplayService.GetDisplays();
            var targetDisplay = GetTargetDisplay(displays);
            if (targetDisplay is null)
            {
                logger.LogWarning("Wallpaper refresh could not choose a target display from {DisplayCount} display(s).", displays.Count);
                return WallpaperRefreshResult.Failed(attemptedAtLocal, 0, "LockPaper couldn't read the current display details.");
            }

            logger.LogInformation(
                "Wallpaper refresh selected target display {DisplayLabel}.",
                FormatDisplayLabel(targetDisplay));

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
            logger.LogInformation("Wallpaper refresh found {MatchingAlbumCount} matching album(s).", matchingAlbumCount);
            if (matchingAlbums.Count == 0)
            {
                logger.LogInformation("Wallpaper refresh stopped because no matching albums were available.");
                return WallpaperRefreshResult.NoMatchingAlbums(attemptedAtLocal);
            }

            foreach (var album in ShuffleAlbums(matchingAlbums))
            {
                logger.LogInformation(
                    "Evaluating matching album '{AlbumName}' ({AlbumId}) for wallpaper refresh.",
                    album.Name,
                    album.Id);

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

                logger.LogInformation(
                    "Album '{AlbumName}' returned {PhotoCount} usable photo(s).",
                    album.Name,
                    photos.Count);

                var selectedPhoto = wallpaperSelectionService.SelectBestPhoto(photos, targetDisplay);
                if (selectedPhoto is null)
                {
                    logger.LogInformation(
                        "Skipping album '{AlbumName}' because no eligible photo could be selected for target display {DisplayLabel}.",
                        album.Name,
                        FormatDisplayLabel(targetDisplay));
                    continue;
                }

                logger.LogInformation(
                    "Selected photo '{PhotoName}' ({PhotoWidth}x{PhotoHeight}) from album '{AlbumName}' for target display {DisplayLabel}.",
                    selectedPhoto.Name,
                    selectedPhoto.PixelWidth,
                    selectedPhoto.PixelHeight,
                    album.Name,
                    FormatDisplayLabel(targetDisplay));

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
                logger.LogInformation(
                    "Saved wallpaper candidate '{PhotoName}' to {LocalFilePath} ({ByteCount} bytes).",
                    selectedPhoto.Name,
                    localFilePath,
                    imageBytes.Length);

                await lockScreenWallpaperService
                    .ApplyAsync(localFilePath, cancellationToken)
                    .ConfigureAwait(false);

                logger.LogInformation(
                    "Applied wallpaper candidate '{PhotoName}' from album '{AlbumName}' to the lock screen.",
                    selectedPhoto.Name,
                    album.Name);

                return WallpaperRefreshResult.Succeeded(
                    attemptedAtLocal,
                    matchingAlbumCount,
                    album.Name,
                    selectedPhoto.Name,
                    localFilePath);
            }

            logger.LogInformation(
                "Wallpaper refresh finished without an eligible photo after evaluating {MatchingAlbumCount} matching album(s).",
                matchingAlbumCount);
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
            logger.LogWarning(exception, "Wallpaper refresh failed after evaluating {MatchingAlbumCount} matching album(s).", matchingAlbumCount);
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

    private static string FormatDisplayLabel(DeviceDisplayInfo display) =>
        $"{display.PixelWidth}x{display.PixelHeight}, primary={display.IsPrimary}";

    private static async Task<string> SaveWallpaperFileAsync(
        OneDriveWallpaperPhoto photo,
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        var wallpapersDirectory = GetWallpapersDirectory();
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

    private static string GetWallpapersDirectory()
    {
#if WINDOWS
        return Path.Combine(
            Windows.Storage.ApplicationData.Current.LocalFolder.Path,
            "Wallpapers");
#else
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LockPaper",
            "Wallpapers");
#endif
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
