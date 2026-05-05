# LockPaper Performance Findings

This note captures the current Azure Application Insights performance findings and the additional instrumentation added to support future diagnosis.

## Diagnostics review - 2026-05-05

### What the current telemetry can already tell us

- Application Insights already captures useful `dependencies`, `traces`, `exceptions`, `performanceCounters`, and a small amount of `customMetrics`.
- The strongest current signals point to **network-bound delays** in Microsoft authentication, Microsoft Graph album discovery, and OneDrive photo download requests.
- The existing telemetry is enough to identify a few likely hotspots, but it was **not enough to break down end-to-end in-app latency** across startup, display refresh, album discovery, wallpaper selection, file save, and lock-screen apply stages.

### Current hotspots from telemetry

| Area | Observed latency |
| --- | --- |
| Microsoft Graph album discovery (`GET /v1.0/me/drive/bundles`) | WinUI avg about **1144 ms**, p95 about **1871 ms**; one PC sample avg about **2380 ms**, p95 about **3611 ms** |
| OneDrive photo download (`download.aspx`) | WinUI avg about **1061 ms**, p95 about **2049 ms**, max about **5234 ms** |
| Album photo listing (`GET /children`) | WinUI avg about **879 ms**, p95 about **1115 ms** |
| MSAL token exchange | WinUI avg about **893 ms**, p95 about **1386 ms** |

### Other telemetry observations

- Repeated Android `READ_EXTERNAL_STORAGE` exceptions were present in telemetry. These look more like permission-flow noise than the main source of perceived sluggishness, but they are still useful to track.
- The current performance counters are too coarse to explain where the app itself spends time during refresh and initialization.
- The existing custom metrics are effectively heartbeat-level signals rather than operation timings.

## Observability gap

Before this follow-up instrumentation work, LockPaper did **not** emit a clean, queryable timing signal for:

- app window creation,
- main-page initialization,
- cached connection refresh,
- album discovery,
- display-summary refresh,
- OneDrive source operations,
- end-to-end wallpaper refresh,
- lock-screen apply.

That meant Azure Application Insights could show **slow external calls**, but not the slowest **in-app stage** around those calls.

## Added instrumentation

Structured timing checkpoints are now emitted through the existing `ILogger` pipeline, which already flows into Application Insights.

Each checkpoint records:

- `OperationName`
- `Outcome`
- `ElapsedMs`

The current checkpoint coverage includes:

1. `App.CreateWindow`
2. `MainPageModel.InitializeAsync`
3. `MainPageModel.RefreshConnectionStateAsync`
4. `MainPageModel.GetAlbumDiscoveryResultIfNeededAsync`
5. `MainPageModel.RefreshWallpaperAsync`
6. `MainPageModel.RefreshDisplaySummaryAsync`
7. `OneDriveAlbumDiscovery.GetMatchingAlbumsAsync`
8. `OneDriveWallpaperSource.GetMatchingAlbumsAsync`
9. `OneDriveWallpaperSource.GetAlbumPhotosAsync`
10. `OneDriveWallpaperSource.DownloadPhotoBytesAsync`
11. `WallpaperRefresh.RefreshAsync`
12. `LockScreenWallpaper.ApplyAsync`

## Recommended Application Insights queries

### 1. Ranked in-app hotspots from the new timing checkpoints

```kusto
traces
| where tostring(customDimensions["OriginalFormat"]) == "Performance checkpoint {OperationName} completed with outcome {Outcome} in {ElapsedMs} ms."
| extend
    OperationName = tostring(customDimensions["OperationName"]),
    Outcome = tostring(customDimensions["Outcome"]),
    ElapsedMs = todouble(customDimensions["ElapsedMs"])
| summarize
    Count = count(),
    AvgMs = round(avg(ElapsedMs), 2),
    P95Ms = round(percentile(ElapsedMs, 95), 2),
    MaxMs = round(max(ElapsedMs), 2)
    by client_Type, OperationName, Outcome
| order by P95Ms desc
```

### 2. Ranked dependency hotspots

```kusto
dependencies
| summarize
    Count = count(),
    AvgMs = avg(duration),
    P95Ms = percentile(duration, 95),
    MaxMs = max(duration)
    by client_Type, target, name, success
| order by P95Ms desc
```

### 3. Recent Android permission-related exceptions

```kusto
exceptions
| where client_Type == "Android"
| project timestamp, type, problemId, outerMessage, operation_Name
| order by timestamp desc
```

## Expected next step

After the next deployed build has been exercised on Windows and Android, rerun the checkpoint query above. That should make it straightforward to answer whether the perceived sluggishness is dominated by:

- Microsoft identity or Graph latency,
- OneDrive content download time,
- display-summary refresh work,
- wallpaper application work,
- or broader app startup/initialization overhead.
