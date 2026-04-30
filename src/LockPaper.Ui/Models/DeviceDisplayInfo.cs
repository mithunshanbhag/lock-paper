namespace LockPaper.Ui.Models;

public sealed class DeviceDisplayInfo
{
    public required int PixelWidth { get; init; }

    public required int PixelHeight { get; init; }

    public required double? ApproximateDiagonalInches { get; init; }

    public required bool IsPrimary { get; init; }
}
