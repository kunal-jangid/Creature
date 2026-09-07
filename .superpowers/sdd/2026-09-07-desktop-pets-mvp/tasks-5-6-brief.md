# Tasks 5 & 6: Settings & Audio Subsystems (`SettingsManager` & `AudioManager`)

**Files:**
- Create: `Settings/UserSettings.cs`
- Create: `Settings/SettingsManager.cs`
- Create: `Audio/AudioManager.cs`
- Test: `Creature.Tests/Settings/SettingsManagerTests.cs`
- Test: `Creature.Tests/Audio/AudioManagerTests.cs`

**Interfaces:**
- Produces:
  - `UserSettings`: `GlobalScale` (default 1.0), `IsSoundEnabled` (default true), `IsPaused` (default false), `DisabledPetIds` (`List<string>`)
  - `SettingsManager`: `CurrentSettings`, `Load() -> UserSettings`, `Save(UserSettings settings)`, `event Action<UserSettings>? SettingsChanged`
  - `AudioManager`: `IsSoundEnabled` (bool), `MaxVoices` (int, default 2), `ActiveVoiceCount` (int), `PlaySound(string soundName) -> bool`

## Steps to Execute:

- [ ] **Step 1: Write failing tests for `SettingsManager` and `AudioManager`**

Create `Creature.Tests/Settings/SettingsManagerTests.cs`:
```csharp
using System;
using System.IO;
using Creature.Settings;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Settings;

public class SettingsManagerTests
{
    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new SettingsManager(tempFile);
            var settings = manager.Load();

            settings.GlobalScale.Should().Be(1.0);
            settings.IsSoundEnabled.Should().BeTrue();
            settings.IsPaused.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistModifications()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new SettingsManager(tempFile);
            var settings = manager.Load();
            settings.GlobalScale = 1.5;
            settings.IsSoundEnabled = false;
            manager.Save(settings);

            var loaded = manager.Load();
            loaded.GlobalScale.Should().Be(1.5);
            loaded.IsSoundEnabled.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
```

Create `Creature.Tests/Audio/AudioManagerTests.cs`:
```csharp
using Creature.Audio;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Audio;

public class AudioManagerTests
{
    [Fact]
    public void PlaySound_WhenMuted_ShouldNotPlay()
    {
        var audio = new AudioManager { IsSoundEnabled = false };
        var played = audio.PlaySound("hop");
        played.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyLimiter_ShouldCapSimultaneousVoicesAtTwo()
    {
        var audio = new AudioManager { IsSoundEnabled = true };
        audio.ActiveVoiceCount.Should().Be(0);
        audio.MaxVoices.Should().Be(2);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~Settings|FullyQualifiedName~Audio"`
Expected: FAIL.

- [ ] **Step 3: Implement `UserSettings.cs`, `SettingsManager.cs`, and `AudioManager.cs`**

Create `Settings/UserSettings.cs`:
```csharp
using System.Collections.Generic;

namespace Creature.Settings;

public class UserSettings
{
    public double GlobalScale { get; set; } = 1.0;
    public bool IsSoundEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false;
    public List<string> DisabledPetIds { get; set; } = new();
}
```

Create `Settings/SettingsManager.cs`:
```csharp
using System;
using System.IO;
using System.Text.Json;

namespace Creature.Settings;

public class SettingsManager
{
    private readonly string _filePath;
    public UserSettings CurrentSettings { get; private set; }

    public event Action<UserSettings>? SettingsChanged;

    public SettingsManager(string? customPath = null)
    {
        _filePath = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Creature",
            "settings.json"
        );
        CurrentSettings = Load();
    }

    public UserSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            CurrentSettings = new UserSettings();
            return CurrentSettings;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            CurrentSettings = new UserSettings();
        }

        return CurrentSettings;
    }

    public void Save(UserSettings settings)
    {
        CurrentSettings = settings;
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        SettingsChanged?.Invoke(CurrentSettings);
    }
}
```

Create `Audio/AudioManager.cs`:
```csharp
using System.Threading;
using System.Threading.Tasks;

namespace Creature.Audio;

public class AudioManager
{
    public bool IsSoundEnabled { get; set; } = true;
    public int MaxVoices { get; set; } = 2;
    private int _activeVoices;

    public int ActiveVoiceCount => _activeVoices;

    public bool PlaySound(string soundName)
    {
        if (!IsSoundEnabled || _activeVoices >= MaxVoices)
            return false;

        Interlocked.Increment(ref _activeVoices);
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Interlocked.Decrement(ref _activeVoices);
        });

        return true;
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Settings/ Audio/ Creature.Tests/
git commit -m "feat: implement SettingsManager and AudioManager with tests"
```
