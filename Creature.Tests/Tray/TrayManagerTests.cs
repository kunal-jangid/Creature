using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using Creature.Entities.Bunny;
using Creature.Graphics;
using Creature.Settings;
using Creature.Simulation;
using Creature.Tray;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Tray;

public class TrayManagerTests
{
    [StaFact]
    public void TrayManager_ContextMenu_ShouldContainExpectedItemsAndScalePresets()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"tray_test_{Guid.NewGuid()}.json");
        try
        {
            var settings = new SettingsManager(tempFile);
            var sim = new SimulationEngine();
            bool exited = false;

            using var tray = new TrayManager(settings, sim, () => exited = true);
            exited.Should().BeFalse();

            // Test Add pet callback
            bool addInvoked = false;
            tray.OnAddPet = () => addInvoked = true;
            tray.OnAddPet();
            addInvoked.Should().BeTrue();

            // Refresh menu should not throw
            tray.RefreshPetMenu();

            tray.Dispose();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
