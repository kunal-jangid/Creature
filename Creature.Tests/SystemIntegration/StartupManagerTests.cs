using System;
using Creature.SystemIntegration;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.SystemIntegration;

public class StartupManagerTests
{
    [Fact]
    public void SetStartup_EnableAndDisable_ShouldToggleRegistryState()
    {
        var testExe = @"C:\Program Files\Creature\Creature.exe";

        // Test enabling startup
        var enabled = StartupManager.SetStartup(true, testExe);
        enabled.Should().BeTrue();
        StartupManager.IsStartupEnabled().Should().BeTrue();

        // Test disabling startup
        var disabled = StartupManager.SetStartup(false);
        disabled.Should().BeTrue();
        StartupManager.IsStartupEnabled().Should().BeFalse();
    }
}

