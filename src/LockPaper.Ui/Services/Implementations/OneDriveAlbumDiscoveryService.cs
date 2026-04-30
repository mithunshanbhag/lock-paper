using LockPaper.Ui.Constants;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text.Json;

namespace LockPaper.Ui.Services.Implementations;

public sealed class OneDriveAlbumDiscoveryService(
    HttpClient httpClient,
    IOneDriveAuthenticationService oneDriveAuthenticationService,
    ILogger<OneDriveAlbumDiscoveryService> logger) : IOneDriveAlbumDiscoveryService
{
    public async Task<OneDriveAlbumDiscoveryResult> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default)
    {
        string accessToken;
        try
        {
            accessToken = await oneDriveAuthenticationService
                .GetMicrosoftGraphAccessTokenAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery could not acquire a Microsoft Graph access token.");
            return OneDriveAlbumDiscoveryResult.Failed("token_unavailable", exception.Message);
        }

        try
        {
            var matchingAlbumNames = new List<string>();
            var nextRequestUri = OneDriveAlbumDiscoveryConstants.AlbumsRequestUri;
            var pageCount = 0;

            while (!string.IsNullOrWhiteSpace(nextRequestUri))
            {
                pageCount++;

                using var request = new HttpRequestMessage(HttpMethod.Get, nextRequestUri);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    var errorPayload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                    logger.LogWarning("OneDrive album discovery failed with HTTP status code {StatusCode}.", response.StatusCode);

                    return OneDriveAlbumDiscoveryResult.Failed(
                        response.StatusCode.ToString(),
                        ExtractGraphErrorMessage(errorPayload));
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken).ConfigureAwait(false);

                matchingAlbumNames.AddRange(GetMatchingAlbumNames(document.RootElement));
                nextRequestUri = GetNextPageRequestUri(document.RootElement);
            }

            var distinctMatchingAlbumNames = matchingAlbumNames
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            logger.LogInformation(
                "OneDrive album discovery found {MatchingAlbumCount} matching album(s) across {PageCount} page(s).",
                distinctMatchingAlbumNames.Length,
                pageCount);

            return distinctMatchingAlbumNames.Length == 0
                ? OneDriveAlbumDiscoveryResult.NotFound()
                : OneDriveAlbumDiscoveryResult.Succeeded(distinctMatchingAlbumNames);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery failed while calling Microsoft Graph.");
            return OneDriveAlbumDiscoveryResult.Failed("network_error", exception.Message);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery received an unexpected Microsoft Graph payload.");
            return OneDriveAlbumDiscoveryResult.Failed("invalid_response", exception.Message);
        }
    }

    private static string[] GetMatchingAlbumNames(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("value", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return itemsElement
            .EnumerateArray()
            .Where(IsAlbumItem)
            .Select(GetAlbumName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Where(name => OneDriveAlbumDiscoveryConstants.MatchingAlbumNames.Contains(name!, StringComparer.OrdinalIgnoreCase))
            .Select(name => name!)
            .ToArray();
    }

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

    private static string? GetAlbumName(JsonElement itemElement) =>
        itemElement.TryGetProperty("name", out var nameElement)
            ? nameElement.GetString()?.Trim()
            : null;

    private static string ExtractGraphErrorMessage(string errorPayload)
    {
        if (string.IsNullOrWhiteSpace(errorPayload))
        {
            return "Microsoft Graph did not return an album list.";
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
