using Creature.Core.Physics;
using Creature.Core.StateMachine;

namespace Creature.Entities.Werewolf;

public class WerewolfFlinchState : IState<DesktopPet>
{
    private double _elapsed;
    private const double FlinchDuration = 0.8;

    public void Enter(DesktopPet pet)
    {
        pet.Physics.Velocity = Vector2D.Zero;
        pet.PlayAnimation("WerewolfFlinch", loop: false);
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= FlinchDuration)
        {
            pet.StateMachine.ChangeState(new WerewolfIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
