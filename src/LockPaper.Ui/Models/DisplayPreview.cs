namespace LockPaper.Ui.Models;

public sealed class DisplayPreview
{
    public required string ResolutionText { get; init; }

    public required string PreviewColor { get; init; }

    public string WallpaperThumbnailPath { get; init; } = string.Empty;

    public required double PreviewWidth { get; init; }

    public required double PreviewHeight { get; init; }

    public bool ShowWallpaperThumbnail => !string.IsNullOrWhiteSpace(WallpaperThumbnailPath);
}
