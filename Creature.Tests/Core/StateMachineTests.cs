using Creature.Core.StateMachine;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Core;

public class StateMachineTests
{
    private class TestContext { public int Value; }

    private class IncrementState : IState<TestContext>
    {
        public void Enter(TestContext context) => context.Value += 10;
        public void Update(TestContext context, double dt) => context.Value += 1;
        public void Exit(TestContext context) => context.Value += 100;
    }

    private class DecrementState : IState<TestContext>
    {
        public void Enter(TestContext context) => context.Value -= 5;
        public void Update(TestContext context, double dt) => context.Value -= 1;
        public void Exit(TestContext context) => context.Value -= 20;
    }

    [Fact]
    public void StateMachine_ShouldExecuteEnterUpdateAndExitCallbacks()
    {
        var context = new TestContext();
        var sm = new StateMachine<TestContext>(context);

        var state1 = new IncrementState();
        sm.ChangeState(state1);
        sm.CurrentState.Should().Be(state1);
        context.Value.Should().Be(10); // Enter

        sm.Update(0.016);
        context.Value.Should().Be(11); // Update

        sm.ChangeState(null);
        sm.CurrentState.Should().BeNull();
        context.Value.Should().Be(111); // Exit
    }

    [Fact]
    public void StateMachine_WhenTransitioningBetweenStates_ShouldExitOldAndEnterNew()
    {
        var context = new TestContext();
        var sm = new StateMachine<TestContext>(context);

        var state1 = new IncrementState();
        var state2 = new DecrementState();

        sm.ChangeState(state1);
        context.Value.Should().Be(10);

        sm.ChangeState(state2);
        // Exited state1 (+100) -> 110, then entered state2 (-5) -> 105
        context.Value.Should().Be(105);
        sm.CurrentState.Should().Be(state2);
    }
}
