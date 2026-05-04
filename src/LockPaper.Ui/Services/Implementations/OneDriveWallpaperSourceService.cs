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

    public async Task<IReadOnlyList<OneDriveWallpaperPhoto>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);

        logger.LogInformation("Loading usable OneDrive photos for album {AlbumId}.", albumId);

        var accessToken = await oneDriveAuthenticationService
            .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        var nextRequestUri = $"me/drive/items/{Uri.EscapeDataString(albumId)}/children?$select=id,name,image&$top=200";
        var photos = new List<OneDriveWallpaperPhoto>();
        var pageNumber = 0;

        while (!string.IsNullOrWhiteSpace(nextRequestUri))
        {
            pageNumber++;
            using var response = await SendAsync(nextRequestUri, accessToken, cancellationToken).ConfigureAwait(false);
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            var pagePhotos = GetPhotos(document.RootElement);
            photos.AddRange(pagePhotos);
            nextRequestUri = GetNextPageRequestUri(document.RootElement);

            logger.LogInformation(
                "Processed OneDrive photo page {PageNumber} for album {AlbumId}. Usable photos on page: {PagePhotoCount}. Has another page: {HasNextPage}.",
                pageNumber,
                albumId,
                pagePhotos.Count,
                !string.IsNullOrWhiteSpace(nextRequestUri));
        }

        var uniquePhotos = photos
            .GroupBy(photo => photo.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

        logger.LogInformation(
            "Loaded {PhotoCount} usable OneDrive photo(s) for album {AlbumId} across {PageCount} page(s).",
            uniquePhotos.Length,
            albumId,
            pageNumber);

        return uniquePhotos;
    }

    public async Task<byte[]> DownloadPhotoBytesAsync(string photoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoId);

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

    private static IReadOnlyList<OneDriveWallpaperPhoto> GetPhotos(JsonElement rootElement)
    {
        if (!TryGetItemsArray(rootElement, out var itemsElement))
        {
            return [];
        }

        return itemsElement
            .EnumerateArray()
            .Where(IsUsablePhotoItem)
            .Select(itemElement => new OneDriveWallpaperPhoto
            {
                Id = GetRequiredStringProperty(itemElement, "id"),
                Name = GetRequiredStringProperty(itemElement, "name"),
                PixelWidth = itemElement.GetProperty("image").GetProperty("width").GetInt32(),
                PixelHeight = itemElement.GetProperty("image").GetProperty("height").GetInt32(),
            })
            .Where(photo => photo.PixelWidth > 0 && photo.PixelHeight > 0)
            .ToArray();
    }

    private static bool IsUsablePhotoItem(JsonElement itemElement) =>
        IsImageItem(itemElement)
        && IsSupportedWallpaperFileName(
            GetOptionalStringProperty(itemElement, "name"),
            restrictToWindowsCompatibleFormats: OperatingSystem.IsWindows());

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

    private static bool IsImageItem(JsonElement itemElement) =>
        itemElement.TryGetProperty("image", out var imageElement)
        && imageElement.ValueKind == JsonValueKind.Object
        && imageElement.TryGetProperty("width", out var widthElement)
        && widthElement.ValueKind == JsonValueKind.Number
        && imageElement.TryGetProperty("height", out var heightElement)
        && heightElement.ValueKind == JsonValueKind.Number
        && !string.IsNullOrWhiteSpace(GetOptionalStringProperty(itemElement, "id"))
        && !string.IsNullOrWhiteSpace(GetOptionalStringProperty(itemElement, "name"));

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
}
