using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Constants;
using LockPaper.Ui.Misc.Telemetry;
using LockPaper.Ui.Misc.Utilities;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace LockPaper.Ui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.Configuration.AddConfiguration(
            AppSettingsConfigurationLoader.LoadEmbeddedJsonConfiguration(typeof(MauiProgram).Assembly, "appsettings.json"));

        var appInsightsConfig = builder.Configuration
            .GetRequiredSection(ConfigKeys.ApplicationInsightsSection)
            .Get<ApplicationInsightsConfig>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{ConfigKeys.ApplicationInsightsSection}' is missing or invalid.");

        if (string.IsNullOrWhiteSpace(appInsightsConfig.ConnectionString))
        {
            throw new InvalidOperationException(
                $"Configuration value '{ConfigKeys.ApplicationInsightsConnectionString}' must be set.");
        }

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
            });

        builder.Services.Configure<ApplicationInsightsConfig>(options =>
        {
            options.ConnectionString = appInsightsConfig.ConnectionString;
        });
        builder.Services.AddSingleton<ITelemetryInitializer, LockPaperTelemetryInitializer>();

        builder.Services.AddApplicationInsightsTelemetryWorkerService(options =>
        {
            options.ConnectionString = appInsightsConfig.ConnectionString;
        });

        builder.Logging.ClearProviders();
        builder.Logging.AddApplicationInsights(
            configureTelemetryConfiguration: telemetryConfiguration =>
            {
                telemetryConfiguration.ConnectionString = appInsightsConfig.ConnectionString;
            },
            configureApplicationInsightsLoggerOptions: _ => { });

#if DEBUG
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
        builder.Logging.AddDebug();
        builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Logging.AddFilter<ApplicationInsightsLoggerProvider>(string.Empty, LogLevel.Information);
#endif

        builder.Services.AddSingleton<IDeviceDisplayService, DeviceDisplayService>();
        builder.Services.AddSingleton<IUiDispatcher, MauiUiDispatcher>();
        builder.Services.AddSingleton<IRandomizer, SystemRandomizer>();
        builder.Services.AddSingleton<IOneDriveTokenCacheStore, SecureOneDriveTokenCacheStore>();
        builder.Services.AddSingleton<IOneDriveAuthenticationService, OneDriveAuthenticationService>();
        builder.Services.AddSingleton(new HttpClient
        {
            BaseAddress = new Uri(OneDriveAlbumDiscoveryConstants.GraphBaseAddress),
        });
        builder.Services.AddSingleton<IOneDriveWallpaperSourceService, OneDriveWallpaperSourceService>();
        builder.Services.AddSingleton<IOneDriveAlbumDiscoveryService, OneDriveAlbumDiscoveryService>();
        builder.Services.AddSingleton<IWallpaperSelectionService, WallpaperSelectionService>();
        builder.Services.AddSingleton<ILockScreenWallpaperService, LockScreenWallpaperService>();
        builder.Services.AddSingleton<IPlatformPermissionService, PlatformPermissionService>();
        builder.Services.AddSingleton<IWallpaperRefreshService, WallpaperRefreshService>();
        builder.Services.AddSingleton<MainPageModel>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }
}
