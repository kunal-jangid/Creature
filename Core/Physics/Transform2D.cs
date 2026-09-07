using System.Windows;

namespace Creature.Core.Physics;

public class Transform2D
{
    public Vector2D Position { get; set; } = Vector2D.Zero;
    public double Scale { get; set; } = 1.0;
    public bool IsFacingLeft { get; set; } = false;
    public double BaseWidth { get; set; } = 32;
    public double BaseHeight { get; set; } = 32;

    public double ScaledWidth => BaseWidth * Scale;
    public double ScaledHeight => BaseHeight * Scale;

    public Rect BoundingBox => new(Position.X, Position.Y, ScaledWidth, ScaledHeight);
}
