using System;
using Creature.Core.Physics;
using Creature.Core.StateMachine;

namespace Creature.Entities.Gorgon;

public class GorgonIdleState : IState<DesktopPet>
{
    private double _idleDuration;
    private double _elapsed;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.Physics.Velocity = Vector2D.Zero;
        pet.PlayAnimation("GorgonIdle", loop: true);
        _idleDuration = _random.NextDouble() * 4.0 + 3.0;
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= _idleDuration)
        {
            pet.StateMachine.ChangeState(new GorgonRoamState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
