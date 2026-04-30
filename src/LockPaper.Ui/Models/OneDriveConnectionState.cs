namespace LockPaper.Ui.Models;

public sealed class OneDriveConnectionState
{
    public required OneDriveConnectionStatus Status { get; init; }

    public required string AccountLabel { get; init; }

    public static OneDriveConnectionState CreateSignedOut() =>
        new()
        {
            Status = OneDriveConnectionStatus.SignedOut,
            AccountLabel = string.Empty,
        };

    public static OneDriveConnectionState CreateConnected(string accountLabel) =>
        new()
        {
            Status = OneDriveConnectionStatus.Connected,
            AccountLabel = accountLabel,
        };

    public static OneDriveConnectionState CreateReauthenticationRequired(string accountLabel) =>
        new()
        {
            Status = OneDriveConnectionStatus.ReauthenticationRequired,
            AccountLabel = accountLabel,
        };
}
