# LockPaper

![Status: Prototype](https://img.shields.io/badge/status-prototype-6f42c1)
![Build: Placeholder](https://img.shields.io/badge/build-placeholder-lightgrey)
![Coverage: Placeholder](https://img.shields.io/badge/coverage-placeholder-lightgrey)
![Platform: .NET%20MAUI](https://img.shields.io/badge/platform-.NET%20MAUI-512BD4)

LockPaper is a minimal **.NET MAUI** app for **Windows** and **Android** that changes your **lock-screen wallpaper** using photos from a OneDrive album. The v1 goal is intentionally small: sign in with a personal Microsoft account, find a matching album, rotate the lock-screen image on a best-effort hourly cadence, and let the user trigger a manual refresh from one simple screen.

![LockPaper preview placeholder](https://placehold.co/1200x760/png?text=LockPaper+UI+Preview)

> Preview placeholder: app screenshots have not been captured yet. For current UI references, see the [connected mockup](docs/ui-mockups/LockPaperConnected/index.html) and [disconnected mockup](docs/ui-mockups/LockPaperDisconnected/index.html).

## ✨ Current scope

- Personal Microsoft account sign-in.
- OneDrive album discovery for `lockpaper`, `lock-paper`, or `lock paper`.
- Lock-screen wallpaper updates only.
- Orientation-aware photo selection with random fallback.
- Best-effort hourly refresh plus a manual **Change now** action.
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

## 🚀 Usage

The current repository is focused on the core app shell and placeholder state flows for the v1 screen. After launching the app, the current prototype lets you:

1. start from the signed-out state,
2. simulate connecting to OneDrive,
3. inspect placeholder states such as connected, album missing, album empty, and last-attempt failed,
4. trigger a placeholder **Change now** action, and
5. log out from the title-bar affordance.

This keeps the UI and behavior aligned with the current product spec while the real OneDrive, wallpaper, and scheduling integrations are built out.

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

