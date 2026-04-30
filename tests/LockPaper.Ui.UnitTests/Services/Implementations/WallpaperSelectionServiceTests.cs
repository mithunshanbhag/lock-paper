using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Services.Interfaces;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public class WallpaperSelectionServiceTests
{
    #region PositiveCases

    [Fact]
    public void SelectBestPhoto_WhenMatchingOrientationPhotosExist_ShouldRandomizeWithinThatPool()
    {
        var service = new WallpaperSelectionService(new FakeRandomizer(1));
        var display = new DeviceDisplayInfo
        {
            PixelWidth = 1080,
            PixelHeight = 1920,
            ApproximateDiagonalInches = 6.7d,
            IsPrimary = true,
        };
        OneDriveWallpaperPhoto[] photos =
        [
            new OneDriveWallpaperPhoto
            {
                Id = "wide",
                Name = "wide.jpg",
                PixelWidth = 3840,
                PixelHeight = 2160,
            },
            new OneDriveWallpaperPhoto
            {
                Id = "close-portrait",
                Name = "close-portrait.jpg",
                PixelWidth = 1440,
                PixelHeight = 2560,
            },
            new OneDriveWallpaperPhoto
            {
                Id = "far-portrait",
                Name = "far-portrait.jpg",
                PixelWidth = 3000,
                PixelHeight = 5000,
            },
        ];

        var selectedPhoto = service.SelectBestPhoto(photos, display);

        Assert.NotNull(selectedPhoto);
        Assert.Equal("far-portrait.jpg", selectedPhoto!.Name);
    }

    [Fact]
    public void SelectBestPhoto_WhenNoPhotoMatchesOrientation_ShouldFallbackToRandomPhoto()
    {
        var service = new WallpaperSelectionService(new FakeRandomizer(1));
        var display = new DeviceDisplayInfo
        {
            PixelWidth = 1920,
            PixelHeight = 1080,
            ApproximateDiagonalInches = 24d,
            IsPrimary = true,
        };
        OneDriveWallpaperPhoto[] photos =
        [
            new OneDriveWallpaperPhoto
            {
                Id = "portrait-1",
                Name = "portrait-1.jpg",
                PixelWidth = 1080,
                PixelHeight = 1920,
            },
            new OneDriveWallpaperPhoto
            {
                Id = "portrait-2",
                Name = "portrait-2.jpg",
                PixelWidth = 1440,
                PixelHeight = 2560,
            },
        ];

        var selectedPhoto = service.SelectBestPhoto(photos, display);

        Assert.NotNull(selectedPhoto);
        Assert.Equal("portrait-2.jpg", selectedPhoto!.Name);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Fact]
    public void SelectBestPhoto_WhenNoCandidatesExist_ShouldReturnNull()
    {
        var service = new WallpaperSelectionService(new FakeRandomizer());
        var display = new DeviceDisplayInfo
        {
            PixelWidth = 1920,
            PixelHeight = 1080,
            ApproximateDiagonalInches = 24d,
            IsPrimary = true,
        };

        var selectedPhoto = service.SelectBestPhoto([], display);

        Assert.Null(selectedPhoto);
    }

    #endregion

    private sealed class FakeRandomizer(int nextValue = 0) : IRandomizer
    {
        public int Next(int maxExclusive)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxExclusive);
            return nextValue >= maxExclusive
                ? throw new InvalidOperationException("The fake randomizer was asked for an index outside the configured range.")
                : nextValue;
        }
    }
}
