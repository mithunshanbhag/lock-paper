# LockPaper Future Auto-Refresh Options

This note collates three independent Windows-only reviews run with **Claude Sonnet 4.6**, **GPT-5.3-Codex**, and **GPT-5.4**. Findings are prioritized first by reviewer consensus, then by fit for the current repository shape.

## Repo constraints that shape every Windows option

- `src\LockPaper.Ui\LockPaper.Ui.csproj` targets Windows as a packaged **MSIX** app.
- `src\LockPaper.Ui\Services\Implementations\LockScreenWallpaperService.cs` applies the Windows lock-screen image through `UserProfilePersonalizationSettings`.
- That API path only works when LockPaper is running with its packaged app identity in the signed-in user's interactive session.

Those constraints make the Windows question much narrower than a generic "background job" decision.

## Consensus summary

| Rank | Support                 | Option                                                        | Consensus                                                                                                                             |
| ---- | ----------------------- | ------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| 1    | 2/3 primary, 3/3 viable | **StartupTask + hidden resident helper/process**              | Best fit for the current MAUI + MSIX architecture. Keep the background runner hidden by default; add tray UX only if needed.          |
| 2    | 3/3 viable, 1/3 primary | **Scheduled Task + packaged headless helper/background mode** | Strongest clock-alignment option and no idle process, but launch plumbing is more awkward for a MAUI app.                             |
| 3    | 3/3 secondary only      | **WinRT background task / app service family**                | Too much Windows-specific plumbing for this app, not a clean "on the hour" story, and app service alone is not a scheduler.           |
| 4    | 3/3 rejected            | **Windows Service**                                           | Not compatible with the packaged interactive user-context lock-screen API that LockPaper already depends on.                          |
| 5    | 1/3 refinement idea     | **Separate lightweight companion EXE in the same MSIX**       | Worth considering later if a resident MAUI process feels too heavy, but more moving parts than the first implementation likely needs. |

## 1. StartupTask + hidden resident helper/process

### How it would work

- Register a Windows **StartupTask** so LockPaper starts when the user signs in.
- In background mode, suppress the main window and keep a small resident process alive.
- Compute the next local top-of-hour timestamp, wait until then, run the existing refresh/apply pipeline, and then reschedule.
- If the app later needs a visible "running" affordance, add a tray icon on top of this model rather than making the tray the core architecture.

### Why two reviewers ranked it first

- It preserves the exact packaged user-context that the current Windows wallpaper API already needs.
- It reuses the app's current token cache, local state, and refresh pipeline with the least new packaging work.
- It avoids Task Scheduler registration, cleanup, and headless-launch edge cases.

### Tradeoffs

- It leaves a process running all day.
- Sleep, hibernate, crashes, or user-disabled startup can still cause missed runs.
- A true tray experience is extra Windows-only work; it should be treated as optional UX, not as the architectural reason to choose this option.

### Bottom line

If we want the **lowest-risk first implementation inside the current app shape**, this is the consensus favorite.

## 2. Scheduled Task + packaged headless helper/background mode

### How it would work

- Register a **per-user Scheduled Task** aligned to the top of the hour.
- Launch LockPaper through a packaged background entry point so it keeps package identity, runs one refresh, and exits.
- Enable missed-run recovery so a sleeping machine can catch up on wake rather than silently skipping a cycle.

### Why all three reviewers kept it near the top

- It is the strongest option for aiming at **top-of-hour** execution.
- It avoids an always-running resident process.
- It is easy to reason about operationally: launch, refresh once, exit.

### Tradeoffs

- MAUI is not naturally headless, so background launch suppression is more fiddly than in a lightweight helper.
- Scheduled-task registration, updates, and cleanup become part of the app's lifecycle.
- If the main MAUI process is what launches, startup cost becomes part of every hourly refresh.

### Bottom line

If we care more about **clock alignment and zero idle footprint** than about implementation simplicity, this is the best alternative and a very credible fallback.

## 3. WinRT background task / app service family

### What the reviewers agreed on

- This family is **not the right first choice** for LockPaper.
- It adds substantial Windows-specific plumbing compared with the current MAUI app shape.
- It does not solve the "on the hour" problem cleanly.
- **App Service** is not a scheduler by itself; at best it is a supporting IPC mechanism.

### Bottom line

Possible in theory, but the complexity-to-benefit ratio looks wrong for this repo.

## 4. Windows Service

### Consensus

All three reviewers rejected this option.

### Why it is out

- A Windows Service runs in the wrong context for LockPaper's current Windows lock-screen API path.
- The current implementation already assumes a packaged app with personalization access in the interactive user session.
- Moving to a service would fight the platform requirements rather than work with them.

### Bottom line

Do **not** convert LockPaper into a Windows Service for this feature.

## 5. Separate lightweight companion EXE inside the same MSIX

One review highlighted a useful future refinement:

- keep the main MAUI UI app as-is,
- add a smaller packaged helper executable for background work,
- use that helper with either **StartupTask** or **Scheduled Task**.

This could reduce idle memory or cold-start cost, but it also adds packaging and shared-state coordination work. It feels more like a **phase-two optimization** than the first implementation step.

## Recommendation

1. **Default recommendation:** start with **StartupTask + hidden background mode** in the packaged app, not a tray-first redesign.
2. **Fallback if clock alignment matters more:** use a **per-user Scheduled Task** that launches a packaged background entry point hourly.
3. **If the resident MAUI process later feels too heavy:** move the background runner into a separate lightweight executable inside the same MSIX package.

## Answer to the tray-app question

**Not as the core decision.** The real decision is **resident background process vs scheduled launches**.

- If we pick the resident-process model, a tray icon can be added as an optional Windows affordance.
- If we pick the scheduled-launch model, a tray icon is unnecessary.

So the right question is not "Should LockPaper become a system tray app?" but rather:

> Should LockPaper keep a packaged background runner alive after logon, or should Windows launch a packaged background entry point each hour?

## "On the hour" needs honest product wording

All three reviews agreed that Windows can only offer **best-effort** hourly execution, not a hard guarantee at exactly `HH:00:00`.

Reasons include:

- sleep or hibernate,
- logoff,
- startup delays,
- scheduler jitter,
- network and token-refresh latency,
- the user disabling startup behavior.

The current spec wording is already close to correct: aim for **top-of-hour targeting where platform scheduling allows it**, but promise only a **best-effort** attempt.

## Scope note

This note focuses on **Windows** alternatives only. Android will need its own background-execution decision, but whichever Windows path we choose should keep the actual refresh/apply pipeline reusable across both platforms.
