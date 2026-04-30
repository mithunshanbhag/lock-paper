# LockPaper UI Specification

This document expands the UI portion of the v1 product spec in `docs/specs/README.md`.

## UI scope for v1

The first release needs a **single portrait-oriented primary screen** that adapts to the user's current state. The same layout should be used on both Windows and Android to keep the experience simple and consistent.

## Screen inventory

### 1. Disconnected state

Purpose: help the user start the OneDrive connection flow with the least possible UI.

Required elements:

- App title.
- Primary **Connect to OneDrive** button.
- No extra explanatory cards are required in the default disconnected mockup.

### 2. Connected state

Purpose: show the current status and provide the manual wallpaper change action.

Required elements:

- App title.
- A lightweight logout affordance next to the app title, such as a green tick icon.
- Primary action button.
- Compact status summary showing:
  - the last attempt, with a lightweight success/failure indicator
  - next scheduled attempt target or scheduling note

### 3. Problem states

The connected/disconnected screen should adapt to these situations instead of branching into more complex flows:

- **Signed out** - show sign-in call to action.
- **Album missing** - show instructions to create or rename the album.
- **Album empty or ineligible** - explain that usable photos are required.
- **Change in progress** - disable the primary button and show progress text.
- **Last attempt failed** - show a concise error summary and let the user retry.

## Layout guidance

- Use one shared portrait layout across Android and Windows.
- Keep the main action visible without scrolling on common phone-sized dimensions.
- Use a clean, card-based layout inspired by MudBlazor patterns.
- Prioritize one clear primary action and only the few status fields the user actually needs.
- Avoid extra surrounding presentation elements in the mockups; focus on the screen itself.

## Content guidance

- Use plain, direct language.
- Avoid implementation jargon such as "Graph API" or "background worker" in the main UI.
- Present scheduling honestly as **best effort**.
- Display all times using the current device's local time.
- Do not imply that desktop wallpaper is managed in v1.

## Interaction rules

- The primary action must be disabled only while a wallpaper change is already running.
- Manual refresh should update the visible status immediately after completion.
- Activating the connected-state title icon should open a confirmation dialog before log out.
- Error messages should stay concise and user-facing.
- V1 should not require a settings page to complete the basic experience.

## Mockup reference

- Disconnected mockup: `docs/ui-mockups/LockPaperDisconnected/index.html`
- Connected mockup: `docs/ui-mockups/LockPaperConnected/index.html`
