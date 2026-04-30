using LockPaper.Ui.Misc.Utilities;
using LockPaper.Ui.Models;

namespace LockPaper.Ui.UnitTests.Misc.Utilities;

public class DeviceDisplayInfoFactoryTests
{
    #region PositiveCases

    [Fact]
    public void CreateOrdered_WhenSnapshotsContainPrimaryAndSecondaryDisplays_ShouldKeepPrimaryFirstAndThenOrderByPosition()
    {
        var displays = DeviceDisplayInfoFactory.CreateOrdered(
        [
            new DisplaySnapshot
            {
                PixelWidth = 1920,
                PixelHeight = 1080,
                PositionX = 1920,
                PositionY = 0,
                PixelsPerInch = 96d,
                IsPrimary = false,
            },
            new DisplaySnapshot
            {
                PixelWidth = 2560,
                PixelHeight = 1440,
                PositionX = 0,
                PositionY = 0,
                PixelsPerInch = 96d,
                IsPrimary = true,
            },
            new DisplaySnapshot
            {
                PixelWidth = 1080,
                PixelHeight = 1920,
                PositionX = 1920,
                PositionY = 1080,
                PixelsPerInch = 96d,
                IsPrimary = false,
            },
        ]);

        Assert.Equal(3, displays.Count);
        Assert.True(displays[0].IsPrimary);
        Assert.Equal(2560, displays[0].PixelWidth);
        Assert.Equal(1920, displays[1].PixelWidth);
        Assert.Equal(1080, displays[2].PixelWidth);
        Assert.NotNull(displays[0].ApproximateDiagonalInches);
        Assert.InRange(displays[0].ApproximateDiagonalInches!.Value, 30.5d, 30.7d);
    }

    #endregion

    #region NegativeCases

    [Fact]
    public void Create_WhenPixelsPerInchIsMissing_ShouldLeaveApproximateDiagonalUnset()
    {
        var display = DeviceDisplayInfoFactory.Create(
            new DisplaySnapshot
            {
                PixelWidth = 1920,
                PixelHeight = 1080,
                PositionX = 0,
                PositionY = 0,
                PixelsPerInch = null,
                IsPrimary = true,
            });

        Assert.Null(display.ApproximateDiagonalInches);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Fact]
    public void CreateOrdered_WhenSnapshotsAreEmpty_ShouldReturnEmptyCollection()
    {
        var displays = DeviceDisplayInfoFactory.CreateOrdered([]);

        Assert.Empty(displays);
    }

    #endregion
}
