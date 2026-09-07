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

            settings.GlobalScale.Should().Be(2.0);
            settings.ShowPetNames.Should().BeTrue();
            settings.IsPaused.Should().BeFalse();
            settings.Pets.Should().BeEmpty();
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
            settings.GlobalScale = 2.5;
            settings.ShowPetNames = false;
            settings.IsPaused = true;
            settings.Pets.Add(new PetProfile { Id = "bunny-1", Name = "Fluffy", Species = "Bunny" });
            settings.DisabledPetIds.Add("fox");
            manager.Save(settings);

            var loaded = manager.Load();
            loaded.GlobalScale.Should().Be(2.5);
            loaded.ShowPetNames.Should().BeFalse();
            loaded.IsPaused.Should().BeTrue();
            loaded.Pets.Should().HaveCount(1);
            loaded.Pets[0].Name.Should().Be("Fluffy");
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
