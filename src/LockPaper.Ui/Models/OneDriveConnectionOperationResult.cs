namespace LockPaper.Ui.Models;

public sealed class OneDriveConnectionOperationResult
{
    public required OneDriveConnectionOperationStatus Status { get; init; }

    public required OneDriveConnectionState State { get; init; }

    public string ErrorCode { get; init; } = string.Empty;

    public string ErrorMessage { get; init; } = string.Empty;

    public static OneDriveConnectionOperationResult Succeeded(OneDriveConnectionState state) =>
        new()
        {
            Status = OneDriveConnectionOperationStatus.Succeeded,
            State = state,
        };

    public static OneDriveConnectionOperationResult Cancelled(OneDriveConnectionState state) =>
        new()
        {
            Status = OneDriveConnectionOperationStatus.Cancelled,
            State = state,
        };

    public static OneDriveConnectionOperationResult Failed(
        OneDriveConnectionState state,
        string? errorCode = null,
        string? errorMessage = null) =>
        new()
        {
            Status = OneDriveConnectionOperationStatus.Failed,
            State = state,
            ErrorCode = errorCode ?? string.Empty,
            ErrorMessage = errorMessage ?? string.Empty,
        };
}
