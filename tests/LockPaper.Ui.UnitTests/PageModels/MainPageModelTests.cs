using LockPaper.Ui.PageModels;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace LockPaper.Ui.UnitTests.PageModels;

public class MainPageModelTests
{
    #region PositiveCases

    [Fact]
    public void Constructor_ShouldStartInSignedOutState()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeDeviceDisplayService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal(string.Empty, model.AccountStatusText);
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
            new FakeDeviceDisplayService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal("Refresh OneDrive connection", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.True(model.IsLogoutVisible);
        Assert.False(model.ShowSignedOutLayout);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal("family@example.com", model.AccountStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal("No wallpaper change has run yet.", model.LastAttemptText);
        Assert.Equal("Waiting for wallpaper scheduling.", model.NextAttemptText);
        Assert.Single(model.DisplayPreviews);
        Assert.Equal("1080 x 1920", model.DisplayPreviews[0].ResolutionText);
        Assert.Equal(1, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenSignInSucceeds_ShouldShowConnectedState()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService(),
            new FakeDeviceDisplayService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.Equal("Refresh OneDrive connection", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowSignedOutLayout);
        Assert.Equal("family@example.com", model.AccountStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal(2, dispatcher.DispatchCount);
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
            new FakeDeviceDisplayService(),
            new FakeUiDispatcher(),
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal("Reconnect to OneDrive", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("sign in again", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
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
            new FakeDeviceDisplayService(),
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowSignedOutLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("did not change your OneDrive connection", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
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
            new FakeDeviceDisplayService(),
            new FakeUiDispatcher(),
            NullLogger<MainPageModel>.Instance);

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowFeedback);
        Assert.Contains("desktop redirect URI", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://localhost", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
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
        var model = new MainPageModel(service, new FakeDeviceDisplayService(), dispatcher, NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();
        await model.LogOutAsync();

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.False(model.ShowFeedback);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal(string.Empty, model.AccountStatusText);
        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.Equal(string.Empty, model.LastAttemptText);
        Assert.Equal(string.Empty, model.NextAttemptText);
        Assert.Empty(model.DisplayPreviews);
        Assert.Equal(2, dispatcher.DispatchCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenMultipleDisplaysDetected_ShouldShowDisplayCountAndPreviewCards()
    {
        var dispatcher = new FakeUiDispatcher();
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            },
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
            dispatcher,
            NullLogger<MainPageModel>.Instance);

        await model.InitializeAsync();

        Assert.Equal(string.Empty, model.DisplaySummaryText);
        Assert.False(model.ShowDisplaySummaryText);
        Assert.Equal(2, model.DisplayPreviews.Count);
        Assert.Equal("2560 x 1440", model.DisplayPreviews[0].ResolutionText);
        Assert.Equal("1080 x 1920", model.DisplayPreviews[1].ResolutionText);
        Assert.Equal(108d, model.DisplayPreviews[0].PreviewWidth);
        Assert.True(model.DisplayPreviews[1].PreviewHeight > model.DisplayPreviews[1].PreviewWidth);
        Assert.Equal(1, dispatcher.DispatchCount);
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

        public Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CurrentState);

        public Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SignInResult);

        public Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(SignOutResult);
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
}
