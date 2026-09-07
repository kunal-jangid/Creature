using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Bunny;

public class BunnyEntity : DesktopPet
{
    public BunnyEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea, string name = "Bunny")
        : base(id, spriteManager, monitorWorkingArea, name)
    {
        SimpnessFactor = 0.85;
        StateMachine.ChangeState(new BunnyIdleState());
    }

    protected override string GetIdleAnimationName() => "BunnyLieDown";
    protected override string GetRunAnimationName() => "BunnyRun";

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
