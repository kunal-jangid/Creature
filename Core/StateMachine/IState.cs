namespace Creature.Core.StateMachine;

public interface IState<TContext>
{
    void Enter(TContext context);
    void Update(TContext context, double dt);
    void Exit(TContext context);
}
