using LockPaper.Ui.Constants;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui.Services.Implementations;

public sealed class OneDriveAuthenticationService(ILogger<OneDriveAuthenticationService> logger) : IOneDriveAuthenticationService
{
    private readonly IPublicClientApplication _publicClientApplication = CreatePublicClientApplication();

    public async Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default)
    {
        var account = await GetPrimaryAccountAsync().ConfigureAwait(false);
        if (account is null)
        {
            return OneDriveConnectionState.CreateSignedOut();
        }

        try
        {
            var authenticationResult = await _publicClientApplication
                .AcquireTokenSilent(OneDriveAuthenticationConstants.Scopes, account)
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return OneDriveConnectionState.CreateConnected(GetAccountLabel(authenticationResult.Account ?? account));
        }
        catch (MsalUiRequiredException exception)
        {
            logger.LogInformation(exception, "OneDrive connection needs reauthentication.");
            return OneDriveConnectionState.CreateReauthenticationRequired(GetAccountLabel(account));
        }
        catch (MsalException exception)
        {
            logger.LogWarning(exception, "LockPaper could not validate the cached OneDrive session.");
            return OneDriveConnectionState.CreateReauthenticationRequired(GetAccountLabel(account));
        }
    }

    public async Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var authenticationRequest = _publicClientApplication
                .AcquireTokenInteractive(OneDriveAuthenticationConstants.Scopes)
                .WithPrompt(Prompt.SelectAccount);

#if ANDROID
            authenticationRequest = authenticationRequest
                .WithUseEmbeddedWebView(false)
                .WithParentActivityOrWindow(GetParentActivity());
#elif WINDOWS
            authenticationRequest = authenticationRequest
                .WithUseEmbeddedWebView(false)
                .WithParentActivityOrWindow(GetParentWindowHandle())
                .WithSystemWebViewOptions(CreateSystemWebViewOptions());
#else
            authenticationRequest = authenticationRequest.WithUseEmbeddedWebView(false);
#endif

            var authenticationResult = await authenticationRequest
                .ExecuteAsync(cancellationToken)
                .ConfigureAwait(false);

            return OneDriveConnectionOperationResult.Succeeded(
                OneDriveConnectionState.CreateConnected(GetAccountLabel(authenticationResult.Account)));
        }
        catch (MsalClientException exception) when (exception.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            logger.LogInformation(exception, "OneDrive sign-in was canceled.");
            return OneDriveConnectionOperationResult.Cancelled(
                await GetCurrentConnectionStateAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (MsalException exception)
        {
            logger.LogWarning(exception, "OneDrive sign-in failed.");
            return OneDriveConnectionOperationResult.Failed(
                await GetCurrentConnectionStateAsync(cancellationToken).ConfigureAwait(false),
                exception.ErrorCode,
                exception.Message);
        }
    }

    public async Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var accounts = await _publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _publicClientApplication.RemoveAsync(account).ConfigureAwait(false);
            }

            return OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut());
        }
        catch (MsalException exception)
        {
            logger.LogWarning(exception, "OneDrive sign-out failed.");
            return OneDriveConnectionOperationResult.Failed(
                await GetCurrentConnectionStateAsync(cancellationToken).ConfigureAwait(false),
                exception.ErrorCode,
                exception.Message);
        }
    }

    private async Task<IAccount?> GetPrimaryAccountAsync()
    {
        var accounts = await _publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
        return accounts.FirstOrDefault();
    }

    private static string GetAccountLabel(IAccount account) =>
        string.IsNullOrWhiteSpace(account.Username)
            ? string.Empty
            : account.Username;

    private static IPublicClientApplication CreatePublicClientApplication()
    {
        var builder = PublicClientApplicationBuilder
            .Create(OneDriveAuthenticationConstants.ClientId)
            .WithAuthority(OneDriveAuthenticationConstants.Authority);

#if ANDROID
        builder = builder.WithRedirectUri(OneDriveAuthenticationConstants.AndroidRedirectUri);
#elif WINDOWS
        builder = builder.WithRedirectUri(OneDriveAuthenticationConstants.WindowsRedirectUri);
#else
        builder = builder.WithDefaultRedirectUri();
#endif

        return builder.Build();
    }

#if ANDROID
    private static object GetParentActivity() =>
        Microsoft.Maui.ApplicationModel.Platform.CurrentActivity
        ?? throw new InvalidOperationException("LockPaper needs an active Android activity for OneDrive sign-in.");
#endif

#if WINDOWS
    private static SystemWebViewOptions CreateSystemWebViewOptions() =>
        new()
        {
            HtmlMessageSuccess = """
                <html>
                  <head>
                    <meta charset="utf-8" />
                    <title>Return to LockPaper</title>
                    <style>
                      body {
                        font-family: Segoe UI, sans-serif;
                        margin: 40px;
                        color: #13203A;
                      }

                      h1 {
                        font-size: 28px;
                        margin-bottom: 12px;
                      }

                      p {
                        font-size: 18px;
                        line-height: 1.5;
                        max-width: 560px;
                      }
                    </style>
                  </head>
                  <body>
                    <h1>Return to LockPaper</h1>
                    <p>Microsoft sign-in is complete. LockPaper is still finishing the connection, so return to the app and wait a moment.</p>
                  </body>
                </html>
                """,
        };

    private static nint GetParentWindowHandle()
    {
        var mauiWindow = Application.Current?.Windows.FirstOrDefault()
            ?? throw new InvalidOperationException("LockPaper needs an active window for OneDrive sign-in.");
        var platformWindow = mauiWindow.Handler?.PlatformView as Microsoft.UI.Xaml.Window
            ?? throw new InvalidOperationException("LockPaper could not access the WinUI window for OneDrive sign-in.");

        return WinRT.Interop.WindowNative.GetWindowHandle(platformWindow);
    }
#endif
}
