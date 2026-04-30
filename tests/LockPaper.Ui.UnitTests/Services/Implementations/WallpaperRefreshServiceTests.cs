using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public sealed class WallpaperRefreshServiceTests
{
    #region PositiveCases

    [Fact]
    public async Task RefreshAsync_WhenWallpaperApplySucceeds_ShouldReturnSuccess()
    {
        var service = new WallpaperRefreshService(
            new FakeDeviceDisplayService(),
            new FakeLockScreenWallpaperService(),
            new FakeOneDriveWallpaperSourceService(),
            new FakeRandomizer(),
            new FakeWallpaperSelectionService(),
            NullLogger<WallpaperRefreshService>.Instance);

        var result = await service.RefreshAsync();

        Assert.Equal(WallpaperRefreshStatus.Succeeded, result.Status);
        Assert.Equal(1, result.MatchingAlbumCount);
        Assert.Equal("lockpaper", result.AlbumName);
        Assert.Equal("sunrise.jpg", result.PhotoName);
    }

    #endregion

    #region NegativeCases

    [Fact]
    public async Task RefreshAsync_WhenWallpaperApplyThrowsInvalidOperationException_ShouldReturnFailed()
    {
        var service = new WallpaperRefreshService(
            new FakeDeviceDisplayService(),
            new FakeLockScreenWallpaperService
            {
                ApplyException = new InvalidOperationException(
                    "Windows rejected the lock-screen image. Make sure the packaged app can personalize the lock screen."),
            },
            new FakeOneDriveWallpaperSourceService(),
            new FakeRandomizer(),
            new FakeWallpaperSelectionService(),
            NullLogger<WallpaperRefreshService>.Instance);

        var result = await service.RefreshAsync();

        Assert.Equal(WallpaperRefreshStatus.Failed, result.Status);
        Assert.Equal(1, result.MatchingAlbumCount);
        Assert.Contains("Windows rejected the lock-screen image", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RefreshAsync_WhenOneDriveSessionIsInvalid_ShouldReturnReauthenticationRequired()
    {
        var service = new WallpaperRefreshService(
            new FakeDeviceDisplayService(),
            new FakeLockScreenWallpaperService(),
            new FakeOneDriveWallpaperSourceService
            {
                MatchingAlbumsException = new InvalidOperationException(
                    "LockPaper needs you to sign in again before it can read your OneDrive albums."),
            },
            new FakeRandomizer(),
            new FakeWallpaperSelectionService(),
            NullLogger<WallpaperRefreshService>.Instance);

        var result = await service.RefreshAsync();

        Assert.Equal(WallpaperRefreshStatus.ReauthenticationRequired, result.Status);
        Assert.Contains("sign in again", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    private sealed class FakeDeviceDisplayService : IDeviceDisplayService
    {
        public IReadOnlyList<DeviceDisplayInfo> Displays { get; set; } =
        [
            new DeviceDisplayInfo
            {
                PixelWidth = 1920,
                PixelHeight = 1080,
                ApproximateDiagonalInches = 24,
                IsPrimary = true,
            },
        ];

        public IReadOnlyList<DeviceDisplayInfo> GetDisplays() => Displays;
    }

    private sealed class FakeLockScreenWallpaperService : ILockScreenWallpaperService
    {
        public Exception? ApplyException { get; set; }

        public Task ApplyAsync(string localFilePath, CancellationToken cancellationToken = default)
        {
            if (ApplyException is not null)
            {
                throw ApplyException;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeOneDriveWallpaperSourceService : IOneDriveWallpaperSourceService
    {
        public Exception? MatchingAlbumsException { get; set; }

        public IReadOnlyList<OneDriveWallpaperAlbum> MatchingAlbums { get; set; } =
        [
            new OneDriveWallpaperAlbum
            {
                Id = "album-1",
                Name = "lockpaper",
            },
        ];

        public IReadOnlyList<OneDriveWallpaperPhoto> AlbumPhotos { get; set; } =
        [
            new OneDriveWallpaperPhoto
            {
                Id = "photo-1",
                Name = "sunrise.jpg",
                PixelWidth = 1920,
                PixelHeight = 1080,
            },
        ];

        public byte[] PhotoBytes { get; set; } = [1, 2, 3, 4];

        public Task<IReadOnlyList<OneDriveWallpaperAlbum>> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default)
        {
            if (MatchingAlbumsException is not null)
            {
                throw MatchingAlbumsException;
            }

            return Task.FromResult(MatchingAlbums);
        }

        public Task<IReadOnlyList<OneDriveWallpaperPhoto>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default) =>
            Task.FromResult(AlbumPhotos);

        public Task<byte[]> DownloadPhotoBytesAsync(string photoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(PhotoBytes);
    }

    private sealed class FakeRandomizer : IRandomizer
    {
        public int Next(int maxValue) => 0;
    }

    private sealed class FakeWallpaperSelectionService : IWallpaperSelectionService
    {
        public OneDriveWallpaperPhoto? SelectBestPhoto(
            IReadOnlyList<OneDriveWallpaperPhoto> photos,
            DeviceDisplayInfo display) => photos.FirstOrDefault();
    }
}
