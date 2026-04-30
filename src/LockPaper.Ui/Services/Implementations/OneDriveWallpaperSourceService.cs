using LockPaper.Ui.Constants;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
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
        var accessToken = await oneDriveAuthenticationService
            .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        var matchingAlbums = new List<OneDriveWallpaperAlbum>();
        var nextRequestUri = OneDriveAlbumDiscoveryConstants.AlbumsRequestUri;

        while (!string.IsNullOrWhiteSpace(nextRequestUri))
        {
            using var response = await SendAsync(nextRequestUri, accessToken, cancellationToken).ConfigureAwait(false);
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            matchingAlbums.AddRange(GetMatchingAlbums(document.RootElement));
            nextRequestUri = GetNextPageRequestUri(document.RootElement);
        }

        return matchingAlbums
            .GroupBy(album => album.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    public async Task<IReadOnlyList<OneDriveWallpaperPhoto>> GetAlbumPhotosAsync(string albumId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(albumId);

        var accessToken = await oneDriveAuthenticationService
            .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        var nextRequestUri = $"me/drive/items/{Uri.EscapeDataString(albumId)}/children?$select=id,name,image&$top=200";
        var photos = new List<OneDriveWallpaperPhoto>();

        while (!string.IsNullOrWhiteSpace(nextRequestUri))
        {
            using var response = await SendAsync(nextRequestUri, accessToken, cancellationToken).ConfigureAwait(false);
            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

            photos.AddRange(GetPhotos(document.RootElement));
            nextRequestUri = GetNextPageRequestUri(document.RootElement);
        }

        return photos
            .GroupBy(photo => photo.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();
    }

    public async Task<byte[]> DownloadPhotoBytesAsync(string photoId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(photoId);

        var accessToken = await oneDriveAuthenticationService
            .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
            .ConfigureAwait(false);

        using var response = await SendAsync(
            $"me/drive/items/{Uri.EscapeDataString(photoId)}/content",
            accessToken,
            cancellationToken).ConfigureAwait(false);

        return await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
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
            return response;
        }

        var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        var message = ExtractGraphErrorMessage(errorPayload);

        logger.LogWarning(
            "OneDrive wallpaper request to {RequestUri} failed with HTTP status code {StatusCode}.",
            requestUri,
            response.StatusCode);

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
            .Where(IsImageItem)
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
