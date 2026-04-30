using LockPaper.Ui.PageModels;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;

namespace LockPaper.Ui.UnitTests.PageModels;

public class MainPageModelTests
{
    #region PositiveCases

    [Fact]
    public void Constructor_ShouldStartInSignedOutState()
    {
        var model = new MainPageModel(new FakeOneDriveAuthenticationService());

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal(string.Empty, model.PrimaryStatusLabel);
        Assert.Equal(string.Empty, model.PrimaryStatusText);
        Assert.Equal(string.Empty, model.SecondaryStatusLabel);
        Assert.Equal(string.Empty, model.SecondaryStatusText);
    }

    [Fact]
    public async Task InitializeAsync_WhenCachedConnectionExists_ShouldShowConnectedState()
    {
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            });

        await model.InitializeAsync();

        Assert.Equal("Refresh OneDrive connection", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.True(model.IsLogoutVisible);
        Assert.False(model.ShowSignedOutLayout);
        Assert.True(model.ShowConnectedLayout);
        Assert.False(model.ShowFeedback);
        Assert.Equal("Microsoft account", model.PrimaryStatusLabel);
        Assert.Equal("family@example.com", model.PrimaryStatusText);
        Assert.Equal("Session", model.SecondaryStatusLabel);
        Assert.Equal("Connected", model.SecondaryStatusText);
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
            });

        await model.InitializeAsync();

        Assert.Equal("Reconnect to OneDrive", model.PrimaryActionText);
        Assert.True(model.ShowConnectedLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("sign in again", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Needs sign-in", model.SecondaryStatusText);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenSignInIsCancelled_ShouldRemainSignedOutAndShowFeedback()
    {
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                SignInResult = OneDriveConnectionOperationResult.Cancelled(OneDriveConnectionState.CreateSignedOut()),
            });

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowSignedOutLayout);
        Assert.True(model.ShowFeedback);
        Assert.Contains("did not change your OneDrive connection", model.FeedbackText, StringComparison.OrdinalIgnoreCase);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
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
            });

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
        var service = new FakeOneDriveAuthenticationService
        {
            CurrentState = OneDriveConnectionState.CreateConnected("family@example.com"),
            SignOutResult = OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut()),
        };
        var model = new MainPageModel(service);

        await model.InitializeAsync();
        await model.LogOutAsync();

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.False(model.ShowFeedback);
        Assert.False(model.IsLogoutVisible);
        Assert.True(model.ShowSignedOutLayout);
        Assert.False(model.ShowConnectedLayout);
        Assert.Equal(string.Empty, model.PrimaryStatusLabel);
        Assert.Equal(string.Empty, model.PrimaryStatusText);
        Assert.Equal(string.Empty, model.SecondaryStatusLabel);
        Assert.Equal(string.Empty, model.SecondaryStatusText);
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
}
