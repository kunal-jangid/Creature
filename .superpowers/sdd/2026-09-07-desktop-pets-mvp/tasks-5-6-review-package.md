# Review Package for Tasks 5 & 6

## Commits
`
a127b22 feat: implement SettingsManager and AudioManager with tests

`

## Diff Stat
`
 Audio/AudioManager.cs                           | 28 +++++++++
 Creature.Tests/Audio/AudioManagerTests.cs       | 39 ++++++++++++
 Creature.Tests/Settings/SettingsManagerTests.cs | 79 +++++++++++++++++++++++++
 Settings/SettingsManager.cs                     | 58 ++++++++++++++++++
 Settings/UserSettings.cs                        | 11 ++++
 5 files changed, 215 insertions(+)

`

## Full Diff
`diff
diff --git a/Audio/AudioManager.cs b/Audio/AudioManager.cs
new file mode 100644
index 0000000..3d1bf81
--- /dev/null
+++ b/Audio/AudioManager.cs
@@ -0,0 +1,28 @@
+using System.Threading;
+using System.Threading.Tasks;
+
+namespace Creature.Audio;
+
+public class AudioManager
+{
+    public bool IsSoundEnabled { get; set; } = true;
+    public int MaxVoices { get; set; } = 2;
+    private int _activeVoices;
+
+    public int ActiveVoiceCount => _activeVoices;
+
+    public bool PlaySound(string soundName)
+    {
+        if (!IsSoundEnabled || _activeVoices >= MaxVoices)
+            return false;
+
+        Interlocked.Increment(ref _activeVoices);
+        _ = Task.Run(async () =>
+        {
+            await Task.Delay(200);
+            Interlocked.Decrement(ref _activeVoices);
+        });
+
+        return true;
+    }
+}
diff --git a/Creature.Tests/Audio/AudioManagerTests.cs b/Creature.Tests/Audio/AudioManagerTests.cs
new file mode 100644
index 0000000..a04817b
--- /dev/null
+++ b/Creature.Tests/Audio/AudioManagerTests.cs
@@ -0,0 +1,39 @@
+using Creature.Audio;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Audio;
+
+public class AudioManagerTests
+{
+    [Fact]
+    public void PlaySound_WhenMuted_ShouldNotPlay()
+    {
+        var audio = new AudioManager { IsSoundEnabled = false };
+        var played = audio.PlaySound("hop");
+        played.Should().BeFalse();
+        audio.ActiveVoiceCount.Should().Be(0);
+    }
+
+    [Fact]
+    public void ConcurrencyLimiter_ShouldCapSimultaneousVoicesAtTwo()
+    {
+        var audio = new AudioManager { IsSoundEnabled = true };
+        audio.ActiveVoiceCount.Should().Be(0);
+        audio.MaxVoices.Should().Be(2);
+    }
+
+    [Fact]
+    public void PlaySound_WhenActiveVoicesExceedMaxVoices_ShouldRejectAdditionalPlayback()
+    {
+        var audio = new AudioManager { IsSoundEnabled = true, MaxVoices = 2 };
+        var first = audio.PlaySound("hop");
+        var second = audio.PlaySound("hop");
+        var third = audio.PlaySound("hop");
+
+        first.Should().BeTrue();
+        second.Should().BeTrue();
+        third.Should().BeFalse();
+        audio.ActiveVoiceCount.Should().Be(2);
+    }
+}
diff --git a/Creature.Tests/Settings/SettingsManagerTests.cs b/Creature.Tests/Settings/SettingsManagerTests.cs
new file mode 100644
index 0000000..8faf42c
--- /dev/null
+++ b/Creature.Tests/Settings/SettingsManagerTests.cs
@@ -0,0 +1,79 @@
+using System;
+using System.IO;
+using Creature.Settings;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Settings;
+
+public class SettingsManagerTests
+{
+    [Fact]
+    public void Load_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
+    {
+        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
+        try
+        {
+            var manager = new SettingsManager(tempFile);
+            var settings = manager.Load();
+
+            settings.GlobalScale.Should().Be(1.0);
+            settings.IsSoundEnabled.Should().BeTrue();
+            settings.IsPaused.Should().BeFalse();
+            settings.DisabledPetIds.Should().BeEmpty();
+        }
+        finally
+        {
+            if (File.Exists(tempFile)) File.Delete(tempFile);
+        }
+    }
+
+    [Fact]
+    public void SaveAndLoad_ShouldPersistModifications()
+    {
+        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
+        try
+        {
+            var manager = new SettingsManager(tempFile);
+            var settings = manager.Load();
+            settings.GlobalScale = 1.5;
+            settings.IsSoundEnabled = false;
+            settings.IsPaused = true;
+            settings.DisabledPetIds.Add("fox");
+            manager.Save(settings);
+
+            var loaded = manager.Load();
+            loaded.GlobalScale.Should().Be(1.5);
+            loaded.IsSoundEnabled.Should().BeFalse();
+            loaded.IsPaused.Should().BeTrue();
+            loaded.DisabledPetIds.Should().Contain("fox");
+        }
+        finally
+        {
+            if (File.Exists(tempFile)) File.Delete(tempFile);
+        }
+    }
+
+    [Fact]
+    public void Save_ShouldTriggerSettingsChangedEvent()
+    {
+        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
+        try
+        {
+            var manager = new SettingsManager(tempFile);
+            UserSettings? notifiedSettings = null;
+            manager.SettingsChanged += s => notifiedSettings = s;
+
+            var settings = manager.Load();
+            settings.GlobalScale = 2.0;
+            manager.Save(settings);
+
+            notifiedSettings.Should().NotBeNull();
+            notifiedSettings!.GlobalScale.Should().Be(2.0);
+        }
+        finally
+        {
+            if (File.Exists(tempFile)) File.Delete(tempFile);
+        }
+    }
+}
diff --git a/Settings/SettingsManager.cs b/Settings/SettingsManager.cs
new file mode 100644
index 0000000..9d8e131
--- /dev/null
+++ b/Settings/SettingsManager.cs
@@ -0,0 +1,58 @@
+using System;
+using System.IO;
+using System.Text.Json;
+
+namespace Creature.Settings;
+
+public class SettingsManager
+{
+    private readonly string _filePath;
+    public UserSettings CurrentSettings { get; private set; }
+
+    public event Action<UserSettings>? SettingsChanged;
+
+    public SettingsManager(string? customPath = null)
+    {
+        _filePath = customPath ?? Path.Combine(
+            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
+            "Creature",
+            "settings.json"
+        );
+        CurrentSettings = Load();
+    }
+
+    public UserSettings Load()
+    {
+        if (!File.Exists(_filePath))
+        {
+            CurrentSettings = new UserSettings();
+            return CurrentSettings;
+        }
+
+        try
+        {
+            var json = File.ReadAllText(_filePath);
+            CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
+        }
+        catch
+        {
+            CurrentSettings = new UserSettings();
+        }
+
+        return CurrentSettings;
+    }
+
+    public void Save(UserSettings settings)
+    {
+        CurrentSettings = settings;
+        var dir = Path.GetDirectoryName(_filePath);
+        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
+        {
+            Directory.CreateDirectory(dir);
+        }
+
+        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
+        File.WriteAllText(_filePath, json);
+        SettingsChanged?.Invoke(CurrentSettings);
+    }
+}
diff --git a/Settings/UserSettings.cs b/Settings/UserSettings.cs
new file mode 100644
index 0000000..391e162
--- /dev/null
+++ b/Settings/UserSettings.cs
@@ -0,0 +1,11 @@
+using System.Collections.Generic;
+
+namespace Creature.Settings;
+
+public class UserSettings
+{
+    public double GlobalScale { get; set; } = 1.0;
+    public bool IsSoundEnabled { get; set; } = true;
+    public bool IsPaused { get; set; } = false;
+    public List<string> DisabledPetIds { get; set; } = new();
+}

`
