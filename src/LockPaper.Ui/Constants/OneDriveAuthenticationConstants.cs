namespace LockPaper.Ui.Constants;

public static class OneDriveAuthenticationConstants
{
    public const string ClientId = "ab40323b-cea7-401f-ac37-de0bdf27ee9f";
    public const string Authority = "https://login.microsoftonline.com/consumers";
    public const string WindowsRedirectUri = "http://localhost";
    public const string AndroidRedirectUriScheme = "msal" + ClientId;
    public const string AndroidRedirectUriHost = "auth";
    public const string AndroidRedirectUri = AndroidRedirectUriScheme + "://" + AndroidRedirectUriHost;

    public static readonly string[] Scopes =
    [
        "Files.Read",
    ];
}
