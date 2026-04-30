using LockPaper.Ui.PageModels;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui.Pages;

public partial class MainPage : ContentPage
{
    private readonly ILogger<MainPage> _logger;

    public MainPage(MainPageModel model, ILogger<MainPage> logger)
    {
        InitializeComponent();
        BindingContext = model;
        _logger = logger;
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
}
