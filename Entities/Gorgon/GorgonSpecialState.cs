using Creature.Core.Physics;
using Creature.Core.StateMachine;

namespace Creature.Entities.Gorgon;

public class GorgonSpecialState : IState<DesktopPet>
{
    private double _elapsed;
    private const double SpecialDuration = 1.0;

    public void Enter(DesktopPet pet)
    {
        pet.Physics.Velocity = Vector2D.Zero;
        pet.PlayAnimation("GorgonSpecial", loop: false);
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= SpecialDuration)
        {
            pet.StateMachine.ChangeState(new GorgonIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
