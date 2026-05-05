using LockPaper.Ui.Misc.Telemetry;
using LockPaper.Ui.Constants;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LockPaper.Ui.Services.Implementations;

public sealed class OneDriveWallpaperSourceService(
    HttpClient httpClient,
    IOneDriveAuthenticationService oneDriveAuthenticationService,
    ILogger<OneDriveWallpaperSourceService> logger) : IOneDriveWallpaperSourceService
{
    public async Task<IReadOnlyList<OneDriveWallpaperAlbum>> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default)
    {
        var checkpoint = PerformanceCheckpoint.StartNew("OneDriveWallpaperSource.GetMatchingAlbumsAsync");
        var outcome = "Succeeded";

        try
        {
            logger.LogInformation("Loading matching OneDrive albums from Microsoft Graph.");
            var accessToken = await oneDriveAuthenticationService
                .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            var matchingAlbums = new List<OneDriveWallpaperAlbum>();
            var nextRequestUri = OneDriveAlbumDiscoveryConstants.AlbumsRequestUri;
            var pageNumber = 0;

            while (!string.IsNullOrWhiteSpace(nextRequestUri))
            {
                pageNumber++;
                using var response = await SendAsync(nextRequestUri, accessToken, cancellationToken).ConfigureAwait(false);
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var pageAlbums = GetMatchingAlbums(document.RootElement);
                matchingAlbums.AddRange(pageAlbums);
                nextRequestUri = GetNextPageRequestUri(document.RootElement);

                logger.LogInformation(
                    "Processed OneDrive album page {PageNumber}. Matching albums on page: {PageAlbumCount}. Has another page: {HasNextPage}.",
                    pageNumber,
                    pageAlbums.Count,
                    !string.IsNullOrWhiteSpace(nextRequestUri));
            }

            var uniqueAlbums = matchingAlbums
                .GroupBy(album => album.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();

            logger.LogInformation(
                "Loaded {AlbumCount} matching OneDrive album(s) across {PageCount} page(s).",
                uniqueAlbums.Length,
                pageNumber);

            return uniqueAlbums;
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelled";
            throw;
        }
        catch (Exception)
        {
            outcome = "Failed";
            throw;
        }
        finally
        {
            checkpoint.LogCompleted(logger, outcome);
        }
    }

    public async Task<IReadOnlyList<OneDriveWallpaperPhoto>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default)
    {
        var checkpoint = PerformanceCheckpoint.StartNew("OneDriveWallpaperSource.GetAlbumPhotosAsync");
        var outcome = "Succeeded";

        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);

        try
        {
            logger.LogInformation("Loading usable OneDrive photos for album {AlbumId}.", albumId);

            var accessToken = await oneDriveAuthenticationService
                .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            var nextRequestUri = $"me/drive/items/{Uri.EscapeDataString(albumId)}/children?$select=id,name,image&$top=200";
            var photos = new List<OneDriveWallpaperPhoto>();
            var pageNumber = 0;
            var restrictToWindowsCompatibleFormats = OperatingSystem.IsWindows();

            while (!string.IsNullOrWhiteSpace(nextRequestUri))
            {
                pageNumber++;
                using var response = await SendAsync(nextRequestUri, accessToken, cancellationToken).ConfigureAwait(false);
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

                var pagePhotoResult = GetPhotoPageResult(document.RootElement, restrictToWindowsCompatibleFormats);
                photos.AddRange(pagePhotoResult.Photos);
                nextRequestUri = GetNextPageRequestUri(document.RootElement);

                logger.LogInformation(
                    "Processed OneDrive photo page {PageNumber} for album {AlbumId}. Child items on page: {PageItemCount}. Image items on page: {PageImageItemCount}. Supported image items on page: {PageSupportedImageCount}. Usable photos on page: {PagePhotoCount}. Skipped unsupported image items on page: {SkippedUnsupportedImageCount}. Skipped image items with missing metadata on page: {SkippedMissingMetadataImageCount}. Skipped image items with invalid dimensions on page: {SkippedInvalidDimensionImageCount}. Has another page: {HasNextPage}.",
                    pageNumber,
                    albumId,
                    pagePhotoResult.TotalItemCount,
                    pagePhotoResult.ImageItemCount,
                    pagePhotoResult.SupportedImageItemCount,
                    pagePhotoResult.Photos.Count,
                    pagePhotoResult.SkippedUnsupportedImageCount,
                    pagePhotoResult.SkippedMissingMetadataImageCount,
                    pagePhotoResult.SkippedInvalidDimensionImageCount,
                    !string.IsNullOrWhiteSpace(nextRequestUri));
            }

            var uniquePhotos = photos
                .GroupBy(photo => photo.Id, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray();
            var duplicatePhotoCount = photos.Count - uniquePhotos.Length;

            logger.LogInformation(
                "Loaded {PhotoCount} usable OneDrive photo(s) for album {AlbumId} across {PageCount} page(s). Duplicate photo ids removed: {DuplicatePhotoCount}.",
                uniquePhotos.Length,
                albumId,
                pageNumber,
                duplicatePhotoCount);

            return uniquePhotos;
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelled";
            throw;
        }
        catch (Exception)
        {
            outcome = "Failed";
            throw;
        }
        finally
        {
            checkpoint.LogCompleted(logger, outcome);
        }
    }

    public async Task<byte[]> DownloadPhotoBytesAsync(string photoId, CancellationToken cancellationToken = default)
    {
        var checkpoint = PerformanceCheckpoint.StartNew("OneDriveWallpaperSource.DownloadPhotoBytesAsync");
        var outcome = "Succeeded";

        ArgumentException.ThrowIfNullOrWhiteSpace(photoId);

        try
        {
            logger.LogInformation("Downloading OneDrive photo content for photo {PhotoId}.", photoId);

            var accessToken = await oneDriveAuthenticationService
                .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
                .ConfigureAwait(false);

            using var response = await SendAsync(
                $"me/drive/items/{Uri.EscapeDataString(photoId)}/content",
                accessToken,
                cancellationToken).ConfigureAwait(false);

            var photoBytes = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Downloaded {ByteCount} bytes for OneDrive photo {PhotoId}.", photoBytes.Length, photoId);
            return photoBytes;
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelled";
            throw;
        }
        catch (Exception)
        {
            outcome = "Failed";
            throw;
        }
        finally
        {
            checkpoint.LogCompleted(logger, outcome);
        }
    }

    private async Task<HttpResponseMessage> SendAsync(
        string requestUri,
        string accessToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken).ConfigureAwait(false);

        if (response.IsSuccessStatusCode)
        {
            logger.LogInformation(
                "OneDrive wallpaper request to {RequestUri} succeeded with HTTP status code {StatusCode}.",
                requestUri,
                response.StatusCode);
            return response;
        }

        var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var message = ExtractGraphErrorMessage(errorPayload);

        logger.LogWarning(
            "OneDrive wallpaper request to {RequestUri} failed with HTTP status code {StatusCode}. Graph message: {GraphMessage}",
            requestUri,
            response.StatusCode,
            message);

        response.Dispose();
        throw new HttpRequestException(message, null, response.StatusCode);
    }

    private static IReadOnlyList<OneDriveWallpaperAlbum> GetMatchingAlbums(JsonElement rootElement)
    {
        if (!TryGetItemsArray(rootElement, out var itemsElement))
        {
            return [];
        }

        return itemsElement
            .EnumerateArray()
            .Where(IsMatchingAlbumItem)
            .Select(itemElement => new OneDriveWallpaperAlbum
            {
                Id = GetRequiredStringProperty(itemElement, "id"),
                Name = GetRequiredStringProperty(itemElement, "name"),
            })
            .ToArray();
    }

    private static PhotoPageResult GetPhotoPageResult(
        JsonElement rootElement,
        bool restrictToWindowsCompatibleFormats)
    {
        if (!TryGetItemsArray(rootElement, out var itemsElement))
        {
            return PhotoPageResult.Empty;
        }

        var photos = new List<OneDriveWallpaperPhoto>();
        var totalItemCount = 0;
        var imageItemCount = 0;
        var supportedImageItemCount = 0;
        var skippedUnsupportedImageCount = 0;
        var skippedMissingMetadataImageCount = 0;
        var skippedInvalidDimensionImageCount = 0;

        foreach (var itemElement in itemsElement.EnumerateArray())
        {
            totalItemCount++;

            if (!TryGetImageDimensions(itemElement, out var width, out var height))
            {
                continue;
            }

            imageItemCount++;

            var id = GetOptionalStringProperty(itemElement, "id");
            var name = GetOptionalStringProperty(itemElement, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                skippedMissingMetadataImageCount++;
                continue;
            }

            if (!IsSupportedWallpaperFileName(name, restrictToWindowsCompatibleFormats))
            {
                skippedUnsupportedImageCount++;
                continue;
            }

            supportedImageItemCount++;

            if (string.IsNullOrWhiteSpace(id))
            {
                skippedMissingMetadataImageCount++;
                continue;
            }

            if (width <= 0 || height <= 0)
            {
                skippedInvalidDimensionImageCount++;
                continue;
            }

            photos.Add(new OneDriveWallpaperPhoto
            {
                Id = id,
                Name = name,
                PixelWidth = width,
                PixelHeight = height,
            });
        }

        return new PhotoPageResult(
            photos,
            totalItemCount,
            imageItemCount,
            supportedImageItemCount,
            skippedUnsupportedImageCount,
            skippedMissingMetadataImageCount,
            skippedInvalidDimensionImageCount);
    }

    private static bool TryGetItemsArray(JsonElement rootElement, out JsonElement itemsElement)
    {
        if (rootElement.TryGetProperty("value", out itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        itemsElement = default;
        return false;
    }

    private static bool IsMatchingAlbumItem(JsonElement itemElement)
    {
        if (!IsAlbumItem(itemElement))
        {
            return false;
        }

        var name = GetOptionalStringProperty(itemElement, "name");
        return !string.IsNullOrWhiteSpace(name)
            && OneDriveAlbumDiscoveryConstants.MatchingAlbumNames.Contains(name, StringComparer.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(GetOptionalStringProperty(itemElement, "id"));
    }

    private static bool IsAlbumItem(JsonElement itemElement)
    {
        if (itemElement.TryGetProperty("bundle", out var bundleElement)
            && bundleElement.ValueKind == JsonValueKind.Object
            && bundleElement.TryGetProperty("album", out var albumElement)
            && albumElement.ValueKind == JsonValueKind.Object)
        {
            return true;
        }

        return itemElement.TryGetProperty("album", out var legacyAlbumElement)
            && legacyAlbumElement.ValueKind == JsonValueKind.Object;
    }

    private static bool TryGetImageDimensions(
        JsonElement itemElement,
        out int width,
        out int height)
    {
        width = 0;
        height = 0;

        if (!itemElement.TryGetProperty("image", out var imageElement)
            || imageElement.ValueKind != JsonValueKind.Object
            || !imageElement.TryGetProperty("width", out var widthElement)
            || widthElement.ValueKind != JsonValueKind.Number
            || !imageElement.TryGetProperty("height", out var heightElement)
            || heightElement.ValueKind != JsonValueKind.Number)
        {
            return false;
        }

        width = widthElement.GetInt32();
        height = heightElement.GetInt32();
        return true;
    }

    internal static bool IsSupportedWallpaperFileName(
        string? fileName,
        bool restrictToWindowsCompatibleFormats)
    {
        var fileExtension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(fileExtension))
        {
            return false;
        }

        if (!restrictToWindowsCompatibleFormats)
        {
            return true;
        }

        return fileExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || fileExtension.Equals(".bmp", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetRequiredStringProperty(JsonElement element, string propertyName)
    {
        var value = GetOptionalStringProperty(element, propertyName);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        throw new JsonException($"Microsoft Graph did not return a usable '{propertyName}' value.");
    }

    private static string? GetOptionalStringProperty(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var propertyElement)
            ? propertyElement.GetString()?.Trim()
            : null;

    private static string? GetNextPageRequestUri(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("@odata.nextLink", out var nextLinkElement))
        {
            return null;
        }

        var nextLink = nextLinkElement.GetString();
        return string.IsNullOrWhiteSpace(nextLink)
            ? null
            : nextLink;
    }

    private static string ExtractGraphErrorMessage(string errorPayload)
    {
        if (string.IsNullOrWhiteSpace(errorPayload))
        {
            return "Microsoft Graph did not return the expected wallpaper data.";
        }

        try
        {
            using var document = JsonDocument.Parse(errorPayload);
            if (document.RootElement.TryGetProperty("error", out var errorElement)
                && errorElement.TryGetProperty("message", out var messageElement)
                && !string.IsNullOrWhiteSpace(messageElement.GetString()))
            {
                return messageElement.GetString()!;
            }
        }
        catch (JsonException)
        {
        }

        return errorPayload;
    }

    private sealed record PhotoPageResult(
        IReadOnlyList<OneDriveWallpaperPhoto> Photos,
        int TotalItemCount,
        int ImageItemCount,
        int SupportedImageItemCount,
        int SkippedUnsupportedImageCount,
        int SkippedMissingMetadataImageCount,
        int SkippedInvalidDimensionImageCount)
    {
        public static PhotoPageResult Empty { get; } = new([], 0, 0, 0, 0, 0, 0);
    }
}
