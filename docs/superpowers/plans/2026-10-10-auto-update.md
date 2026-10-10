# Auto-Update & 'Check for Updates' Feature Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Implement an update subsystem for Desktop Pets that checks GitHub Releases for the latest version tag and performs native upgrades via `winget` CLI with a graceful browser fallback.

**Architecture:** A standalone `UpdateManager` service handles version checking against GitHub Releases API and triggers `winget upgrade --id KunalJangid.DesktopPets` (or opens the release URL if winget is unavailable/fails). `TrayManager` surfaces a manual "Check for Updates..." context menu option, while `App.xaml.cs` schedules non-intrusive background checks.

**Tech Stack:** .NET 10, C# 13, WPF, System.Net.Http, System.Text.Json, xUnit.

**Spec:** [docs/superpowers/specs/2026-10-10-auto-update-design.md](file:///D:/projects/Creature/docs/superpowers/specs/2026-10-10-auto-update-design.md)

## Global Constraints

- GitHub Repo Owner: `kunal-jangid`
- GitHub Repo Name: `Creature`
- Winget Package ID: `KunalJangid.DesktopPets`
- GitHub Release API URL: `https://api.github.com/repos/kunal-jangid/Creature/releases/latest`
- User-Agent header for GitHub API requests: `DesktopPets-App`
- Clean semver parsing: `v` prefix stripped before parsing (`v1.2.0` -> `1.2.0`).

---

### Task 1: Version Parser & UpdateCheckResult Model

**Files:**
- Create: `SystemIntegration/UpdateCheckResult.cs`
- Create: `SystemIntegration/UpdateManager.cs` (initial skeleton with version compare helper)
- Create: `Creature.Tests/SystemIntegration/UpdateManagerTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public record UpdateCheckResult(
      bool IsUpdateAvailable,
      Version CurrentVersion,
      Version? LatestVersion,
      string ReleaseNotes,
      string ReleaseUrl,
      string? ErrorMessage = null
  );

  public partial class UpdateManager : IDisposable
  {
      public static bool TryParseVersion(string? tag, out Version? version);
      public static bool IsNewerVersion(Version current, Version latest);
  }
  ```

- [ ] **Step 1: Write the failing tests in `Creature.Tests/SystemIntegration/UpdateManagerTests.cs`**

```csharp
using System;
using Creature.SystemIntegration;
using Xunit;

namespace Creature.Tests.SystemIntegration;

public class UpdateManagerTests
{
    [Theory]
    [InlineData("v1.0.0", 1, 0, 0)]
    [InlineData("1.2.3", 1, 2, 3)]
    [InlineData("V2.1.0.4", 2, 1, 0, 4)]
    public void TryParseVersion_ValidTags_ParsesCorrectly(string tag, int major, int minor, int build, int revision = -1)
    {
        var success = UpdateManager.TryParseVersion(tag, out var version);

        Assert.True(success);
        Assert.NotNull(version);
        Assert.Equal(major, version.Major);
        Assert.Equal(minor, version.Minor);
        Assert.Equal(build, version.Build);
        if (revision >= 0)
        {
            Assert.Equal(revision, version.Revision);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-tag")]
    public void TryParseVersion_InvalidTags_ReturnsFalse(string? tag)
    {
        var success = UpdateManager.TryParseVersion(tag, out var version);

        Assert.False(success);
        Assert.Null(version);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.0", "1.1.0", true)]
    [InlineData("1.0.0", "2.0.0", true)]
    [InlineData("1.0.0", "1.0.0", false)]
    [InlineData("1.2.0", "1.1.9", false)]
    public void IsNewerVersion_ComparesCorrectly(string currentStr, string latestStr, bool expected)
    {
        var current = Version.Parse(currentStr);
        var latest = Version.Parse(latestStr);

        var isNewer = UpdateManager.IsNewerVersion(current, latest);

        Assert.Equal(expected, isNewer);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter UpdateManagerTests`  
Expected: FAIL (types do not exist yet)

- [ ] **Step 3: Create `UpdateCheckResult.cs` and `UpdateManager.cs`**

`SystemIntegration/UpdateCheckResult.cs`:
```csharp
using System;

namespace Creature.SystemIntegration;

public record UpdateCheckResult(
    bool IsUpdateAvailable,
    Version CurrentVersion,
    Version? LatestVersion,
    string ReleaseNotes,
    string ReleaseUrl,
    string? ErrorMessage = null
);
```

`SystemIntegration/UpdateManager.cs`:
```csharp
using System;

namespace Creature.SystemIntegration;

public partial class UpdateManager : IDisposable
{
    public static bool TryParseVersion(string? tag, out Version? version)
    {
        version = null;
        if (string.IsNullOrWhiteSpace(tag))
        {
            return false;
        }

        var clean = tag.Trim().TrimStart('v', 'V');
        return Version.TryParse(clean, out version);
    }

    public static bool IsNewerVersion(Version current, Version latest)
    {
        return latest > current;
    }

    public virtual void Dispose()
    {
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter UpdateManagerTests`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add SystemIntegration/UpdateCheckResult.cs SystemIntegration/UpdateManager.cs Creature.Tests/SystemIntegration/UpdateManagerTests.cs
git commit -m "feat(update): add UpdateCheckResult and version parsing logic"
```

---

### Task 2: GitHub API Release Client & Winget / Browser Runner

**Files:**
- Modify: `SystemIntegration/UpdateManager.cs`
- Modify: `Creature.Tests/SystemIntegration/UpdateManagerTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public partial class UpdateManager
  {
      public const string GitHubRepoOwner = "kunal-jangid";
      public const string GitHubRepoName = "Creature";
      public const string WingetPackageId = "KunalJangid.DesktopPets";

      public UpdateManager(HttpClient? httpClient = null);
      public Version GetCurrentVersion();
      public Task<UpdateCheckResult> CheckForUpdatesAsync();
      public Task<bool> ApplyUpdateAsync(string releaseUrl);
      public static bool IsWingetInstalled();
  }
  ```

- [ ] **Step 1: Write failing tests for CheckForUpdatesAsync & GitHub parsing in `UpdateManagerTests.cs`**

```csharp
    [Fact]
    public void ParseGitHubReleaseJson_ValidJson_ReturnsReleaseInfo()
    {
        var json = """
        {
            "tag_name": "v1.2.0",
            "html_url": "https://github.com/kunal-jangid/Creature/releases/tag/v1.2.0",
            "body": "Fixed animations and improved stability."
        }
        """;

        var (tag, url, notes) = UpdateManager.ParseGitHubReleaseJson(json);

        Assert.Equal("v1.2.0", tag);
        Assert.Equal("https://github.com/kunal-jangid/Creature/releases/tag/v1.2.0", url);
        Assert.Equal("Fixed animations and improved stability.", notes);
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter ParseGitHubReleaseJson`  
Expected: FAIL

- [ ] **Step 3: Implement GitHub Release fetching, JSON parser, and Winget/Browser runner in `UpdateManager.cs`**

```csharp
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace Creature.SystemIntegration;

public partial class UpdateManager : IDisposable
{
    public const string GitHubRepoOwner = "kunal-jangid";
    public const string GitHubRepoName = "Creature";
    public const string WingetPackageId = "KunalJangid.DesktopPets";
    public const string DefaultReleaseUrl = "https://github.com/kunal-jangid/Creature/releases";

    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public UpdateManager(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
            _ownsHttpClient = false;
        }
        else
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DesktopPets-App", "1.0"));
            _ownsHttpClient = true;
        }
    }

    public virtual Version GetCurrentVersion()
    {
        var ver = Assembly.GetExecutingAssembly().GetName().Version;
        return ver ?? new Version(1, 0, 0);
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync()
    {
        var currentVersion = GetCurrentVersion();
        var apiUrl = $"https://api.github.com/repos/{GitHubRepoOwner}/{GitHubRepoName}/releases/latest";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
            {
                request.Headers.UserAgent.Add(new ProductInfoHeaderValue("DesktopPets-App", "1.0"));
            }

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseNotes: string.Empty,
                    ReleaseUrl: DefaultReleaseUrl,
                    ErrorMessage: $"GitHub API returned status {(int)response.StatusCode}: {response.ReasonPhrase}"
                );
            }

            var json = await response.Content.ReadAsStringAsync();
            var (tag, url, notes) = ParseGitHubReleaseJson(json);

            if (!TryParseVersion(tag, out var latestVersion) || latestVersion == null)
            {
                return new UpdateCheckResult(
                    IsUpdateAvailable: false,
                    CurrentVersion: currentVersion,
                    LatestVersion: null,
                    ReleaseNotes: notes,
                    ReleaseUrl: url,
                    ErrorMessage: $"Could not parse release tag: {tag}"
                );
            }

            var isNewer = IsNewerVersion(currentVersion, latestVersion);
            return new UpdateCheckResult(
                IsUpdateAvailable: isNewer,
                CurrentVersion: currentVersion,
                LatestVersion: latestVersion,
                ReleaseNotes: notes,
                ReleaseUrl: url
            );
        }
        catch (Exception ex)
        {
            return new UpdateCheckResult(
                IsUpdateAvailable: false,
                CurrentVersion: currentVersion,
                LatestVersion: null,
                ReleaseNotes: string.Empty,
                ReleaseUrl: DefaultReleaseUrl,
                ErrorMessage: ex.Message
            );
        }
    }

    public static (string Tag, string Url, string Notes) ParseGitHubReleaseJson(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var tag = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? "" : "";
        var url = root.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() ?? DefaultReleaseUrl : DefaultReleaseUrl;
        var notes = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? "" : "";

        return (tag, url, notes);
    }

    public static bool IsWingetInstalled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "winget",
                Arguments = "--version",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });

            if (process == null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ApplyUpdateAsync(string releaseUrl)
    {
        if (IsWingetInstalled())
        {
            try
            {
                var processInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = $"upgrade --id {WingetPackageId} --accept-source-agreements --accept-package-agreements",
                    UseShellExecute = true
                };

                using var process = Process.Start(processInfo);
                if (process != null)
                {
                    await Task.Delay(1000);
                    return true;
                }
            }
            catch
            {
                // Fallback to browser
            }
        }

        OpenBrowser(releaseUrl);
        return true;
    }

    public static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }

    public override void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter UpdateManagerTests`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add SystemIntegration/UpdateManager.cs Creature.Tests/SystemIntegration/UpdateManagerTests.cs
git commit -m "feat(update): add GitHub release checking and winget upgrade runner"
```

