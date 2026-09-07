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
        _settingsManager.SettingsChanged += s =>
        {
            if (_audioManager != null)
            {
                _audioManager.IsSoundEnabled = s.IsSoundEnabled;
            }
        };

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
