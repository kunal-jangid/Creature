# Auto-Update & 'Check for Updates' Feature Design Specification

**Date:** 2026-10-10  
**Status:** Approved  
**Scope:** Update checking subsystem, Tray UI integration, Winget execution & GitHub Releases fallback.

---

## 1. Overview & Goals

The **Desktop Pets** application requires an update subsystem to keep installed copies up-to-date with minimal friction. The feature will support both manual update checks from the system tray context menu and automatic background checks, leveraging Microsoft Windows Package Manager (`winget`) as the primary installation mechanism with an automatic fallback to GitHub Releases in the default web browser.

### Key Goals:
- **Instant Version Detection**: Query the GitHub Releases API directly (`api.github.com/repos/kunal-jangid/Creature/releases/latest`) to avoid Winget repository cache delays.
- **Native Winget Upgrades**: Run `winget upgrade --id KunalJangid.DesktopPets` seamlessly so users who installed via winget or standalone installer get updated packages registered properly.
- **Graceful Fallbacks**: If `winget` CLI is missing, non-functional, or hasn't yet indexed the newly published release manifest, cleanly open the GitHub Release web page.
- **Unobtrusive UX**: Manual checks report status directly via dialogs; background checks run silently and only prompt the user if an update is found.

---

## 2. Architecture & Components

```
                +------------------------------------+
                |            TrayManager             |
                |  (Context Menu: "Check for Updates")|
                +-----------------+------------------+
                                  | Triggers Check (manual)
                                  v
+-------------------+   +------------------------------------+
| App.xaml.cs       |-->|           UpdateManager            |
| (Startup & Timer) |   | - GitHub API Client                |
+-------------------+   | - Version Comparer                 |
  Triggers Check        | - Winget CLI / Browser Runner      |
  (background)          +-----------------+------------------+
                                          |
                     +--------------------+--------------------+
                     |                                         |
                     v                                         v
        +-------------------------+               +-------------------------+
        |   GitHub Releases API   |               |   Winget / Browser      |
        |  (api.github.com/...)   |               | (winget upgrade / http) |
        +-------------------------+               +-------------------------+
```

### Component Details

### 2.1 `UpdateManager.cs` (`Creature.SystemIntegration`)
Responsible for version retrieval, comparison, periodic scheduling, and installer invocation.

- **Properties & Constants**:
  - `GitHubRepoOwner`: `"kunal-jangid"`
  - `GitHubRepoName`: `"Creature"`
  - `WingetPackageId`: `"KunalJangid.DesktopPets"`
  - `GitHubReleaseApiUrl`: `"https://api.github.com/repos/kunal-jangid/Creature/releases/latest"`
- **Methods**:
  - `Task<UpdateCheckResult> CheckForUpdatesAsync(bool isManual)`: Queries the latest GitHub release info and compares against current assembly version (`Assembly.GetExecutingAssembly().GetName().Version`).
  - `Task<bool> ApplyUpdateAsync(string releaseUrl)`: Attempts upgrade via `winget.exe`. If winget is not present or exits with a non-zero code, opens `releaseUrl` in the user's default browser.
  - `void StartPeriodicChecks(TimeSpan initialDelay, TimeSpan interval)`: Schedules a background timer to check for updates periodically.
  - `bool IsWingetAvailable()`: Checks if `winget.exe` is found in `PATH` or standard Windows app alias directories.
  - `void Dispose()`: Cleans up HTTP clients and timers.

### 2.2 `UpdateCheckResult.cs` (`Creature.SystemIntegration`)
Model holding the evaluation results:
- `IsUpdateAvailable`: boolean
- `CurrentVersion`: `Version`
- `LatestVersion`: `Version`
- `ReleaseNotes`: string
- `ReleaseUrl`: string
- `ErrorMessage`: string? (if check failed)

### 2.3 `TrayManager.cs` Integration
- Adds `"Check for Updates..."` to the system tray context menu.
- Displays feedback on manual checks:
  - If up-to-date: `MessageBox.Show("You are running the latest version (vX.Y.Z).", "Desktop Pets", ...)`
  - If update available: `MessageBox.Show("Version vX.Y.Z is available! Would you like to update now?", ...)` with `Yes/No` buttons. If `Yes`, calls `UpdateManager.ApplyUpdateAsync(...)`.
  - If error: `MessageBox.Show("Could not check for updates. Please check your internet connection.", ...)`

### 2.4 `SettingsManager.cs` / `AppSettings.cs`
- Adds `AutoCheckUpdates`: boolean (default: `true`)
- Adds `LastUpdateCheckUtc`: DateTime? (timestamp of the last successful check)

---

## 3. Detailed Data & Execution Flow

### 3.1 Version Comparison Logic
- Current Version: Extracted from `Assembly.GetExecutingAssembly().GetName().Version` (or `FileVersionInfo`).
- Remote Version: Tag string from GitHub API (e.g. `"v1.2.0"` or `"1.2.0"`), sanitized with `TrimStart('v', 'V')` and parsed using `System.Version.TryParse`.
- Comparison: Returns `true` if `latestVersion > currentVersion`.

### 3.2 Winget Execution & Fallback Logic
1. Check if `winget` command exists on system.
2. If `winget` is found:
   - Spawn process: `winget upgrade --id KunalJangid.DesktopPets --accept-source-agreements --accept-package-agreements`
   - If process starts and returns exit code 0 or initiates the Inno Setup installer, Inno Setup's `KillProcess('DesktopPets.exe')` terminates the running instance and restarts upon installation.
   - If winget returns error code (e.g. package not found / manifest not yet published on winget repository), show prompt and open `ReleaseUrl` in web browser via `Process.Start(new ProcessStartInfo(releaseUrl) { UseShellExecute = true })`.
3. If `winget` is NOT found:
   - Open `ReleaseUrl` directly in the default web browser.

---

## 4. Error Handling & Edge Cases

| Scenario | Handling |
|---|---|
| **No Internet Connection** | Manual check: displays polite error message. Background check: silently logs/ignores without disrupting user. |
| **GitHub API Rate Limit** | Sets custom `User-Agent: DesktopPets-App` header to avoid generic IP block, handles HTTP 403/429 gracefully. |
| **Malformed Tag Name** | `Version.TryParse` guard prevents crashes if non-semver tag is encountered. |
| **Winget Pending Manifest** | Immediate fallback to browser download if Winget index hasn't caught up with the GitHub release yet. |
| **Multiple Instances / Setup Run** | Inno Setup handles process kill and relaunch automatically. |

---

## 5. Testing Strategy

1. **Unit Tests** in `Creature.Tests`:
   - `UpdateManagerTests.cs`:
     - Test semantic version parsing (`v1.0.0`, `1.2.1.0`, invalid formats).
     - Test version comparison (`1.0.0` vs `1.0.1`, `1.1.0` vs `1.0.9`, identical versions).
     - Test update check result formatting.
2. **Integration Verification**:
   - Verify `TrayManager` menu item triggers `CheckForUpdatesAsync`.
   - Verify manual and background notification triggers.
   - Verify simulated winget execution & browser fallback.
