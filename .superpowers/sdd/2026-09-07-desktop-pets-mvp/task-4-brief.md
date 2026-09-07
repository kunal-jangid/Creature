# Task 4: Pet Entity Model & Bunny MVP Behaviors

**Files:**
- Create: `Entities/DesktopPet.cs`
- Create: `Entities/Bunny/BunnyEntity.cs`
- Create: `Entities/Bunny/BunnyIdleState.cs`
- Create: `Entities/Bunny/BunnyRoamState.cs`
- Create: `Entities/Bunny/BunnyJumpState.cs`
- Test: `Creature.Tests/Entities/BunnyEntityTests.cs`

**Interfaces:**
- Produces:
  - `DesktopPet` base class owning `Transform`, `PhysicsBody`, `StateMachine<DesktopPet>`, `VisualElement` (WPF `Image`), `PlayAnimation(string clipName, bool loop = true)`, `Update(double dt)`, `HitTest(Point point)`, `UpdateFloor()`, `SyncVisualTransform()`, and abstract `OnCursorHover()`
  - `BunnyEntity : DesktopPet` with initial state `BunnyIdleState`
  - `BunnyIdleState` playing `BunnyLieDown` (looping) and transitioning to roam after 3–7s
  - `BunnyRoamState` playing `BunnyRun` (looping), moving at ~50 units/sec, flipping `ScaleTransform.ScaleX` on direction changes, turning around when hitting `MonitorWorkingArea.Left` or `MonitorWorkingArea.Right`, and returning to idle after 2–5s
  - `BunnyJumpState` playing `BunnyJump` (18 frames * 100ms, non-looping), applying initial jump impulse `Vy = -350`, parabolic arc via physics, and returning to idle upon landing

## Steps to Execute:

- [ ] **Step 1: Write failing tests for `BunnyEntity` behaviors**

Create `Creature.Tests/Entities/BunnyEntityTests.cs`:
```csharp
using System;
using System.IO;
using System.Windows;
using Creature.Entities.Bunny;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Entities;

public class BunnyEntityTests
{
    private SpriteManager CreateTestSpriteManager()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));
        sm.LoadAnimation("BunnyRun", Path.Combine(baseDir, "BunnyRun.json"), Path.Combine(baseDir, "BunnyRun.png"));
        sm.LoadAnimation("BunnyJump", Path.Combine(baseDir, "BunnyJump.json"), Path.Combine(baseDir, "BunnyJump.png"), loop: false);
        return sm;
    }

    [StaFact]
    public void BunnyEntity_ShouldStartInIdleState()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.CurrentStateName.Should().Be("Idle");
        bunny.Transform.Position.Y.Should().Be(1080 - 32);
    }

    [StaFact]
    public void BunnyEntity_OnCursorHover_ShouldTransitionToJumpStateImmediately()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 1920, 1080);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.OnCursorHover();
        bunny.CurrentStateName.Should().Be("Jump");
        bunny.Physics.Velocity.Y.Should().BeLessThan(0); // Upward impulse
    }

    [StaFact]
    public void BunnyEntity_WhenReachingBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 100, 100);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        // Position bunny at right boundary moving right
        bunny.Transform.Position = new Core.Physics.Vector2D(100 - 32, 100 - 32);
        bunny.SetRoamDirection(movingRight: true, speed: 50);

        bunny.Update(0.1); // Move past edge

        bunny.Transform.IsFacingLeft.Should().BeTrue();
        bunny.Physics.Velocity.X.Should().BeLessThan(0);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~BunnyEntityTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `DesktopPet.cs` and Bunny state classes**

Create `Entities/DesktopPet.cs`:
```csharp
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Graphics;

namespace Creature.Entities;

public abstract class DesktopPet
{
    public string Id { get; }
    public Transform2D Transform { get; } = new();
    public PhysicsBody Physics { get; }
    public Rect MonitorWorkingArea { get; set; }
    public SpriteManager SpriteManager { get; }
    public StateMachine<DesktopPet> StateMachine { get; }
    public Image VisualElement { get; }

    private AnimationClip? _currentClip;
    private int _currentFrameIndex;
    private double _frameTimer;

    public string CurrentStateName => StateMachine.CurrentState?.GetType().Name.Replace("Bunny", "").Replace("State", "") ?? "None";

