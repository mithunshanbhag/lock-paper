using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Loads matching OneDrive wallpaper albums, their image metadata, and the image content needed for wallpaper refreshes.
/// </summary>
public interface IOneDriveWallpaperSourceService
{
    /// <summary>
    /// Finds the albums whose names match LockPaper's supported wallpaper album names.
    /// </summary>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The matching albums, if any.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the current device does not have a usable signed-in OneDrive session.</exception>
    /// <exception cref="HttpRequestException">Thrown when Microsoft Graph rejects or fails the request.</exception>
    /// <exception cref="JsonException">Thrown when Microsoft Graph returns an unexpected payload.</exception>
    Task<IReadOnlyList<OneDriveWallpaperAlbum>> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the usable image items in a specific OneDrive album.
    /// </summary>
    /// <param name="albumId">The OneDrive album identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The image items that include metadata needed for wallpaper selection.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="albumId"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current device does not have a usable signed-in OneDrive session.</exception>
    /// <exception cref="HttpRequestException">Thrown when Microsoft Graph rejects or fails the request.</exception>
    /// <exception cref="JsonException">Thrown when Microsoft Graph returns an unexpected payload.</exception>
    Task<IReadOnlyList<OneDriveWallpaperPhoto>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads the bytes for a specific OneDrive image item.
    /// </summary>
    /// <param name="photoId">The OneDrive image item identifier.</param>
    /// <param name="cancellationToken">A token that can cancel the operation.</param>
    /// <returns>The downloaded image content.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="photoId"/> is blank.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the current device does not have a usable signed-in OneDrive session.</exception>
    /// <exception cref="HttpRequestException">Thrown when Microsoft Graph rejects or fails the request.</exception>
    Task<byte[]> DownloadPhotoBytesAsync(string photoId, CancellationToken cancellationToken = default);
}
