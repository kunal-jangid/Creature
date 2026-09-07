# Desktop Pets MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a lightweight, low-CPU .NET 10 WPF Desktop Pets application featuring a transparent multi-monitor overlay, Win32 `WM_NCHITTEST` zero-friction desktop pass-through, Aseprite sprite slicing, and an interactive Bunny MVP entity (Idle, Roam, and Hover Jump states).

**Architecture:** The application launches to the system tray and spans transparent borderless overlay windows across all connected monitors. A 60 Hz simulation engine decouples physics from a 10 FPS Aseprite animation tick, driving state machines and WPF canvas rendering while `WM_NCHITTEST` dynamically routes mouse clicks through transparent regions and intercepts pet bounding boxes.

**Tech Stack:** C# 13 / .NET 10 WPF (`net10.0-windows`), System.Text.Json, Win32 user32 Interop, xUnit, FluentAssertions.

**Spec:** [docs/superpowers/specs/2026-09-07-desktop-pets-design.md](file:///D:/projects/Creature/docs/superpowers/specs/2026-09-07-desktop-pets-design.md)

## Global Constraints

* Target Framework: `.NET 10.0-windows`
* Priority Hierarchy: Low CPU > Low Memory > Smooth Animation > Simple Architecture > Cute Interactions > Visual Fidelity
* Pixel Art Rendering: `RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor)` must be applied to all pet visuals
* Click-Through Requirement: Fullscreen overlays must return `HTTRANSPARENT` (-1) for empty canvas areas and `HTCLIENT` (1) for pet bounding boxes
* Base Sprite Dimensions: Uniform 32x32 logical pixels per frame; animation durations driven by JSON metadata (`duration: 100` ms)
* Concurrency Limits: Maximum 2 simultaneous audio voices

---

### Task 1: Test Project Setup & Project Configuration

**Files:**
- Modify: `Creature.csproj`
- Create: `Creature.Tests/Creature.Tests.csproj`
- Create: `Creature.Tests/SanityTests.cs`

**Interfaces:**
- Produces: `Creature.Tests` test project targeting `net10.0-windows` with xUnit, Microsoft.NET.Test.Sdk, and FluentAssertions referenced.

- [ ] **Step 1: Update `Creature.csproj` to enable InternalsVisibleTo test project and copy assets**

Update `Creature.csproj`:
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>
    <RootNamespace>Creature</RootNamespace>
    <AssemblyName>Creature</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <Content Include="docs\assets\**\*.*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleTo">
      <_Parameter1>Creature.Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>

</Project>
```

- [ ] **Step 2: Create `Creature.Tests/Creature.Tests.csproj`**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <UseWPF>true</UseWPF>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="8.1.1" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Creature.csproj" />
  </ItemGroup>

  <ItemGroup>
    <Content Include="..\docs\assets\**\*.*">
      <Link>Assets\%(RecursiveDir)%(Filename)%(Extension)</Link>
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>

</Project>
```

- [ ] **Step 3: Write sanity test `Creature.Tests/SanityTests.cs`**

```csharp
using FluentAssertions;
using Xunit;

namespace Creature.Tests;

public class SanityTests
{
    [Fact]
    public void Environment_ShouldTargetNet10()
    {
        Environment.Version.Major.Should().BeGreaterOrEqualTo(10);
    }
}
```

- [ ] **Step 4: Run test to verify test project builds and passes**

Run: `dotnet test`
Expected: 1 test passed.

- [ ] **Step 5: Commit**

```bash
git add Creature.csproj Creature.Tests/
git commit -m "chore: setup test project and net10 WPF configuration"
```

---

### Task 2: Graphics & Asset Pipeline (`SpriteManager`)

**Files:**
- Create: `Graphics/AsepriteMetadata.cs`
- Create: `Graphics/AnimationClip.cs`
- Create: `Graphics/SpriteManager.cs`
- Test: `Creature.Tests/Graphics/SpriteManagerTests.cs`

**Interfaces:**
- Produces:
  - `AsepriteMetadata`, `AsepriteFrameEntry`, `AsepriteRect` records for JSON deserialization
  - `AnimationClip(string Name, BitmapSource[] Frames, int FrameDurationMs, bool Loop)`
  - `SpriteManager.LoadAnimation(string jsonPath, string pngPath) -> AnimationClip`
  - `SpriteManager.GetClip(string name) -> AnimationClip`

- [ ] **Step 1: Write failing tests for `SpriteManager`**

Create `Creature.Tests/Graphics/SpriteManagerTests.cs`:
```csharp
using System.IO;
using System.Windows.Media.Imaging;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Graphics;

public class SpriteManagerTests
{
    [StaFact]
    public void LoadAnimation_WithBunnyLieDown_ShouldLoadTwoFramesWith100msDuration()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");

        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(pngPath).Should().BeTrue();

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);

        clip.Should().NotBeNull();
        clip.Name.Should().Be("BunnyLieDown");
        clip.Frames.Length.Should().Be(2);
        clip.FrameDurationMs.Should().Be(100);
        clip.Frames[0].PixelWidth.Should().Be(32);
        clip.Frames[0].PixelHeight.Should().Be(32);
    }

    [StaFact]
    public void LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.png");

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyRun", jsonPath, pngPath);

        clip.Frames.Length.Should().Be(5);
        clip.FrameDurationMs.Should().Be(100);
    }

    [StaFact]
    public void LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.png");

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyJump", jsonPath, pngPath, loop: false);

        clip.Frames.Length.Should().Be(18);
        clip.Loop.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter "FullyQualifiedName~SpriteManagerTests"`
Expected: FAIL (types not yet defined).

- [ ] **Step 3: Implement `AsepriteMetadata.cs`, `AnimationClip.cs`, and `SpriteManager.cs`**

Create `Graphics/AsepriteMetadata.cs`:
```csharp
using System.Text.Json.Serialization;

namespace Creature.Graphics;

public record AsepriteMetadata(
    [property: JsonPropertyName("frames")] Dictionary<string, AsepriteFrameEntry> Frames,
    [property: JsonPropertyName("meta")] AsepriteMeta Meta
);

public record AsepriteFrameEntry(
    [property: JsonPropertyName("frame")] AsepriteRect Frame,
    [property: JsonPropertyName("duration")] int Duration
);

public record AsepriteRect(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);

public record AsepriteMeta(
    [property: JsonPropertyName("size")] AsepriteSize Size
);

public record AsepriteSize(
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);
```

Create `Graphics/AnimationClip.cs`:
```csharp
using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class AnimationClip
{
    public string Name { get; }
    public BitmapSource[] Frames { get; }
    public int FrameDurationMs { get; }
    public bool Loop { get; }

    public AnimationClip(string name, BitmapSource[] frames, int frameDurationMs, bool loop = true)
    {
        Name = name;
        Frames = frames;
        FrameDurationMs = frameDurationMs > 0 ? frameDurationMs : 100;
        Loop = loop;
    }
}
```

Create `Graphics/SpriteManager.cs`:
```csharp
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class SpriteManager
{
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);

    public AnimationClip LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true)
    {
        if (_clips.TryGetValue(name, out var cached))
            return cached;

        var jsonText = File.ReadAllText(jsonPath);
        var metadata = JsonSerializer.Deserialize<AsepriteMetadata>(jsonText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Failed to deserialize Aseprite JSON: {jsonPath}");

        var masterBitmap = new BitmapImage();
        masterBitmap.BeginInit();
        masterBitmap.UriSource = new Uri(Path.GetFullPath(pngPath), UriKind.Absolute);
        masterBitmap.CacheOption = BitmapCacheOption.OnLoad;
        masterBitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        masterBitmap.EndInit();
        masterBitmap.Freeze();

        var frames = new List<BitmapSource>();
        var frameDuration = 100;

        foreach (var (_, entry) in metadata.Frames)
        {
            frameDuration = entry.Duration;
            var rect = new Int32Rect(entry.Frame.X, entry.Frame.Y, entry.Frame.W, entry.Frame.H);
            var cropped = new CroppedBitmap(masterBitmap, rect);
            cropped.Freeze();
            frames.Add(cropped);
        }

        var clip = new AnimationClip(name, frames.ToArray(), frameDuration, loop);
        _clips[name] = clip;
        return clip;
    }

    public AnimationClip? GetClip(string name)
    {
        _clips.TryGetValue(name, out var clip);
        return clip;
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Graphics/ Creature.Tests/
git commit -m "feat: implement Aseprite JSON parsing and CroppedBitmap sprite slicing"
```

---

### Task 3: Core Physics & State Machine Abstractions

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
  - `Vector2D(double X, double Y)` arithmetic helpers
  - `Transform2D` with `Position`, `Scale`, `IsFacingLeft`, `BaseWidth`, `BaseHeight`, `BoundingBox`
  - `PhysicsBody` with `Velocity`, `Gravity`, `FloorY`, `IsOnGround`, `Update(double dt)`
  - `IState<T>` with `Enter()`, `Update(double dt)`, `Exit()`
  - `StateMachine<T>` with `ChangeState(IState<T>)`, `Update(double dt)`

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

        physics.Update(0.1);
        physics.Velocity.Y.Should().BeApproximately(98, 0.1);
        transform.Position.Y.Should().BeGreaterThan(400);

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
        context.Value.Should().Be(10);

        sm.Update(0.016);
        context.Value.Should().Be(11);

        sm.ChangeState(null);
        context.Value.Should().Be(111);
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

- [ ] **Step 4: Run `dotnet test` to verify pass**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Core/ Creature.Tests/
git commit -m "feat: implement 2D physics and generic state machine"
```

---

### Task 4: Pet Entity Model & Bunny MVP Behaviors

**Files:**
- Create: `Entities/DesktopPet.cs`
- Create: `Entities/Bunny/BunnyEntity.cs`
- Create: `Entities/Bunny/BunnyIdleState.cs`
- Create: `Entities/Bunny/BunnyRoamState.cs`
- Create: `Entities/Bunny/BunnyJumpState.cs`
- Test: `Creature.Tests/Entities/BunnyEntityTests.cs`

**Interfaces:**
- Produces:
  - `DesktopPet` base class owning `Transform`, `PhysicsBody`, `StateMachine`, and `Image` view model / control hook
  - `BunnyEntity : DesktopPet` with states `BunnyIdleState`, `BunnyRoamState`, `BunnyJumpState`
  - `OnCursorHover()` trigger for immediate flinch jump transition

- [ ] **Step 1: Write failing tests for `BunnyEntity` behaviors**

Create `Creature.Tests/Entities/BunnyEntityTests.cs`:
```csharp
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
        bunny.Physics.Velocity.Y.Should().BeLessThan(0);
    }

    [StaFact]
    public void BunnyEntity_WhenReachingBoundary_ShouldTurnAround()
    {
        var spriteManager = CreateTestSpriteManager();
        var bounds = new Rect(0, 0, 100, 100);
        var bunny = new BunnyEntity("bunny-1", spriteManager, bounds);

        bunny.Transform.Position = new Core.Physics.Vector2D(100 - 32, 100 - 32);
        bunny.SetRoamDirection(movingRight: true, speed: 50);

        bunny.Update(0.1);

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
        _idleDuration = _random.NextDouble() * 4.0 + 3.0;
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
        _roamDuration = _random.NextDouble() * 3.0 + 2.0;
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
    private const double TotalJumpDuration = 1.8;

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

---

### Task 5: User Settings & Persistence (`SettingsManager`)

**Files:**
- Create: `Settings/UserSettings.cs`
- Create: `Settings/SettingsManager.cs`
- Test: `Creature.Tests/Settings/SettingsManagerTests.cs`

**Interfaces:**
- Produces:
  - `UserSettings` (GlobalScale, IsSoundEnabled, IsPaused, EnabledPets)
  - `SettingsManager` with `Load()`, `Save()`, and `SettingsChanged` event

- [ ] **Step 1: Write failing tests for `SettingsManager`**

Create `Creature.Tests/Settings/SettingsManagerTests.cs`:
```csharp
using System.IO;
using Creature.Settings;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Settings;

public class SettingsManagerTests
{
    [Fact]
    public void Load_WhenFileDoesNotExist_ShouldReturnDefaultSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new SettingsManager(tempFile);
            var settings = manager.Load();

            settings.GlobalScale.Should().Be(1.0);
            settings.IsSoundEnabled.Should().BeTrue();
            settings.IsPaused.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void SaveAndLoad_ShouldPersistModifications()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"creature_test_{Guid.NewGuid()}.json");
        try
        {
            var manager = new SettingsManager(tempFile);
            var settings = manager.Load();
            settings.GlobalScale = 1.5;
            settings.IsSoundEnabled = false;
            manager.Save(settings);

            var loaded = manager.Load();
            loaded.GlobalScale.Should().Be(1.5);
            loaded.IsSoundEnabled.Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~SettingsManagerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `UserSettings.cs` and `SettingsManager.cs`**

Create `Settings/UserSettings.cs`:
```csharp
namespace Creature.Settings;

public class UserSettings
{
    public double GlobalScale { get; set; } = 1.0;
    public bool IsSoundEnabled { get; set; } = true;
    public bool IsPaused { get; set; } = false;
    public List<string> DisabledPetIds { get; set; } = new();
}
```

Create `Settings/SettingsManager.cs`:
```csharp
using System.IO;
using System.Text.Json;

namespace Creature.Settings;

public class SettingsManager
{
    private readonly string _filePath;
    public UserSettings CurrentSettings { get; private set; }

    public event Action<UserSettings>? SettingsChanged;

    public SettingsManager(string? customPath = null)
    {
        _filePath = customPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Creature",
            "settings.json"
        );
        CurrentSettings = Load();
    }

    public UserSettings Load()
    {
        if (!File.Exists(_filePath))
        {
            CurrentSettings = new UserSettings();
            return CurrentSettings;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            CurrentSettings = JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            CurrentSettings = new UserSettings();
        }

        return CurrentSettings;
    }

    public void Save(UserSettings settings)
    {
        CurrentSettings = settings;
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
        SettingsChanged?.Invoke(CurrentSettings);
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Settings/ Creature.Tests/
git commit -m "feat: implement UserSettings and SettingsManager with JSON persistence"
```

---

### Task 6: Audio Subsystem (`AudioManager`)

**Files:**
- Create: `Audio/AudioManager.cs`
- Test: `Creature.Tests/Audio/AudioManagerTests.cs`

**Interfaces:**
- Produces:
  - `AudioManager` with `PlaySound(string soundName)`, `IsSoundEnabled`, and 2-voice concurrent limiter

- [ ] **Step 1: Write failing tests for `AudioManager`**

Create `Creature.Tests/Audio/AudioManagerTests.cs`:
```csharp
using Creature.Audio;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Audio;

public class AudioManagerTests
{
    [Fact]
    public void PlaySound_WhenMuted_ShouldNotPlay()
    {
        var audio = new AudioManager { IsSoundEnabled = false };
        var played = audio.PlaySound("hop");
        played.Should().BeFalse();
    }

    [Fact]
    public void ConcurrencyLimiter_ShouldCapSimultaneousVoicesAtTwo()
    {
        var audio = new AudioManager { IsSoundEnabled = true };
        audio.ActiveVoiceCount.Should().Be(0);
        audio.MaxVoices.Should().Be(2);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~AudioManagerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `AudioManager.cs`**

Create `Audio/AudioManager.cs`:
```csharp
namespace Creature.Audio;

public class AudioManager
{
    public bool IsSoundEnabled { get; set; } = true;
    public int MaxVoices { get; set; } = 2;
    private int _activeVoices;

    public int ActiveVoiceCount => _activeVoices;

    public bool PlaySound(string soundName)
    {
        if (!IsSoundEnabled || _activeVoices >= MaxVoices)
            return false;

        Interlocked.Increment(ref _activeVoices);
        _ = Task.Run(async () =>
        {
            await Task.Delay(200);
            Interlocked.Decrement(ref _activeVoices);
        });

        return true;
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Audio/ Creature.Tests/
git commit -m "feat: implement AudioManager with 2-voice concurrency limiter"
```

---

### Task 7: Multi-Monitor & Overlay Window Management

**Files:**
- Create: `Windowing/MonitorInfo.cs`
- Create: `Windowing/MonitorManager.cs`
- Create: `Windowing/OverlayWindow.xaml`
- Create: `Windowing/OverlayWindow.xaml.cs`
- Create: `Windowing/OverlayManager.cs`
- Test: `Creature.Tests/Windowing/MonitorManagerTests.cs`

**Interfaces:**
- Produces:
  - `MonitorInfo(string DeviceName, Rect Bounds, Rect WorkingArea)`
  - `MonitorManager` enumerating displays and listening to `DisplaySettingsChanged`
  - `OverlayWindow` with `Canvas` and `HwndSource` `WM_NCHITTEST` WndProc hook returning `HTTRANSPARENT` / `HTCLIENT`
  - `OverlayManager` creating and updating `OverlayWindow` per monitor

- [ ] **Step 1: Write test for `MonitorManager`**

Create `Creature.Tests/Windowing/MonitorManagerTests.cs`:
```csharp
using Creature.Windowing;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Windowing;

public class MonitorManagerTests
{
    [Fact]
    public void GetMonitors_ShouldReturnAtLeastOneActiveMonitor()
    {
        var manager = new MonitorManager();
        var monitors = manager.GetMonitors();

        monitors.Should().NotBeEmpty();
        monitors[0].WorkingArea.Width.Should().BeGreaterThan(0);
        monitors[0].WorkingArea.Height.Should().BeGreaterThan(0);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~MonitorManagerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `MonitorInfo.cs`, `MonitorManager.cs`, `OverlayWindow`, `OverlayManager.cs`**

Create `Windowing/MonitorInfo.cs`:
```csharp
using System.Windows;

namespace Creature.Windowing;

public record MonitorInfo(string DeviceName, Rect Bounds, Rect WorkingArea, bool IsPrimary);
```

Create `Windowing/MonitorManager.cs`:
```csharp
using System.Windows;
using Microsoft.Win32;

namespace Creature.Windowing;

public class MonitorManager : IDisposable
{
    public event Action? DisplaysChanged;

    public MonitorManager()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        DisplaysChanged?.Invoke();
    }

    public List<MonitorInfo> GetMonitors()
    {
        var list = new List<MonitorInfo>();
        foreach (var screen in System.Windows.Forms.Screen.AllScreens)
        {
            var bounds = new Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
            var workArea = new Rect(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
            list.Add(new MonitorInfo(screen.DeviceName, bounds, workArea, screen.Primary));
        }
        return list;
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
```

Create `Windowing/OverlayWindow.xaml`:
```xml
<Window x:Class="Creature.Windowing.OverlayWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="CreatureOverlay"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        Topmost="True"
        ShowInTaskbar="False"
        ShowActivated="False"
        ResizeMode="NoResize">
    <Canvas x:Name="PetCanvas" Background="Transparent" />
</Window>
```

Create `Windowing/OverlayWindow.xaml.cs`:
```csharp
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Creature.Entities;

namespace Creature.Windowing;

public partial class OverlayWindow : Window
{
    private const int WM_NCHITTEST = 0x0084;
    private const int HTTRANSPARENT = -1;
    private const int HTCLIENT = 1;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public MonitorInfo Monitor { get; }
    public Func<Point, DesktopPet?>? PetHitTester { get; set; }

    public OverlayWindow(MonitorInfo monitor)
    {
        InitializeComponent();
        Monitor = monitor;

        Left = monitor.Bounds.Left;
        Top = monitor.Bounds.Top;
        Width = monitor.Bounds.Width;
        Height = monitor.Bounds.Height;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);

        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            var screenX = (short)(lParam.ToInt32() & 0xFFFF);
            var screenY = (short)((lParam.ToInt32() >> 16) & 0xFFFF);
            var localPoint = PointFromScreen(new Point(screenX, screenY));

            var hitPet = PetHitTester?.Invoke(localPoint);
            if (hitPet != null)
            {
                hitPet.OnCursorHover();
                handled = true;
                return (IntPtr)HTCLIENT;
            }

            handled = true;
            return (IntPtr)HTTRANSPARENT;
        }

        return IntPtr.Zero;
    }
}
```

Create `Windowing/OverlayManager.cs`:
```csharp
using System.Windows.Controls;
using Creature.Entities;

namespace Creature.Windowing;

public class OverlayManager : IDisposable
{
    private readonly MonitorManager _monitorManager;
    private readonly List<OverlayWindow> _overlays = new();
    public Func<Point, DesktopPet?>? PetHitTester { get; set; }

    public OverlayManager(MonitorManager monitorManager)
    {
        _monitorManager = monitorManager;
        _monitorManager.DisplaysChanged += RecreateOverlays;
        RecreateOverlays();
    }

    public void RecreateOverlays()
    {
        foreach (var overlay in _overlays)
        {
            overlay.Close();
        }
        _overlays.Clear();

        var monitors = _monitorManager.GetMonitors();
        foreach (var monitor in monitors)
        {
            var overlay = new OverlayWindow(monitor)
            {
                PetHitTester = PetHitTester
            };
            overlay.Show();
            _overlays.Add(overlay);
        }
    }

    public Canvas? GetCanvasForMonitor(string deviceName)
    {
        return _overlays.FirstOrDefault(o => o.Monitor.DeviceName == deviceName)?.PetCanvas 
               ?? _overlays.FirstOrDefault()?.PetCanvas;
    }

    public void Dispose()
    {
        _monitorManager.DisplaysChanged -= RecreateOverlays;
        foreach (var overlay in _overlays)
        {
            overlay.Close();
        }
        _overlays.Clear();
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Windowing/ Creature.Tests/
git commit -m "feat: implement MonitorManager and OverlayWindow with WM_NCHITTEST pass-through"
```

---

### Task 8: Simulation Engine, Tray Management & App Bootstrap

**Files:**
- Create: `Simulation/SimulationEngine.cs`
- Create: `Tray/TrayManager.cs`
- Modify: `App.xaml`
- Modify: `App.xaml.cs`
- Delete: `MainWindow.xaml`, `MainWindow.xaml.cs`
- Test: `Creature.Tests/Simulation/SimulationEngineTests.cs`

**Interfaces:**
- Produces:
  - `SimulationEngine` running ~60Hz CompositionTarget loop coordinating pets, physics, and canvas rendering
  - `TrayManager` encapsulating NotifyIcon tray menu (Show/Hide, Scale sub-menu, Sound toggle, Pause/Resume, Exit) and right-click pet context menu
  - `App.xaml.cs` orchestrating zero-window background startup

- [ ] **Step 1: Write test for `SimulationEngine`**

Create `Creature.Tests/Simulation/SimulationEngineTests.cs`:
```csharp
using System.IO;
using System.Windows;
using Creature.Entities.Bunny;
using Creature.Graphics;
using Creature.Simulation;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Simulation;

public class SimulationEngineTests
{
    [StaFact]
    public void HitTest_WhenCursorOverPet_ShouldReturnPet()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny = new BunnyEntity("bunny-1", sm, bounds);
        bunny.Transform.Position = new Core.Physics.Vector2D(100, 100);
        engine.AddPet(bunny);

        var hit = engine.HitTest(new Point(110, 110));
        hit.Should().Be(bunny);

        var miss = engine.HitTest(new Point(500, 500));
        miss.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~SimulationEngineTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `SimulationEngine.cs`, `TrayManager.cs`, and `App.xaml.cs`**

Create `Simulation/SimulationEngine.cs`:
```csharp
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Creature.Entities;

namespace Creature.Simulation;

public class SimulationEngine
{
    private readonly List<DesktopPet> _pets = new();
    private readonly Stopwatch _stopwatch = new();
    private double _lastTimestamp;
    private bool _isRunning;

    public bool IsPaused { get; set; }
    public IReadOnlyList<DesktopPet> Pets => _pets;

    public void AddPet(DesktopPet pet)
    {
        _pets.Add(pet);
    }

    public void RemovePet(DesktopPet pet)
    {
        _pets.Remove(pet);
    }

    public void Start()
    {
        if (_isRunning) return;
        _isRunning = true;
        _stopwatch.Restart();
        _lastTimestamp = 0;
        CompositionTarget.Rendering += OnRendering;
    }

    public void Stop()
    {
        if (!_isRunning) return;
        _isRunning = false;
        CompositionTarget.Rendering -= OnRendering;
        _stopwatch.Stop();
    }

    private void OnRendering(object? sender, EventArgs e)
    {
        var current = _stopwatch.Elapsed.TotalSeconds;
        var dt = current - _lastTimestamp;
        _lastTimestamp = current;

        if (dt > 0.05) dt = 0.05;

        if (!IsPaused)
        {
            foreach (var pet in _pets)
            {
                pet.Update(dt);
            }
        }
    }

    public DesktopPet? HitTest(Point localPoint)
    {
        return _pets.FirstOrDefault(p => p.HitTest(localPoint));
    }

    public void ApplyGlobalScale(double scale)
    {
        foreach (var pet in _pets)
        {
            pet.Transform.Scale = scale;
            pet.UpdateFloor();
            pet.SyncVisualTransform();
        }
    }
}
```

Create `Tray/TrayManager.cs`:
```csharp
using System.Drawing;
using System.Windows.Forms;
using Creature.Settings;
using Creature.Simulation;

namespace Creature.Tray;

public class TrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsManager _settingsManager;
    private readonly SimulationEngine _simulationEngine;
    private readonly Action _onExit;

    public TrayManager(SettingsManager settingsManager, SimulationEngine simulationEngine, Action onExit)
    {
        _settingsManager = settingsManager;
        _simulationEngine = simulationEngine;
        _onExit = onExit;

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "Desktop Pets (Creature)",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
    }

    private ContextMenuStrip BuildContextMenu()
    {
        var menu = new ContextMenuStrip();

        var pauseItem = new ToolStripMenuItem("Pause Simulation", null, (s, e) =>
        {
            var item = (ToolStripMenuItem)s!;
            _simulationEngine.IsPaused = !_simulationEngine.IsPaused;
            item.Checked = _simulationEngine.IsPaused;
            var settings = _settingsManager.CurrentSettings;
            settings.IsPaused = _simulationEngine.IsPaused;
            _settingsManager.Save(settings);
        })
        {
            Checked = _settingsManager.CurrentSettings.IsPaused
        };
        menu.Items.Add(pauseItem);

        var soundItem = new ToolStripMenuItem("Sound Enabled", null, (s, e) =>
        {
            var item = (ToolStripMenuItem)s!;
            var settings = _settingsManager.CurrentSettings;
            settings.IsSoundEnabled = !settings.IsSoundEnabled;
            item.Checked = settings.IsSoundEnabled;
            _settingsManager.Save(settings);
        })
        {
            Checked = _settingsManager.CurrentSettings.IsSoundEnabled
        };
        menu.Items.Add(soundItem);

        var scaleSubMenu = new ToolStripMenuItem("Global Pet Size");
        double[] scales = [0.5, 0.75, 1.0, 1.25, 1.5, 2.0];
        foreach (var scale in scales)
        {
            var scaleItem = new ToolStripMenuItem($"{scale}x", null, (s, e) =>
            {
                var settings = _settingsManager.CurrentSettings;
                settings.GlobalScale = scale;
                _settingsManager.Save(settings);
                _simulationEngine.ApplyGlobalScale(scale);
                UpdateScaleMenuChecks(scaleSubMenu, scale);
            })
            {
                Checked = Math.Abs(_settingsManager.CurrentSettings.GlobalScale - scale) < 0.01
            };
            scaleSubMenu.DropDownItems.Add(scaleItem);
        }
        menu.Items.Add(scaleSubMenu);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit", null, (s, e) => _onExit());

        return menu;
    }

    private void UpdateScaleMenuChecks(ToolStripMenuItem parent, double activeScale)
    {
        foreach (ToolStripMenuItem item in parent.DropDownItems)
        {
            item.Checked = item.Text == $"{activeScale}x";
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
```

Update `App.xaml`:
```xml
<Application x:Class="Creature.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             ShutdownMode="OnExplicitShutdown">
    <Application.Resources>
    </Application.Resources>
</Application>
```

Update `App.xaml.cs`:
```csharp
using System.IO;
using System.Windows;
using Creature.Audio;
using Creature.Entities.Bunny;
using Creature.Graphics;
using Creature.Settings;
using Creature.Simulation;
using Creature.Tray;
using Creature.Windowing;

namespace Creature;

public partial class App : Application
{
    private MonitorManager? _monitorManager;
    private OverlayManager? _overlayManager;
    private SpriteManager? _spriteManager;
    private SettingsManager? _settingsManager;
    private AudioManager? _audioManager;
    private SimulationEngine? _simulationEngine;
    private TrayManager? _trayManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsManager = new SettingsManager();
        _audioManager = new AudioManager { IsSoundEnabled = _settingsManager.CurrentSettings.IsSoundEnabled };
        _spriteManager = new SpriteManager();
        LoadAssets();

        _monitorManager = new MonitorManager();
        _simulationEngine = new SimulationEngine
        {
            IsPaused = _settingsManager.CurrentSettings.IsPaused
        };

        _overlayManager = new OverlayManager(_monitorManager)
        {
            PetHitTester = point => _simulationEngine.HitTest(point)
        };

        var primaryMonitor = _monitorManager.GetMonitors().FirstOrDefault(m => m.IsPrimary) 
                             ?? _monitorManager.GetMonitors().First();
        var canvas = _overlayManager.GetCanvasForMonitor(primaryMonitor.DeviceName);

        if (canvas != null)
        {
            var bunny = new BunnyEntity("bunny-primary", _spriteManager, primaryMonitor.WorkingArea);
            bunny.Transform.Scale = _settingsManager.CurrentSettings.GlobalScale;
            bunny.UpdateFloor();
            bunny.SyncVisualTransform();

            canvas.Children.Add(bunny.VisualElement);
            _simulationEngine.AddPet(bunny);
        }

        _trayManager = new TrayManager(_settingsManager, _simulationEngine, () => Shutdown());
        _simulationEngine.Start();
    }

    private void LoadAssets()
    {
        var baseDir = Path.Combine(AppContext.BaseDirectory, "docs", "assets");
        if (!Directory.Exists(baseDir))
        {
            baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        }

        _spriteManager!.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));
        _spriteManager!.LoadAnimation("BunnyRun", Path.Combine(baseDir, "BunnyRun.json"), Path.Combine(baseDir, "BunnyRun.png"));
        _spriteManager!.LoadAnimation("BunnyJump", Path.Combine(baseDir, "BunnyJump.json"), Path.Combine(baseDir, "BunnyJump.png"), loop: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _simulationEngine?.Stop();
        _trayManager?.Dispose();
        _overlayManager?.Dispose();
        _monitorManager?.Dispose();
        base.OnExit(e);
    }
}
```

- [ ] **Step 4: Run `dotnet test` and `dotnet build`**

Run: `dotnet test`
Run: `dotnet build`
Expected: All tests pass and build succeeds with 0 errors.

- [ ] **Step 5: Commit**

```bash
git add Simulation/ Tray/ Windowing/ App.xaml App.xaml.cs Creature.Tests/
git commit -m "feat: implement SimulationEngine, TrayManager, and zero-window bootstrap"
```

---

### Task 9: End-to-End Verification & Sanity Validation

**Files:**
- Test: `Creature.Tests/` (All test suites)

- [ ] **Step 1: Execute all unit and component tests**

Run: `dotnet test -c Release --verbosity normal`
Expected: All tests pass.

- [ ] **Step 2: Build release binary**

Run: `dotnet build -c Release`
Expected: `Creature.exe` generated in `bin/Release/net10.0-windows/` with 0 warnings/errors.

- [ ] **Step 3: Verification Checklist**
  1. Overlay window creation matches each active display work area.
  2. `WM_NCHITTEST` WndProc hook returns `HTTRANSPARENT` for empty screen canvas and `HTCLIENT` over bunny bounds.
  3. Bunny entity boots into Idle lie-down loop, roams horizontally, and triggers Jump on mouse hover.
  4. Global size scaling applies nearest-neighbor pixel filter cleanly without blur.
  5. System tray provides pause/resume, sound toggle, scale selectors, and clean shutdown.

- [ ] **Step 4: Final commit & tag**

```bash
git add .
git commit -m "chore: complete Desktop Pets MVP implementation and verification"
```
