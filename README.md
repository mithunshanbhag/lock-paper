# LockPaper

![Status: Prototype](https://img.shields.io/badge/status-prototype-6f42c1)
![Build: Placeholder](https://img.shields.io/badge/build-placeholder-lightgrey)
![Coverage: Placeholder](https://img.shields.io/badge/coverage-placeholder-lightgrey)
![Platform: .NET%20MAUI](https://img.shields.io/badge/platform-.NET%20MAUI-512BD4)

LockPaper is a minimal **.NET MAUI** app for **Windows** and **Android** that changes your **lock-screen wallpaper** using photos from a OneDrive album. The v1 goal is intentionally small: sign in with a personal Microsoft account, find a matching album, rotate the lock-screen image on a best-effort hourly cadence, and let the user trigger a manual refresh from one simple screen.

![LockPaper preview placeholder](https://placehold.co/1200x760/png?text=LockPaper+UI+Preview)

> Preview placeholder: app screenshots have not been captured yet. For current UI references, see the [connected mockup](docs/ui-mockups/LockPaperConnected/index.html), the [no albums found mockup](docs/ui-mockups/NoAlbumsFound/index.html), and the [disconnected mockup](docs/ui-mockups/LockPaperDisconnected/index.html).

## ✨ Current scope

- Personal Microsoft account sign-in.
- OneDrive album discovery for `lockpaper`, `lock-paper`, or `lock paper`.
- Lock-screen wallpaper updates only.
- Orientation-aware photo selection with random fallback.
- Best-fit manual lock-screen refresh from a matching OneDrive album.
- Best-effort hourly refresh remains pending.
- A sparse, single-screen UI designed for a tiny personal/family audience.

For the full product definition, see:

- [docs/specs/README.md](docs/specs/README.md)
- [docs/specs/ui.md](docs/specs/ui.md)
- [docs/specs/future.md](docs/specs/future.md)

## 📦 Installation

### Prerequisites

- **.NET 10 SDK**
- **.NET MAUI workload**
- **Windows** for the local Windows app workflow
- **Android SDK / MAUI Android tooling** if you want to build the Android target locally

### Setup

```powershell
git clone https://github.com/mithunshanbhag/lock-paper.git
cd lock-paper
dotnet workload install maui
dotnet restore .\src\LockPaper.Ui\LockPaper.Ui.csproj
```

The shared MAUI client now uses a public Microsoft identity app registration for personal-account sign-in. Before testing OneDrive login, make sure that Azure app registration includes these redirect URIs:

- `msalab40323b-cea7-401f-ac37-de0bdf27ee9f://auth`
- `http://localhost`

Also make sure the app registration is configured like this:

- **Supported account types** includes personal Microsoft accounts.
- **Mobile and desktop applications** platform is present.
- **Allow public client flows** is enabled.
- **Microsoft Graph -> Delegated permissions** includes `Files.Read`.

If Windows sign-in shows:

> invalid_request: The provided value for the input parameter 'redirect_uri' is not valid

then the desktop redirect URI is missing from the app registration. Add:

- `http://localhost`

under the app registration's **Mobile and desktop applications** platform and try again.

## 🚀 Usage

After launching the app, you can:

1. start from the signed-out state,
2. sign in with a personal Microsoft account,
3. let the app check OneDrive for matching wallpaper albums immediately after the connection succeeds,
4. confirm that the connected state shows the Microsoft account, wallpaper album status, current display summary, and wallpaper attempt status,
5. use **Refresh lockscreen wallpaper** to pick a best-fit random photo from a matching album and apply it to the lock screen,
6. review the inline error guidance when no matching OneDrive albums are found or the matching albums do not contain usable photos, and
7. log out from the title-bar affordance.

Notes:

- Windows uses the supported packaged personalization API for lock-screen updates.
- Windows applies one shared lock-screen image across all monitors because the platform does not support different lock-screen images per monitor.
- Hourly scheduling is still pending. The current implementation covers the manual refresh flow and the related status feedback.

## 🛠️ Build and run locally

The repository includes a convenience script for local workflows:

```powershell
.\run-local.ps1 -Target app
```

Notes:

- The `app` target is currently intended for **Windows** because it launches the MAUI Windows app locally.
- The script builds `src\LockPaper.Ui\LockPaper.Ui.csproj` and starts the app executable if it is not already running.

If you only want to build the app manually:

```powershell
dotnet format .\src\LockPaper.Ui\LockPaper.Ui.csproj --no-restore
dotnet build .\src\LockPaper.Ui\LockPaper.Ui.csproj --nologo --no-restore
```

## 🧪 Run the tests

Run unit tests through the local script:

```powershell
.\run-local.ps1 -Target unit-tests
```

Or run the current unit-test project directly:

```powershell
dotnet test .\tests\LockPaper.Ui.UnitTests\LockPaper.Ui.UnitTests.csproj --nologo
```

Additional test targets:

```powershell
.\run-local.ps1 -Target tests
.\run-local.ps1 -Target e2e-tests
```

> The repository currently includes a basic placeholder unit-test project under `tests\LockPaper.Ui.UnitTests`.

## 📁 Project layout

```text
.
├── .github
├── docs
│   ├── specs
│   └── ui-mockups
├── src
│   └── LockPaper.Ui
├── tests
│   └── LockPaper.Ui.UnitTests
└── run-local.ps1
```

## 🤖 AI readiness

This repository includes Copilot-focused project guidance in:

- [.github/copilot-instructions.md](.github/copilot-instructions.md)
- [.github/LEARNINGS.md](.github/LEARNINGS.md)
- [docs/specs/README.md](docs/specs/README.md)
- [docs/specs/ui.md](docs/specs/ui.md)

