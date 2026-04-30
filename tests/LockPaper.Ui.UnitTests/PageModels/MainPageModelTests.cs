using LockPaper.Ui.PageModels;

namespace LockPaper.Ui.UnitTests.PageModels;

public class MainPageModelTests
{
    #region PositiveCases

    [Fact]
    public void Constructor_ShouldStartInSignedOutState()
    {
        var model = new MainPageModel();

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowNotice);
        Assert.False(model.ShowStatusSummary);
        Assert.False(model.ShowPlaceholderControls);
        Assert.Equal(string.Empty, model.LastAttemptText);
        Assert.Equal(string.Empty, model.NextAttemptText);
    }

    [Fact]
    public async Task PrimaryActionCommand_WhenConnectedPlaceholderIsSelected_ShouldShowConnectedState()
    {
        var model = new MainPageModel
        {
            SelectedPlaceholderState = "Connected",
        };

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.Equal("Change now", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.True(model.IsLogoutVisible);
        Assert.False(model.ShowNotice);
        Assert.True(model.ShowStatusSummary);
        Assert.True(model.ShowPlaceholderControls);
        Assert.StartsWith("✓ Today, ", model.LastAttemptText);
        Assert.StartsWith("Around ", model.NextAttemptText);
    }

    #endregion

    #region NegativeCases

    [Fact]
    public async Task PrimaryActionCommand_WhenAlbumMissingPlaceholderIsSelected_ShouldShowGuidance()
    {
        var model = new MainPageModel
        {
            SelectedPlaceholderState = "Album missing",
        };

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.True(model.ShowNotice);
        Assert.Equal("Album not found", model.NoticeTitle);
        Assert.Contains("lockpaper", model.NoticeMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("After the album is available", model.NextAttemptText);
        Assert.True(model.ShowStatusSummary);
        Assert.True(model.ShowPlaceholderControls);
    }

    [Fact]
    public async Task LogOut_ShouldReturnModelToSignedOutState()
    {
        var model = new MainPageModel
        {
            SelectedPlaceholderState = "Connected",
        };

        await model.PrimaryActionCommand.ExecuteAsync(null);
        model.LogOut();

        Assert.Equal("Connect to OneDrive", model.PrimaryActionText);
        Assert.True(model.IsPrimaryActionEnabled);
        Assert.False(model.IsLogoutVisible);
        Assert.False(model.ShowNotice);
        Assert.False(model.ShowStatusSummary);
        Assert.False(model.ShowPlaceholderControls);
        Assert.Equal("Connected", model.SelectedPlaceholderState);
        Assert.Equal(string.Empty, model.LastAttemptText);
        Assert.Equal(string.Empty, model.NextAttemptText);
    }

    #endregion

    #region BoundaryAndEdgeCases

    [Fact]
    public async Task PrimaryActionCommand_WhenPlaceholderStateIsUnknown_ShouldFallBackToConnectedState()
    {
        var model = new MainPageModel
        {
            SelectedPlaceholderState = "Unexpected state",
        };

        await model.PrimaryActionCommand.ExecuteAsync(null);

        Assert.Equal("Change now", model.PrimaryActionText);
        Assert.False(model.ShowNotice);
        Assert.True(model.IsLogoutVisible);
        Assert.True(model.ShowStatusSummary);
        Assert.True(model.ShowPlaceholderControls);
        Assert.StartsWith("✓ Today, ", model.LastAttemptText);
        Assert.StartsWith("Around ", model.NextAttemptText);
    }

    #endregion
}
