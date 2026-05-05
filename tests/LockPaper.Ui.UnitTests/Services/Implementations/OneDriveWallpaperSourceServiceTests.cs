using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using LockPaper.Ui.Services.Implementations;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace LockPaper.Ui.UnitTests.Services.Implementations;

public sealed class OneDriveWallpaperSourceServiceTests
{
    #region PositiveCases

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    [InlineData("photo.bmp")]
    [InlineData("photo.JPEG")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreRequired_ShouldAllowSupportedExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: true);

        Assert.True(isSupported);
    }

    [Theory]
    [InlineData("photo.heic")]
    [InlineData("photo.heif")]
    [InlineData("photo.webp")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreNotRequired_ShouldAllowOtherImageExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: false);

        Assert.True(isSupported);
    }

    #endregion

    #region NegativeCases

    [Theory]
    [InlineData("photo.heic")]
    [InlineData("photo.heif")]
    [InlineData("photo.webp")]
    [InlineData("photo.gif")]
    public void IsSupportedWallpaperFileName_WhenWindowsCompatibleFormatsAreRequired_ShouldRejectUnsupportedExtensions(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: true);

        Assert.False(isSupported);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Theory]
    [InlineData("")]
    [InlineData("photo")]
    [InlineData("photo.")]
    public void IsSupportedWallpaperFileName_WhenFileNameDoesNotContainUsableExtension_ShouldRejectIt(string fileName)
    {
        var isSupported = OneDriveWallpaperSourceService.IsSupportedWallpaperFileName(
            fileName,
            restrictToWindowsCompatibleFormats: false);

        Assert.False(isSupported);
    }

    [Fact]
    public async Task GetAlbumPhotosAsync_WhenAlbumContainsMixedItems_ShouldLogPhotoDiagnostics()
    {
        var logger = new TestLogger<OneDriveWallpaperSourceService>();
        var handler = new StubHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "value": [
                        { "id": "photo-1", "name": "portrait.jpg", "image": { "width": 1080, "height": 1920 } },
                        { "id": "photo-2", "name": "missing-extension", "image": { "width": 1440, "height": 2560 } },
                        { "id": "photo-3", "name": "zero-width.jpg", "image": { "width": 0, "height": 2560 } },
                        { "id": "", "name": "missing-id.jpg", "image": { "width": 1200, "height": 1600 } },
                        { "id": "folder-1", "name": "Not a photo", "folder": { "childCount": 2 } }
                      ]
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            });
        var service = new OneDriveWallpaperSourceService(
            new HttpClient(handler)
            {
                BaseAddress = new Uri("https://graph.microsoft.com/v1.0/"),
            },
            new FakeOneDriveAuthenticationService(),
            logger);

        var photos = await service.GetAlbumPhotosAsync("album-1");

        Assert.Single(photos);
        Assert.Contains(
            logger.Messages,
            message => message.Contains("Child items on page: 5", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Image items on page: 4", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Supported image items on page: 3", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Usable photos on page: 1", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Skipped unsupported image items on page: 1", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Skipped image items with missing metadata on page: 1", StringComparison.OrdinalIgnoreCase)
                && message.Contains("Skipped image items with invalid dimensions on page: 1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            logger.Messages,
            message => message.Contains("Performance checkpoint OneDriveWallpaperSource.GetAlbumPhotosAsync completed with outcome Succeeded", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    private sealed class FakeOneDriveAuthenticationService : IOneDriveAuthenticationService
    {
        public Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionState.CreateConnected("family@example.com"));

        public Task<string> GetMicrosoftGraphAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult("fake-token");

        public Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateConnected("family@example.com")));

        public Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut()));
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var authorization = request.Headers.Authorization;
            Assert.NotNull(authorization);
            Assert.Equal(new AuthenticationHeaderValue("Bearer", "fake-token"), authorization);
            return Task.FromResult(responseFactory(request));
        }
    }
}