---

### Task 3: Settings & Background Periodic Update Timer

**Files:**
- Modify: `Settings/AppSettings.cs`
- Modify: `SystemIntegration/UpdateManager.cs`
- Modify: `Creature.Tests/Settings/SettingsManagerTests.cs`

**Interfaces:**
- Produces:
  ```csharp
  public class AppSettings
  {
      public bool AutoCheckUpdates { get; set; } = true;
      public DateTime? LastUpdateCheckUtc { get; set; }
  }

  public partial class UpdateManager
  {
      public void StartPeriodicChecks(TimeSpan initialDelay, TimeSpan interval, Action<UpdateCheckResult> onUpdateFound);
      public void StopPeriodicChecks();
  }
  ```

- [ ] **Step 1: Write test for AppSettings serialization of update settings**

In `Creature.Tests/Settings/SettingsManagerTests.cs`:
```csharp
    [Fact]
    public void AppSettings_UpdateSettings_SerializeCorrectly()
    {
        var settings = new AppSettings
        {
            AutoCheckUpdates = true,
            LastUpdateCheckUtc = new DateTime(2026, 10, 10, 12, 0, 0, DateTimeKind.Utc)
        };

        var json = JsonSerializer.Serialize(settings);
        var deserialized = JsonSerializer.Deserialize<AppSettings>(json);

        Assert.NotNull(deserialized);
        Assert.True(deserialized.AutoCheckUpdates);
        Assert.Equal(settings.LastUpdateCheckUtc, deserialized.LastUpdateCheckUtc);
    }
```