    protected DesktopPet(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
    {
        Id = id;
        SpriteManager = spriteManager;
        MonitorWorkingArea = monitorWorkingArea;
        Physics = new PhysicsBody(Transform);
        StateMachine = new StateMachine<DesktopPet>(this);

        VisualElement = new Image
        {
            Width = Transform.BaseWidth,
            Height = Transform.BaseHeight,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new ScaleTransform(1.0, 1.0)
        };
        RenderOptions.SetBitmapScalingMode(VisualElement, BitmapScalingMode.NearestNeighbor);

        UpdateFloor();
        Transform.Position = new Vector2D(monitorWorkingArea.Left + (monitorWorkingArea.Width - Transform.ScaledWidth) / 2, Physics.FloorY);
    }

    public void UpdateFloor()
    {
        Physics.FloorY = MonitorWorkingArea.Bottom - Transform.ScaledHeight;
    }

    public void PlayAnimation(string clipName, bool loop = true)
    {
        var clip = SpriteManager.GetClip(clipName);
        if (clip == null || clip == _currentClip)
            return;

        _currentClip = clip;
        _currentFrameIndex = 0;
        _frameTimer = 0;
        if (_currentClip.Frames.Length > 0)
        {
            VisualElement.Source = _currentClip.Frames[0];
        }
    }

    public virtual void Update(double dt)
    {
        StateMachine.Update(dt);
        Physics.Update(dt);
        UpdateAnimation(dt);
        SyncVisualTransform();
    }

    private void UpdateAnimation(double dt)
    {
        if (_currentClip == null || _currentClip.Frames.Length == 0)
            return;

        _frameTimer += dt * 1000.0;
        if (_frameTimer >= _currentClip.FrameDurationMs)
        {
            _frameTimer -= _currentClip.FrameDurationMs;
            _currentFrameIndex++;
            if (_currentFrameIndex >= _currentClip.Frames.Length)
            {
                if (_currentClip.Loop)
                {
                    _currentFrameIndex = 0;
                }
                else
                {
                    _currentFrameIndex = _currentClip.Frames.Length - 1;
                }
            }
            VisualElement.Source = _currentClip.Frames[_currentFrameIndex];
        }
    }

    public void SyncVisualTransform()
    {
        Canvas.SetLeft(VisualElement, Transform.Position.X);
        Canvas.SetTop(VisualElement, Transform.Position.Y);
        VisualElement.Width = Transform.ScaledWidth;
        VisualElement.Height = Transform.ScaledHeight;

        var scaleX = Transform.IsFacingLeft ? -Transform.Scale : Transform.Scale;
        VisualElement.RenderTransform = new ScaleTransform(scaleX, Transform.Scale);
    }

    public bool HitTest(Point point)
    {
        return Transform.BoundingBox.Contains(point);
    }

    public abstract void OnCursorHover();
}
```

Create `Entities/Bunny/BunnyIdleState.cs`:
```csharp
using System;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyIdleState : IState<DesktopPet>
{
    private double _idleDuration;
    private double _elapsed;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.Physics.Velocity = Core.Physics.Vector2D.Zero;
        pet.PlayAnimation("BunnyLieDown", loop: true);
        _idleDuration = _random.NextDouble() * 4.0 + 3.0; // 3 to 7 seconds
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= _idleDuration)
        {
            pet.StateMachine.ChangeState(new BunnyRoamState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
```

Create `Entities/Bunny/BunnyRoamState.cs`:
```csharp
using System;
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyRoamState : IState<DesktopPet>
{
    private double _roamDuration;
    private double _elapsed;
    private double _speed = 50.0;
    private readonly Random _random = new();

    public void Enter(DesktopPet pet)
    {
        pet.PlayAnimation("BunnyRun", loop: true);
        _roamDuration = _random.NextDouble() * 3.0 + 2.0; // 2 to 5 seconds
        _elapsed = 0;

        var movingRight = _random.Next(2) == 0;
        ApplyDirection(pet, movingRight);
    }

    public void ApplyDirection(DesktopPet pet, bool movingRight)
    {
        pet.Transform.IsFacingLeft = !movingRight;
        pet.Physics.Velocity = new Vector2D(movingRight ? _speed : -_speed, 0);
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;

        // Turn around at monitor boundaries
        if (pet.Transform.Position.X <= pet.MonitorWorkingArea.Left && pet.Physics.Velocity.X < 0)
        {
            ApplyDirection(pet, movingRight: true);
        }
        else if (pet.Transform.Position.X + pet.Transform.ScaledWidth >= pet.MonitorWorkingArea.Right && pet.Physics.Velocity.X > 0)
        {
            ApplyDirection(pet, movingRight: false);
        }

        if (_elapsed >= _roamDuration)
        {
            pet.StateMachine.ChangeState(new BunnyIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
```

Create `Entities/Bunny/BunnyJumpState.cs`:
```csharp
using Creature.Core.Physics;
using Creature.Core.StateMachine;
using Creature.Entities;

namespace Creature.Entities.Bunny;

public class BunnyJumpState : IState<DesktopPet>
{
    private const double JumpVelocity = 350.0;
    private double _elapsed;
    private const double TotalJumpDuration = 1.8; // 18 frames * 100ms

    public void Enter(DesktopPet pet)
    {
        pet.PlayAnimation("BunnyJump", loop: false);
        pet.Physics.Velocity = new Vector2D(pet.Physics.Velocity.X * 0.5, -JumpVelocity);
        _elapsed = 0;
    }

    public void Update(DesktopPet pet, double dt)
    {
        _elapsed += dt;
        if (_elapsed >= TotalJumpDuration && pet.Physics.IsOnGround)
        {
            pet.StateMachine.ChangeState(new BunnyIdleState());
        }
    }

    public void Exit(DesktopPet pet) { }
}
```

Create `Entities/Bunny/BunnyEntity.cs`:
```csharp
using System.Windows;
using Creature.Graphics;

namespace Creature.Entities.Bunny;

public class BunnyEntity : DesktopPet
{
    public BunnyEntity(string id, SpriteManager spriteManager, Rect monitorWorkingArea)
        : base(id, spriteManager, monitorWorkingArea)
    {
        StateMachine.ChangeState(new BunnyIdleState());
    }

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
        roam.ApplyDirection(this, movingRight);
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Entities/ Creature.Tests/
git commit -m "feat: implement BunnyEntity with Idle, Roam, and Jump FSM states"
```
