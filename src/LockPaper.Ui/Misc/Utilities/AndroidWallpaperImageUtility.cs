#if ANDROID
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Media;
using System.IO;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
using AndroidRectF = Android.Graphics.RectF;

namespace LockPaper.Ui.Misc.Utilities;

internal static class AndroidWallpaperImageUtility
{
    private const int ExifOrientationNormal = 1;
    private const int ExifOrientationFlipHorizontal = 2;
    private const int ExifOrientationRotate180 = 3;
    private const int ExifOrientationFlipVertical = 4;
    private const int ExifOrientationTranspose = 5;
    private const int ExifOrientationRotate90 = 6;
    private const int ExifOrientationTransverse = 7;
    private const int ExifOrientationRotate270 = 8;

    public static AndroidWallpaperTarget GetPortraitLockScreenTarget(Context context, WallpaperManager wallpaperManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(wallpaperManager);

        var displayMetrics = context.Resources?.DisplayMetrics;
        var physicalShortSide = GetPositiveMinimum(displayMetrics?.WidthPixels ?? 0, displayMetrics?.HeightPixels ?? 0);
        var physicalLongSide = GetPositiveMaximum(displayMetrics?.WidthPixels ?? 0, displayMetrics?.HeightPixels ?? 0);
        var desiredShortSide = GetPositiveMinimum(wallpaperManager.DesiredMinimumWidth, wallpaperManager.DesiredMinimumHeight);
        var desiredLongSide = GetPositiveMaximum(wallpaperManager.DesiredMinimumWidth, wallpaperManager.DesiredMinimumHeight);

        var targetWidth = Math.Max(physicalShortSide, desiredShortSide);
        var targetHeight = Math.Max(physicalLongSide, desiredLongSide);
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            throw new InvalidOperationException("LockPaper couldn't determine the Android lock-screen size for wallpaper preparation.");
        }

