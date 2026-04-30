using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LockPaper.Ui.Services.Implementations;

public sealed class LockScreenWallpaperService(ILogger<LockScreenWallpaperService> logger) : ILockScreenWallpaperService
{
    public async Task ApplyAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        if (!File.Exists(localFilePath))
        {
            throw new FileNotFoundException("LockPaper couldn't find the downloaded wallpaper image.", localFilePath);
        }

#if ANDROID
        logger.LogInformation("Applying lock-screen wallpaper from {LocalFilePath} on Android.", localFilePath);
        await ApplyAndroidAsync(localFilePath).ConfigureAwait(false);
#elif WINDOWS
        logger.LogInformation("Applying lock-screen wallpaper from {LocalFilePath} on Windows.", localFilePath);
        await ApplyWindowsAsync(localFilePath, cancellationToken).ConfigureAwait(false);
#else
        throw new PlatformNotSupportedException("LockPaper can only change the lock-screen wallpaper on Android and Windows.");
#endif
    }

#if ANDROID
    private static Task ApplyAndroidAsync(string localFilePath)
    {
        var context = Android.App.Application.Context;
        if (context is null)
        {
            throw new InvalidOperationException("LockPaper needs an active Android application context before it can change the lock-screen wallpaper.");
        }

        var wallpaperManager = Android.App.WallpaperManager.GetInstance(context);

        using var stream = File.OpenRead(localFilePath);
        wallpaperManager.SetStream(stream, null, true, Android.App.WallpaperManagerFlags.Lock);
        return Task.CompletedTask;
    }
#endif

#if WINDOWS
    private static async Task ApplyWindowsAsync(string localFilePath, CancellationToken cancellationToken)
    {
        if (!Windows.System.UserProfile.UserProfilePersonalizationSettings.IsSupported())
        {
            throw new InvalidOperationException(
                "Windows lock-screen wallpaper changes require the packaged LockPaper app with personalization access.");
        }

        var file = await Windows.Storage.StorageFile
            .GetFileFromPathAsync(localFilePath)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);
        var personalizationSettings = Windows.System.UserProfile.UserProfilePersonalizationSettings.Current;
        var wasApplied = await personalizationSettings
            .TrySetLockScreenImageAsync(file)
            .AsTask(cancellationToken)
            .ConfigureAwait(false);

        if (!wasApplied)
        {
            throw new InvalidOperationException(
                "Windows rejected the lock-screen image. Make sure the packaged app can personalize the lock screen.");
        }
    }
#endif
}
