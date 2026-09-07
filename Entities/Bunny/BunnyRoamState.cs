using System;
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyRoamState : IState<DesktopPet>
{
    private double _roamDuration;
    private double _elapsed;
    private double _speed = 50.0;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.PlayAnimation("BunnyRun", loop: true);
        _roamDuration = _random.NextDouble() * 3.0 + 2.0; // 2 to 5 seconds
        _elapsed = 0;

        var movingRight = _random.Next(2) == 0;
        ApplyDirection(pet, movingRight);
    }

    public void ApplyDirection(DesktopPet pet, bool movingRight, double? speed = null)
    {
        if (speed.HasValue)
        {
            _speed = speed.Value;
        }

        pet.Transform.IsFacingLeft = !movingRight;
        pet.Physics.Velocity = new Vector2D(movingRight ? _speed : -_speed, 0);
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;

        // Turn around at monitor boundaries
        if (pet.Transform.Position.X <= pet.MonitorWorkingArea.Left && pet.Physics.Velocity.X < 0)
        {
            ApplyDirection(pet, movingRight: true);
        }
        else if (pet.Transform.Position.X + pet.Transform.ScaledWidth >= pet.MonitorWorkingArea.Right && pet.Physics.Velocity.X > 0)
        {
            ApplyDirection(pet, movingRight: false);
        }

        if (_elapsed >= _roamDuration)
        {
            pet.StateMachine.ChangeState(new BunnyIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
