using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LockPaper.Ui.Models;

namespace LockPaper.Ui.PageModels;

public partial class MainPageModel : ObservableObject
{
    private const string ConnectedPlaceholderState = "Connected";
    private const string AlbumMissingPlaceholderState = "Album missing";
    private const string AlbumEmptyPlaceholderState = "Album empty";
    private const string LastAttemptFailedPlaceholderState = "Last attempt failed";

    private bool _isUpdatingPlaceholderSelection;
    private LockPaperScenario _placeholderScenario = LockPaperScenario.Connected;

    public IReadOnlyList<string> PlaceholderStates { get; } =
    [
        ConnectedPlaceholderState,
        AlbumMissingPlaceholderState,
        AlbumEmptyPlaceholderState,
        LastAttemptFailedPlaceholderState,
    ];

    [ObservableProperty]
    private bool _isLogoutVisible;

    [ObservableProperty]
    private string _primaryActionText = "Connect to OneDrive";

    [ObservableProperty]
    private bool _isPrimaryActionEnabled = true;

    [ObservableProperty]
    private bool _showNotice;

    [ObservableProperty]
    private string _noticeTitle = string.Empty;

    [ObservableProperty]
    private string _noticeMessage = string.Empty;

    [ObservableProperty]
    private bool _showStatusSummary;

    [ObservableProperty]
    private string _lastAttemptText = string.Empty;

    [ObservableProperty]
    private string _nextAttemptText = string.Empty;

    [ObservableProperty]
    private bool _showPlaceholderControls;

    [ObservableProperty]
    private string _selectedPlaceholderState = ConnectedPlaceholderState;

    public MainPageModel()
    {
        ApplyScenario(LockPaperScenario.SignedOut);
    }

    partial void OnSelectedPlaceholderStateChanged(string value)
    {
        if (_isUpdatingPlaceholderSelection)
        {
            return;
        }

        _placeholderScenario = ParsePlaceholderScenario(value);

        if (ShowPlaceholderControls)
        {
            ApplyScenario(_placeholderScenario);
        }
    }

    public void LogOut()
    {
        SetPlaceholderState(ConnectedPlaceholderState);
        ApplyScenario(LockPaperScenario.SignedOut);
    }

    [RelayCommand]
    private async Task PrimaryActionAsync()
    {
        if (!ShowPlaceholderControls)
        {
            ApplyScenario(_placeholderScenario);
            return;
        }

        await SimulateWallpaperChangeAsync();
    }

    private async Task SimulateWallpaperChangeAsync()
    {
        ApplyScenario(LockPaperScenario.ChangeInProgress);
        await Task.Delay(900);
        ApplyScenario(_placeholderScenario);
    }

    private void ApplyScenario(LockPaperScenario scenario)
    {
        var now = DateTime.Now;

        IsLogoutVisible = scenario != LockPaperScenario.SignedOut;
        PrimaryActionText = scenario switch
        {
            LockPaperScenario.SignedOut => "Connect to OneDrive",
            LockPaperScenario.ChangeInProgress => "Changing now...",
            _ => "Change now",
        };
        IsPrimaryActionEnabled = scenario != LockPaperScenario.ChangeInProgress;
        ShowStatusSummary = scenario != LockPaperScenario.SignedOut;
        ShowPlaceholderControls = scenario != LockPaperScenario.SignedOut;

        switch (scenario)
        {
            case LockPaperScenario.SignedOut:
                ShowNotice = false;
                NoticeTitle = string.Empty;
                NoticeMessage = string.Empty;
                LastAttemptText = string.Empty;
                NextAttemptText = string.Empty;
                break;
            case LockPaperScenario.Connected:
                ShowNotice = false;
                NoticeTitle = string.Empty;
                NoticeMessage = string.Empty;
                LastAttemptText = $"✓ Today, {now:HH:mm}";
                NextAttemptText = $"Around {GetNextScheduledAttempt(now):HH:mm}";
                break;
            case LockPaperScenario.AlbumMissing:
                ShowNotice = true;
                NoticeTitle = "Album not found";
                NoticeMessage = "Create or rename a OneDrive album to lockpaper, lock-paper, or lock paper.";
                LastAttemptText = $"! Missing album at {now:HH:mm}";
                NextAttemptText = "After the album is available";
                break;
            case LockPaperScenario.AlbumEmpty:
                ShowNotice = true;
                NoticeTitle = "No usable photos yet";
                NoticeMessage = "Add a few image files to the album before trying again.";
                LastAttemptText = $"! No eligible photos at {now:HH:mm}";
                NextAttemptText = "After usable photos are added";
                break;
            case LockPaperScenario.LastAttemptFailed:
                ShowNotice = true;
                NoticeTitle = "Last change failed";
                NoticeMessage = "LockPaper couldn't reach OneDrive. You can retry whenever you're ready.";
                LastAttemptText = $"! Retry failed at {now:HH:mm}";
                NextAttemptText = $"Best effort around {GetNextScheduledAttempt(now):HH:mm}";
                break;
            case LockPaperScenario.ChangeInProgress:
                ShowNotice = true;
                NoticeTitle = "Changing wallpaper";
                NoticeMessage = "LockPaper is picking a photo and updating your lock screen.";
                LastAttemptText = "... Running now";
                NextAttemptText = "Updating after this run finishes";
                break;
        }
    }

    private static DateTime GetNextScheduledAttempt(DateTime now)
    {
        var topOfHour = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, now.Kind);
        return topOfHour.AddHours(1);
    }

    private void SetPlaceholderState(string placeholderState)
    {
        _isUpdatingPlaceholderSelection = true;
        SelectedPlaceholderState = placeholderState;
        _isUpdatingPlaceholderSelection = false;
        _placeholderScenario = ParsePlaceholderScenario(placeholderState);
    }

    private static LockPaperScenario ParsePlaceholderScenario(string value) =>
        value switch
        {
            AlbumMissingPlaceholderState => LockPaperScenario.AlbumMissing,
            AlbumEmptyPlaceholderState => LockPaperScenario.AlbumEmpty,
            LastAttemptFailedPlaceholderState => LockPaperScenario.LastAttemptFailed,
            _ => LockPaperScenario.Connected,
        };
}
