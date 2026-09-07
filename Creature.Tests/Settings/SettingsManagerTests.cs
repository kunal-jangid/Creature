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
            settings.DisabledPetIds.Should().BeEmpty();
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
            settings.IsPaused = true;
            settings.DisabledPetIds.Add("fox");
            manager.Save(settings);

            var loaded = manager.Load();
            loaded.GlobalScale.Should().Be(1.5);
            loaded.IsSoundEnabled.Should().BeFalse();
            loaded.IsPaused.Should().BeTrue();
            loaded.DisabledPetIds.Should().Contain("fox");
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void Save_ShouldTriggerSettingsChangedEvent()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new SettingsManager(tempFile);
            UserSettings? notifiedSettings = null;
            manager.SettingsChanged += s => notifiedSettings = s;

            var settings = manager.Load();
            settings.GlobalScale = 2.0;
            manager.Save(settings);

            notifiedSettings.Should().NotBeNull();
            notifiedSettings!.GlobalScale.Should().Be(2.0);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
