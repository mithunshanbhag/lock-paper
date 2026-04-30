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
        Assert.False(model.ShowNotice);
        Assert.False(model.ShowStatusSummary);
        Assert.Equal(string.Empty, model.AccountSummaryText);
        Assert.Equal(string.Empty, model.ConnectionSummaryText);
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
        Assert.False(model.ShowNotice);
        Assert.True(model.ShowStatusSummary);
        Assert.Equal("family@example.com", model.AccountSummaryText);
        Assert.Equal("Connected", model.ConnectionSummaryText);
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

        Assert.True(model.ShowNotice);
        Assert.Equal("Reconnect to OneDrive", model.NoticeTitle);
        Assert.Contains("sign in again", model.NoticeMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Reconnect to OneDrive", model.PrimaryActionText);
        Assert.Equal("Reconnect required", model.ConnectionSummaryText);
        Assert.True(model.ShowStatusSummary);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenSignInIsCancelled_ShouldRemainSignedOutAndShowNotice()
    {
        var model = new MainPageModel(
            new FakeOneDriveAuthenticationService
            {
                SignInResult = OneDriveConnectionOperationResult.Cancelled(OneDriveConnectionState.CreateSignedOut()),
            });

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowNotice);
        Assert.Equal("Sign-in cancelled", model.NoticeTitle);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowStatusSummary);
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

        Assert.True(model.ShowNotice);
        Assert.Equal("Couldn't connect", model.NoticeTitle);
        Assert.Contains("desktop redirect URI", model.NoticeMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://localhost", model.NoticeMessage, StringComparison.OrdinalIgnoreCase);
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
        Assert.False(model.ShowNotice);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowStatusSummary);
        Assert.Equal(string.Empty, model.AccountSummaryText);
        Assert.Equal(string.Empty, model.ConnectionSummaryText);
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
