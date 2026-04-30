using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockPaper.Ui.Constants;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace LockPaper.Ui.PageModels;

public partial class MainPageModel : ObservableObject
{
    private const string DefaultAccountLabel = "Personal Microsoft account";

    private readonly IOneDriveAlbumDiscoveryService _oneDriveAlbumDiscoveryService;
    private readonly IDeviceDisplayService _deviceDisplayService;
    private readonly ILogger<MainPageModel> _logger;
    private readonly IOneDriveAuthenticationService _oneDriveAuthenticationService;
    private readonly IUiDispatcher _uiDispatcher;
    private bool _hasInitialized;

    private static readonly string[] DisplayPreviewColors =
    [
        "#5B6DF8",
        "#2AA7A1",
        "#A768E8",
        "#F09A43",
    ];

    [ObservableProperty]
    private bool _isLogoutVisible;

    [ObservableProperty]
    private string _primaryActionText = "Connect to OneDrive";

    [ObservableProperty]
    private bool _isPrimaryActionEnabled = true;

    [ObservableProperty]
    private bool _showSignedOutLayout = true;

    [ObservableProperty]
    private bool _showConnectedLayout;

    [ObservableProperty]
    private bool _showFeedback;

    [ObservableProperty]
    private string _feedbackText = string.Empty;

    [ObservableProperty]
    private string _accountStatusText = string.Empty;

    [ObservableProperty]
    private string _albumStatusText = string.Empty;

    [ObservableProperty]
    private string _displaySummaryText = string.Empty;

    [ObservableProperty]
    private bool _showDisplaySummaryText;

    [ObservableProperty]
    private string _lastAttemptText = string.Empty;

    [ObservableProperty]
    private string _nextAttemptText = string.Empty;

    public ObservableCollection<DisplayPreview> DisplayPreviews { get; } = [];

    public MainPageModel(
        IOneDriveAuthenticationService oneDriveAuthenticationService,
        IOneDriveAlbumDiscoveryService oneDriveAlbumDiscoveryService,
        IDeviceDisplayService deviceDisplayService,
        IUiDispatcher uiDispatcher,
        ILogger<MainPageModel> logger)
    {
        _oneDriveAuthenticationService = oneDriveAuthenticationService;
        _oneDriveAlbumDiscoveryService = oneDriveAlbumDiscoveryService;
        _deviceDisplayService = deviceDisplayService;
        _uiDispatcher = uiDispatcher;
        _logger = logger;
        ApplyScenario(LockPaperScenario.SignedOut);
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing main page model. Already initialized: {HasInitialized}", _hasInitialized);

        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await RefreshConnectionStateAsync();
    }

    public async Task LogOutAsync()
    {
        _logger.LogInformation("Starting OneDrive sign-out flow.");
        var result = await _oneDriveAuthenticationService.SignOutAsync();
        _logger.LogInformation("OneDrive sign-out returned status {Status} with resulting state {StateStatus}.", result.Status, result.State.Status);

        try
        {
            await _uiDispatcher.DispatchAsync(() => HandleSignOutResult(result));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying sign-out result on the UI thread failed.");
            throw;
        }
    }

    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        _logger.LogInformation("Primary action invoked. Starting OneDrive sign-in flow.");

        try
        {
            await _uiDispatcher.DispatchAsync(() => ApplyScenario(LockPaperScenario.Connecting));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying the connecting scenario on the UI thread failed.");
            throw;
        }

        var result = await _oneDriveAuthenticationService.SignInAsync();
        _logger.LogInformation(
            "OneDrive sign-in returned status {Status} with resulting state {StateStatus} for account {AccountLabel}.",
            result.Status,
            result.State.Status,
            result.State.AccountLabel);
        var albumDiscoveryResult = await GetAlbumDiscoveryResultIfNeededAsync(result.State);

