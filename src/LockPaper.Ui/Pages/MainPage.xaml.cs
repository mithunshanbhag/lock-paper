using LockPaper.Ui.PageModels;

namespace LockPaper.Ui.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is MainPageModel model)
        {
            await model.InitializeAsync();
        }
    }

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        if (BindingContext is not MainPageModel model)
        {
            return;
        }

        var shouldLogOut = await DisplayAlertAsync(
            "Log out?",
            "This will disconnect LockPaper from OneDrive on this device.",
            "Log out",
            "Cancel");

        if (shouldLogOut)
        {
            await model.LogOutAsync();
        }
    }
}
