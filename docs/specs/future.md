# LockPaper Future Notes

This file captures ideas that are useful for later planning but intentionally out of scope for v1.

## Deferred product ideas

- Support Windows desktop wallpaper changes in addition to lock-screen updates.
- Support Microsoft work or school accounts.
- Let users choose a refresh cadence other than hourly.
- Add a preview of the most recently applied wallpaper.
- Add smarter repeat avoidance, such as shuffle-without-repeat history.
- Let users choose how photos should fit the screen:
  - crop to fill
  - fit with padding
  - platform default
- Add richer troubleshooting and settings screens.

## Product questions to revisit later

- Should the chosen matching album stay sticky between runs, or should the app keep choosing randomly when multiple matching albums exist?
- Should users be able to override orientation preference or exclude certain photos?
- Should the app support more than one source album or additional cloud providers?

## Diagnostics review - 2026-05-05

- **Observability gap:** Azure Application Insights queries for the last 30 minutes returned no exceptions, failed requests, warning/error traces, or any other telemetry items in that window.
- **Why this is a problem:** the app still has telemetry over the last 24 hours, so the pipeline is not completely dead, but the requested 30-minute window was too quiet to distinguish between `healthy but idle` and `recent activity is not being observed`.
- **Future follow-up:** emit a lightweight startup/heartbeat signal and manual-refresh begin/end telemetry so future diagnostics can quickly tell whether the app is active and whether the telemetry pipeline is healthy.
