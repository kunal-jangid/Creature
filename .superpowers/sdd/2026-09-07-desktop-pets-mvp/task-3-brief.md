# Task 3: Core Physics & State Machine Abstractions

**Files:**
- Create: `Core/Physics/Vector2D.cs`
- Create: `Core/Physics/Transform2D.cs`
- Create: `Core/Physics/PhysicsBody.cs`
- Create: `Core/StateMachine/IState.cs`
- Create: `Core/StateMachine/StateMachine.cs`
- Test: `Creature.Tests/Core/PhysicsTests.cs`
- Test: `Creature.Tests/Core/StateMachineTests.cs`

**Interfaces:**
- Produces:
  - `Vector2D(double X, double Y)` arithmetic helpers (+, -, *, Zero)
  - `Transform2D` with `Position`, `Scale`, `IsFacingLeft`, `BaseWidth`, `BaseHeight`, `ScaledWidth`, `ScaledHeight`, `BoundingBox`
  - `PhysicsBody` with `Velocity`, `Gravity`, `FloorY`, `IsOnGround`, `Update(double dt)`
  - `IState<TContext>` with `Enter(TContext context)`, `Update(TContext context, double dt)`, `Exit(TContext context)`
  - `StateMachine<TContext>` with `CurrentState`, `ChangeState(IState<TContext>? newState)`, `Update(double dt)`

## Steps to Execute:

- [ ] **Step 1: Write failing tests for Physics & State Machine**

Create `Creature.Tests/Core/PhysicsTests.cs`:
```csharp
using Creature.Core.Physics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Core;

public class PhysicsTests
{
    [Fact]
    public void PhysicsBody_WhenAboveFloor_ShouldApplyGravityUntilLanding()
    {
        var transform = new Transform2D { Position = new Vector2D(100, 400) };
        var physics = new PhysicsBody(transform)
        {
            FloorY = 500,
            Gravity = 980,
            Velocity = new Vector2D(0, 0)
        };

        physics.IsOnGround.Should().BeFalse();

        // Simulate 0.1s
        physics.Update(0.1);
        physics.Velocity.Y.Should().BeApproximately(98, 0.1);
        transform.Position.Y.Should().BeGreaterThan(400);

        // Simulate enough time to hit ground
        for (int i = 0; i < 20; i++)
            physics.Update(0.1);

        transform.Position.Y.Should().Be(500);
        physics.IsOnGround.Should().BeTrue();
        physics.Velocity.Y.Should().Be(0);
    }
}
```

Create `Creature.Tests/Core/StateMachineTests.cs`:
```csharp
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

    [Fact]
    public void StateMachine_ShouldExecuteEnterUpdateAndExitCallbacks()
    {
        var context = new TestContext();
        var sm = new StateMachine<TestContext>(context);

        var state1 = new IncrementState();
        sm.ChangeState(state1);
        context.Value.Should().Be(10); // Enter

        sm.Update(0.016);
        context.Value.Should().Be(11); // Update

        sm.ChangeState(null);
        context.Value.Should().Be(111); // Exit
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter "FullyQualifiedName~Core"`
Expected: FAIL.

- [ ] **Step 3: Implement `Vector2D.cs`, `Transform2D.cs`, `PhysicsBody.cs`, `IState.cs`, `StateMachine.cs`**

Create `Core/Physics/Vector2D.cs`:
```csharp
namespace Creature.Core.Physics;

public record struct Vector2D(double X, double Y)
{
    public static readonly Vector2D Zero = new(0, 0);

    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);
}
```

Create `Core/Physics/Transform2D.cs`:
```csharp
using System.Windows;

namespace Creature.Core.Physics;

public class Transform2D
{
    public Vector2D Position { get; set; } = Vector2D.Zero;
    public double Scale { get; set; } = 1.0;
    public bool IsFacingLeft { get; set; } = false;
    public double BaseWidth { get; set; } = 32;
    public double BaseHeight { get; set; } = 32;

    public double ScaledWidth => BaseWidth * Scale;
    public double ScaledHeight => BaseHeight * Scale;

    public Rect BoundingBox => new(Position.X, Position.Y, ScaledWidth, ScaledHeight);
}
```

Create `Core/Physics/PhysicsBody.cs`:
```csharp
namespace Creature.Core.Physics;

public class PhysicsBody
{
    private readonly Transform2D _transform;

    public Vector2D Velocity { get; set; } = Vector2D.Zero;
    public double Gravity { get; set; } = 980.0;
    public double FloorY { get; set; } = 0.0;
    public bool IsOnGround { get; private set; } = false;

    public PhysicsBody(Transform2D transform)
    {
        _transform = transform;
    }

    public void Update(double dt)
    {
        if (!_transform.Position.Y.Equals(FloorY) || Velocity.Y < 0)
        {
            Velocity = new Vector2D(Velocity.X, Velocity.Y + Gravity * dt);
        }

        var newX = _transform.Position.X + Velocity.X * dt;
        var newY = _transform.Position.Y + Velocity.Y * dt;

        if (newY >= FloorY)
        {
            newY = FloorY;
            Velocity = new Vector2D(Velocity.X, 0);
            IsOnGround = true;
        }
        else
        {
            IsOnGround = false;
        }

        _transform.Position = new Vector2D(newX, newY);
    }
}
```

Create `Core/StateMachine/IState.cs`:
```csharp
namespace Creature.Core.StateMachine;

public interface IState<TContext>
{
    void Enter(TContext context);
    void Update(TContext context, double dt);
    void Exit(TContext context);
}
```

Create `Core/StateMachine/StateMachine.cs`:
```csharp
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
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Core/ Creature.Tests/
git commit -m "feat: implement 2D physics and generic state machine"
```
