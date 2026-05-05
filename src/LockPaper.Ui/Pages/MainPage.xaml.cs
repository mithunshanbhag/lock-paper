using LockPaper.Ui.PageModels;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui.Pages;

public partial class MainPage : ContentPage
{
    private const int PostConnectionPermissionRequestDelayMilliseconds = 200;

    private readonly ILogger<MainPage> _logger;

    public MainPage(MainPageModel model, ILogger<MainPage> logger)
    {
        InitializeComponent();
        BindingContext = model;
        _logger = logger;
        model.PostConnectionPermissionRequestNeeded += OnPostConnectionPermissionRequestNeeded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageModel model)
        {
            try
            {
                _logger.LogInformation("Main page appearing. Initializing page model.");
                await model.InitializeAsync();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Main page initialization failed during OnAppearing.");
                throw;
            }
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not MainPageModel model)
        {
            return;
        }

        bool shouldLogOut;
        try
        {
            shouldLogOut = await DisplayAlertAsync(
                "Log out?",
                "This will disconnect LockPaper from OneDrive on this device.",
                "Log out",
                "Cancel");
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Displaying the logout confirmation failed.");
            throw;
        }

        if (shouldLogOut)
        {
            try
            {
                _logger.LogInformation("Logout confirmed from the main page.");
                await model.LogOutAsync();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Logging out from the main page failed.");
                throw;
            }
        }
    }

    private async void OnPostConnectionPermissionRequestNeeded(object? sender, EventArgs e)
    {
        if (sender is not MainPageModel model)
        {
            return;
        }

        var completionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            _logger.LogInformation(
                "Waiting for the connected layout to render before requesting platform permissions.");

            Dispatcher.DispatchDelayed(
                TimeSpan.FromMilliseconds(PostConnectionPermissionRequestDelayMilliseconds),
                async () =>
                {
                    try
                    {
                        await model.RequestPostConnectionPermissionsAsync();
                        completionSource.SetResult();
                    }
                    catch (Exception exception)
                    {
                        completionSource.SetException(exception);
                    }
                });

            await completionSource.Task;
        }
        catch (Exception exception)
        {
            _logger.LogCritical(exception, "Requesting platform permissions from the main page failed.");
            throw;
        }
    }
}
