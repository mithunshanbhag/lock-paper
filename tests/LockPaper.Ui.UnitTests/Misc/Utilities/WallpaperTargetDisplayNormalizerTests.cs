using LockPaper.Ui.Misc.Utilities;
using LockPaper.Ui.Models;

namespace LockPaper.Ui.UnitTests.Misc.Utilities;

public sealed class WallpaperTargetDisplayNormalizerTests
{
    #region PositiveCases

    [Fact]
    public void NormalizeForPortraitLockScreen_WhenDisplayIsLandscape_ShouldSwapDimensions()
    {
        var display = new DeviceDisplayInfo
        {
            PixelWidth = 2400,
            PixelHeight = 1080,
            ApproximateDiagonalInches = 6.7d,
            IsPrimary = true,
        };

        var normalizedDisplay = WallpaperTargetDisplayNormalizer.NormalizeForPortraitLockScreen(display);

        Assert.Equal(1080, normalizedDisplay.PixelWidth);
        Assert.Equal(2400, normalizedDisplay.PixelHeight);
        Assert.Equal(6.7d, normalizedDisplay.ApproximateDiagonalInches);
        Assert.True(normalizedDisplay.IsPrimary);
    }

    [Fact]
    public void NormalizeForPortraitLockScreen_WhenDisplayIsAlreadyPortrait_ShouldKeepDimensions()
    {
        var display = new DeviceDisplayInfo
        {
            PixelWidth = 1080,
            PixelHeight = 2400,
            ApproximateDiagonalInches = 6.7d,
            IsPrimary = true,
        };

        var normalizedDisplay = WallpaperTargetDisplayNormalizer.NormalizeForPortraitLockScreen(display);

        Assert.Equal(1080, normalizedDisplay.PixelWidth);
        Assert.Equal(2400, normalizedDisplay.PixelHeight);
    }

    #endregion
}
