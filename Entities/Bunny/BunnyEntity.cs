using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Bunny;

public class BunnyEntity : DesktopPet
{
    public BunnyEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
        : base(id, spriteManager, monitorWorkingArea)
    {
        StateMachine.ChangeState(new BunnyIdleState());
    }

    public override void OnCursorHover()
    {
        if (StateMachine.CurrentState is not BunnyJumpState)
        {
            StateMachine.ChangeState(new BunnyJumpState());
        }
    }

    public void SetRoamDirection(bool movingRight, double speed = 50.0)
    {
        var roam = new BunnyRoamState();
        StateMachine.ChangeState(roam);
        roam.ApplyDirection(this, movingRight, speed);
    }
}
