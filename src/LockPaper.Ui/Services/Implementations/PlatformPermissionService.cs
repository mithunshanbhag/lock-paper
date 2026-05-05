using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.ApplicationModel;
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;
#if ANDROID
using LockPaper.Ui.Misc.Permissions;
#endif

namespace LockPaper.Ui.Services.Implementations;

public sealed class PlatformPermissionService(ILogger<PlatformPermissionService> logger) : IPlatformPermissionService
{
    public async Task<PlatformPermissionRequestResult> RequestPostConnectionPermissionsAsync(
        CancellationToken cancellationToken = default)
    {
#if ANDROID
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentStatus = await MauiPermissions.CheckStatusAsync<AndroidWallpaperPreviewPermission>();
            logger.LogInformation(
                "Checked Android wallpaper preview permission. Current status: {PermissionStatus}.",
                currentStatus);

            if (currentStatus == PermissionStatus.Granted)
            {
                return PlatformPermissionRequestResult.NotRequired();
            }

            cancellationToken.ThrowIfCancellationRequested();

            var requestedStatus = await MauiPermissions.RequestAsync<AndroidWallpaperPreviewPermission>();
            logger.LogInformation(
                "Requested Android wallpaper preview permission. Result: {PermissionStatus}.",
                requestedStatus);

            return requestedStatus == PermissionStatus.Granted
                ? PlatformPermissionRequestResult.Granted(shouldRefreshDisplaySummary: true)
                : PlatformPermissionRequestResult.Denied(
                    "Allow photo access so LockPaper can read the current Android lock-screen preview. Until then, the display summary will keep the solid-color fallback.");
        }
        catch (PermissionException exception)
        {
            logger.LogWarning(exception, "Requesting Android wallpaper preview permission failed.");
            return PlatformPermissionRequestResult.Failed(
                "LockPaper couldn't request Android photo access. The display summary will keep the solid-color fallback until that access is available.");
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "Requesting Android wallpaper preview permission could not be completed.");
            return PlatformPermissionRequestResult.Failed(
                "LockPaper couldn't request Android photo access. The display summary will keep the solid-color fallback until that access is available.");
        }
#elif WINDOWS
        logger.LogInformation("Windows does not require a runtime permission prompt for LockPaper.");
        return PlatformPermissionRequestResult.NotRequired();
#else
        logger.LogInformation("This platform does not require a runtime permission prompt for LockPaper.");
        return PlatformPermissionRequestResult.NotRequired();
#endif
    }
}
