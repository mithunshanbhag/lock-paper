using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Loads the OneDrive albums that LockPaper can use as wallpaper sources.
/// </summary>
public interface IOneDriveAlbumDiscoveryService
{
    /// <summary>
    /// Finds the albums whose names match LockPaper's supported wallpaper album names.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The discovery result, including whether any matching albums were found.</returns>
    Task<OneDriveAlbumDiscoveryResult> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default);
}
