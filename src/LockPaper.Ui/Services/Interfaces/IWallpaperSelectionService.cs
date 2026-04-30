using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Chooses the best-fit photo for a specific display target.
/// </summary>
public interface IWallpaperSelectionService
{
    /// <summary>
    /// Selects a random photo from the best-fit group for the specified display.
    /// </summary>
    /// <param name="photos">The candidate photos.</param>
    /// <param name="display">The display target that the photo should fit.</param>
    /// <returns>The selected photo, or <see langword="null"/> when no candidates are available.</returns>
    OneDriveWallpaperPhoto? SelectBestPhoto(IReadOnlyList<OneDriveWallpaperPhoto> photos, DeviceDisplayInfo display);
}