        try
        {
            await _uiDispatcher.DispatchAsync(() => HandleSignInResult(result, albumDiscoveryResult));
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Applying sign-in result on the UI thread failed. Result status: {Status}, connection state: {StateStatus}.",
                result.Status,
                result.State.Status);
            throw;
        }
    }

    private async Task RefreshConnectionStateAsync()
    {
        _logger.LogInformation("Refreshing cached OneDrive connection state.");
        var connectionState = await _oneDriveAuthenticationService.GetCurrentConnectionStateAsync();
        _logger.LogInformation("Fetched cached OneDrive state {StateStatus} for account {AccountLabel}.", connectionState.Status, connectionState.AccountLabel);
        var albumDiscoveryResult = await GetAlbumDiscoveryResultIfNeededAsync(connectionState);

        try
        {
            await _uiDispatcher.DispatchAsync(() => ApplyConnectionState(connectionState, albumDiscoveryResult));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying cached connection state on the UI thread failed.");
            throw;
        }
    }

    private void ApplyScenario(LockPaperScenario scenario)
    {
        _logger.LogInformation("Applying UI scenario {Scenario}.", scenario);
        PrimaryActionText = scenario switch
        {
            LockPaperScenario.SignedOut => "Connect to OneDrive",
            LockPaperScenario.Connecting => "Connecting to OneDrive...",
            LockPaperScenario.ReauthenticationRequired => "Reconnect to OneDrive",
            _ => "Refresh OneDrive connection",
        };
        IsPrimaryActionEnabled = scenario != LockPaperScenario.Connecting;
        IsLogoutVisible = scenario is LockPaperScenario.Connected or LockPaperScenario.ReauthenticationRequired;
        ShowConnectedLayout = scenario is LockPaperScenario.Connected or LockPaperScenario.ReauthenticationRequired;
        ShowSignedOutLayout = !ShowConnectedLayout;

        switch (scenario)
        {
            case LockPaperScenario.SignedOut:
                ClearStatusSummary();
                ClearFeedback();
                break;
            case LockPaperScenario.Connecting:
                ClearStatusSummary();
                SetFeedback("Finish the Microsoft sign-in flow to connect LockPaper.");
                break;
            case LockPaperScenario.Connected:
                ClearFeedback();
                break;
            case LockPaperScenario.ReauthenticationRequired:
                SetFeedback("LockPaper needs you to sign in again before it can keep using OneDrive.");
                break;
        }
    }

    private void ApplyConnectionState(
        OneDriveConnectionState connectionState,
        OneDriveAlbumDiscoveryResult? albumDiscoveryResult = null)
    {
        var scenario = connectionState.Status switch
        {
            OneDriveConnectionStatus.SignedOut => LockPaperScenario.SignedOut,
            OneDriveConnectionStatus.ReauthenticationRequired => LockPaperScenario.ReauthenticationRequired,
            _ => LockPaperScenario.Connected,
        };

        _logger.LogInformation(
            "Applying connection state {ConnectionStatus}. Derived scenario: {Scenario}. Account label: {AccountLabel}.",
            connectionState.Status,
            scenario,
            connectionState.AccountLabel);

        ApplyScenario(scenario);

        if (scenario == LockPaperScenario.SignedOut)
        {
            return;
        }

        AccountStatusText = FormatAccountLabel(connectionState.AccountLabel);
        ApplyAlbumStatus(scenario, albumDiscoveryResult);

        IReadOnlyList<DeviceDisplayInfo> displays;
        try
        {
            displays = _deviceDisplayService.GetDisplays();
            _logger.LogInformation("Retrieved {DisplayCount} display(s) from the device display service.", displays.Count);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Reading device display details failed.");
            throw;
        }

        ApplyDisplaySummary(displays);
        ApplyAttemptStatus(scenario, albumDiscoveryResult);
    }

    private void HandleSignInResult(
        OneDriveConnectionOperationResult result,
        OneDriveAlbumDiscoveryResult? albumDiscoveryResult)
    {
        _logger.LogInformation("Handling sign-in result {Status}.", result.Status);
        ApplyConnectionState(result.State, albumDiscoveryResult);

        switch (result.Status)
        {
            case OneDriveConnectionOperationStatus.Cancelled:
                SetFeedback("LockPaper did not change your OneDrive connection.");
                break;
            case OneDriveConnectionOperationStatus.Failed:
                SetFeedback(BuildSignInFailureMessage(result));
                break;
        }
    }

    private void HandleSignOutResult(OneDriveConnectionOperationResult result)
    {
        _logger.LogInformation("Handling sign-out result {Status}.", result.Status);
        ApplyConnectionState(result.State);

        if (result.Status == OneDriveConnectionOperationStatus.Failed)
        {
            SetFeedback(BuildSignOutFailureMessage(result));
            return;
        }

        ClearFeedback();
    }

    private void ClearStatusSummary()
    {
        AccountStatusText = string.Empty;
        AlbumStatusText = string.Empty;
        DisplaySummaryText = string.Empty;
        ShowDisplaySummaryText = false;
        LastAttemptText = string.Empty;
        NextAttemptText = string.Empty;
        DisplayPreviews.Clear();
    }

    private void ClearFeedback()
    {
        ShowFeedback = false;
        FeedbackText = string.Empty;
    }

    private void SetFeedback(string message)
    {
        ShowFeedback = true;
        FeedbackText = message;
    }

    private void ApplyDisplaySummary(IReadOnlyList<DeviceDisplayInfo> displays)
    {
        _logger.LogInformation("Applying display summary for {DisplayCount} display(s).", displays.Count);

        if (displays.Count == 0)
        {
            DisplaySummaryText = "Display details are not available on this device.";
            ShowDisplaySummaryText = true;
            DisplayPreviews.Clear();
            return;
        }

        DisplaySummaryText = string.Empty;
        ShowDisplaySummaryText = false;

        ReplaceDisplayPreviews(displays.Select((display, index) => BuildDisplayPreview(display, index)));
    }

    private void ApplyAlbumStatus(LockPaperScenario scenario, OneDriveAlbumDiscoveryResult? albumDiscoveryResult)
    {
        if (scenario == LockPaperScenario.ReauthenticationRequired)
        {
            AlbumStatusText = "Reconnect to check matching albums.";
            return;
        }

        if (scenario != LockPaperScenario.Connected)
        {
            AlbumStatusText = string.Empty;
            return;
        }

        var resolvedDiscoveryResult = albumDiscoveryResult
            ?? OneDriveAlbumDiscoveryResult.Failed("not_checked", "LockPaper couldn't confirm your OneDrive albums yet.");

        switch (resolvedDiscoveryResult.Status)
        {
            case OneDriveAlbumDiscoveryStatus.Found:
                AlbumStatusText = resolvedDiscoveryResult.MatchingAlbumNames.Count == 1
                    ? "1 matching album is ready."
                    : $"{resolvedDiscoveryResult.MatchingAlbumNames.Count} matching albums are ready.";
                break;
            case OneDriveAlbumDiscoveryStatus.NotFound:
                AlbumStatusText = "No matching albums found.";
                SetFeedback(BuildNoMatchingAlbumsMessage());
                break;
            case OneDriveAlbumDiscoveryStatus.Failed:
                AlbumStatusText = "Couldn't check matching albums.";
                SetFeedback(BuildAlbumDiscoveryFailureMessage(resolvedDiscoveryResult));
                break;
        }
    }

    private void ApplyAttemptStatus(
        LockPaperScenario scenario,
        OneDriveAlbumDiscoveryResult? albumDiscoveryResult)
    {
        if (scenario == LockPaperScenario.ReauthenticationRequired)
        {
            LastAttemptText = "Paused until OneDrive is reconnected.";
            NextAttemptText = "Will resume after you sign in again.";
            return;
        }

        if (scenario == LockPaperScenario.Connected && albumDiscoveryResult is not null)
        {
            switch (albumDiscoveryResult.Status)
            {
                case OneDriveAlbumDiscoveryStatus.NotFound:
                    LastAttemptText = "Waiting for a matching OneDrive album.";
                    NextAttemptText = "Will resume after a matching album is available.";
                    return;
                case OneDriveAlbumDiscoveryStatus.Failed:
                    LastAttemptText = "Couldn't verify the OneDrive album yet.";
                    NextAttemptText = "Will resume after OneDrive album access works.";
                    return;
            }
        }

        LastAttemptText = "No wallpaper change has run yet.";
        NextAttemptText = "Waiting for wallpaper scheduling.";
    }

    private void ReplaceDisplayPreviews(IEnumerable<DisplayPreview> previews)
    {
        DisplayPreviews.Clear();
        foreach (var preview in previews)
        {
            _logger.LogInformation(
                "Adding display preview with resolution {ResolutionText}.",
                preview.ResolutionText);
            DisplayPreviews.Add(preview);
        }
    }

    private static DisplayPreview BuildDisplayPreview(DeviceDisplayInfo display, int index)
    {
        var largestDimension = Math.Max(display.PixelWidth, display.PixelHeight);
        var scale = largestDimension == 0 ? 1d : 108d / largestDimension;
        var previewWidth = Math.Clamp(Math.Round(display.PixelWidth * scale, 0), 72d, 108d);
        var previewHeight = Math.Clamp(Math.Round(display.PixelHeight * scale, 0), 56d, 108d);

        return new DisplayPreview
        {
            ResolutionText = $"{display.PixelWidth} x {display.PixelHeight}",
            PreviewColor = DisplayPreviewColors[index % DisplayPreviewColors.Length],
            PreviewWidth = previewWidth,
            PreviewHeight = previewHeight,
        };
    }

    private async Task<OneDriveAlbumDiscoveryResult?> GetAlbumDiscoveryResultIfNeededAsync(OneDriveConnectionState connectionState)
    {
        if (connectionState.Status != OneDriveConnectionStatus.Connected)
        {
            return null;
        }

        _logger.LogInformation("Loading matching OneDrive albums for account {AccountLabel}.", connectionState.AccountLabel);
        return await _oneDriveAlbumDiscoveryService.GetMatchingAlbumsAsync().ConfigureAwait(false);
    }

    private static string BuildNoMatchingAlbumsMessage() =>
        $"No matching OneDrive albums were found. Create or rename an album to {FormatAlbumNamesForUi()}";

    private static string BuildAlbumDiscoveryFailureMessage(OneDriveAlbumDiscoveryResult result)
    {
        if (Matches(result, "token_unavailable"))
        {
            return "LockPaper needs you to sign in again before it can read your OneDrive albums.";
        }

        if (Matches(result, "Forbidden") || Matches(result, "Unauthorized") || Matches(result, "accessDenied"))
        {
            return "LockPaper couldn't read your OneDrive albums. Check OneDrive access and the Files.Read permission, then sign in again.";
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return TrimErrorMessage(result.ErrorMessage);
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorCode))
        {
            return result.ErrorCode;
        }

        return "LockPaper couldn't read your OneDrive albums. Try again.";
    }

    private static string BuildSignInFailureMessage(OneDriveConnectionOperationResult result)
    {
        if (Matches(result, "redirect_uri") || Matches(result, "AADSTS50011"))
        {
            return "Azure is missing the desktop redirect URI. Add http://localhost under Mobile and desktop applications.";
        }

        if (Matches(result, "AADSTS50020") || Matches(result, "personal Microsoft account"))
        {
            return "This app registration is not set up for personal Microsoft accounts yet. Change Supported account types to include personal accounts.";
        }

        if (Matches(result, "unauthorized_client") || Matches(result, "public client"))
        {
            return "Azure is blocking desktop sign-in for this app. Enable the Mobile and desktop applications platform and allow public client flows.";
        }

        if (Matches(result, "invalid_scope") || Matches(result, "Files.Read"))
        {
            return "The app registration is missing Microsoft Graph delegated permission Files.Read.";
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorCode))
        {
            return $"{result.ErrorCode}: {TrimErrorMessage(result.ErrorMessage)}";
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return TrimErrorMessage(result.ErrorMessage);
        }

        return "LockPaper couldn't finish the OneDrive sign-in. Try again.";
    }

    private static string BuildSignOutFailureMessage(OneDriveConnectionOperationResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorCode))
        {
            return $"{result.ErrorCode}: {TrimErrorMessage(result.ErrorMessage)}";
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return TrimErrorMessage(result.ErrorMessage);
        }

        return "LockPaper couldn't clear the OneDrive session. Try again.";
    }

    private static bool Matches(OneDriveConnectionOperationResult result, string value) =>
        result.ErrorCode.Contains(value, StringComparison.OrdinalIgnoreCase)
        || result.ErrorMessage.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static bool Matches(OneDriveAlbumDiscoveryResult result, string value) =>
        result.ErrorCode.Contains(value, StringComparison.OrdinalIgnoreCase)
        || result.ErrorMessage.Contains(value, StringComparison.OrdinalIgnoreCase);

    private static string TrimErrorMessage(string errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return "The Microsoft sign-in service returned an unknown error.";
        }

        var singleLineMessage = errorMessage
            .ReplaceLineEndings(" ")
            .Trim();
        if (singleLineMessage.Length <= 180)
        {
            return singleLineMessage;
        }

        return $"{singleLineMessage[..177]}...";
    }

    private static string FormatAlbumNamesForUi() =>
        $"'{OneDriveAlbumDiscoveryConstants.MatchingAlbumNames[0]}', '{OneDriveAlbumDiscoveryConstants.MatchingAlbumNames[1]}', or '{OneDriveAlbumDiscoveryConstants.MatchingAlbumNames[2]}'.";

    private static string FormatAccountLabel(string accountLabel) =>
        string.IsNullOrWhiteSpace(accountLabel)
            ? DefaultAccountLabel
            : accountLabel;
}
