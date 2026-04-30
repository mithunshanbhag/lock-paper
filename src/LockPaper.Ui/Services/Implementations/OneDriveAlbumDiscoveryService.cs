using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LockPaper.Ui.Services.Implementations;

public sealed class OneDriveAlbumDiscoveryService(
    IOneDriveWallpaperSourceService oneDriveWallpaperSourceService,
    ILogger<OneDriveAlbumDiscoveryService> logger) : IOneDriveAlbumDiscoveryService
{
    public async Task<OneDriveAlbumDiscoveryResult> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var matchingAlbums = await oneDriveWallpaperSourceService
                .GetMatchingAlbumsAsync(cancellationToken)
                .ConfigureAwait(false);

            logger.LogInformation(
                "OneDrive album discovery found {MatchingAlbumCount} matching album(s).",
                matchingAlbums.Count);

            var matchingAlbumNames = matchingAlbums
                .Select(album => album.Name)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return matchingAlbumNames.Length == 0
                ? OneDriveAlbumDiscoveryResult.NotFound()
                : OneDriveAlbumDiscoveryResult.Succeeded(matchingAlbumNames);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery could not acquire a Microsoft Graph access token.");
            return OneDriveAlbumDiscoveryResult.Failed("token_unavailable", exception.Message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery failed while calling Microsoft Graph.");
            return OneDriveAlbumDiscoveryResult.Failed(
                exception.StatusCode?.ToString() ?? "network_error",
                exception.Message);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery received an unexpected Microsoft Graph payload.");
            return OneDriveAlbumDiscoveryResult.Failed("invalid_response", exception.Message);
        }
    }
}
