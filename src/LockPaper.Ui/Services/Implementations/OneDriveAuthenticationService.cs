using LockPaper.Ui.Constants;
using LockPaper.Ui.Services.Interfaces;
using Microsoft.Identity.Client;
using Microsoft.Extensions.Logging;

namespace LockPaper.Ui.Services.Implementations;

public sealed class OneDriveAuthenticationService : IOneDriveAuthenticationService
{
    private readonly ILogger<OneDriveAuthenticationService> _logger;
    private readonly IOneDriveTokenCacheStore _tokenCacheStore;
    private readonly IPublicClientApplication _publicClientApplication;

    public OneDriveAuthenticationService(
        ILogger<OneDriveAuthenticationService> logger,
        IOneDriveTokenCacheStore tokenCacheStore)
    {
        _logger = logger;
        _tokenCacheStore = tokenCacheStore;
        _publicClientApplication = CreatePublicClientApplication();
        EnablePersistentTokenCache(_publicClientApplication.UserTokenCache);
    }

    public async Task<OneDriveConnectionState> GetCurrentConnectionStateAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Checking current OneDrive connection state.");
        var account = await GetPrimaryAccountAsync().ConfigureAwait(false);
        if (account is null)
        {
            _logger.LogInformation("No cached OneDrive account found.");
            return OneDriveConnectionState.CreateSignedOut();
        }

        try
        {
            var authenticationResult = await AcquireTokenSilentAsync(account, cancellationToken).ConfigureAwait(false);

            var accountLabel = GetAccountLabel(authenticationResult.Account ?? account);
            _logger.LogInformation("Cached OneDrive session is valid for account {AccountLabel}.", accountLabel);
            return OneDriveConnectionState.CreateConnected(accountLabel);
        }
        catch (MsalUiRequiredException exception)
        {
            _logger.LogInformation(exception, "OneDrive connection needs reauthentication.");
            return OneDriveConnectionState.CreateReauthenticationRequired(GetAccountLabel(account));
        }
        catch (MsalException exception)
        {
            _logger.LogWarning(exception, "LockPaper could not validate the cached OneDrive session.");
            return OneDriveConnectionState.CreateReauthenticationRequired(GetAccountLabel(account));
        }
    }

    public async Task<OneDriveConnectionOperationResult> SignInAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting interactive OneDrive sign-in.");
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

            _logger.LogInformation("Interactive OneDrive sign-in succeeded for account {AccountLabel}.", GetAccountLabel(authenticationResult.Account));
            return OneDriveConnectionOperationResult.Succeeded(
                OneDriveConnectionState.CreateConnected(GetAccountLabel(authenticationResult.Account)));
        }
        catch (MsalClientException exception) when (exception.ErrorCode == MsalError.AuthenticationCanceledError)
        {
            _logger.LogInformation(exception, "OneDrive sign-in was canceled.");
            return OneDriveConnectionOperationResult.Cancelled(
                await GetCurrentConnectionStateAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (MsalException exception)
        {
            _logger.LogWarning(exception, "OneDrive sign-in failed.");
            return OneDriveConnectionOperationResult.Failed(
                await GetCurrentConnectionStateAsync(cancellationToken).ConfigureAwait(false),
                exception.ErrorCode,
                exception.Message);
        }
    }

    public async Task<string> GetMicrosoftGraphAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Acquiring a Microsoft Graph access token for OneDrive album discovery.");
        var account = await GetPrimaryAccountAsync().ConfigureAwait(false)
            ?? throw new InvalidOperationException("LockPaper needs a signed-in Microsoft account before it can read your OneDrive albums.");

        try
        {
            var authenticationResult = await AcquireTokenSilentAsync(account, cancellationToken).ConfigureAwait(false);
            return authenticationResult.AccessToken;
        }
        catch (MsalUiRequiredException exception)
        {
            _logger.LogInformation(exception, "OneDrive album discovery needs the user to sign in again.");
            throw new InvalidOperationException("LockPaper needs you to sign in again before it can read your OneDrive albums.", exception);
        }
        catch (MsalException exception)
        {
            _logger.LogWarning(exception, "LockPaper could not acquire a Microsoft Graph access token for album discovery.");
            throw new InvalidOperationException("LockPaper couldn't get a valid OneDrive access token for album discovery.", exception);
        }
    }

    public async Task<OneDriveConnectionOperationResult> SignOutAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting OneDrive sign-out.");
        try
        {
            var accounts = await _publicClientApplication.GetAccountsAsync().ConfigureAwait(false);
            foreach (var account in accounts)
            {
                await _publicClientApplication.RemoveAsync(account).ConfigureAwait(false);
            }

            await _tokenCacheStore.ClearAsync(cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("OneDrive sign-out completed.");
            return OneDriveConnectionOperationResult.Succeeded(OneDriveConnectionState.CreateSignedOut());
        }
        catch (MsalException exception)
        {
            _logger.LogWarning(exception, "OneDrive sign-out failed.");
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

    private Task<AuthenticationResult> AcquireTokenSilentAsync(IAccount account, CancellationToken cancellationToken) =>
        _publicClientApplication
            .AcquireTokenSilent(OneDriveAuthenticationConstants.Scopes, account)
            .ExecuteAsync(cancellationToken);

    private void EnablePersistentTokenCache(ITokenCache tokenCache)
    {
        tokenCache.SetBeforeAccessAsync(OnBeforeTokenCacheAccessAsync);
        tokenCache.SetAfterAccessAsync(OnAfterTokenCacheAccessAsync);
    }

    private async Task OnBeforeTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        try
        {
            var cacheBytes = await _tokenCacheStore.ReadAsync(args.CancellationToken).ConfigureAwait(false);
            if (cacheBytes is { Length: > 0 })
            {
                args.TokenCache.DeserializeMsalV3(cacheBytes);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "LockPaper could not read the persisted OneDrive token cache; clearing the saved cache.");
            await _tokenCacheStore.ClearAsync(args.CancellationToken).ConfigureAwait(false);
        }
    }

    private async Task OnAfterTokenCacheAccessAsync(TokenCacheNotificationArgs args)
    {
        if (!args.HasStateChanged)
        {
            return;
        }

        try
        {
            var cacheBytes = args.TokenCache.SerializeMsalV3();
            await _tokenCacheStore.WriteAsync(cacheBytes, args.CancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "LockPaper could not persist the OneDrive token cache.");
        }
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
