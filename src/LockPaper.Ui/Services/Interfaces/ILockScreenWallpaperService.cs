namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Applies a locally cached image file to the current device's lock screen.
/// </summary>
public interface ILockScreenWallpaperService
{
    /// <summary>
    /// Resolves a local file path for the current lock-screen wallpaper preview, if one is available.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>
    /// A local file path that the UI can bind to for a wallpaper thumbnail, or <see langword="null" />
    /// when the current lock-screen wallpaper cannot be resolved.
    /// </returns>
    Task<string?> GetCurrentWallpaperPreviewFilePathAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies the specified image file as the lock-screen wallpaper.
    /// </summary>
    /// <param name="localFilePath">The local image file path.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>A task that completes when the wallpaper change finishes.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="localFilePath"/> is blank.</exception>
    /// <exception cref="IOException">Thrown when the local image file cannot be read.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the platform rejects the lock-screen image.</exception>
    /// <exception cref="PlatformNotSupportedException">Thrown when the current platform cannot apply lock-screen wallpapers.</exception>
    Task ApplyAsync(string localFilePath, CancellationToken cancellationToken = default);
}