- [ ] **Step 2: Run test to verify it fails (missing properties on AppSettings)**

Run: `dotnet test --filter AppSettings_UpdateSettings_SerializeCorrectly`  
Expected: FAIL

- [ ] **Step 3: Update `AppSettings.cs` and add timer methods to `UpdateManager.cs`**

In `Settings/AppSettings.cs`:
```csharp
    public bool AutoCheckUpdates { get; set; } = true;
    public DateTime? LastUpdateCheckUtc { get; set; }
```

In `SystemIntegration/UpdateManager.cs`:
```csharp
    private System.Threading.Timer? _periodicTimer;

    public void StartPeriodicChecks(TimeSpan initialDelay, TimeSpan interval, Action<UpdateCheckResult> onUpdateFound)
    {
        _periodicTimer?.Dispose();
        _periodicTimer = new System.Threading.Timer(async _ =>
        {
            var result = await CheckForUpdatesAsync();
            if (result.IsUpdateAvailable)
            {
                onUpdateFound(result);
            }
        }, null, initialDelay, interval);
    }

    public void StopPeriodicChecks()
    {
        _periodicTimer?.Dispose();
        _periodicTimer = null;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test`  
Expected: PASS (all tests pass)

- [ ] **Step 5: Commit**

```bash
git add Settings/AppSettings.cs SystemIntegration/UpdateManager.cs Creature.Tests/Settings/SettingsManagerTests.cs
git commit -m "feat(update): add update settings and periodic background timer"
```

