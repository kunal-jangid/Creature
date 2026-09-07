using System;
using System.IO;
using System.Windows;
using Creature.Core.Physics;
using Creature.Entities.Bunny;
using Creature.Graphics;
using Creature.Simulation;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Simulation;

public class SimulationEngineTests
{
    [StaFact]
    public void HitTest_WhenCursorOverPet_ShouldReturnPet()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny = new BunnyEntity("bunny-1", sm, bounds);
        bunny.Transform.Position = new Vector2D(100, 100);
        engine.AddPet(bunny);

        var hit = engine.HitTest(new Point(110, 110));
        hit.Should().Be(bunny);

        var miss = engine.HitTest(new Point(500, 500));
        miss.Should().BeNull();
    }

    [StaFact]
    public void ApplyGlobalScale_ShouldUpdateScaleAndFloorForAllPets()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny = new BunnyEntity("bunny-1", sm, bounds);
        engine.AddPet(bunny);

        engine.ApplyGlobalScale(1.5);

        bunny.Transform.Scale.Should().Be(1.5);
        bunny.Transform.ScaledWidth.Should().Be(48);
        bunny.Transform.ScaledHeight.Should().Be(48);
        bunny.Physics.FloorY.Should().Be(1080 - 48);
    }

    [StaFact]
    public void AddAndRemovePet_ShouldUpdatePetsList()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny = new BunnyEntity("bunny-1", sm, bounds);

        engine.AddPet(bunny);
        engine.Pets.Should().Contain(bunny);

        engine.RemovePet(bunny);
        engine.Pets.Should().NotContain(bunny);
    }

    [Fact]
    public void IsPaused_ShouldDefaultToFalse_AndBeSettable()
    {
        var engine = new SimulationEngine();
        engine.IsPaused.Should().BeFalse();

        engine.IsPaused = true;
        engine.IsPaused.Should().BeTrue();
    }

    [StaFact]
    public void StartAndStop_ShouldManageRunningStateWithoutException()
    {
        var engine = new SimulationEngine();
        var actStart = () => engine.Start();
        actStart.Should().NotThrow();

        // Calling start again when already running should be a no-op
        actStart.Should().NotThrow();

        var actStop = () => engine.Stop();
        actStop.Should().NotThrow();

        // Calling stop again when stopped should be a no-op
        actStop.Should().NotThrow();
    }

    [StaFact]
    public void SetShowPetNames_ShouldUpdateVisibilityForAllPets()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny1 = new BunnyEntity("bunny-1", sm, bounds, "B1");
        var bunny2 = new BunnyEntity("bunny-2", sm, bounds, "B2");
        engine.AddPet(bunny1);
        engine.AddPet(bunny2);

        engine.SetShowPetNames(false);
        bunny1.NameLabel.Visibility.Should().Be(Visibility.Collapsed);
        bunny2.NameLabel.Visibility.Should().Be(Visibility.Collapsed);

        engine.SetShowPetNames(true);
        bunny1.NameLabel.Visibility.Should().Be(Visibility.Visible);
        bunny2.NameLabel.Visibility.Should().Be(Visibility.Visible);
    }
}
