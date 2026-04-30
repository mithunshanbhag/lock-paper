using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Coordinates the manual lock-screen wallpaper refresh flow.
/// </summary>
public interface IWallpaperRefreshService
{
    /// <summary>
    /// Refreshes the current device's lock-screen wallpaper from a matching OneDrive album.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The outcome of the refresh attempt.</returns>
    Task<WallpaperRefreshResult> RefreshAsync(CancellationToken cancellationToken = default);
}
