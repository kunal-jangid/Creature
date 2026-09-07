using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Werewolf;

public class WerewolfEntity : DesktopPet
{
    public WerewolfEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea, string name = "Werewolf")
        : base(id, spriteManager, monitorWorkingArea, name, baseWidth: 64, baseHeight: 64)
    {
        StateMachine.ChangeState(new WerewolfIdleState());
    }

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
