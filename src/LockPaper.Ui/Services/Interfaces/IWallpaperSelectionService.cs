using LockPaper.Ui.Models;

namespace LockPaper.Ui.Services.Interfaces;

/// <summary>
/// Chooses a random photo for a specific display target while respecting the platform's orientation-selection strategy.
/// </summary>
public interface IWallpaperSelectionService
{
    /// <summary>
    /// Selects a random photo for the specified display. Platforms that validate orientation again after download may choose
    /// from the full candidate set so EXIF-correct photos are not starved by pre-download metadata.
    /// </summary>
    /// <param name="photos">The candidate photos.</param>
    /// <param name="display">The display target that the photo should fit.</param>
    /// <returns>The selected photo, or <see langword="null"/> when no candidates are available.</returns>
    OneDriveWallpaperPhoto? SelectBestPhoto(IReadOnlyList<OneDriveWallpaperPhoto> photos, DeviceDisplayInfo display);
}
