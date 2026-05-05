# LockPaper Future Drift Audit

This file collates three independent repository audits run with **Claude Sonnet 4.6**, **GPT-5.3-Codex**, and **GPT-5.4**. Findings are ordered first by **reviewer consensus**, then by impact.

## Consensus summary

| Rank | Consensus | Severity | Finding |
| --- | --- | --- | --- |
| 1 | 3/3 | High | Hourly refresh is still in the v1 spec, but the product is manual-refresh only today. |
| 2 | 3/3 | Medium | The connected mockup is stale and omits the required **Wallpaper albums** card. |
| 3 | 2/3 | Medium | Last-attempt persistence is promised by the spec, but the current app forgets it after restart. |
| 4 | 2/3 | Low | `README.md` still describes the unit-test project as a basic placeholder. |
| 5 | 1/3 | Medium | `README.md` says `appsettings.json` contains a placeholder App Insights connection string, but the source contains a concrete one. |
| 6 | 1/3 | Low | Connected-state mockup copy no longer matches the implemented UI text. |
| 7 | 1/3 | Low | Platform-specific lock-screen apply paths are only lightly represented in automated tests. |

## 3/3 reviewer consensus

### 1. Hourly refresh is still in the v1 spec, but the product is manual-refresh only today

- **Affected surfaces:** `docs\specs\README.md`, `README.md`, `src\LockPaper.Ui`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `docs\specs\README.md:44,103-108` keeps best-effort hourly refresh inside v1 scope and FR4.
  - `README.md:21,97` says hourly refresh is still pending.
  - `src\LockPaper.Ui\PageModels\MainPageModel.cs:443-455,470-471` only exposes placeholder text such as `Waiting for wallpaper scheduling.`
  - `tests\LockPaper.Ui.UnitTests\PageModels\MainPageModelTests.cs:68-69,409-410,448-449` asserts the placeholder scheduling state.
- **Why it matters:** the spec describes scheduled refresh as shipped behavior, while the app, tests, and README all reflect a manual-refresh-first product.
- **Recommendation:** either implement scheduling and real next-attempt behavior, or move hourly refresh out of v1 scope in `docs\specs\README.md`.

### 2. The connected mockup is stale and omits the required **Wallpaper albums** card

- **Affected surfaces:** `docs\ui-mockups`, `docs\specs\ui.md`, `README.md`, `src\LockPaper.Ui\Pages\MainPage.xaml`
- **Evidence:**
  - `docs\specs\ui.md:25-35` requires the connected state to show **Microsoft account**, **Wallpaper albums**, **Display summary**, **Last attempt**, and **Next attempt** cards.
  - `docs\ui-mockups\LockPaperConnected\index.html:22-50` shows the account, display, last-attempt, and next-attempt cards, but omits **Wallpaper albums**.
  - `docs\ui-mockups\NoAlbumsFound\index.html:32-35` does include the **Wallpaper albums** card.
  - `src\LockPaper.Ui\Pages\MainPage.xaml:82-89` implements the **Wallpaper albums** card.
  - `README.md:12` points readers to the connected mockup as a current UI reference.
- **Why it matters:** the main happy-path mockup is no longer a reliable reference for design reviews or future UI work.
- **Recommendation:** update `docs\ui-mockups\LockPaperConnected\index.html` to match the implemented card set and order.

## 2/3 reviewer consensus

### 3. Last-attempt persistence is promised by the spec, but the current app forgets it after restart

- **Affected surfaces:** `docs\specs\README.md`, `src\LockPaper.Ui`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `docs\specs\README.md:114` says the app should store enough local state to report the last attempted wallpaper change and its outcome.
  - `src\LockPaper.Ui\PageModels\MainPageModel.cs:24,338,437-445,607` keeps the last result only in `_lastWallpaperRefreshResult` and falls back to `No wallpaper change has run yet.`
  - `tests\LockPaper.Ui.UnitTests\PageModels\MainPageModelTests.cs:68,239,478` expects the in-memory reset behavior.
  - `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs:12,33,153-171` persists the current wallpaper preview path, but not the last-attempt status payload.
