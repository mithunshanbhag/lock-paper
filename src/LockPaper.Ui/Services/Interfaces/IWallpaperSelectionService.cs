using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Chooses a random photo for a specific display target while preferring the matching orientation.
/// </summary>
public interface IWallpaperSelectionService
{
    /// <summary>
    /// Selects a random photo for the specified display, preferring matching-orientation photos before falling back to any photo.
    /// </summary>
    /// <param name="photos">The candidate photos.</param>
    /// <param name="display">The display target that the photo should fit.</param>
    /// <returns>The selected photo, or <see langword="null"/> when no candidates are available.</returns>
    OneDriveWallpaperPhoto? SelectBestPhoto(IReadOnlyList<OneDriveWallpaperPhoto> photos, DeviceDisplayInfo display);
}
