using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Implementations;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public class OneDriveAlbumDiscoveryServiceTests
{
    #region PositiveCases

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenGraphReturnsMatchingAlbumNames_ShouldReturnMatches()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "value": [
                        { "id": "album-1", "name": "LockPaper", "bundle": { "album": {} } },
                        { "id": "album-2", "name": "Summer photos", "bundle": { "album": {} } },
                        { "id": "album-3", "name": "lock paper", "bundle": { "album": {} } }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });

        var service = CreateService(handler);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.Found, result.Status);
        Assert.Equal(2, result.MatchingAlbumNames.Count);
        Assert.Contains("LockPaper", result.MatchingAlbumNames);
        Assert.Contains("lock paper", result.MatchingAlbumNames);
        Assert.Equal("Bearer", handler.LastRequest?.Headers.Authorization?.Scheme);
        Assert.Equal("fake-token", handler.LastRequest?.Headers.Authorization?.Parameter);
        Assert.Equal(
            "/v1.0/me/drive/bundles?$filter=bundle/album ne null&$top=200",
            Uri.UnescapeDataString(handler.LastRequest?.RequestUri?.PathAndQuery ?? string.Empty));
    }

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenAlbumIsOnLaterPage_ShouldFollowNextLinkAndReturnMatches()
    {
        var responseIndex = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            responseIndex++;

            return responseIndex switch
            {
                1 => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "value": [
                            { "id": "album-family", "name": "Family", "bundle": { "album": {} } }
                          ],
                          "@odata.nextLink": "https://graph.microsoft.com/v1.0/me/drive/bundles?$skiptoken=next-page"
                        }
                        """,
                        Encoding.UTF8,
                        "application/json"),
                },
                2 => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "value": [
                            { "id": "album-lockpaper", "name": "lockpaper", "bundle": { "album": {} } }
                          ]
                        }
                        """,
                        Encoding.UTF8,
                        "application/json"),
                },
                _ => throw new Xunit.Sdk.XunitException("Unexpected extra request."),
            };
        });

        var service = CreateService(handler);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.Found, result.Status);
        Assert.Single(result.MatchingAlbumNames);
        Assert.Contains("lockpaper", result.MatchingAlbumNames);
        Assert.Equal(2, handler.Requests.Count);
        Assert.Equal(
            "/v1.0/me/drive/bundles?$filter=bundle/album ne null&$top=200",
            Uri.UnescapeDataString(handler.Requests[0].RequestUri?.PathAndQuery ?? string.Empty));
        Assert.Equal(
            "/v1.0/me/drive/bundles?$skiptoken=next-page",
            Uri.UnescapeDataString(handler.Requests[1].RequestUri?.PathAndQuery ?? string.Empty));
    }

    #endregion

    #region NegativeCases

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenNoMatchingAlbumsExist_ShouldReturnNotFound()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "value": [
                        { "id": "album-camera-roll", "name": "Camera Roll", "bundle": { "album": {} } },
                        { "id": "album-wallpaper-ideas", "name": "Wallpaper ideas", "bundle": { "album": {} } }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });

        var service = CreateService(handler);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.NotFound, result.Status);
        Assert.Empty(result.MatchingAlbumNames);
    }

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenMatchingNameIsNotAnAlbumBundle_ShouldReturnNotFound()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "value": [
                        { "id": "item-1", "name": "lockpaper", "bundle": { "childCount": 3 } },
                        { "id": "item-2", "name": "lock paper", "folder": { "childCount": 4 } }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });

        var service = CreateService(handler);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.NotFound, result.Status);
        Assert.Empty(result.MatchingAlbumNames);
    }

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenAccessTokenIsUnavailable_ShouldReturnFailure()
    {
        var service = new OneDriveAlbumDiscoveryService(
            new OneDriveWallpaperSourceService(
                new HttpClient(new StubHttpMessageHandler(_ => throw new Xunit.Sdk.XunitException("HTTP should not be called.")))
                {
                    BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
                },
                new FakeOneDriveAuthenticationService
                {
                    AccessTokenException = new InvalidOperationException("Sign in again."),
                },
                NullLogger<OneDriveWallpaperSourceService>.Instance),
            NullLogger<OneDriveAlbumDiscoveryService>.Instance);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.Failed, result.Status);
        Assert.Equal("token_unavailable", result.ErrorCode);
        Assert.Contains("Sign in again", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Fact]
    public async Task GetMatchingAlbumsAsync_WhenGraphReturnsForbidden_ShouldSurfaceGraphMessage()
    {
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Forbidden)
            {
                Content = new StringContent(
                    """
                    {
                      "error": {
                        "message": "Access denied."
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });

        var service = CreateService(handler);

        var result = await service.GetMatchingAlbumsAsync();

        Assert.Equal(OneDriveAlbumDiscoveryStatus.Failed, result.Status);
        Assert.Equal("Forbidden", result.ErrorCode);
        Assert.Equal("Access denied.", result.ErrorMessage);
    }

    #endregion

    private static OneDriveAlbumDiscoveryService CreateService(StubHttpMessageHandler handler) =>
        new(
            new OneDriveWallpaperSourceService(
                new HttpClient(handler)
                {
                    BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
                },
                new FakeOneDriveAuthenticationService(),
                NullLogger<OneDriveWallpaperSourceService>.Instance),
            NullLogger<OneDriveAlbumDiscoveryService>.Instance);

    private sealed class FakeOneDriveAuthenticationService : IOneDriveAuthenticationService
    {
        public string AccessToken { get; set; } = "fake-token";

        public Exception? AccessTokenException { get; set; }

        public Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionState.CreateConnected("family@example.com"));

        public Task<string> GetMicrosoftGraphAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (AccessTokenException is not null)
            {
                throw AccessTokenException;
            }

            return Task.FromResult(AccessToken);
        }

        public Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateConnected("family@example.com")));

        public Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut()));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = CloneRequest(request);
            Requests.Add(LastRequest);
            return Task.FromResult(responseFactory(request));
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri);
            foreach (var header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }
    }
}
