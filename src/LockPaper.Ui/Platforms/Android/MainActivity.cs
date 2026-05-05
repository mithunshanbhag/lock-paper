using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace LockPaper.Ui
{
    [Activity(Theme = "@style/LockPaper.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Microsoft.Maui.ApplicationModel.Platform.Init(this, savedInstanceState);
            GetLogger()?.LogInformation("MainActivity created.");
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            GetLogger()?.LogInformation(
                "MainActivity received activity result. Request code: {RequestCode}. Result code: {ResultCode}. Data present: {HasData}.",
                requestCode,
                resultCode,
                data is not null);
            AuthenticationContinuationHelper.SetAuthenticationContinuationEventArgs(requestCode, resultCode, data);
        }

        protected override void OnResume()
        {
            base.OnResume();
            GetLogger()?.LogInformation("MainActivity resumed.");
        }

        private static ILogger? GetLogger() =>
            IPlatformApplication.Current?.Services
                ?.GetService<ILoggerFactory>()
                ?.CreateLogger<MainActivity>();
    }
}
