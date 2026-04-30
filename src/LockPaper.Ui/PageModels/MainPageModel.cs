using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockPaper.Ui.Models;
using LockPaper.Ui.Services.Interfaces;

namespace LockPaper.Ui.PageModels;

public partial class MainPageModel : ObservableObject
{
    private const string DefaultAccountLabel = "Personal Microsoft account";
    private const string AccountStatusLabel = "Microsoft account";
    private const string SessionStatusLabel = "Session";

    private readonly IOneDriveAuthenticationService _oneDriveAuthenticationService;
    private bool _hasInitialized;

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
    private string _primaryStatusLabel = string.Empty;

    [ObservableProperty]
    private string _primaryStatusText = string.Empty;

    [ObservableProperty]
    private string _secondaryStatusLabel = string.Empty;

    [ObservableProperty]
    private string _secondaryStatusText = string.Empty;

    public MainPageModel(IOneDriveAuthenticationService oneDriveAuthenticationService)
    {
        _oneDriveAuthenticationService = oneDriveAuthenticationService;
        ApplyScenario(LockPaperScenario.SignedOut);
    }

    public async Task InitializeAsync()
    {
        if (_hasInitialized)
        {
            return;
        }

        _hasInitialized = true;
        await RefreshConnectionStateAsync();
    }

    public async Task LogOutAsync()
    {
        var result = await _oneDriveAuthenticationService.SignOutAsync();
        HandleSignOutResult(result);
    }

    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        ApplyScenario(LockPaperScenario.Connecting);

        var result = await _oneDriveAuthenticationService.SignInAsync();
        HandleSignInResult(result);
    }

    private async Task RefreshConnectionStateAsync()
    {
        var connectionState = await _oneDriveAuthenticationService.GetCurrentConnectionStateAsync();
        ApplyConnectionState(connectionState);
    }

    private void ApplyScenario(LockPaperScenario scenario)
    {
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

    private void ApplyConnectionState(OneDriveConnectionState connectionState)
    {
        var scenario = connectionState.Status switch
        {
            OneDriveConnectionStatus.SignedOut => LockPaperScenario.SignedOut,
            OneDriveConnectionStatus.ReauthenticationRequired => LockPaperScenario.ReauthenticationRequired,
            _ => LockPaperScenario.Connected,
        };

        ApplyScenario(scenario);

        if (scenario == LockPaperScenario.SignedOut)
        {
            return;
        }

        PrimaryStatusLabel = AccountStatusLabel;
        PrimaryStatusText = FormatAccountLabel(connectionState.AccountLabel);
        SecondaryStatusLabel = SessionStatusLabel;
        SecondaryStatusText = scenario == LockPaperScenario.ReauthenticationRequired
            ? "Needs sign-in"
            : "Connected";
    }

    private void HandleSignInResult(OneDriveConnectionOperationResult result)
    {
        ApplyConnectionState(result.State);

        switch (result.Status)
        {
            case OneDriveConnectionOperationStatus.Succeeded:
                ClearFeedback();
                break;
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
        PrimaryStatusLabel = string.Empty;
        PrimaryStatusText = string.Empty;
        SecondaryStatusLabel = string.Empty;
        SecondaryStatusText = string.Empty;
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

    private static string FormatAccountLabel(string accountLabel) =>
        string.IsNullOrWhiteSpace(accountLabel)
            ? DefaultAccountLabel
            : accountLabel;
}
