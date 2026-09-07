using System;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyIdleState : IState<DesktopPet>
{
    private double _idleDuration;
    private double _elapsed;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.Physics.Velocity = Core.Physics.Vector2D.Zero;
        pet.PlayAnimation("BunnyLieDown", loop: true);
        _idleDuration = _random.NextDouble() * 4.0 + 3.0; // 3 to 7 seconds
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= _idleDuration)
        {
            pet.StateMachine.ChangeState(new BunnyRoamState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
