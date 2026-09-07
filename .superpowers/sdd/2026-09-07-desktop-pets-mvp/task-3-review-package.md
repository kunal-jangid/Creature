# Review Package for Task 3

## Commits
`
e04d0aa feat: implement 2D physics and generic state machine

`

## Diff Stat
`
 Core/Physics/PhysicsBody.cs              | 40 +++++++++++++++++++
 Core/Physics/Transform2D.cs              | 17 ++++++++
 Core/Physics/Vector2D.cs                 | 10 +++++
 Core/StateMachine/IState.cs              |  8 ++++
 Core/StateMachine/StateMachine.cs        | 24 +++++++++++
 Creature.Tests/Core/PhysicsTests.cs      | 68 ++++++++++++++++++++++++++++++++
 Creature.Tests/Core/StateMachineTests.cs | 61 ++++++++++++++++++++++++++++
 7 files changed, 228 insertions(+)

`

## Full Diff
`diff
diff --git a/Core/Physics/PhysicsBody.cs b/Core/Physics/PhysicsBody.cs
new file mode 100644
index 0000000..feb033d
--- /dev/null
+++ b/Core/Physics/PhysicsBody.cs
@@ -0,0 +1,40 @@
+namespace Creature.Core.Physics;
+
+public class PhysicsBody
+{
+    private readonly Transform2D _transform;
+
+    public Vector2D Velocity { get; set; } = Vector2D.Zero;
+    public double Gravity { get; set; } = 980.0;
+    public double FloorY { get; set; } = 0.0;
+    public bool IsOnGround { get; private set; } = false;
+
+    public PhysicsBody(Transform2D transform)
+    {
+        _transform = transform;
+    }
+
+    public void Update(double dt)
+    {
+        if (!_transform.Position.Y.Equals(FloorY) || Velocity.Y < 0)
+        {
+            Velocity = new Vector2D(Velocity.X, Velocity.Y + Gravity * dt);
+        }
+
+        var newX = _transform.Position.X + Velocity.X * dt;
+        var newY = _transform.Position.Y + Velocity.Y * dt;
+
+        if (newY >= FloorY)
+        {
+            newY = FloorY;
+            Velocity = new Vector2D(Velocity.X, 0);
+            IsOnGround = true;
+        }
+        else
+        {
+            IsOnGround = false;
+        }
+
+        _transform.Position = new Vector2D(newX, newY);
+    }
+}
diff --git a/Core/Physics/Transform2D.cs b/Core/Physics/Transform2D.cs
new file mode 100644
index 0000000..36dbb41
--- /dev/null
+++ b/Core/Physics/Transform2D.cs
@@ -0,0 +1,17 @@
+using System.Windows;
+
+namespace Creature.Core.Physics;
+
+public class Transform2D
+{
+    public Vector2D Position { get; set; } = Vector2D.Zero;
+    public double Scale { get; set; } = 1.0;
+    public bool IsFacingLeft { get; set; } = false;
+    public double BaseWidth { get; set; } = 32;
+    public double BaseHeight { get; set; } = 32;
+
+    public double ScaledWidth => BaseWidth * Scale;
+    public double ScaledHeight => BaseHeight * Scale;
+
+    public Rect BoundingBox => new(Position.X, Position.Y, ScaledWidth, ScaledHeight);
+}
diff --git a/Core/Physics/Vector2D.cs b/Core/Physics/Vector2D.cs
new file mode 100644
index 0000000..9f05731
--- /dev/null
+++ b/Core/Physics/Vector2D.cs
@@ -0,0 +1,10 @@
+namespace Creature.Core.Physics;
+
+public record struct Vector2D(double X, double Y)
+{
+    public static readonly Vector2D Zero = new(0, 0);
+
+    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
+    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
+    public static Vector2D operator *(Vector2D a, double scalar) => new(a.X * scalar, a.Y * scalar);
+}
diff --git a/Core/StateMachine/IState.cs b/Core/StateMachine/IState.cs
new file mode 100644
index 0000000..61a38e0
--- /dev/null
+++ b/Core/StateMachine/IState.cs
@@ -0,0 +1,8 @@
+namespace Creature.Core.StateMachine;
+
+public interface IState<TContext>
+{
+    void Enter(TContext context);
+    void Update(TContext context, double dt);
+    void Exit(TContext context);
+}
diff --git a/Core/StateMachine/StateMachine.cs b/Core/StateMachine/StateMachine.cs
new file mode 100644
index 0000000..44df13b
--- /dev/null
+++ b/Core/StateMachine/StateMachine.cs
@@ -0,0 +1,24 @@
+namespace Creature.Core.StateMachine;
+
+public class StateMachine<TContext>
+{
+    private readonly TContext _context;
+    public IState<TContext>? CurrentState { get; private set; }
+
+    public StateMachine(TContext context)
+    {
+        _context = context;
+    }
+
+    public void ChangeState(IState<TContext>? newState)
+    {
+        CurrentState?.Exit(_context);
+        CurrentState = newState;
+        CurrentState?.Enter(_context);
+    }
+
+    public void Update(double dt)
+    {
+        CurrentState?.Update(_context, dt);
+    }
+}
diff --git a/Creature.Tests/Core/PhysicsTests.cs b/Creature.Tests/Core/PhysicsTests.cs
new file mode 100644
index 0000000..8c63a61
--- /dev/null
+++ b/Creature.Tests/Core/PhysicsTests.cs
@@ -0,0 +1,68 @@
+using Creature.Core.Physics;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Core;
+
+public class PhysicsTests
+{
+    [Fact]
+    public void PhysicsBody_WhenAboveFloor_ShouldApplyGravityUntilLanding()
+    {
+        var transform = new Transform2D { Position = new Vector2D(100, 400) };
+        var physics = new PhysicsBody(transform)
+        {
+            FloorY = 500,
+            Gravity = 980,
+            Velocity = new Vector2D(0, 0)
+        };
+
+        physics.IsOnGround.Should().BeFalse();
+
+        // Simulate 0.1s
+        physics.Update(0.1);
+        physics.Velocity.Y.Should().BeApproximately(98, 0.1);
+        transform.Position.Y.Should().BeGreaterThan(400);
+
+        // Simulate enough time to hit ground
+        for (int i = 0; i < 20; i++)
+            physics.Update(0.1);
+
+        transform.Position.Y.Should().Be(500);
+        physics.IsOnGround.Should().BeTrue();
+        physics.Velocity.Y.Should().Be(0);
+    }
+
+    [Fact]
+    public void Vector2D_ArithmeticOperations_ShouldBehaveCorrectly()
+    {
+        var a = new Vector2D(10, 20);
+        var b = new Vector2D(3, 7);
+
+        (a + b).Should().Be(new Vector2D(13, 27));
+        (a - b).Should().Be(new Vector2D(7, 13));
+        (a * 2.5).Should().Be(new Vector2D(25, 50));
+        Vector2D.Zero.Should().Be(new Vector2D(0, 0));
+    }
+
+    [Fact]
+    public void Transform2D_ScaledDimensionsAndBoundingBox_ShouldReflectScaleAndPosition()
+    {
+        var transform = new Transform2D
+        {
+            Position = new Vector2D(50, 60),
+            Scale = 2.0,
+            BaseWidth = 32,
+            BaseHeight = 32,
+            IsFacingLeft = true
+        };
+
+        transform.ScaledWidth.Should().Be(64);
+        transform.ScaledHeight.Should().Be(64);
+        transform.BoundingBox.X.Should().Be(50);
+        transform.BoundingBox.Y.Should().Be(60);
+        transform.BoundingBox.Width.Should().Be(64);
+        transform.BoundingBox.Height.Should().Be(64);
+        transform.IsFacingLeft.Should().BeTrue();
+    }
+}
diff --git a/Creature.Tests/Core/StateMachineTests.cs b/Creature.Tests/Core/StateMachineTests.cs
new file mode 100644
index 0000000..855e3a4
--- /dev/null
+++ b/Creature.Tests/Core/StateMachineTests.cs
@@ -0,0 +1,61 @@
+using Creature.Core.StateMachine;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Core;
+
+public class StateMachineTests
+{
+    private class TestContext { public int Value; }
+
+    private class IncrementState : IState<TestContext>
+    {
+        public void Enter(TestContext context) => context.Value += 10;
+        public void Update(TestContext context, double dt) => context.Value += 1;
+        public void Exit(TestContext context) => context.Value += 100;
+    }
+
+    private class DecrementState : IState<TestContext>
+    {
+        public void Enter(TestContext context) => context.Value -= 5;
+        public void Update(TestContext context, double dt) => context.Value -= 1;
+        public void Exit(TestContext context) => context.Value -= 20;
+    }
+
+    [Fact]
+    public void StateMachine_ShouldExecuteEnterUpdateAndExitCallbacks()
+    {
+        var context = new TestContext();
+        var sm = new StateMachine<TestContext>(context);
+
+        var state1 = new IncrementState();
+        sm.ChangeState(state1);
+        sm.CurrentState.Should().Be(state1);
+        context.Value.Should().Be(10); // Enter
+
+        sm.Update(0.016);
+        context.Value.Should().Be(11); // Update
+
+        sm.ChangeState(null);
+        sm.CurrentState.Should().BeNull();
+        context.Value.Should().Be(111); // Exit
+    }
+
+    [Fact]
+    public void StateMachine_WhenTransitioningBetweenStates_ShouldExitOldAndEnterNew()
+    {
+        var context = new TestContext();
+        var sm = new StateMachine<TestContext>(context);
+
+        var state1 = new IncrementState();
+        var state2 = new DecrementState();
+
+        sm.ChangeState(state1);
+        context.Value.Should().Be(10);
+
+        sm.ChangeState(state2);
+        // Exited state1 (+100) -> 110, then entered state2 (-5) -> 105
+        context.Value.Should().Be(105);
+        sm.CurrentState.Should().Be(state2);
+    }
+}

`
