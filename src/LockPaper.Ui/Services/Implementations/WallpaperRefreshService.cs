using LockPaper.Ui.Misc.Utilities;
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
    private const string PersistedWallpaperPhotoKeyFileName = "current-lockscreen-photo-key.txt";

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

            var wallpaperTargetDisplay = GetWallpaperTargetDisplay(targetDisplay);
            logger.LogInformation(
                "Wallpaper refresh selected target display {DisplayLabel}.",
                FormatDisplayLabel(wallpaperTargetDisplay));

            var currentWallpaperPhotoKey = await TryGetPersistedWallpaperPhotoKeyAsync(cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(currentWallpaperPhotoKey))
            {
                logger.LogInformation("Wallpaper refresh will avoid reusing the currently applied OneDrive photo when possible.");
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

                var remainingPhotos = photos.ToList();
                while (remainingPhotos.Count > 0)
                {
                    var selectedPhoto = wallpaperSelectionService.SelectBestPhoto(remainingPhotos, wallpaperTargetDisplay);
                    if (selectedPhoto is null)
                    {
                        break;
                    }

                    var selectedPhotoKey = BuildWallpaperPhotoKey(album.Id, selectedPhoto.Id);
                    if (string.Equals(selectedPhotoKey, currentWallpaperPhotoKey, StringComparison.Ordinal))
                    {
                        logger.LogInformation(
                            "Skipping photo '{PhotoName}' from album '{AlbumName}' because it is already applied to the lock screen.",
                            selectedPhoto.Name,
                            album.Name);
                        remainingPhotos.RemoveAll(photo => string.Equals(photo.Id, selectedPhoto.Id, StringComparison.OrdinalIgnoreCase));
                        continue;
                    }

                    logger.LogInformation(
                        "Selected photo '{PhotoName}' ({PhotoWidth}x{PhotoHeight}) from album '{AlbumName}' for target display {DisplayLabel}.",
                        selectedPhoto.Name,
                        selectedPhoto.PixelWidth,
                        selectedPhoto.PixelHeight,
                        album.Name,
                        FormatDisplayLabel(wallpaperTargetDisplay));

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

#if ANDROID
                    var orientationMatchesTarget = AndroidWallpaperImageUtility.TryMatchesTargetOrientation(
                        imageBytes,
                        wallpaperTargetDisplay.PixelWidth,
                        wallpaperTargetDisplay.PixelHeight);
                    if (orientationMatchesTarget is null)
                    {
                        logger.LogWarning(
                            "LockPaper could not inspect the EXIF-aware dimensions for photo '{PhotoName}'. Proceeding with wallpaper apply.",
                            selectedPhoto.Name);
                    }
                    else if (!orientationMatchesTarget.Value && remainingPhotos.Count > 1)
                    {
                        logger.LogInformation(
                            "Skipping photo '{PhotoName}' for Android lock-screen wallpaper because its EXIF-aware dimensions do not match target display {DisplayLabel}. Remaining candidates in album: {RemainingCandidateCount}.",
                            selectedPhoto.Name,
                            FormatDisplayLabel(wallpaperTargetDisplay),
                            remainingPhotos.Count - 1);
                        remainingPhotos.RemoveAll(photo => string.Equals(photo.Id, selectedPhoto.Id, StringComparison.OrdinalIgnoreCase));
                        continue;
                    }
#endif

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

                    await PersistWallpaperPhotoKeyAsync(selectedPhotoKey, cancellationToken).ConfigureAwait(false);

                    return WallpaperRefreshResult.Succeeded(
                        attemptedAtLocal,
                        matchingAlbumCount,
                        album.Name,
                        selectedPhoto.Name,
                        localFilePath);
                }

                logger.LogInformation(
                    "Skipping album '{AlbumName}' because no eligible photo could be selected for target display {DisplayLabel}.",
                    album.Name,
                    FormatDisplayLabel(wallpaperTargetDisplay));
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

    private static DeviceDisplayInfo GetWallpaperTargetDisplay(DeviceDisplayInfo display)
    {
#if ANDROID
        return WallpaperTargetDisplayNormalizer.NormalizeForPortraitLockScreen(display);
#else
        return display;
#endif
    }

    private static string FormatDisplayLabel(DeviceDisplayInfo display) =>
        $"{display.PixelWidth}x{display.PixelHeight}, primary={display.IsPrimary}";

    private static string BuildWallpaperPhotoKey(string albumId, string photoId) => $"{albumId}:{photoId}";

    private static async Task PersistWallpaperPhotoKeyAsync(string photoKey, CancellationToken cancellationToken)
    {
        var wallpaperStateDirectory = GetWallpaperStateDirectory();
        Directory.CreateDirectory(wallpaperStateDirectory);

        await File
            .WriteAllTextAsync(GetPersistedWallpaperPhotoKeyFilePath(), photoKey, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<string?> TryGetPersistedWallpaperPhotoKeyAsync(CancellationToken cancellationToken)
    {
        var persistedWallpaperPhotoKeyFilePath = GetPersistedWallpaperPhotoKeyFilePath();
        if (!File.Exists(persistedWallpaperPhotoKeyFilePath))
        {
            return null;
        }

        var persistedWallpaperPhotoKey = (await File.ReadAllTextAsync(persistedWallpaperPhotoKeyFilePath, cancellationToken).ConfigureAwait(false)).Trim();
        return string.IsNullOrWhiteSpace(persistedWallpaperPhotoKey)
            ? null
            : persistedWallpaperPhotoKey;
    }

    private static async Task<string> SaveWallpaperFileAsync(
        OneDriveWallpaperPhoto photo,
        byte[] imageBytes,
        CancellationToken cancellationToken)
    {
        var wallpapersDirectory = GetWallpapersDirectory();
        Directory.CreateDirectory(wallpapersDirectory);

#if ANDROID
        var fileExtension = ".jpg";
#else
        var fileExtension = Path.GetExtension(photo.Name);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            fileExtension = ".jpg";
        }
#endif

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

    private static string GetPersistedWallpaperPhotoKeyFilePath() =>
        Path.Combine(GetWallpaperStateDirectory(), PersistedWallpaperPhotoKeyFileName);

    private static string GetWallpaperStateDirectory()
    {
#if WINDOWS
        return Path.Combine(
            Windows.Storage.ApplicationData.Current.LocalFolder.Path,
            "WallpaperState");
#else
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LockPaper",
            "WallpaperState");
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
