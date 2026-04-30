using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Services.Interfaces;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public class WallpaperSelectionServiceTests
{
    #region PositiveCases

    [Fact]
    public void SelectBestPhoto_WhenPortraitDisplayExists_ShouldPreferPortraitPhotoWithCloserFit()
    {
        var service = new WallpaperSelectionService(new FakeRandomizer());
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
        Assert.Equal("close-portrait.jpg", selectedPhoto!.Name);
    }

    [Fact]
    public void SelectBestPhoto_WhenNoPhotoCoversDisplay_ShouldPickClosestUndersizedOption()
    {
        var service = new WallpaperSelectionService(new FakeRandomizer());
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
                Id = "small-close",
                Name = "small-close.jpg",
                PixelWidth = 1600,
                PixelHeight = 900,
            },
            new OneDriveWallpaperPhoto
            {
                Id = "small-far",
                Name = "small-far.jpg",
                PixelWidth = 1200,
                PixelHeight = 1200,
            },
        ];

        var selectedPhoto = service.SelectBestPhoto(photos, display);

        Assert.NotNull(selectedPhoto);
        Assert.Equal("small-close.jpg", selectedPhoto!.Name);
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

    private sealed class FakeRandomizer : IRandomizer
    {
        public int Next(int maxExclusive) => 0;
    }
}
