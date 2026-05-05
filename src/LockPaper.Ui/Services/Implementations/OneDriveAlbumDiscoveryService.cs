using LockPaper.Ui.Misc.Telemetry;
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
        var checkpoint = PerformanceCheckpoint.StartNew("OneDriveAlbumDiscovery.GetMatchingAlbumsAsync");
        var outcome = "Failed";

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

            outcome = matchingAlbumNames.Length == 0 ? "NotFound" : "Found";
            return matchingAlbumNames.Length == 0
                ? OneDriveAlbumDiscoveryResult.NotFound()
                : OneDriveAlbumDiscoveryResult.Succeeded(matchingAlbumNames);
        }
        catch (InvalidOperationException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery could not acquire a Microsoft Graph access token.");
            outcome = "Failed:token_unavailable";
            return OneDriveAlbumDiscoveryResult.Failed("token_unavailable", exception.Message);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery failed while calling Microsoft Graph.");
            outcome = "Failed:network_error";
            return OneDriveAlbumDiscoveryResult.Failed(
                exception.StatusCode?.ToString() ?? "network_error",
                exception.Message);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "OneDrive album discovery received an unexpected Microsoft Graph payload.");
            outcome = "Failed:invalid_response";
            return OneDriveAlbumDiscoveryResult.Failed("invalid_response", exception.Message);
        }
        finally
        {
            checkpoint.LogCompleted(logger, outcome);
        }
    }
}
