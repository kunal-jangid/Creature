using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Werewolf;

public class WerewolfEntity : DesktopPet
{
    public WerewolfEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea, string name = "Werewolf")
        : base(id, spriteManager, monitorWorkingArea, name, baseWidth: 64, baseHeight: 64)
    {
        SimpnessFactor = 0.75;
        StateMachine.ChangeState(new WerewolfIdleState());
    }

    protected override string GetIdleAnimationName() => "WerewolfIdle";
    protected override string GetRunAnimationName() => "WerewolfRun";

    public override void OnCursorHover()
    {
        if (StateMachine.CurrentState is not WerewolfFlinchState)
        {
            StateMachine.ChangeState(new WerewolfFlinchState());
        }
    }

    public void SetRoamDirection(bool movingRight, double speed = 60.0)
    {
        var roam = new WerewolfRoamState();
        StateMachine.ChangeState(roam);
        roam.ApplyDirection(this, movingRight, speed);
    }
}
