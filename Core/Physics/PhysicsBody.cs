namespace Creature.Core.Physics;

public class PhysicsBody
{
    private readonly Transform2D _transform;

    public Vector2D Velocity { get; set; } = Vector2D.Zero;
    public double Gravity { get; set; } = 980.0;
    public double FloorY { get; set; } = 0.0;
    public bool IsOnGround { get; private set; } = false;

    public PhysicsBody(Transform2D transform)
    {
        _transform = transform;
    }

    public void Update(double dt)
    {
        if (!_transform.Position.Y.Equals(FloorY) || Velocity.Y < 0)
        {
            Velocity = new Vector2D(Velocity.X, Velocity.Y + Gravity * dt);
        }

        var newX = _transform.Position.X + Velocity.X * dt;
        var newY = _transform.Position.Y + Velocity.Y * dt;

        if (newY >= FloorY)
        {
            newY = FloorY;
            Velocity = new Vector2D(Velocity.X, 0);
            IsOnGround = true;
        }
        else
        {
            IsOnGround = false;
        }

        _transform.Position = new Vector2D(newX, newY);
    }
}
