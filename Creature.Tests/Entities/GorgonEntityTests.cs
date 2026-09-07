using System;
using System.IO;
using System.Windows;
using Creature.Core.Physics;
using Creature.Entities.Gorgon;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Entities;

public class GorgonEntityTests
{
    private SpriteManager CreateTestSpriteManager()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Gorgon");
        sm.LoadAnimation("GorgonIdle", Path.Combine(baseDir, "Idle.json"), Path.Combine(baseDir, "Idle-sheet.png"));
        sm.LoadAnimation("GorgonRun", Path.Combine(baseDir, "Run.json"), Path.Combine(baseDir, "Run-sheet.png"));
        sm.LoadAnimation("GorgonSpecial", Path.Combine(baseDir, "Special.json"), Path.Combine(baseDir, "Special-sheet.png"), loop: false);
        return sm;
    }

    [StaFact]
    public void GorgonEntity_ShouldStartInIdleState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var gorgon = new GorgonEntity("gorgon-1", spriteManager, bounds, "Medusa");

        gorgon.Name.Should().Be("Medusa");
        gorgon.CurrentStateName.Should().Be("Idle");
        gorgon.SimpnessFactor.Should().Be(0.30);
        gorgon.Transform.BaseWidth.Should().Be(64);
        gorgon.Transform.BaseHeight.Should().Be(64);
        gorgon.Transform.Position.Y.Should().Be(1080 - 64);

        // Faces cursor when near (pet center is around X=960, cursor at X=800 is dx=-160)
        gorgon.HandleCursorInteraction(new Point(800, 1080 - 64), 0.016);
        gorgon.Transform.IsFacingLeft.Should().BeTrue();
    }

    [StaFact]
    public void GorgonEntity_OnCursorHover_ShouldTransitionToSpecialState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var gorgon = new GorgonEntity("gorgon-1", spriteManager, bounds);

        gorgon.OnCursorHover();
        gorgon.CurrentStateName.Should().Be("Special");

        // After special completes, should return to Idle
        for (int i = 0; i < 20; i++)
        {
            gorgon.Update(0.1);
        }

        gorgon.CurrentStateName.Should().Be("Idle");
    }

    [StaFact]
    public void GorgonEntity_WhenReachingBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 200, 200);
        var gorgon = new GorgonEntity("gorgon-1", spriteManager, bounds);

        gorgon.Transform.Position = new Vector2D(200 - 64, 200 - 64);
        gorgon.SetRoamDirection(movingRight: true, speed: 40);

        gorgon.Update(0.1);

        gorgon.Transform.IsFacingLeft.Should().BeTrue();
        gorgon.Physics.Velocity.X.Should().BeLessThan(0);
    }
}
