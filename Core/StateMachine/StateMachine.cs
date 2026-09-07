namespace Creature.Core.StateMachine;

public class StateMachine<TContext>
{
    private readonly TContext _context;
    public IState<TContext>? CurrentState { get; private set; }

    public StateMachine(TContext context)
    {
        _context = context;
    }

    public void ChangeState(IState<TContext>? newState)
    {
        CurrentState?.Exit(_context);
        CurrentState = newState;
        CurrentState?.Enter(_context);
    }

    public void Update(double dt)
    {
        CurrentState?.Update(_context, dt);
    }
}
