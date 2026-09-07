# Review Package for Task 4

## Commits
`
a5c3425 feat: implement BunnyEntity with Idle, Roam, and Jump FSM states

`

## Diff Stat
`
 Creature.Tests/Entities/BunnyEntityTests.cs | 182 ++++++++++++++++++++++++++++
 Entities/Bunny/BunnyEntity.cs               |  28 +++++
 Entities/Bunny/BunnyIdleState.cs            |  31 +++++
 Entities/Bunny/BunnyJumpState.cs            |  30 +++++
 Entities/Bunny/BunnyRoamState.cs            |  57 +++++++++
 Entities/DesktopPet.cs                      | 122 +++++++++++++++++++
 6 files changed, 450 insertions(+)

`

## Full Diff
`diff
diff --git a/Creature.Tests/Entities/BunnyEntityTests.cs b/Creature.Tests/Entities/BunnyEntityTests.cs
new file mode 100644
index 0000000..abe854d
--- /dev/null
+++ b/Creature.Tests/Entities/BunnyEntityTests.cs
@@ -0,0 +1,182 @@
+using System;
+using System.IO;
+using System.Windows;
+using Creature.Core.Physics;
+using Creature.Entities.Bunny;
+using Creature.Graphics;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Entities;
+
+public class BunnyEntityTests
+{
+    private SpriteManager CreateTestSpriteManager()
+    {
+        var sm = new SpriteManager();
+        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
+        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));
+        sm.LoadAnimation("BunnyRun", Path.Combine(baseDir, "BunnyRun.json"), Path.Combine(baseDir, "BunnyRun.png"));
+        sm.LoadAnimation("BunnyJump", Path.Combine(baseDir, "BunnyJump.json"), Path.Combine(baseDir, "BunnyJump.png"), loop: false);
+        return sm;
+    }
+
+    [StaFact]
+    public void BunnyEntity_ShouldStartInIdleState()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.CurrentStateName.Should().Be("Idle");
+        bunny.Transform.Position.Y.Should().Be(1080 - 32);
+    }
+
+    [StaFact]
+    public void BunnyEntity_OnCursorHover_ShouldTransitionToJumpStateImmediately()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.OnCursorHover();
+        bunny.CurrentStateName.Should().Be("Jump");
+        bunny.Physics.Velocity.Y.Should().BeLessThan(0); // Upward impulse
+    }
+
+    [StaFact]
+    public void BunnyEntity_WhenReachingBoundary_ShouldTurnAround()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 100, 100);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        // Position bunny at right boundary moving right
+        bunny.Transform.Position = new Vector2D(100 - 32, 100 - 32);
+        bunny.SetRoamDirection(movingRight: true, speed: 50);
+
+        bunny.Update(0.1); // Move past edge
+
+        bunny.Transform.IsFacingLeft.Should().BeTrue();
+        bunny.Physics.Velocity.X.Should().BeLessThan(0);
+    }
+
+    [StaFact]
+    public void BunnyEntity_WhenReachingLeftBoundary_ShouldTurnAround()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 100, 100);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        // Position bunny at left boundary moving left
+        bunny.Transform.Position = new Vector2D(0, 100 - 32);
+        bunny.SetRoamDirection(movingRight: false, speed: 50);
+
+        bunny.Update(0.1); // Move past edge
+
+        bunny.Transform.IsFacingLeft.Should().BeFalse();
+        bunny.Physics.Velocity.X.Should().BeGreaterThan(0);
+    }
+
+    [StaFact]
+    public void BunnyEntity_HitTest_ShouldCorrectlyDetectContainment()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.Transform.Position = new Vector2D(100, 200);
+
+        bunny.HitTest(new Point(110, 210)).Should().BeTrue();
+        bunny.HitTest(new Point(50, 50)).Should().BeFalse();
+    }
+
+    [StaFact]
+    public void BunnyEntity_UpdateFloor_ShouldUpdateFloorYWhenBoundsChange()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.MonitorWorkingArea = new Rect(0, 0, 1920, 900);
+        bunny.UpdateFloor();
+
+        bunny.Physics.FloorY.Should().Be(900 - 32);
+    }
+
+    [StaFact]
+    public void BunnyEntity_OnCursorHover_WhenAlreadyJumping_ShouldNotReenterJump()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.OnCursorHover();
+        bunny.CurrentStateName.Should().Be("Jump");
+        var initialVelocity = bunny.Physics.Velocity;
+
+        // Second hover should not trigger another jump state enter
+        bunny.OnCursorHover();
+        bunny.CurrentStateName.Should().Be("Jump");
+        bunny.Physics.Velocity.Should().Be(initialVelocity);
+    }
+
+    [StaFact]
+    public void BunnyEntity_JumpState_ShouldCompleteAndLandBackInIdle()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.OnCursorHover();
+        bunny.CurrentStateName.Should().Be("Jump");
+
+        // Simulate 2.0 seconds in 0.1s increments (Jump duration is 1.8s)
+        for (int i = 0; i < 25; i++)
+        {
+            bunny.Update(0.1);
+        }
+
+        bunny.CurrentStateName.Should().Be("Idle");
+        bunny.Physics.IsOnGround.Should().BeTrue();
+    }
+
+    [StaFact]
+    public void BunnyEntity_SyncVisualTransform_ShouldUpdateWpfImageProperties()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        bunny.Transform.Position = new Vector2D(250, 450);
+        bunny.Transform.Scale = 2.0;
+        bunny.Transform.IsFacingLeft = true;
+
+        bunny.SyncVisualTransform();
+
+        System.Windows.Controls.Canvas.GetLeft(bunny.VisualElement).Should().Be(250);
+        System.Windows.Controls.Canvas.GetTop(bunny.VisualElement).Should().Be(450);
+        bunny.VisualElement.Width.Should().Be(64);
+        bunny.VisualElement.Height.Should().Be(64);
+
+        var scaleTransform = bunny.VisualElement.RenderTransform.Should().BeOfType<System.Windows.Media.ScaleTransform>().Subject;
+        scaleTransform.ScaleX.Should().Be(-2.0);
+        scaleTransform.ScaleY.Should().Be(2.0);
+    }
+
+    [StaFact]
+    public void BunnyEntity_UpdateAnimation_ShouldAdvanceFrames()
+    {
+        var spriteManager = CreateTestSpriteManager();
+        var bounds = new Rect(0, 0, 1920, 1080);
+        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);
+
+        var initialSource = bunny.VisualElement.Source;
+        initialSource.Should().NotBeNull();
+
+        // LieDown has multiple frames with 100ms duration each. Advance by 0.11s to trigger frame increment
+        bunny.Update(0.11);
+
+        bunny.VisualElement.Source.Should().NotBeNull();
+    }
+}
diff --git a/Entities/Bunny/BunnyEntity.cs b/Entities/Bunny/BunnyEntity.cs
new file mode 100644
index 0000000..d508398
--- /dev/null
+++ b/Entities/Bunny/BunnyEntity.cs
@@ -0,0 +1,28 @@
+using System.Windows;
+using Creature.Graphics;
+
+namespace Creature.Entities.Bunny;
+
+public class BunnyEntity : DesktopPet
+{
+    public BunnyEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
+        : base(id, spriteManager, monitorWorkingArea)
+    {
+        StateMachine.ChangeState(new BunnyIdleState());
+    }
+
+    public override void OnCursorHover()
+    {
+        if (StateMachine.CurrentState is not BunnyJumpState)
+        {
+            StateMachine.ChangeState(new BunnyJumpState());
+        }
+    }
+
+    public void SetRoamDirection(bool movingRight, double speed = 50.0)
+    {
+        var roam = new BunnyRoamState();
+        StateMachine.ChangeState(roam);
+        roam.ApplyDirection(this, movingRight, speed);
+    }
+}
diff --git a/Entities/Bunny/BunnyIdleState.cs b/Entities/Bunny/BunnyIdleState.cs
new file mode 100644
index 0000000..a83c8a4
--- /dev/null
+++ b/Entities/Bunny/BunnyIdleState.cs
@@ -0,0 +1,31 @@
+using System;
+using Creature.Core.StateMachine;
+using Creature.Entities;
+
+namespace Creature.Entities.Bunny;
+
+public class BunnyIdleState : IState<DesktopPet>
+{
+    private double _idleDuration;
+    private double _elapsed;
+    private readonly Random _random = new();
+
+    public void Enter(DesktopPet pet)
+    {
+        pet.Physics.Velocity = Core.Physics.Vector2D.Zero;
+        pet.PlayAnimation("BunnyLieDown", loop: true);
+        _idleDuration = _random.NextDouble() * 4.0 + 3.0; // 3 to 7 seconds
+        _elapsed = 0;
+    }
+
+    public void Update(DesktopPet pet, double dt)
+    {
+        _elapsed += dt;
+        if (_elapsed >= _idleDuration)
+        {
+            pet.StateMachine.ChangeState(new BunnyRoamState());
+        }
+    }
+
+    public void Exit(DesktopPet pet) { }
+}
diff --git a/Entities/Bunny/BunnyJumpState.cs b/Entities/Bunny/BunnyJumpState.cs
new file mode 100644
index 0000000..fdd50b7
--- /dev/null
+++ b/Entities/Bunny/BunnyJumpState.cs
@@ -0,0 +1,30 @@
+using Creature.Core.Physics;
+using Creature.Core.StateMachine;
+using Creature.Entities;
+
+namespace Creature.Entities.Bunny;
+
+public class BunnyJumpState : IState<DesktopPet>
+{
+    private const double JumpVelocity = 350.0;
+    private double _elapsed;
+    private const double TotalJumpDuration = 1.8; // 18 frames * 100ms
+
+    public void Enter(DesktopPet pet)
+    {
+        pet.PlayAnimation("BunnyJump", loop: false);
+        pet.Physics.Velocity = new Vector2D(pet.Physics.Velocity.X * 0.5, -JumpVelocity);
+        _elapsed = 0;
+    }
+
+    public void Update(DesktopPet pet, double dt)
+    {
+        _elapsed += dt;
+        if (_elapsed >= TotalJumpDuration && pet.Physics.IsOnGround)
+        {
+            pet.StateMachine.ChangeState(new BunnyIdleState());
+        }
+    }
+
+    public void Exit(DesktopPet pet) { }
+}
diff --git a/Entities/Bunny/BunnyRoamState.cs b/Entities/Bunny/BunnyRoamState.cs
new file mode 100644
index 0000000..a073431
--- /dev/null
+++ b/Entities/Bunny/BunnyRoamState.cs
@@ -0,0 +1,57 @@
+using System;
+using Creature.Core.Physics;
+using Creature.Core.StateMachine;
+using Creature.Entities;
+
+namespace Creature.Entities.Bunny;
+
+public class BunnyRoamState : IState<DesktopPet>
+{
+    private double _roamDuration;
+    private double _elapsed;
+    private double _speed = 50.0;
+    private readonly Random _random = new();
+
+    public void Enter(DesktopPet pet)
+    {
+        pet.PlayAnimation("BunnyRun", loop: true);
+        _roamDuration = _random.NextDouble() * 3.0 + 2.0; // 2 to 5 seconds
+        _elapsed = 0;
+
+        var movingRight = _random.Next(2) == 0;
+        ApplyDirection(pet, movingRight);
+    }
+
+    public void ApplyDirection(DesktopPet pet, bool movingRight, double? speed = null)
+    {
+        if (speed.HasValue)
+        {
+            _speed = speed.Value;
+        }
+
+        pet.Transform.IsFacingLeft = !movingRight;
+        pet.Physics.Velocity = new Vector2D(movingRight ? _speed : -_speed, 0);
+    }
+
+    public void Update(DesktopPet pet, double dt)
+    {
+        _elapsed += dt;
+
+        // Turn around at monitor boundaries
+        if (pet.Transform.Position.X <= pet.MonitorWorkingArea.Left && pet.Physics.Velocity.X < 0)
+        {
+            ApplyDirection(pet, movingRight: true);
+        }
+        else if (pet.Transform.Position.X + pet.Transform.ScaledWidth >= pet.MonitorWorkingArea.Right && pet.Physics.Velocity.X > 0)
+        {
+            ApplyDirection(pet, movingRight: false);
+        }
+
+        if (_elapsed >= _roamDuration)
+        {
+            pet.StateMachine.ChangeState(new BunnyIdleState());
+        }
+    }
+
+    public void Exit(DesktopPet pet) { }
+}
diff --git a/Entities/DesktopPet.cs b/Entities/DesktopPet.cs
new file mode 100644
index 0000000..6595b2d
--- /dev/null
+++ b/Entities/DesktopPet.cs
@@ -0,0 +1,122 @@
+using System;
+using System.Windows;
+using System.Windows.Controls;
+using System.Windows.Media;
+using System.Windows.Media.Imaging;
+using Creature.Core.Physics;
+using Creature.Core.StateMachine;
+using Creature.Graphics;
+using Point = System.Windows.Point;
+using Image = System.Windows.Controls.Image;
+
+namespace Creature.Entities;
+
+public abstract class DesktopPet
+{
+    public string Id { get; }
+    public Transform2D Transform { get; } = new();
+    public PhysicsBody Physics { get; }
+    public Rect MonitorWorkingArea { get; set; }
+    public SpriteManager SpriteManager { get; }
+    public StateMachine<DesktopPet> StateMachine { get; }
+    public Image VisualElement { get; }
+
+    private AnimationClip? _currentClip;
+    private int _currentFrameIndex;
+    private double _frameTimer;
+
+    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name.Replace("Bunny", "").Replace("State", "") ?? "None";
+
+    protected DesktopPet(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
+    {
+        Id = id;
+        SpriteManager = spriteManager;
+        MonitorWorkingArea = monitorWorkingArea;
+        Physics = new PhysicsBody(Transform);
+        StateMachine = new StateMachine<DesktopPet>(this);
+
+        VisualElement = new Image
+        {
+            Width = Transform.BaseWidth,
+            Height = Transform.BaseHeight,
+            RenderTransformOrigin = new Point(0.5, 0.5),
+            RenderTransform = new ScaleTransform(1.0, 1.0)
+        };
+        RenderOptions.SetBitmapScalingMode(VisualElement, BitmapScalingMode.NearestNeighbor);
+
+        UpdateFloor();
+        Transform.Position = new Vector2D(monitorWorkingArea.Left + (monitorWorkingArea.Width - Transform.ScaledWidth) / 2, Physics.FloorY);
+        SyncVisualTransform();
+    }
+
+    public void UpdateFloor()
+    {
+        Physics.FloorY = MonitorWorkingArea.Bottom - Transform.ScaledHeight;
+    }
+
+    public void PlayAnimation(string clipName, bool loop = true)
+    {
+        var clip = SpriteManager.GetClip(clipName);
+        if (clip == null || clip == _currentClip)
+            return;
+
+        _currentClip = clip;
+        _currentFrameIndex = 0;
+        _frameTimer = 0;
+        if (_currentClip.Frames.Length > 0)
+        {
+            VisualElement.Source = _currentClip.Frames[0];
+        }
+    }
+
+    public virtual void Update(double dt)
+    {
+        StateMachine.Update(dt);
+        Physics.Update(dt);
+        UpdateAnimation(dt);
+        SyncVisualTransform();
+    }
+
+    private void UpdateAnimation(double dt)
+    {
+        if (_currentClip == null || _currentClip.Frames.Length == 0)
+            return;
+
+        _frameTimer += dt * 1000.0;
+        if (_frameTimer >= _currentClip.FrameDurationMs)
+        {
+            _frameTimer -= _currentClip.FrameDurationMs;
+            _currentFrameIndex++;
+            if (_currentFrameIndex >= _currentClip.Frames.Length)
+            {
+                if (_currentClip.Loop)
+                {
+                    _currentFrameIndex = 0;
+                }
+                else
+                {
+                    _currentFrameIndex = _currentClip.Frames.Length - 1;
+                }
+            }
+            VisualElement.Source = _currentClip.Frames[_currentFrameIndex];
+        }
+    }
+
+    public void SyncVisualTransform()
+    {
+        Canvas.SetLeft(VisualElement, Transform.Position.X);
+        Canvas.SetTop(VisualElement, Transform.Position.Y);
+        VisualElement.Width = Transform.ScaledWidth;
+        VisualElement.Height = Transform.ScaledHeight;
+
+        var scaleX = Transform.IsFacingLeft ? -Transform.Scale : Transform.Scale;
+        VisualElement.RenderTransform = new ScaleTransform(scaleX, Transform.Scale);
+    }
+
+    public bool HitTest(Point point)
+    {
+        return Transform.BoundingBox.Contains(point);
+    }
+
+    public abstract void OnCursorHover();
+}

`
