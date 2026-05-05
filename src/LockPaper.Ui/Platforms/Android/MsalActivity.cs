using Android.App;
using Android.Content;
using Android.Content.PM;
using LockPaper.Ui.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        GetLogger()?.LogInformation("MsalActivity created for the Android browser sign-in callback.");
    }

    protected override void OnResume()
    {
        base.OnResume();
        GetLogger()?.LogInformation("MsalActivity resumed for the Android browser sign-in callback.");
    }

    private static ILogger? GetLogger() =>
        IPlatformApplication.Current?.Services
            ?.GetService<ILoggerFactory>()
            ?.CreateLogger<MsalActivity>();
}
