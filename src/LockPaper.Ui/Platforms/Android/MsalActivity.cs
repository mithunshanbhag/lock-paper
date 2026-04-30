using Android.App;
using Android.Content;
using Android.Content.PM;
using LockPaper.Ui.Constants;
using Microsoft.Identity.Client;

namespace LockPaper.Ui;

[Activity(
    Exported = true,
    LaunchMode = LaunchMode.SingleTask,
    NoHistory = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = OneDriveAuthenticationConstants.AndroidRedirectUriScheme,
    DataHost = OneDriveAuthenticationConstants.AndroidRedirectUriHost)]
public class MsalActivity : BrowserTabActivity
{
}
