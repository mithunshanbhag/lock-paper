using LockPaper.Ui.Misc.Utilities;
using LockPaper.Ui.Misc.Telemetry;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
#if WINDOWS
using System.Runtime.InteropServices.WindowsRuntime;
#endif

namespace LockPaper.Ui.Services.Implementations;

public sealed class LockScreenWallpaperService(ILogger<LockScreenWallpaperService> logger) : ILockScreenWallpaperService
{
    private const string PersistedWallpaperPathFileName = "current-lockscreen-wallpaper.txt";

    public async Task ApplyAsync(string localFilePath, CancellationToken cancellationToken = default)
    {
        var checkpoint = PerformanceCheckpoint.StartNew("LockScreenWallpaper.ApplyAsync");
        var outcome = "Succeeded";

        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        try
        {
            if (!File.Exists(localFilePath))
            {
                outcome = "FileNotFound";
                throw new FileNotFoundException("LockPaper couldn't find the downloaded wallpaper image.", localFilePath);
            }

#if ANDROID
            logger.LogInformation("Applying lock-screen wallpaper from {LocalFilePath} on Android.", localFilePath);
            ApplyAndroid(localFilePath);
#elif WINDOWS
            logger.LogInformation("Applying lock-screen wallpaper from {LocalFilePath} on Windows.", localFilePath);
            await ApplyWindowsAsync(localFilePath, cancellationToken).ConfigureAwait(false);
#else
            outcome = "PlatformNotSupported";
            throw new PlatformNotSupportedException("LockPaper can only change the lock-screen wallpaper on Android and Windows.");
#endif

            await PersistAppliedWallpaperFilePathAsync(localFilePath, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelled";
            throw;
        }
        catch (Exception) when (outcome == "Succeeded")
        {
            outcome = "Failed";
            throw;
        }
        finally
        {
            checkpoint.LogCompleted(logger, outcome);
        }
    }

    public async Task<string?> GetCurrentWallpaperPreviewFilePathAsync(CancellationToken cancellationToken = default)
    {
#if ANDROID
        string? previewFilePath;
        try
        {
            previewFilePath = await GetAndroidCurrentWallpaperPreviewFilePathAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            LogAndroidWallpaperPreviewFailure(exception);
            previewFilePath = null;
        }
#elif WINDOWS
        var previewFilePath = await GetWindowsCurrentWallpaperPreviewFilePathAsync(cancellationToken).ConfigureAwait(false);
#else
        var previewFilePath = default(string);
#endif

        if (!string.IsNullOrWhiteSpace(previewFilePath))
        {
            logger.LogInformation("Resolved the current lock-screen wallpaper preview from the platform.");
            return previewFilePath;
        }

        var persistedWallpaperFilePath = await TryGetPersistedWallpaperFilePathAsync(cancellationToken).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(persistedWallpaperFilePath))
        {
            logger.LogInformation("Falling back to the persisted lock-screen wallpaper path for the display preview.");
        }
        else
        {
            logger.LogInformation("No current lock-screen wallpaper preview was available from the platform or persisted state.");
        }

        return persistedWallpaperFilePath;
    }

#if ANDROID
    private void ApplyAndroid(string localFilePath)
    {
        var context = Android.App.Application.Context;
        if (context is null)
        {
            throw new InvalidOperationException("LockPaper needs an active Android application context before it can change the lock-screen wallpaper.");
        }

        var wallpaperManager = Android.App.WallpaperManager.GetInstance(context)
            ?? throw new InvalidOperationException("LockPaper couldn't access the Android wallpaper manager.");

        var wallpaperTarget = AndroidWallpaperImageUtility.GetPortraitLockScreenTarget(context, wallpaperManager);
        logger.LogInformation(
            "Preparing Android lock-screen wallpaper to fit portrait target {TargetWidth}x{TargetHeight}.",
            wallpaperTarget.Width,
            wallpaperTarget.Height);
        AndroidWallpaperImageUtility.PrepareWallpaperFile(
            localFilePath,
            wallpaperTarget.Width,
            wallpaperTarget.Height);

        using var stream = File.OpenRead(localFilePath);
        wallpaperManager.SetStream(stream, null, true, Android.App.WallpaperManagerFlags.Lock);
    }

