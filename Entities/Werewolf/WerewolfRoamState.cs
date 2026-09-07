using System;
using Creature.Core.Physics;
using Creature.Core.StateMachine;

namespace Creature.Entities.Werewolf;

public class WerewolfRoamState : IState<DesktopPet>
{
    private double _roamDuration;
    private double _elapsed;
    private double _speed = 60.0;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.PlayAnimation("WerewolfRun", loop: true);
        _roamDuration = _random.NextDouble() * 4.0 + 2.0;
        _elapsed = 0;

        var movingRight = _random.Next(2) == 0;
        ApplyDirection(pet, movingRight, _speed);
    }

    public void ApplyDirection(DesktopPet pet, bool movingRight, double speed = 60.0)
    {
        _speed = speed;
        pet.Transform.IsFacingLeft = !movingRight;
        pet.Physics.Velocity = new Vector2D(movingRight ? _speed : -_speed, 0);
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;

        if (pet.Transform.Position.X <= pet.MonitorWorkingArea.Left && pet.Physics.Velocity.X < 0)
        {
            ApplyDirection(pet, movingRight: true, _speed);
        }
        else if (pet.Transform.Position.X + pet.Transform.ScaledWidth >= pet.MonitorWorkingArea.Right && pet.Physics.Velocity.X > 0)
        {
            ApplyDirection(pet, movingRight: false, _speed);
        }

        if (_elapsed >= _roamDuration)
        {
            pet.StateMachine.ChangeState(new WerewolfIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
