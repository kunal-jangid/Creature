using Creature.Core.Physics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Core;

public class PhysicsTests
{
    [Fact]
    public void PhysicsBody_WhenAboveFloor_ShouldApplyGravityUntilLanding()
    {
        var transform = new Transform2D { Position = new Vector2D(100, 400) };
        var physics = new PhysicsBody(transform)
        {
            FloorY = 500,
            Gravity = 980,
            Velocity = new Vector2D(0, 0)
        };

        physics.IsOnGround.Should().BeFalse();

        // Simulate 0.1s
        physics.Update(0.1);
        physics.Velocity.Y.Should().BeApproximately(98, 0.1);
        transform.Position.Y.Should().BeGreaterThan(400);

        // Simulate enough time to hit ground
        for (int i = 0; i < 20; i++)
            physics.Update(0.1);

        transform.Position.Y.Should().Be(500);
        physics.IsOnGround.Should().BeTrue();
        physics.Velocity.Y.Should().Be(0);
    }

    [Fact]
    public void Vector2D_ArithmeticOperations_ShouldBehaveCorrectly()
    {
        var a = new Vector2D(10, 20);
        var b = new Vector2D(3, 7);

        (a + b).Should().Be(new Vector2D(13, 27));
        (a - b).Should().Be(new Vector2D(7, 13));
        (a * 2.5).Should().Be(new Vector2D(25, 50));
        Vector2D.Zero.Should().Be(new Vector2D(0, 0));
    }

    [Fact]
    public void Transform2D_ScaledDimensionsAndBoundingBox_ShouldReflectScaleAndPosition()
    {
        var transform = new Transform2D
        {
            Position = new Vector2D(50, 60),
            Scale = 2.0,
            BaseWidth = 32,
            BaseHeight = 32,
            IsFacingLeft = true
        };

        transform.ScaledWidth.Should().Be(64);
        transform.ScaledHeight.Should().Be(64);
        transform.BoundingBox.X.Should().Be(50);
        transform.BoundingBox.Y.Should().Be(60);
        transform.BoundingBox.Width.Should().Be(64);
        transform.BoundingBox.Height.Should().Be(64);
        transform.IsFacingLeft.Should().BeTrue();
    }
}
