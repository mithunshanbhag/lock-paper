namespace LockPaper.Ui.Models;

internal sealed class DisplaySnapshot
{
    public required int PixelWidth { get; init; }

    public required int PixelHeight { get; init; }

    public required int PositionX { get; init; }

    public required int PositionY { get; init; }

    public required double? PixelsPerInch { get; init; }

    public required bool IsPrimary { get; init; }
}
