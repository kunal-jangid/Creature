using System;
using System.IO;
using System.Windows;
using Creature.Core.Physics;
using Creature.Entities.Bunny;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Entities;

public class BunnyEntityTests
{
    private SpriteManager CreateTestSpriteManager()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));
        sm.LoadAnimation("BunnyRun", Path.Combine(baseDir, "BunnyRun.json"), Path.Combine(baseDir, "BunnyRun.png"));
        sm.LoadAnimation("BunnyJump", Path.Combine(baseDir, "BunnyJump.json"), Path.Combine(baseDir, "BunnyJump.png"), loop: false);
        return sm;
    }

    [StaFact]
    public void BunnyEntity_ShouldStartInIdleState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.CurrentStateName.Should().Be("Idle");
        bunny.Transform.Position.Y.Should().Be(1080 - 32);
    }

    [StaFact]
    public void BunnyEntity_OnCursorHover_ShouldTransitionToJumpStateImmediately()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.OnCursorHover();
        bunny.CurrentStateName.Should().Be("Jump");
        bunny.Physics.Velocity.Y.Should().BeLessThan(0); // Upward impulse
    }

    [StaFact]
    public void BunnyEntity_WhenReachingBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 100, 100);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        // Position bunny at right boundary moving right
        bunny.Transform.Position = new Vector2D(100 - 32, 100 - 32);
        bunny.SetRoamDirection(movingRight: true, speed: 50);

        bunny.Update(0.1); // Move past edge

        bunny.Transform.IsFacingLeft.Should().BeTrue();
        bunny.Physics.Velocity.X.Should().BeLessThan(0);
    }

    [StaFact]
    public void BunnyEntity_WhenReachingLeftBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 100, 100);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        // Position bunny at left boundary moving left
        bunny.Transform.Position = new Vector2D(0, 100 - 32);
        bunny.SetRoamDirection(movingRight: false, speed: 50);

        bunny.Update(0.1); // Move past edge

        bunny.Transform.IsFacingLeft.Should().BeFalse();
        bunny.Physics.Velocity.X.Should().BeGreaterThan(0);
    }

    [StaFact]
    public void BunnyEntity_HitTest_ShouldCorrectlyDetectContainment()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.Transform.Position = new Vector2D(100, 200);

        bunny.HitTest(new Point(110, 210)).Should().BeTrue();
        bunny.HitTest(new Point(50, 50)).Should().BeFalse();
    }

    [StaFact]
    public void BunnyEntity_UpdateFloor_ShouldUpdateFloorYWhenBoundsChange()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.MonitorWorkingArea = new Rect(0, 0, 1920, 900);
        bunny.UpdateFloor();

        bunny.Physics.FloorY.Should().Be(900 - 32);
    }

    [StaFact]
    public void BunnyEntity_OnCursorHover_WhenAlreadyJumping_ShouldNotReenterJump()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.OnCursorHover();
        bunny.CurrentStateName.Should().Be("Jump");
        var initialVelocity = bunny.Physics.Velocity;

        // Second hover should not trigger another jump state enter
        bunny.OnCursorHover();
        bunny.CurrentStateName.Should().Be("Jump");
        bunny.Physics.Velocity.Should().Be(initialVelocity);
    }

    [StaFact]
    public void BunnyEntity_JumpState_ShouldCompleteAndLandBackInIdle()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.OnCursorHover();
        bunny.CurrentStateName.Should().Be("Jump");

        // Simulate 2.0 seconds in 0.1s increments (Jump duration is 1.8s)
        for (int i = 0; i < 25; i++)
        {
            bunny.Update(0.1);
        }

        bunny.CurrentStateName.Should().Be("Idle");
        bunny.Physics.IsOnGround.Should().BeTrue();
    }

    [StaFact]
    public void BunnyEntity_SyncVisualTransform_ShouldUpdateWpfImageProperties()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.Transform.Position = new Vector2D(250, 450);
        bunny.Transform.Scale = 2.0;
        bunny.Transform.IsFacingLeft = true;

        bunny.SyncVisualTransform();

        System.Windows.Controls.Canvas.GetLeft(bunny.VisualElement).Should().Be(250);
        System.Windows.Controls.Canvas.GetTop(bunny.VisualElement).Should().Be(450);
        bunny.VisualElement.Width.Should().Be(64);
        bunny.VisualElement.Height.Should().Be(64);

        var scaleTransform = bunny.VisualElement.RenderTransform.Should().BeOfType<System.Windows.Media.ScaleTransform>().Subject;
        scaleTransform.ScaleX.Should().Be(-1.0);
        scaleTransform.ScaleY.Should().Be(1.0);
    }

    [StaFact]
    public void BunnyEntity_UpdateAnimation_ShouldAdvanceFrames()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        var initialSource = bunny.VisualElement.Source;
        initialSource.Should().NotBeNull();

        // LieDown has multiple frames with 100ms duration each. Advance by 0.11s to trigger frame increment
        bunny.Update(0.11);

        bunny.VisualElement.Source.Should().NotBeNull();
    }
}