- **Why it matters:** the current UX loses the most recent attempt summary after app restart, which does not match the spec.
- **Recommendation:** persist the last refresh result and hydrate it during initialization, or relax the persistence requirement in the spec.

### 4. `README.md` still describes the unit-test project as a basic placeholder

- **Affected surfaces:** `README.md`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `README.md:140` says the repository includes `a basic placeholder unit-test project`.
  - The test project currently contains coverage for page-model logic, album discovery, wallpaper sourcing, wallpaper refresh, wallpaper selection, display utilities, and configuration loading:
    - `tests\LockPaper.Ui.UnitTests\PageModels\MainPageModelTests.cs`
    - `tests\LockPaper.Ui.UnitTests\Services\Implementations\OneDriveAlbumDiscoveryServiceTests.cs`
    - `tests\LockPaper.Ui.UnitTests\Services\Implementations\OneDriveWallpaperSourceServiceTests.cs`
    - `tests\LockPaper.Ui.UnitTests\Services\Implementations\WallpaperRefreshServiceTests.cs`
    - `tests\LockPaper.Ui.UnitTests\Services\Implementations\WallpaperSelectionServiceTests.cs`
    - `tests\LockPaper.Ui.UnitTests\Misc\Utilities\AppSettingsConfigurationLoaderTests.cs`
- **Why it matters:** contributors reading the README get an outdated picture of current coverage and maturity.
- **Recommendation:** replace the placeholder wording with a brief summary of the shared-logic coverage and any remaining gaps.

## 1/3 reviewer consensus, but independently verified

### 5. `README.md` says `appsettings.json` contains a placeholder App Insights connection string, but the source contains a concrete one

- **Affected surfaces:** `README.md`, `src\LockPaper.Ui\appsettings.json`
- **Evidence:**
  - `README.md:60-67` instructs contributors to update a placeholder Application Insights connection string in `src\LockPaper.Ui\appsettings.json`.
  - `src\LockPaper.Ui\appsettings.json:1-4` currently contains a concrete Application Insights connection string rather than a placeholder value.
- **Why it matters:** the documentation no longer matches the checked-in configuration, and local runs may send telemetry to a shared App Insights resource by default.
- **Recommendation:** either move the real value out of source control and keep the README guidance, or update the README to describe the current intended setup.

### 6. Connected-state mockup copy no longer matches the implemented UI text

- **Affected surfaces:** `docs\ui-mockups`, `src\LockPaper.Ui`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `docs\ui-mockups\LockPaperConnected\index.html:20` and `docs\ui-mockups\NoAlbumsFound\index.html:20` use the primary button label `Change now`.
  - `src\LockPaper.Ui\PageModels\MainPageModel.cs:211` sets the connected-state label to `Refresh lockscreen wallpaper`.
  - `tests\LockPaper.Ui.UnitTests\PageModels\MainPageModelTests.cs:58,90,406,442` assert `Refresh lockscreen wallpaper`.
  - `docs\ui-mockups\LockPaperConnected\index.html:42-49` uses `Not available yet`, while `src\LockPaper.Ui\PageModels\MainPageModel.cs:443` shows `No wallpaper change has run yet.`
- **Why it matters:** the mockups are no longer a faithful representation of the shipped copy.
- **Recommendation:** refresh the connected-state mockups so they match the code and tests.

### 7. Platform-specific lock-screen apply paths are only lightly represented in automated tests

- **Affected surfaces:** `docs\specs\README.md`, `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `docs\specs\README.md:112-114` makes Windows and Android lock-screen application a core requirement.
  - `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs:14-34,72-150` contains the real Android and Windows apply/read paths.
  - `tests\LockPaper.Ui.UnitTests\Services\Implementations\WallpaperRefreshServiceTests.cs:15-22,138-158` replaces `ILockScreenWallpaperService` with a fake instead of exercising the concrete platform code.
- **Why it matters:** the core platform integration points are not directly covered by the current automated suite.
- **Recommendation:** add targeted platform smoke/integration coverage, or explicitly document that these paths are still manually verified.
