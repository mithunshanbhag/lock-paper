using LockPaper.Ui.Services.Implementations;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public sealed class OneDriveWallpaperSourceServiceTests
{
    #region PositiveCases

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.bmp")]
    [InlineData("photo.JPEG")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreRequired_ShouldAllowSupportedExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: true);

        Assert.True(isSupported);
    }

    [Theory]
    [InlineData("photo.heic")]
    [InlineData("photo.heif")]
    [InlineData("photo.webp")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreNotRequired_ShouldAllowOtherImageExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: false);

        Assert.True(isSupported);
    }

    #endregion

    #region NegativeCases

    [Theory]
    [InlineData("photo.heic")]
    [InlineData("photo.heif")]
    [InlineData("photo.webp")]
    [InlineData("photo.gif")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreRequired_ShouldRejectUnsupportedExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: true);

        Assert.False(isSupported);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Theory]
    [InlineData("")]
    [InlineData("photo")]
    [InlineData("photo.")]
    public void IsSupportedWallpaperFileName_WhenFileNameDoesNotContainUsableExtension_ShouldRejectIt(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: false);

        Assert.False(isSupported);
    }

    #endregion
}
