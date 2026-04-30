using Android.App;
using Android.Content;
using LockPaper.Ui.Constants;
using Microsoft.Identity.Client;

namespace LockPaper.Ui;

[Activity(Exported = true, NoHistory = true)]
[IntentFilter(
    [Intent.ActionView],
    Categories = [Intent.CategoryDefault, Intent.CategoryBrowsable],
    DataScheme = OneDriveAuthenticationConstants.AndroidRedirectUriScheme,
    DataHost = OneDriveAuthenticationConstants.AndroidRedirectUriHost)]
public class MsalActivity : BrowserTabActivity
{
}
