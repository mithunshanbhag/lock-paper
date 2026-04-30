namespace LockPaper.Ui.Models;

public sealed class OneDriveAlbumDiscoveryResult
{
    public required OneDriveAlbumDiscoveryStatus Status { get; init; }

    public IReadOnlyList<string> MatchingAlbumNames { get; init; } = [];

    public string ErrorCode { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public static OneDriveAlbumDiscoveryResult Succeeded(IEnumerable<string> matchingAlbumNames) =>
        new()
        {
            Status = OneDriveAlbumDiscoveryStatus.Found,
            MatchingAlbumNames = matchingAlbumNames.ToArray(),
        };

    public static OneDriveAlbumDiscoveryResult NotFound() =>
        new()
        {
            Status = OneDriveAlbumDiscoveryStatus.NotFound,
        };

    public static OneDriveAlbumDiscoveryResult Failed(
        string? errorCode = null,
        string? errorMessage = null) =>
        new()
        {
            Status = OneDriveAlbumDiscoveryStatus.Failed,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
        };
}