        return new AndroidWallpaperTarget(targetWidth, targetHeight);
    }

    public static bool? TryMatchesTargetOrientation(byte[] imageBytes, int targetWidth, int targetHeight)
    {
        ArgumentNullException.ThrowIfNull(imageBytes);

        try
        {
            if (imageBytes.Length == 0)
            {
                return null;
            }

            var decodeBounds = new BitmapFactory.Options
            {
                InJustDecodeBounds = true,
            };
            BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length, decodeBounds);
            if (decodeBounds.OutWidth <= 0 || decodeBounds.OutHeight <= 0)
            {
                return null;
            }

            using var stream = new MemoryStream(imageBytes, writable: false);
            using var exif = new ExifInterface(stream);
            var exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, ExifOrientationNormal);
            var effectiveWidth = ShouldSwapDimensions(exifOrientation) ? decodeBounds.OutHeight : decodeBounds.OutWidth;
            var effectiveHeight = ShouldSwapDimensions(exifOrientation) ? decodeBounds.OutWidth : decodeBounds.OutHeight;

            var targetOrientation = GetOrientation(targetWidth, targetHeight);
            var photoOrientation = GetOrientation(effectiveWidth, effectiveHeight);
            return photoOrientation == WallpaperOrientation.Square || photoOrientation == targetOrientation;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static void PrepareWallpaperFile(string localFilePath, int targetWidth, int targetHeight)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localFilePath);

        var decodeBounds = new BitmapFactory.Options
        {
            InJustDecodeBounds = true,
        };
        BitmapFactory.DecodeFile(localFilePath, decodeBounds);
        if (decodeBounds.OutWidth <= 0 || decodeBounds.OutHeight <= 0)
        {
            throw new InvalidOperationException("LockPaper couldn't decode the Android wallpaper image.");
        }

        using var exif = new ExifInterface(localFilePath);
        var exifOrientation = exif.GetAttributeInt(ExifInterface.TagOrientation, ExifOrientationNormal);
        var effectiveWidth = ShouldSwapDimensions(exifOrientation) ? decodeBounds.OutHeight : decodeBounds.OutWidth;
        var effectiveHeight = ShouldSwapDimensions(exifOrientation) ? decodeBounds.OutWidth : decodeBounds.OutHeight;

        var decodeOptions = new BitmapFactory.Options
        {
            InSampleSize = CalculateInSampleSize(effectiveWidth, effectiveHeight, targetWidth, targetHeight),
        };

        using var sourceBitmap = BitmapFactory.DecodeFile(localFilePath, decodeOptions)
            ?? throw new InvalidOperationException("LockPaper couldn't load the Android wallpaper image.");
        using var orientedBitmap = CreateOrientedBitmap(sourceBitmap, exifOrientation);
        using var targetBitmap = Bitmap.CreateBitmap(targetWidth, targetHeight, Bitmap.Config.Argb8888!)
            ?? throw new InvalidOperationException("LockPaper couldn't allocate the Android lock-screen wallpaper surface.");
        using var canvas = new Canvas(targetBitmap);
        canvas.DrawColor(AndroidColor.Black);

        using var paint = new AndroidPaint(PaintFlags.AntiAlias | PaintFlags.FilterBitmap | PaintFlags.Dither);
        var scale = Math.Min((float)targetWidth / orientedBitmap.Width, (float)targetHeight / orientedBitmap.Height);
        var scaledWidth = orientedBitmap.Width * scale;
        var scaledHeight = orientedBitmap.Height * scale;
        var left = (targetWidth - scaledWidth) / 2f;
        var top = (targetHeight - scaledHeight) / 2f;
        var destinationRect = new AndroidRectF(left, top, left + scaledWidth, top + scaledHeight);
        canvas.DrawBitmap(orientedBitmap, null, destinationRect, paint);

        using var outputStream = File.Open(localFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        if (!targetBitmap.Compress(Bitmap.CompressFormat.Jpeg!, 95, outputStream))
        {
            throw new InvalidOperationException("LockPaper couldn't encode the Android lock-screen wallpaper image.");
        }
    }

    private static Bitmap CreateOrientedBitmap(Bitmap sourceBitmap, int exifOrientation)
    {
        if (exifOrientation == ExifOrientationNormal)
        {
            return sourceBitmap.Copy(Bitmap.Config.Argb8888!, false)
                ?? throw new InvalidOperationException("LockPaper couldn't copy the Android wallpaper bitmap.");
        }

        using var matrix = CreateOrientationMatrix(exifOrientation);
        return Bitmap.CreateBitmap(
                sourceBitmap,
                0,
                0,
                sourceBitmap.Width,
                sourceBitmap.Height,
                matrix,
                true)
            ?? throw new InvalidOperationException("LockPaper couldn't rotate the Android wallpaper bitmap.");
    }

    private static Matrix CreateOrientationMatrix(int exifOrientation)
    {
        var matrix = new Matrix();

        switch (exifOrientation)
        {
            case ExifOrientationFlipHorizontal:
                matrix.PreScale(-1f, 1f);
                break;
            case ExifOrientationRotate180:
                matrix.PreRotate(180f);
                break;
            case ExifOrientationFlipVertical:
                matrix.PreScale(1f, -1f);
                break;
            case ExifOrientationTranspose:
                matrix.PreRotate(90f);
                matrix.PreScale(-1f, 1f);
                break;
            case ExifOrientationRotate90:
                matrix.PreRotate(90f);
                break;
            case ExifOrientationTransverse:
                matrix.PreRotate(-90f);
                matrix.PreScale(-1f, 1f);
                break;
            case ExifOrientationRotate270:
                matrix.PreRotate(-90f);
                break;
        }

        return matrix;
    }

    private static int CalculateInSampleSize(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
    {
        var sampleSize = 1;
        while ((sourceWidth / sampleSize) > targetWidth * 2 || (sourceHeight / sampleSize) > targetHeight * 2)
        {
            sampleSize *= 2;
        }

        return sampleSize;
    }

    private static bool ShouldSwapDimensions(int exifOrientation) =>
        exifOrientation is ExifOrientationTranspose
            or ExifOrientationRotate90
            or ExifOrientationTransverse
            or ExifOrientationRotate270;

    private static WallpaperOrientation GetOrientation(int width, int height)
    {
        if (width == height)
        {
            return WallpaperOrientation.Square;
        }

        return height > width
            ? WallpaperOrientation.Portrait
            : WallpaperOrientation.Landscape;
    }

    private static int GetPositiveMinimum(params int[] values)
    {
        var positiveValues = values.Where(value => value > 0).ToArray();
        return positiveValues.Length == 0
            ? 0
            : positiveValues.Min();
    }

    private static int GetPositiveMaximum(params int[] values)
    {
        var positiveValues = values.Where(value => value > 0).ToArray();
        return positiveValues.Length == 0
            ? 0
            : positiveValues.Max();
    }

    private enum WallpaperOrientation
    {
        Portrait,
        Landscape,
        Square,
    }
}

internal readonly record struct AndroidWallpaperTarget(int Width, int Height);
#endif
