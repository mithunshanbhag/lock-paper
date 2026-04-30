namespace LockPaper.Ui.Models;

public sealed class WallpaperRefreshResult
{
    public required WallpaperRefreshStatus Status { get; init; }

    public required DateTimeOffset AttemptedAtLocal { get; init; }

    public int MatchingAlbumCount { get; init; }

    public string AlbumName { get; init; } = string.Empty;

    public string PhotoName { get; init; } = string.Empty;

    public string AppliedWallpaperFilePath { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public static WallpaperRefreshResult Succeeded(
        DateTimeOffset attemptedAtLocal,
        int matchingAlbumCount,
        string albumName,
        string photoName,
        string appliedWallpaperFilePath = "") =>
        new()
        {
            Status = WallpaperRefreshStatus.Succeeded,
            AttemptedAtLocal = attemptedAtLocal,
            MatchingAlbumCount = matchingAlbumCount,
            AlbumName = albumName,
            PhotoName = photoName,
            AppliedWallpaperFilePath = appliedWallpaperFilePath,
        };

    public static WallpaperRefreshResult NoMatchingAlbums(DateTimeOffset attemptedAtLocal) =>
        new()
        {
            Status = WallpaperRefreshStatus.NoMatchingAlbums,
            AttemptedAtLocal = attemptedAtLocal,
        };

    public static WallpaperRefreshResult NoEligiblePhotos(
        DateTimeOffset attemptedAtLocal,
        int matchingAlbumCount) =>
        new()
        {
            Status = WallpaperRefreshStatus.NoEligiblePhotos,
            AttemptedAtLocal = attemptedAtLocal,
            MatchingAlbumCount = matchingAlbumCount,
        };

    public static WallpaperRefreshResult ReauthenticationRequired(
        DateTimeOffset attemptedAtLocal,
        string errorMessage) =>
        new()
        {
            Status = WallpaperRefreshStatus.ReauthenticationRequired,
            AttemptedAtLocal = attemptedAtLocal,
            ErrorMessage = errorMessage,
        };

    public static WallpaperRefreshResult Failed(
        DateTimeOffset attemptedAtLocal,
        int matchingAlbumCount,
        string errorMessage) =>
        new()
        {
            Status = WallpaperRefreshStatus.Failed,
            AttemptedAtLocal = attemptedAtLocal,
            MatchingAlbumCount = matchingAlbumCount,
            ErrorMessage = errorMessage,
        };
}
