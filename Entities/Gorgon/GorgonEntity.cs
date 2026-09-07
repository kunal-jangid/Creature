using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Gorgon;

public class GorgonEntity : DesktopPet
{
    public GorgonEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea, string name = "Gorgon")
        : base(id, spriteManager, monitorWorkingArea, name, baseWidth: 64, baseHeight: 64)
    {
        SimpnessFactor = 0.30;
        StateMachine.ChangeState(new GorgonIdleState());
    }

    protected override string GetIdleAnimationName() => "GorgonIdle";
    protected override string GetRunAnimationName() => "GorgonRun";

    public override void OnCursorHover()
    {
        if (StateMachine.CurrentState is not GorgonSpecialState)
        {
            StateMachine.ChangeState(new GorgonSpecialState());
        }
    }

    public void SetRoamDirection(bool movingRight, double speed = 40.0)
    {
        var roam = new GorgonRoamState();
        StateMachine.ChangeState(roam);
        roam.ApplyDirection(this, movingRight, speed);
    }
}
