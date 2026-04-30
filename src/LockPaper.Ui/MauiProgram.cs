using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Constants;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
            });

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddDebug();
        builder.Services.AddLogging(configure => configure.AddDebug());
#endif

        builder.Services.AddSingleton<IDeviceDisplayService, DeviceDisplayService>();
        builder.Services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
        builder.Services.AddSingleton<IOneDriveAuthenticationService, OneDriveAuthenticationService>();
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(OneDriveAlbumDiscoveryConstants.GraphBaseAddress),
        });
        builder.Services.AddSingleton<IOneDriveAlbumDiscoveryService, OneDriveAlbumDiscoveryService>();
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
