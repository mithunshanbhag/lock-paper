namespace LockPaper.Ui.Constants;

public static class OneDriveAlbumDiscoveryConstants
{
    public const string GraphBaseAddress = "https://graph.microsoft.com/v1.0/";
    public const int AlbumsPageSize = 200;
    public const string AlbumsRequestUri = "me/drive/bundles?$filter=bundle/album ne null&$top=200";

    public static readonly string[] MatchingAlbumNames =
    [
        "lockpaper",
        "lock-paper",
        "lock paper",
    ];
}