    private static async Task<string?> GetAndroidCurrentWallpaperPreviewFilePathAsync(CancellationToken cancellationToken)
    {
        var context = Android.App.Application.Context;
        if (context is null)
        {
            throw new InvalidOperationException("LockPaper needs an active Android application context before it can read the current lock-screen wallpaper.");
        }

        var wallpaperManager = Android.App.WallpaperManager.GetInstance(context)
            ?? throw new InvalidOperationException("LockPaper couldn't access the Android wallpaper manager.");
        using var lockWallpaperFile = wallpaperManager.GetWallpaperFile(Android.App.WallpaperManagerFlags.Lock);
        using var systemWallpaperFile = lockWallpaperFile is null
            ? wallpaperManager.GetWallpaperFile(Android.App.WallpaperManagerFlags.System)
            : null;
        var activeWallpaperFile = lockWallpaperFile ?? systemWallpaperFile;
        if (activeWallpaperFile is null)
        {
            return null;
        }

        using var inputStream = new Android.OS.ParcelFileDescriptor.AutoCloseInputStream(activeWallpaperFile);
        return await SaveCurrentWallpaperPreviewAsync(inputStream, cancellationToken).ConfigureAwait(false);
    }

    private void LogAndroidWallpaperPreviewFailure(Exception exception)
    {
        var sdkInt = (int)Android.OS.Build.VERSION.SdkInt;
        var applicationContext = Android.App.Application.Context;
        bool? hasReadMediaImagesPermission = sdkInt >= 33 && applicationContext is not null
            ? HasAndroidPermission(applicationContext, Android.Manifest.Permission.ReadMediaImages)
            : null;
        bool? hasReadExternalStoragePermission = applicationContext is not null
            ? HasAndroidPermission(applicationContext, Android.Manifest.Permission.ReadExternalStorage)
            : null;

        logger.LogWarning(
            exception,
            "Reading the current Android lock-screen wallpaper preview from WallpaperManager failed. Android SDK: {SdkInt}. READ_MEDIA_IMAGES granted: {HasReadMediaImagesPermission}. READ_EXTERNAL_STORAGE granted: {HasReadExternalStoragePermission}. LockPaper will fall back to the last persisted wallpaper preview when possible.",
            sdkInt,
            hasReadMediaImagesPermission,
            hasReadExternalStoragePermission);
    }

    private static bool HasAndroidPermission(Android.Content.Context context, string permission)
    {
        return context.CheckSelfPermission(permission) == Android.Content.PM.Permission.Granted;
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
                "Windows rejected the lock-screen image. Make sure the packaged app can personalize the lock screen and that the photo uses JPG, JPEG, PNG, or BMP.");
        }
    }

    private static async Task<string?> GetWindowsCurrentWallpaperPreviewFilePathAsync(CancellationToken cancellationToken)
    {
        var imageStream = Windows.System.UserProfile.LockScreen.GetImageStream();
        if (imageStream is null)
        {
            return null;
        }

        using var previewStream = imageStream.AsStreamForRead();
        return await SaveCurrentWallpaperPreviewAsync(previewStream, cancellationToken).ConfigureAwait(false);
    }
