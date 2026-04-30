namespace LockPaper.Ui.Constants;

public static class OneDriveAlbumDiscoveryConstants
{
    public const string GraphBaseAddress = "https://graph.microsoft.com/v1.0/";
    public const string AlbumsRequestUri = "me/drive/bundles?$filter=album ne null&$select=id,name,album";

    public static readonly string[] MatchingAlbumNames =
    [
        "lockpaper",
        "lock-paper",
        "lock paper",
    ];
}
