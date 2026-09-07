using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyJumpState : IState<DesktopPet>
{
    private const double JumpVelocity = 350.0;
    private double _elapsed;
    private const double TotalJumpDuration = 1.8; // 18 frames * 100ms

    public void Enter(DesktopPet pet)
    {
        pet.PlayAnimation("BunnyJump", loop: false);
        pet.Physics.Velocity = new Vector2D(pet.Physics.Velocity.X * 0.5, -JumpVelocity);
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= TotalJumpDuration && pet.Physics.IsOnGround)
        {
            pet.StateMachine.ChangeState(new BunnyIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
