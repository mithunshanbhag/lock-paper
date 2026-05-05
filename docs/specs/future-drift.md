# LockPaper Future Drift Audit

This file collates three independent repository audits run with **Claude Sonnet 4.6**, **GPT-5.3-Codex**, and **GPT-5.4**. Findings are ordered first by **reviewer consensus**, then by impact.

## Consensus summary

| Rank | Consensus | Severity | Finding |
| --- | --- | --- | --- |
| 1 | 2/3 | Medium | Last-attempt persistence is promised by the spec, but the current app forgets it after restart. |
| 2 | 1/3 | Low | Platform-specific lock-screen apply paths are only lightly represented in automated tests. |

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

## 1/3 reviewer consensus, but independently verified

### 7. Platform-specific lock-screen apply paths are only lightly represented in automated tests

- **Affected surfaces:** `docs\specs\README.md`, `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs`, `tests\LockPaper.Ui.UnitTests`
- **Evidence:**
  - `docs\specs\README.md:112-114` makes Windows and Android lock-screen application a core requirement.
  - `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs:14-34,72-150` contains the real Android and Windows apply/read paths.
  - `tests\LockPaper.Ui.UnitTests\Services\Implementations\WallpaperRefreshServiceTests.cs:15-22,138-158` replaces `ILockScreenWallpaperService` with a fake instead of exercising the concrete platform code.
- **Why it matters:** the core platform integration points are not directly covered by the current automated suite.
- **Recommendation:** add targeted platform smoke/integration coverage, or explicitly document that these paths are still manually verified.

