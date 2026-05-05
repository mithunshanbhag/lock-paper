#if ANDROID
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace LockPaper.Ui.Misc.Permissions;

public sealed class AndroidWallpaperPreviewPermission : MauiPermissions.BasePlatformPermission
{
    public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? [(Android.Manifest.Permission.ReadMediaImages, true)]
            : [(Android.Manifest.Permission.ReadExternalStorage, true)];
}
#endif
