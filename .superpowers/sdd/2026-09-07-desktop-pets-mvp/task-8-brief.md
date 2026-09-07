# Task 8: Simulation Engine, Tray Management & App Bootstrap

**Files:**
- Create: `Simulation/SimulationEngine.cs`
- Create: `Tray/TrayManager.cs`
- Modify: `App.xaml`
- Modify: `App.xaml.cs`
- Modify: `Entities/DesktopPet.cs` (align SyncVisualTransform scale transform to avoid compounding: `RenderTransform = new ScaleTransform(Transform.IsFacingLeft ? -1.0 : 1.0, 1.0)`)
- Delete: `MainWindow.xaml`, `MainWindow.xaml.cs`
- Test: `Creature.Tests/Simulation/SimulationEngineTests.cs`

**Interfaces:**
- Produces:
  - `SimulationEngine`: running ~60Hz `CompositionTarget.Rendering` loop coordinating pets, physics, scaling, and canvas rendering. Provides `Start()`, `Stop()`, `AddPet()`, `RemovePet()`, `HitTest(Point localPoint)`, `ApplyGlobalScale(double scale)`, `IsPaused` property.
  - `TrayManager : IDisposable`: encapsulating `NotifyIcon` tray menu with Pause/Resume, Sound toggle, Global Pet Size sub-menu (0.5x, 0.75x, 1.0x, 1.25x, 1.5x, 2.0x), and Exit action.
  - `App.xaml` & `App.xaml.cs`: zero-window background startup with explicit shutdown (`ShutdownMode="OnExplicitShutdown"`), bootstrapping settings, audio, sprite assets, monitor manager, overlay manager, simulation engine, spawning the primary Bunny MVP entity, and initializing the tray manager.

## Steps to Execute:

- [ ] **Step 1: Write test for `SimulationEngine`**

Create `Creature.Tests/Simulation/SimulationEngineTests.cs`:
```csharp
using System;
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

    [StaFact]
    public void ApplyGlobalScale_ShouldUpdateScaleAndFloorForAllPets()
    {
        var sm = new SpriteManager();
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");
        sm.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));

        var bounds = new Rect(0, 0, 1920, 1080);
        var engine = new SimulationEngine();
        var bunny = new BunnyEntity("bunny-1", sm, bounds);
        engine.AddPet(bunny);

        engine.ApplyGlobalScale(1.5);

        bunny.Transform.Scale.Should().Be(1.5);
        bunny.Transform.ScaledWidth.Should().Be(48);
        bunny.Transform.ScaledHeight.Should().Be(48);
        bunny.Physics.FloorY.Should().Be(1080 - 48);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~SimulationEngineTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `SimulationEngine.cs`, `TrayManager.cs`, `DesktopPet.cs` scale fix, and `App.xaml / App.xaml.cs`**

Create `Simulation/SimulationEngine.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

        // Clamp dt to avoid physics spiral on lag spike
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
using System;
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

Update `Entities/DesktopPet.cs` `SyncVisualTransform()`:
```csharp
    public void SyncVisualTransform()
    {
        Canvas.SetLeft(VisualElement, Transform.Position.X);
        Canvas.SetTop(VisualElement, Transform.Position.Y);
        VisualElement.Width = Transform.ScaledWidth;
        VisualElement.Height = Transform.ScaledHeight;

        var scaleX = Transform.IsFacingLeft ? -1.0 : 1.0;
        VisualElement.RenderTransform = new ScaleTransform(scaleX, 1.0);
    }
```
Also update `BunnyEntityTests.cs` to assert `scaleTransform.ScaleX.Should().Be(-1.0); scaleTransform.ScaleY.Should().Be(1.0);`.

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
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Creature.Audio;
using Creature.Entities.Bunny;
using Creature.Graphics;
using Creature.Settings;
using Creature.Simulation;
using Creature.Tray;
using Creature.Windowing;

namespace Creature;

public partial class App : System.Windows.Application
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

        var monitors = _monitorManager.GetMonitors();
        if (monitors.Count > 0)
        {
            var primaryMonitor = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
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

Delete `MainWindow.xaml` and `MainWindow.xaml.cs`.

- [ ] **Step 4: Run `dotnet test` and `dotnet build`**

Run: `dotnet test`
Run: `dotnet build`
Expected: All tests pass and build succeeds.

- [ ] **Step 5: Commit**

```bash
git add Simulation/ Tray/ Entities/ App.xaml App.xaml.cs Creature.Tests/
git rm MainWindow.xaml MainWindow.xaml.cs
git commit -m "feat: implement SimulationEngine, TrayManager, and zero-window bootstrap"
```
