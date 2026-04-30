# Learnings

- `src/LockPaper.Ui` is already a .NET MAUI app targeting Android and Windows, so LockPaper product work should align with that shared app shell before introducing a different client architecture.
- Use `dotnet format .\src\LockPaper.Ui\LockPaper.Ui.csproj --no-restore` followed by `dotnet build .\src\LockPaper.Ui\LockPaper.Ui.csproj --nologo --no-restore` for the MAUI client verification loop.
- `run-local.ps1 -Target unit-tests` runs every `*.UnitTests.csproj` under `.\tests`, so new unit-test projects should keep that naming convention to plug into the local workflow automatically.
- `src/LockPaper.Ui\Pages\MainPage.xaml` now mirrors the disconnected/connected mockups with separate portrait screen shells, compact inline feedback, and connected-state cards for Microsoft account, display summary, last attempt, and next attempt.
- The display summary now keeps all detected screens inside one status card and shows only colored rectangles with resolution text; it still uses `HexColorToBrushConverter` because MAUI `Border.Background` needs a brush rather than a raw hex string binding.
- Windows monitor detection in `src/LockPaper.Ui\Services\Implementations\DeviceDisplayService.cs` now uses Win32 `EnumDisplayMonitors` plus `GetMonitorInfo` because WinUI `DisplayArea.FindAll()` can under-report attached monitors on desktop multi-monitor setups.
- LockPaper branding assets now live in `src/LockPaper.Ui\Resources\AppIcon\appicon.svg`, `appiconfg.svg`, and `Resources\Splash\splash.svg`; Android theme colors are in `Platforms\Android\Resources\values\colors.xml`.
- OneDrive login/logout now uses MSAL in `src/LockPaper.Ui\Services\Implementations\OneDriveAuthenticationService.cs` with the `consumers` authority, the `Files.Read` scope, and Android redirect handling through `src/LockPaper.Ui\Platforms\Android\MsalActivity.cs`.
- Windows OneDrive sign-in follows the non-broker MAUI desktop pattern and uses the redirect URI `http://localhost`, which must be registered under the Azure app's Mobile and desktop applications platform.
- Personal-account OneDrive sign-in also depends on the Azure app registration supporting personal Microsoft accounts, allowing public client flows, and having Microsoft Graph delegated permission `Files.Read`.
- The connected-state screen now checks Microsoft Graph OneDrive album bundles through `src/LockPaper.Ui\Services\Implementations\OneDriveAlbumDiscoveryService.cs` and surfaces a dedicated wallpaper-albums card plus inline guidance when no matching albums are found.