#endif

    private static async Task PersistAppliedWallpaperFilePathAsync(string localFilePath, CancellationToken cancellationToken)
    {
        var wallpaperStateDirectory = GetWallpaperStateDirectory();
        Directory.CreateDirectory(wallpaperStateDirectory);

        await File
            .WriteAllTextAsync(GetPersistedWallpaperPathFilePath(), localFilePath, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<string?> TryGetPersistedWallpaperFilePathAsync(CancellationToken cancellationToken)
    {
        var persistedWallpaperPathFilePath = GetPersistedWallpaperPathFilePath();
        if (!File.Exists(persistedWallpaperPathFilePath))
        {
            return null;
        }

        var persistedWallpaperPath = (await File.ReadAllTextAsync(persistedWallpaperPathFilePath, cancellationToken).ConfigureAwait(false)).Trim();
        if (string.IsNullOrWhiteSpace(persistedWallpaperPath))
        {
            return null;
        }

        if (File.Exists(persistedWallpaperPath))
        {
            return persistedWallpaperPath;
        }

        File.Delete(persistedWallpaperPathFilePath);
        return null;
    }

#if ANDROID
    private static async Task<string?> SaveCurrentWallpaperPreviewAsync(Java.IO.InputStream sourceStream, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        var readBuffer = new byte[81920];

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var bytesRead = sourceStream.Read(readBuffer, 0, readBuffer.Length);
            if (bytesRead <= 0)
            {
                break;
            }

            await buffer.WriteAsync(readBuffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
        }

        return await SaveCurrentWallpaperPreviewAsync(buffer.ToArray(), cancellationToken).ConfigureAwait(false);
    }
#endif

    private static async Task<string?> SaveCurrentWallpaperPreviewAsync(Stream sourceStream, CancellationToken cancellationToken)
    {
        await using var buffer = new MemoryStream();
        if (sourceStream.CanSeek)
        {
            sourceStream.Position = 0;
        }

        await sourceStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return await SaveCurrentWallpaperPreviewAsync(buffer.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<string?> SaveCurrentWallpaperPreviewAsync(byte[] imageBytes, CancellationToken cancellationToken)
    {
        if (imageBytes.Length == 0)
        {
            return null;
        }

        var wallpaperStateDirectory = GetWallpaperStateDirectory();
        Directory.CreateDirectory(wallpaperStateDirectory);

        foreach (var existingPreviewFilePath in Directory.EnumerateFiles(wallpaperStateDirectory, "current-lockscreen-preview.*"))
        {
            File.Delete(existingPreviewFilePath);
        }

        var previewFilePath = Path.Combine(
            wallpaperStateDirectory,
            $"current-lockscreen-preview{GetImageFileExtension(imageBytes)}");

        await File.WriteAllBytesAsync(previewFilePath, imageBytes, cancellationToken).ConfigureAwait(false);
        return previewFilePath;
    }

    private static string GetImageFileExtension(ReadOnlySpan<byte> imageBytes)
    {
        if (imageBytes.Length >= 8
            && imageBytes[0] == 0x89
            && imageBytes[1] == 0x50
            && imageBytes[2] == 0x4E
            && imageBytes[3] == 0x47
            && imageBytes[4] == 0x0D
            && imageBytes[5] == 0x0A
            && imageBytes[6] == 0x1A
            && imageBytes[7] == 0x0A)
        {
            return ".png";
        }

        if (imageBytes.Length >= 3
            && imageBytes[0] == 0xFF
            && imageBytes[1] == 0xD8
            && imageBytes[2] == 0xFF)
        {
            return ".jpg";
        }

        if (imageBytes.Length >= 2
            && imageBytes[0] == 0x42
            && imageBytes[1] == 0x4D)
        {
            return ".bmp";
        }

        if (imageBytes.Length >= 6
            && imageBytes[0] == 0x47
            && imageBytes[1] == 0x49
            && imageBytes[2] == 0x46
            && imageBytes[3] == 0x38
            && (imageBytes[4] == 0x37 || imageBytes[4] == 0x39)
            && imageBytes[5] == 0x61)
        {
            return ".gif";
        }

        if (imageBytes.Length >= 12
            && imageBytes[0] == 0x52
            && imageBytes[1] == 0x49
            && imageBytes[2] == 0x46
            && imageBytes[3] == 0x46
            && imageBytes[8] == 0x57
            && imageBytes[9] == 0x45
            && imageBytes[10] == 0x42
            && imageBytes[11] == 0x50)
        {
            return ".webp";
        }

        return ".jpg";
    }

    private static string GetPersistedWallpaperPathFilePath() =>
        Path.Combine(GetWallpaperStateDirectory(), PersistedWallpaperPathFileName);

    private static string GetWallpaperStateDirectory()
    {
#if WINDOWS
        return Path.Combine(
            Windows.Storage.ApplicationData.Current.LocalFolder.Path,
            "WallpaperState");
#else
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LockPaper",
            "WallpaperState");
#endif
    }
}
