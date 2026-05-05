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
    private readonly IPlatformPermissionService _platformPermissionService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IWallpaperRefreshService _wallpaperRefreshService;
    private bool _hasInitialized;
    private bool _hasRequestedPostConnectionPermissions;
    private LockPaperScenario _currentScenario;
    private string _currentWallpaperPreviewFilePath = string.Empty;
    private WallpaperRefreshResult? _lastWallpaperRefreshResult;

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
    private string _displaySummaryHeading = "Current screen";

    [ObservableProperty]
    private string _displaySummaryMetaText = string.Empty;

    [ObservableProperty]
    private string _displaySummaryText = string.Empty;

    [ObservableProperty]
    private bool _showDisplaySummaryText;

    [ObservableProperty]
    private bool _showDisplayPreviewStage;

    [ObservableProperty]
    private bool _isWallpaperRefreshInProgress;

    [ObservableProperty]
    private string _lastAttemptText = string.Empty;

    [ObservableProperty]
    private string _lastAttemptDetailText = string.Empty;

    [ObservableProperty]
    private bool _showLastAttemptDetailText;

    [ObservableProperty]
    private string _lastAttemptStatusText = string.Empty;

    [ObservableProperty]
    private string _nextAttemptText = string.Empty;

    [ObservableProperty]
    private string _nextAttemptStatusText = string.Empty;

    public ObservableCollection<DisplayPreview> DisplayPreviews { get; } = [];

    public event EventHandler? PostConnectionPermissionRequestNeeded;

    public MainPageModel(
        IOneDriveAuthenticationService oneDriveAuthenticationService,
        IOneDriveAlbumDiscoveryService oneDriveAlbumDiscoveryService,
        IDeviceDisplayService deviceDisplayService,
        IWallpaperRefreshService wallpaperRefreshService,
        IUiDispatcher uiDispatcher,
        ILogger<MainPageModel> logger)
        : this(
            oneDriveAuthenticationService,
            oneDriveAlbumDiscoveryService,
            deviceDisplayService,
            wallpaperRefreshService,
            NoOpPlatformPermissionService.Instance,
            uiDispatcher,
            logger)
    {
    }

    public MainPageModel(
        IOneDriveAuthenticationService oneDriveAuthenticationService,
        IOneDriveAlbumDiscoveryService oneDriveAlbumDiscoveryService,
        IDeviceDisplayService deviceDisplayService,
        IWallpaperRefreshService wallpaperRefreshService,
        IPlatformPermissionService platformPermissionService,
        IUiDispatcher uiDispatcher,
        ILogger<MainPageModel> logger)
    {
        _oneDriveAuthenticationService = oneDriveAuthenticationService;
        _oneDriveAlbumDiscoveryService = oneDriveAlbumDiscoveryService;
        _deviceDisplayService = deviceDisplayService;
        _wallpaperRefreshService = wallpaperRefreshService;
        _platformPermissionService = platformPermissionService;
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

    public async Task RequestPostConnectionPermissionsAsync()
    {
        if (_currentScenario != LockPaperScenario.Connected)
        {
            _logger.LogInformation(
                "Skipping post-connection permission request because the current scenario is {Scenario}.",
                _currentScenario);
            return;
        }

        _logger.LogInformation("Requesting post-connection platform permissions.");
        var result = await _platformPermissionService.RequestPostConnectionPermissionsAsync().ConfigureAwait(false);
        _logger.LogInformation(
            "Post-connection permission request completed with status {Status}. Refresh display summary: {ShouldRefreshDisplaySummary}.",
            result.Status,
            result.ShouldRefreshDisplaySummary);

        if (result.ShouldRefreshDisplaySummary)
        {
            await TryRefreshDisplaySummaryAsync(
                refreshWallpaperPreview: true,
                reason: "post-connection-permission-request").ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(result.FeedbackMessage))
        {
            return;
        }

        try
        {
            await _uiDispatcher.DispatchAsync(() =>
            {
                if (_currentScenario == LockPaperScenario.Connected && !ShowFeedback)
                {
                    SetFeedback(result.FeedbackMessage);
                }
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying post-connection permission feedback on the UI thread failed.");
            throw;
        }
    }

    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        if (_currentScenario == LockPaperScenario.Connected)
        {
            await RefreshWallpaperAsync();
            return;
        }

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

        try
        {
            await _uiDispatcher.DispatchAsync(() => HandleSignInResult(result, null));
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

        if (result.State.Status == OneDriveConnectionStatus.Connected)
        {
            await TryRefreshDisplaySummaryAsync(
                refreshWallpaperPreview: true,
                reason: "post-sign-in");

            QueuePostConnectionPermissionRequest();
        }

        _logger.LogInformation("Refreshing OneDrive album discovery after sign-in.");
        await RefreshAlbumDiscoveryAsync(result.State);
    }

    private async Task RefreshConnectionStateAsync()
    {
        _logger.LogInformation("Refreshing cached OneDrive connection state.");
        var connectionState = await _oneDriveAuthenticationService.GetCurrentConnectionStateAsync();
        _logger.LogInformation("Fetched cached OneDrive state {StateStatus} for account {AccountLabel}.", connectionState.Status, connectionState.AccountLabel);

        try
        {
            await _uiDispatcher.DispatchAsync(() => ApplyConnectionState(connectionState));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying cached connection state on the UI thread failed.");
            throw;
        }

        if (connectionState.Status == OneDriveConnectionStatus.Connected)
        {
            await TryRefreshDisplaySummaryAsync(
                refreshWallpaperPreview: true,
                reason: "connected-state-refresh");

            QueuePostConnectionPermissionRequest();
        }

        _logger.LogInformation("Refreshing OneDrive album discovery after loading the cached connection state.");
        await RefreshAlbumDiscoveryAsync(connectionState);
    }

    private void ApplyScenario(LockPaperScenario scenario)
    {
        _logger.LogInformation("Applying UI scenario {Scenario}.", scenario);
        _currentScenario = scenario;
        PrimaryActionText = scenario switch
        {
            LockPaperScenario.SignedOut => "Connect to OneDrive",
            LockPaperScenario.Connecting => "Connecting to OneDrive...",
            LockPaperScenario.ReauthenticationRequired => "Reconnect to OneDrive",
            _ => "Refresh lock screen",
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
            _lastWallpaperRefreshResult = null;
            return;
        }

        AccountStatusText = FormatAccountLabel(connectionState.AccountLabel);
        ApplyAlbumStatus(scenario, albumDiscoveryResult);

        ApplyAttemptStatus(scenario, albumDiscoveryResult);
    }

    private void ApplyAlbumDiscoveryResult(
        OneDriveConnectionState connectionState,
        OneDriveAlbumDiscoveryResult albumDiscoveryResult)
    {
        if (connectionState.Status != OneDriveConnectionStatus.Connected)
        {
            return;
        }

        var expectedAccountLabel = FormatAccountLabel(connectionState.AccountLabel);
        if (!ShowConnectedLayout
            || !string.Equals(AccountStatusText, expectedAccountLabel, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Skipping album discovery update because the visible connection state changed before discovery completed.");
            return;
        }

        LogAlbumDiscoveryResult(
            "Applying album discovery result",
            albumDiscoveryResult);

        ApplyAlbumStatus(LockPaperScenario.Connected, albumDiscoveryResult);
        ApplyAttemptStatus(LockPaperScenario.Connected, albumDiscoveryResult);
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
        DisplaySummaryHeading = "Current screen";
        DisplaySummaryMetaText = string.Empty;
        DisplaySummaryText = string.Empty;
        ShowDisplaySummaryText = false;
        ShowDisplayPreviewStage = false;
        LastAttemptText = string.Empty;
        LastAttemptDetailText = string.Empty;
        ShowLastAttemptDetailText = false;
        LastAttemptStatusText = string.Empty;
        NextAttemptText = string.Empty;
        NextAttemptStatusText = string.Empty;
        DisplayPreviews.Clear();
        IsWallpaperRefreshInProgress = false;
        _currentWallpaperPreviewFilePath = string.Empty;
        _lastWallpaperRefreshResult = null;
        _hasRequestedPostConnectionPermissions = false;
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

    private void ApplyDisplaySummary(
        IReadOnlyList<DeviceDisplayInfo> displays,
        string wallpaperPreviewFilePath)
    {
        _logger.LogInformation("Applying display summary for {DisplayCount} display(s).", displays.Count);

        if (displays.Count == 0)
        {
            DisplaySummaryHeading = "Current screen";
            DisplaySummaryMetaText = "Unavailable";
            DisplaySummaryText = "Display details are not available on this device.";
            ShowDisplaySummaryText = true;
            ShowDisplayPreviewStage = false;
            DisplayPreviews.Clear();
            return;
        }

        DisplaySummaryHeading = displays.Count == 1 ? "Current screen" : "Current displays";
        DisplaySummaryMetaText = displays.Count == 1
            ? $"{displays[0].PixelWidth} x {displays[0].PixelHeight}"
            : $"{displays.Count} displays";
        DisplaySummaryText = string.Empty;
        ShowDisplaySummaryText = false;
        ShowDisplayPreviewStage = true;

        ReplaceDisplayPreviews(displays.Select((display, index) => BuildDisplayPreview(display, index, wallpaperPreviewFilePath)));
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

        if (albumDiscoveryResult is null)
        {
            AlbumStatusText = "Checking matching albums...";
            return;
        }

        switch (albumDiscoveryResult.Status)
        {
            case OneDriveAlbumDiscoveryStatus.Found:
                AlbumStatusText = albumDiscoveryResult.MatchingAlbumNames.Count == 1
                    ? "1 matching album is ready."
                    : $"{albumDiscoveryResult.MatchingAlbumNames.Count} matching albums are ready.";
                break;
            case OneDriveAlbumDiscoveryStatus.NotFound:
                AlbumStatusText = "No matching albums found.";
                SetFeedback(BuildNoMatchingAlbumsMessage());
                break;
            case OneDriveAlbumDiscoveryStatus.Failed:
                AlbumStatusText = "Couldn't check matching albums.";
                SetFeedback(BuildAlbumDiscoveryFailureMessage(albumDiscoveryResult));
                break;
        }
    }

    private void ApplyAttemptStatus(
        LockPaperScenario scenario,
        OneDriveAlbumDiscoveryResult? albumDiscoveryResult)
    {
        if (scenario == LockPaperScenario.ReauthenticationRequired)
        {
            SetActivityStatus(
                lastAttemptText: "Reconnect needed",
                lastAttemptDetailText: "OneDrive needs another sign-in",
                lastAttemptStatusText: "Paused",
                nextAttemptText: "Sign in again",
                nextAttemptStatusText: "Then refresh");
            return;
        }

        if (scenario == LockPaperScenario.Connected && albumDiscoveryResult is not null)
        {
            switch (albumDiscoveryResult.Status)
            {
                case OneDriveAlbumDiscoveryStatus.NotFound:
                    SetActivityStatus(
                        lastAttemptText: "No matching album",
                        lastAttemptDetailText: "Wallpaper changes are paused",
                        lastAttemptStatusText: "Paused",
                        nextAttemptText: "Add a matching album",
                        nextAttemptStatusText: "Then refresh");
                    return;
                case OneDriveAlbumDiscoveryStatus.Failed:
                    SetActivityStatus(
                        lastAttemptText: "Album check failed",
                        lastAttemptDetailText: "LockPaper couldn't verify OneDrive yet",
                        lastAttemptStatusText: "Check",
                        nextAttemptText: "Fix OneDrive access",
                        nextAttemptStatusText: "Then refresh");
                    return;
            }
        }

        if (_lastWallpaperRefreshResult is not null)
        {
            ApplyWallpaperRefreshAttemptStatus(_lastWallpaperRefreshResult);
            return;
        }

        SetActivityStatus(
            lastAttemptText: "No update yet",
            lastAttemptDetailText: "Ready when you are",
            lastAttemptStatusText: "Ready",
            nextAttemptText: "Manual only for now",
            nextAttemptStatusText: "Hourly later");
    }

    private void ApplyWallpaperRefreshAttemptStatus(WallpaperRefreshResult result)
    {
        var attemptedAtText = FormatAttemptTime(result.AttemptedAtLocal);

        switch (result.Status)
        {
            case WallpaperRefreshStatus.Succeeded:
                SetActivityStatus(
                    lastAttemptText: attemptedAtText,
                    lastAttemptDetailText: BuildSuccessfulRefreshDetail(result.AlbumName),
                    lastAttemptStatusText: "Success",
                    nextAttemptText: "Manual only for now",
                    nextAttemptStatusText: "Hourly later");
                break;
            case WallpaperRefreshStatus.NoMatchingAlbums:
                SetActivityStatus(
                    lastAttemptText: attemptedAtText,
                    lastAttemptDetailText: "No matching album",
                    lastAttemptStatusText: "Paused",
                    nextAttemptText: "Add a matching album",
                    nextAttemptStatusText: "Then refresh");
                break;
            case WallpaperRefreshStatus.NoEligiblePhotos:
                SetActivityStatus(
                    lastAttemptText: attemptedAtText,
                    lastAttemptDetailText: "No usable photos",
                    lastAttemptStatusText: "Paused",
                    nextAttemptText: "Add usable photos",
                    nextAttemptStatusText: "Then refresh");
                break;
            case WallpaperRefreshStatus.ReauthenticationRequired:
                SetActivityStatus(
                    lastAttemptText: "Reconnect needed",
                    lastAttemptDetailText: "OneDrive needs another sign-in",
                    lastAttemptStatusText: "Paused",
                    nextAttemptText: "Sign in again",
                    nextAttemptStatusText: "Then refresh");
                break;
            default:
                SetActivityStatus(
                    lastAttemptText: attemptedAtText,
                    lastAttemptDetailText: "Refresh failed",
                    lastAttemptStatusText: "Error",
                    nextAttemptText: "Manual only for now",
                    nextAttemptStatusText: "Try again");
                break;
        }
    }

    private void SetActivityStatus(
        string lastAttemptText,
        string lastAttemptDetailText,
        string lastAttemptStatusText,
        string nextAttemptText,
        string nextAttemptStatusText)
    {
        LastAttemptText = lastAttemptText;
        LastAttemptDetailText = lastAttemptDetailText;
        ShowLastAttemptDetailText = !string.IsNullOrWhiteSpace(lastAttemptDetailText);
        LastAttemptStatusText = lastAttemptStatusText;
        NextAttemptText = nextAttemptText;
        NextAttemptStatusText = nextAttemptStatusText;
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

    private static DisplayPreview BuildDisplayPreview(
        DeviceDisplayInfo display,
        int index,
        string wallpaperPreviewFilePath)
    {
        var largestDimension = Math.Max(display.PixelWidth, display.PixelHeight);
        var scale = largestDimension == 0 ? 1d : 180d / largestDimension;
        var previewWidth = Math.Clamp(Math.Round(display.PixelWidth * scale, 0), 84d, 180d);
        var previewHeight = Math.Clamp(Math.Round(display.PixelHeight * scale, 0), 72d, 180d);

        return new DisplayPreview
        {
            ResolutionText = $"{display.PixelWidth} x {display.PixelHeight}",
            PreviewColor = DisplayPreviewColors[index % DisplayPreviewColors.Length],
            WallpaperThumbnailPath = wallpaperPreviewFilePath,
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
        try
        {
            var result = await _oneDriveAlbumDiscoveryService.GetMatchingAlbumsAsync().ConfigureAwait(false);
            LogAlbumDiscoveryResult("Album discovery completed", result);
            return result;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Loading matching OneDrive albums failed unexpectedly.");
            return OneDriveAlbumDiscoveryResult.Failed(exception.GetType().Name, "LockPaper couldn't confirm your OneDrive albums yet.");
        }
    }

    private async Task RefreshAlbumDiscoveryAsync(OneDriveConnectionState connectionState)
    {
        var albumDiscoveryResult = await GetAlbumDiscoveryResultIfNeededAsync(connectionState);
        if (albumDiscoveryResult is null)
        {
            return;
        }

        try
        {
            await _uiDispatcher.DispatchAsync(() => ApplyAlbumDiscoveryResult(connectionState, albumDiscoveryResult));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying OneDrive album discovery results on the UI thread failed.");
            throw;
        }
    }

    private async Task RefreshWallpaperAsync()
    {
        _logger.LogInformation("Primary action invoked. Starting manual wallpaper refresh.");

        try
        {
            await _uiDispatcher.DispatchAsync(BeginWallpaperRefresh);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Preparing the wallpaper refresh state on the UI thread failed.");
            throw;
        }

        var result = await _wallpaperRefreshService.RefreshAsync().ConfigureAwait(false);
        _logger.LogInformation(
            "Wallpaper refresh completed with status {Status}. Matching album count: {MatchingAlbumCount}. Album: {AlbumName}. Photo: {PhotoName}.",
            result.Status,
            result.MatchingAlbumCount,
            result.AlbumName,
            result.PhotoName);

        try
        {
            await _uiDispatcher.DispatchAsync(() => HandleWallpaperRefreshResult(result));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying wallpaper refresh results on the UI thread failed.");
            throw;
        }

        if (result.Status == WallpaperRefreshStatus.Succeeded)
        {
            await TryRefreshDisplaySummaryAsync(
                refreshWallpaperPreview: false,
                reason: "post-wallpaper-refresh");
        }
    }

    private void BeginWallpaperRefresh()
    {
        IsWallpaperRefreshInProgress = true;
        IsPrimaryActionEnabled = false;
        PrimaryActionText = "Refreshing lock screen...";
        ClearFeedback();
    }

    private void HandleWallpaperRefreshResult(WallpaperRefreshResult result)
    {
        _logger.LogInformation(
            "Handling wallpaper refresh result {Status}. Matching album count: {MatchingAlbumCount}. Applied wallpaper path: {AppliedWallpaperFilePath}.",
            result.Status,
            result.MatchingAlbumCount,
            result.AppliedWallpaperFilePath);

        _lastWallpaperRefreshResult = result;
        IsWallpaperRefreshInProgress = false;

        switch (result.Status)
        {
            case WallpaperRefreshStatus.Succeeded:
                _currentWallpaperPreviewFilePath = result.AppliedWallpaperFilePath;
                ApplyScenario(LockPaperScenario.Connected);
                AlbumStatusText = FormatMatchingAlbumCount(result.MatchingAlbumCount);
                ApplyWallpaperRefreshAttemptStatus(result);
                ClearFeedback();
                break;
            case WallpaperRefreshStatus.NoMatchingAlbums:
                ApplyScenario(LockPaperScenario.Connected);
                AlbumStatusText = "No matching albums found.";
                ApplyWallpaperRefreshAttemptStatus(result);
                SetFeedback(BuildNoMatchingAlbumsMessage());
                break;
            case WallpaperRefreshStatus.NoEligiblePhotos:
                ApplyScenario(LockPaperScenario.Connected);
                AlbumStatusText = BuildNoEligiblePhotosAlbumStatus(result.MatchingAlbumCount);
                ApplyWallpaperRefreshAttemptStatus(result);
                SetFeedback(BuildNoEligiblePhotosMessage());
                break;
            case WallpaperRefreshStatus.ReauthenticationRequired:
                ApplyScenario(LockPaperScenario.ReauthenticationRequired);
                ApplyWallpaperRefreshAttemptStatus(result);
                SetFeedback(BuildWallpaperRefreshFailureMessage(result));
                break;
            default:
                ApplyScenario(LockPaperScenario.Connected);
                ApplyWallpaperRefreshAttemptStatus(result);
                SetFeedback(BuildWallpaperRefreshFailureMessage(result));
                break;
        }
    }

    private async Task RefreshDisplaySummaryAsync(bool refreshWallpaperPreview)
    {
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

        if (refreshWallpaperPreview)
        {
            try
            {
                _currentWallpaperPreviewFilePath =
                    await _wallpaperRefreshService.GetCurrentWallpaperPreviewFilePathAsync().ConfigureAwait(false)
                    ?? string.Empty;

                _logger.LogInformation(
                    "Resolved wallpaper preview path for the display summary. Has preview: {HasPreview}. Path: {WallpaperPreviewFilePath}.",
                    !string.IsNullOrWhiteSpace(_currentWallpaperPreviewFilePath),
                    _currentWallpaperPreviewFilePath);
            }
            catch (Exception exception) when (
                exception is IOException
                or InvalidOperationException
                or PlatformNotSupportedException
                or UnauthorizedAccessException)
            {
                _logger.LogWarning(exception, "Reading the current lock-screen wallpaper preview failed.");
                _currentWallpaperPreviewFilePath = string.Empty;
            }
        }

        try
        {
            await _uiDispatcher.DispatchAsync(() => ApplyDisplaySummary(displays, _currentWallpaperPreviewFilePath));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Applying the display summary on the UI thread failed.");
            throw;
        }
    }

    private async Task TryRefreshDisplaySummaryAsync(bool refreshWallpaperPreview, string reason)
    {
        _logger.LogInformation(
            "Refreshing display summary. Reason: {Reason}. Refresh wallpaper preview: {RefreshWallpaperPreview}.",
            reason,
            refreshWallpaperPreview);

        try
        {
            await RefreshDisplaySummaryAsync(refreshWallpaperPreview).ConfigureAwait(false);
            _logger.LogInformation("Display summary refresh completed. Reason: {Reason}.", reason);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Display summary refresh failed. Reason: {Reason}. LockPaper will continue without updating the display preview.",
                reason);
        }
    }

    private void QueuePostConnectionPermissionRequest()
    {
        if (_hasRequestedPostConnectionPermissions)
        {
            _logger.LogInformation("Skipping duplicate post-connection permission request notification.");
            return;
        }

        _hasRequestedPostConnectionPermissions = true;
        _logger.LogInformation("Queued a post-connection permission request notification.");
        PostConnectionPermissionRequestNeeded?.Invoke(this, EventArgs.Empty);
    }

    private static string BuildNoMatchingAlbumsMessage() =>
        $"No matching OneDrive albums were found. Create or rename an album to {FormatAlbumNamesForUi()}";

    private static string BuildNoEligiblePhotosMessage() =>
        "LockPaper found matching albums, but none of them contained usable photos for the lock screen. On Windows, use JPG, JPEG, PNG, or BMP images.";

    private void LogAlbumDiscoveryResult(string messagePrefix, OneDriveAlbumDiscoveryResult result)
    {
        if (string.IsNullOrWhiteSpace(result.ErrorCode))
        {
            _logger.LogInformation(
                "{MessagePrefix} with status {Status}. Matching album count: {MatchingAlbumCount}.",
                messagePrefix,
                result.Status,
                result.MatchingAlbumNames.Count);
            return;
        }

        _logger.LogInformation(
            "{MessagePrefix} with status {Status}. Matching album count: {MatchingAlbumCount}. Error code: {ErrorCode}.",
            messagePrefix,
            result.Status,
            result.MatchingAlbumNames.Count,
            result.ErrorCode);
    }

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

    private static string BuildWallpaperRefreshFailureMessage(WallpaperRefreshResult result)
    {
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            return TrimErrorMessage(result.ErrorMessage);
        }

        return "LockPaper couldn't refresh the lock-screen wallpaper. Try again.";
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

    private static string FormatAttemptTime(DateTimeOffset attemptedAtLocal) =>
        attemptedAtLocal.ToString("t");

    private static string FormatMatchingAlbumCount(int matchingAlbumCount) =>
        matchingAlbumCount == 1
            ? "1 matching album is ready."
            : $"{matchingAlbumCount} matching albums are ready.";

    private static string BuildNoEligiblePhotosAlbumStatus(int matchingAlbumCount) =>
        matchingAlbumCount <= 1
            ? "1 matching album was found, but it has no usable photos."
            : $"{matchingAlbumCount} matching albums were found, but none of them have usable photos.";

    private static string BuildSuccessfulRefreshDetail(string albumName) =>
        string.Empty;

    private static string FormatAccountLabel(string accountLabel) =>
        string.IsNullOrWhiteSpace(accountLabel)
            ? DefaultAccountLabel
            : accountLabel;

    private sealed class NoOpPlatformPermissionService : IPlatformPermissionService
    {
        public static NoOpPlatformPermissionService Instance { get; } = new();

        public Task<PlatformPermissionRequestResult> RequestPostConnectionPermissionsAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(PlatformPermissionRequestResult.NotRequired());
    }
}
