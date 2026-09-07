using System;
using System.IO;
using System.Windows;
using Creature.Core.Physics;
using Creature.Entities.Werewolf;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Entities;

public class WerewolfEntityTests
{
    private SpriteManager CreateTestSpriteManager()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Werewolf");
        sm.LoadAnimation("WerewolfIdle", Path.Combine(baseDir, "Idle.json"), Path.Combine(baseDir, "Idle-sheet.png"));
        sm.LoadAnimation("WerewolfRun", Path.Combine(baseDir, "Run.json"), Path.Combine(baseDir, "Run-sheet.png"));
        sm.LoadAnimation("WerewolfFlinch", Path.Combine(baseDir, "Dead.json"), Path.Combine(baseDir, "Dead-sheet.png"), loop: false);
        return sm;
    }

    [StaFact]
    public void WerewolfEntity_ShouldStartInIdleState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var werewolf = new WerewolfEntity("werewolf-1", spriteManager, bounds, "Fang");

        werewolf.Name.Should().Be("Fang");
        werewolf.CurrentStateName.Should().Be("Idle");
        werewolf.Transform.BaseWidth.Should().Be(64);
        werewolf.Transform.BaseHeight.Should().Be(64);
        werewolf.Transform.Position.Y.Should().Be(1080 - 64);
    }

    [StaFact]
    public void WerewolfEntity_OnCursorHover_ShouldTransitionToFlinchState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var werewolf = new WerewolfEntity("werewolf-1", spriteManager, bounds);

        werewolf.OnCursorHover();
        werewolf.CurrentStateName.Should().Be("Flinch");

        // After flinch duration, should return to Idle
        for (int i = 0; i < 20; i++)
        {
            werewolf.Update(0.1);
        }

        werewolf.CurrentStateName.Should().Be("Idle");
    }

    [StaFact]
    public void WerewolfEntity_WhenReachingBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 200, 200);
        var werewolf = new WerewolfEntity("werewolf-1", spriteManager, bounds);

        werewolf.Transform.Position = new Vector2D(200 - 64, 200 - 64);
        werewolf.SetRoamDirection(movingRight: true, speed: 60);

        werewolf.Update(0.1);

        werewolf.Transform.IsFacingLeft.Should().BeTrue();
        werewolf.Physics.Velocity.X.Should().BeLessThan(0);
    }
}
