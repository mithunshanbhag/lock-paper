namespace LockPaper.Ui.Models;

public sealed class PlatformPermissionRequestResult
{
    public required PlatformPermissionRequestStatus Status { get; init; }

    public bool ShouldRefreshDisplaySummary { get; init; }

    public string FeedbackMessage { get; init; } = string.Empty;

    public static PlatformPermissionRequestResult NotRequired() =>
        new()
        {
            Status = PlatformPermissionRequestStatus.NotRequired,
        };

    public static PlatformPermissionRequestResult Granted(bool shouldRefreshDisplaySummary = false) =>
        new()
        {
            Status = PlatformPermissionRequestStatus.Granted,
            ShouldRefreshDisplaySummary = shouldRefreshDisplaySummary,
        };

    public static PlatformPermissionRequestResult Denied(string? feedbackMessage = null) =>
        new()
        {
            Status = PlatformPermissionRequestStatus.Denied,
            FeedbackMessage = feedbackMessage ?? string.Empty,
        };

    public static PlatformPermissionRequestResult Failed(string? feedbackMessage = null) =>
        new()
        {
            Status = PlatformPermissionRequestStatus.Failed,
            FeedbackMessage = feedbackMessage ?? string.Empty,
        };
}