---

### Task 4: Tray UI & App Lifecycle Integration

**Files:**
- Modify: `Tray/TrayManager.cs`
- Modify: `App.xaml.cs`
- Modify: `Creature.Tests/Tray/TrayManagerTests.cs`

**Interfaces:**
- Produces:
  - `TrayManager.OnCheckForUpdates` callback or integrated update trigger.
  - Context menu item `"Check for Updates..."`.
  - Background update prompt logic on UI dispatcher.

- [ ] **Step 1: Write test in `TrayManagerTests.cs` verifying menu item exists**

```csharp
    [Fact]
    public void ContextMenu_ContainsCheckForUpdatesItem()
    {
        // verify "Check for Updates..." is in the generated ContextMenuStrip items
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter ContextMenu_ContainsCheckForUpdatesItem`  
Expected: FAIL

- [ ] **Step 3: Update `TrayManager.cs` and `App.xaml.cs`**

In `TrayManager.cs`:
- Add `public Func<Task>? OnCheckForUpdates { get; set; }`
- Add `"Check for Updates..."` tool strip item above `Exit`:
  ```csharp
  var updateItem = new ToolStripMenuItem("Check for Updates...", null, async (s, e) =>
  {
      if (OnCheckForUpdates != null)
      {
          await OnCheckForUpdates();
      }
  });
  menu.Items.Add(updateItem);
  ```

In `App.xaml.cs`:
- Instantiate `_updateManager = new UpdateManager();`
- Hook `_trayManager.OnCheckForUpdates = HandleManualUpdateCheckAsync;`
- In `OnStartup`: If `_settingsManager.CurrentSettings.AutoCheckUpdates` is true, call `_updateManager.StartPeriodicChecks(TimeSpan.FromSeconds(10), TimeSpan.FromHours(24), PromptUpdateAvailable);`
- In `OnExit`: Call `_updateManager?.Dispose();`
- Implement `HandleManualUpdateCheckAsync()` and `PromptUpdateAvailable()` with appropriate WPF `MessageBox` prompts on the UI Dispatcher.

- [ ] **Step 4: Run all tests to verify they pass**

Run: `dotnet test`  
Expected: PASS (all tests pass)

- [ ] **Step 5: Commit**

```bash
git add Tray/TrayManager.cs App.xaml.cs Creature.Tests/Tray/TrayManagerTests.cs
git commit -m "feat(update): integrate Check for Updates into tray menu and App lifecycle"
```

---

### Task 5: End-to-End Build & Installer Packaging Verification

**Files:**
- No file changes; verification across build, tests, and Inno Setup compiler.

- [ ] **Step 1: Run full test suite**

Run: `dotnet test -c Release`  
Expected: PASS (all tests pass)

- [ ] **Step 2: Publish single-file bundle with assets**

Run:
```powershell
dotnet publish Creature.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish
```
Expected: Build succeeds with code 0.

- [ ] **Step 3: Compile Inno Setup installer**

Run:
```powershell
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" "/dAppVersion=1.0.0" "installer\creature_setup.iss"
```
Expected: Installer compiles successfully to `dist\DesktopPets-Setup-v1.0.0.exe`.

- [ ] **Step 4: Final commit / push prep**

```bash
git status
```
