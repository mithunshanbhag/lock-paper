using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Coordinates the manual lock-screen wallpaper refresh flow.
/// </summary>
public interface IWallpaperRefreshService
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
    /// Refreshes the current device's lock-screen wallpaper from a matching OneDrive album.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The outcome of the refresh attempt.</returns>
    Task<WallpaperRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
}
