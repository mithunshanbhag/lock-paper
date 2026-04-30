using LockPaper.Ui.Models;
using LockPaper.Ui.PageModels;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace LockPaper.Ui.UnitTests.PageModels;

public class MainPageModelTests
{
    private const int WaitForConditionMaxAttempts = 100;
    private const int WaitForConditionDelayMilliseconds = 10;

    #region PositiveCases

    [Fact]
    public void Constructor_ShouldStartInSignedOutState()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal(string.Empty, model.AccountStatusText);
        Assert.Equal(string.Empty, model.AlbumStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.Equal(string.Empty, model.LastAttemptText);
        Assert.Equal(string.Empty, model.NextAttemptText);
        Assert.Empty(model.DisplayPreviews);
    }

    [Fact]
    public async Task InitializeAsync_WhenCachedConnectionExists_ShouldShowConnectedState()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            },
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal("Refresh lockscreen wallpaper", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.True(model.IsLogoutVisible);
        Assert.False(model.ShowSignedOutLayout);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal("family@example.com", model.AccountStatusText);
        Assert.Equal("1 matching album is ready.", model.AlbumStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal("No wallpaper change has run yet.", model.LastAttemptText);
        Assert.Equal("Waiting for wallpaper scheduling.", model.NextAttemptText);
        Assert.Single(model.DisplayPreviews);
        Assert.Equal("1080 x 1920", model.DisplayPreviews[0].ResolutionText);
        Assert.Equal(2, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenSignInSucceeds_ShouldShowConnectedState()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.Equal("Refresh lockscreen wallpaper", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowSignedOutLayout);
        Assert.Equal("family@example.com", model.AccountStatusText);
        Assert.Equal("1 matching album is ready.", model.AlbumStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal(3, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenAlbumDiscoveryIsStillRunning_ShouldLeaveConnectingStateImmediately()
    {
        var dispatcher = new FakeUiDispatcher();
        var albumDiscoveryCompletionSource = new TaskCompletionSource<OneDriveAlbumDiscoveryResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeOneDriveAlbumDiscoveryService
            {
                PendingDiscoveryResult = albumDiscoveryCompletionSource.Task,
            },
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        var commandTask = model.PrimaryActionCommand.ExecuteAsync(null);

        await WaitForConditionAsync(() => model.ShowConnectedLayout);

        Assert.Equal("Refresh lockscreen wallpaper", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowSignedOutLayout);
        Assert.Equal("family@example.com", model.AccountStatusText);
        Assert.Equal("Checking matching albums...", model.AlbumStatusText);
        Assert.False(model.ShowFeedback);
        Assert.Equal(2, dispatcher.DispatchCount);

        albumDiscoveryCompletionSource.SetResult(OneDriveAlbumDiscoveryResult.Succeeded(["lockpaper"]));

        await commandTask;

        Assert.Equal("1 matching album is ready.", model.AlbumStatusText);
        Assert.Equal(3, dispatcher.DispatchCount);
    }

    #endregion

    #region NegativeCases

    [Fact]
    public async Task InitializeAsync_WhenReauthenticationIsRequired_ShouldShowReconnectGuidance()
    {
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                CurrentState = OneDriveConnectionState.CreateReauthenticationRequired("family@example.com"),
            },
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            new FakeUiDispatcher(),
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal("Reconnect to OneDrive", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("sign in again", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Reconnect to check matching albums.", model.AlbumStatusText);
        Assert.Equal("Paused until OneDrive is reconnected.", model.LastAttemptText);
        Assert.Equal("Will resume after you sign in again.", model.NextAttemptText);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenSignInIsCancelled_ShouldRemainSignedOutAndShowFeedback()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                SignInResult = OneDriveConnectionOperationResult.Cancelled(OneDriveConnectionState.CreateSignedOut()),
            },
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowSignedOutLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("did not change your OneDrive connection", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.Equal(string.Empty, model.AlbumStatusText);
        Assert.Equal(2, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenRedirectUriIsRejected_ShouldShowAzureRedirectGuidance()
    {
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                SignInResult = OneDriveConnectionOperationResult.Failed(
                    OneDriveConnectionState.CreateSignedOut(),
                    "invalid_request",
                    "The provided value for the input parameter 'redirect_uri' is not valid."),
            },
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            new FakeUiDispatcher(),
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowFeedback);
        Assert.Contains("desktop redirect URI", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://localhost", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenNoMatchingAlbumsExist_ShouldShowAlbumGuidance()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeOneDriveAlbumDiscoveryService
            {
                DiscoveryResult = OneDriveAlbumDiscoveryResult.NotFound(),
            },
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowSignedOutLayout);
        Assert.True(model.ShowFeedback);
        Assert.Equal("No matching albums found.", model.AlbumStatusText);
        Assert.Contains("Create or rename an album", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lockpaper", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Waiting for a matching OneDrive album.", model.LastAttemptText);
        Assert.Equal("Will resume after a matching album is available.", model.NextAttemptText);
        Assert.Equal(3, dispatcher.DispatchCount);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Fact]
    public async Task LogOutAsync_WhenConnected_ShouldReturnModelToSignedOutState()
    {
        var dispatcher = new FakeUiDispatcher();
        var service = new FakeOneDriveAuthenticationService
        {
            CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            SignOutResult = OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut()),
        };
        var model = new MainPageModel(
            service,
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();
        await model.LogOutAsync();

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.False(model.ShowFeedback);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal(string.Empty, model.AccountStatusText);
        Assert.Equal(string.Empty, model.AlbumStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.Equal(string.Empty, model.LastAttemptText);
        Assert.Equal(string.Empty, model.NextAttemptText);
        Assert.Empty(model.DisplayPreviews);
        Assert.Equal(3, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenMultipleDisplaysDetected_ShouldShowDisplayPreviewCards()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            },
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService
            {
                Displays =
                [
                    new DeviceDisplayInfo
                    {
                        PixelWidth = 2560,
                        PixelHeight = 1440,
                        ApproximateDiagonalInches = 27,
                        IsPrimary = true,
                    },
                    new DeviceDisplayInfo
                    {
                        PixelWidth = 1080,
                        PixelHeight = 1920,
                        ApproximateDiagonalInches = 24,
                        IsPrimary = false,
                    },
                ],
            },
            new FakeWallpaperRefreshService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal("1 matching album is ready.", model.AlbumStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal(2, model.DisplayPreviews.Count);
        Assert.Equal("2560 x 1440", model.DisplayPreviews[0].ResolutionText);
        Assert.Equal("1080 x 1920", model.DisplayPreviews[1].ResolutionText);
        Assert.Equal(108d, model.DisplayPreviews[0].PreviewWidth);
        Assert.True(model.DisplayPreviews[1].PreviewHeight > model.DisplayPreviews[1].PreviewWidth);
        Assert.Equal(2, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenConnected_ShouldRefreshWallpaperInsteadOfSigningIn()
    {
        var dispatcher = new FakeUiDispatcher();
        var authenticationService = new FakeOneDriveAuthenticationService
        {
            CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
        };
        var wallpaperRefreshService = new FakeWallpaperRefreshService
        {
            RefreshResult = WallpaperRefreshResult.Succeeded(
                DateTimeOffset.Parse("2026-04-30T21:15:00+05:30"),
                1,
                "lockpaper",
                "sunrise.jpg"),
        };
        var model = new MainPageModel(
            authenticationService,
            new FakeOneDriveAlbumDiscoveryService(),
            new FakeDeviceDisplayService(),
            wallpaperRefreshService,
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();
        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.Equal(0, authenticationService.SignInCallCount);
        Assert.Equal(1, wallpaperRefreshService.RefreshCallCount);
        Assert.Equal("Refresh lockscreen wallpaper", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.Contains("sunrise.jpg", model.LastAttemptText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("lockpaper", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Waiting for wallpaper scheduling.", model.NextAttemptText);
    }

    #endregion

    private sealed class FakeOneDriveAuthenticationService : IOneDriveAuthenticationService
    {
        public OneDriveConnectionState CurrentState { get; set; } = OneDriveConnectionState.CreateSignedOut();

        public OneDriveConnectionOperationResult SignInResult { get; set; } =
            OneDriveConnectionOperationResult.Succeeded(
                OneDriveConnectionState.CreateConnected("family@example.com"));

        public OneDriveConnectionOperationResult SignOutResult { get; set; } =
            OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut());

        public string AccessToken { get; set; } = "fake-token";

        public int SignInCallCount { get; private set; }

        public int SignOutCallCount { get; private set; }

        public Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentState);

        public Task<string> GetMicrosoftGraphAccessTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(AccessToken);

        public Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default)
        {
            SignInCallCount++;
            return Task.FromResult(SignInResult);
        }

        public Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default)
        {
            SignOutCallCount++;
            return Task.FromResult(SignOutResult);
        }
    }

    private sealed class FakeOneDriveAlbumDiscoveryService : IOneDriveAlbumDiscoveryService
    {
        public OneDriveAlbumDiscoveryResult DiscoveryResult { get; set; } =
            OneDriveAlbumDiscoveryResult.Succeeded(["lockpaper"]);

        public Task<OneDriveAlbumDiscoveryResult>? PendingDiscoveryResult { get; set; }

        public Task<OneDriveAlbumDiscoveryResult> GetMatchingAlbumsAsync(CancellationToken cancellationToken = default) =>
            PendingDiscoveryResult ?? Task.FromResult(DiscoveryResult);
    }

    private sealed class FakeDeviceDisplayService : IDeviceDisplayService
    {
        public IReadOnlyList<DeviceDisplayInfo> Displays { get; set; } =
        [
            new DeviceDisplayInfo
            {
                PixelWidth = 1080,
                PixelHeight = 1920,
                ApproximateDiagonalInches = 6.7d,
                IsPrimary = true,
            },
        ];

        public IReadOnlyList<DeviceDisplayInfo> GetDisplays() => Displays;
    }

    private sealed class FakeWallpaperRefreshService : IWallpaperRefreshService
    {
        public WallpaperRefreshResult RefreshResult { get; set; } =
            WallpaperRefreshResult.Succeeded(DateTimeOffset.Now, 1, "lockpaper", "sunrise.jpg");

        public int RefreshCallCount { get; private set; }

        public Task<WallpaperRefreshResult> RefreshAsync(CancellationToken cancellationToken = default)
        {
            RefreshCallCount++;
            return Task.FromResult(RefreshResult);
        }
    }

    private sealed class FakeUiDispatcher : IUiDispatcher
    {
        public int DispatchCount { get; private set; }

        public Task DispatchAsync(Action action)
        {
            DispatchCount++;
            action();
            return Task.CompletedTask;
        }
    }

    private static async Task WaitForConditionAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < WaitForConditionMaxAttempts; attempt++)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(WaitForConditionDelayMilliseconds);
        }

        throw new TimeoutException("The expected condition was not reached.");
    }
}
