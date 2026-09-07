using System;
using System.IO;
using System.Linq;
using System.Windows;
using Creature.Audio;
using Creature.Entities;
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
            var settings = _settingsManager.CurrentSettings;
            if (settings.Pets.Count == 0)
            {
                settings.Pets.Add(new PetProfile { Id = "bunny-1", Name = "Fluffy", Species = "Bunny" });
                _settingsManager.Save(settings);
            }

            foreach (var petProfile in settings.Pets.ToList())
            {
                SpawnPet(petProfile);
            }
        }

        _trayManager = new TrayManager(_settingsManager, _simulationEngine, () => Shutdown())
        {
            OnAddPet = AddBunny,
            OnRemovePet = RemovePet
        };
        _simulationEngine.Start();
    }

    private void SpawnPet(PetProfile profile)
    {
        if (_monitorManager == null || _overlayManager == null || _spriteManager == null || _settingsManager == null || _simulationEngine == null)
            return;

        var monitors = _monitorManager.GetMonitors();
        if (monitors.Count == 0) return;

        var primaryMonitor = monitors.FirstOrDefault(m => m.IsPrimary) ?? monitors[0];
        var canvas = _overlayManager.GetCanvasForMonitor(primaryMonitor.DeviceName);
        if (canvas == null) return;

        var bunny = new BunnyEntity(profile.Id, _spriteManager, primaryMonitor.WorkingArea, profile.Name);
        bunny.Transform.Scale = _settingsManager.CurrentSettings.GlobalScale;
        bunny.SetShowName(_settingsManager.CurrentSettings.ShowPetNames);
        bunny.UpdateFloor();

        // Stagger spawn positions so pets do not overlap directly on top of each other
        var random = new Random();
        var spawnX = primaryMonitor.WorkingArea.Left + random.NextDouble() * Math.Max(100, primaryMonitor.WorkingArea.Width - bunny.Transform.ScaledWidth);
        bunny.Transform.Position = new Core.Physics.Vector2D(spawnX, bunny.Physics.FloorY);
        bunny.SyncVisualTransform();

        canvas.Children.Add(bunny.VisualElement);
        canvas.Children.Add(bunny.NameLabel);
        _simulationEngine.AddPet(bunny);
    }

    private void AddBunny()
    {
        if (_settingsManager == null || _simulationEngine == null) return;

        var nextIndex = _settingsManager.CurrentSettings.Pets.Count + 1;
        var newProfile = new PetProfile
        {
            Id = $"bunny-{Guid.NewGuid():N}",
            Name = $"Bunny {nextIndex}",
            Species = "Bunny"
        };

        var settings = _settingsManager.CurrentSettings;
        settings.Pets.Add(newProfile);
        _settingsManager.Save(settings);

        SpawnPet(newProfile);
        _trayManager?.RefreshPetMenu();
    }

    private void RemovePet(DesktopPet pet)
    {
        if (_overlayManager == null || _monitorManager == null || _simulationEngine == null || _settingsManager == null) return;

        var monitors = _monitorManager.GetMonitors();
        foreach (var monitor in monitors)
        {
            var canvas = _overlayManager.GetCanvasForMonitor(monitor.DeviceName);
            if (canvas != null)
            {
                canvas.Children.Remove(pet.VisualElement);
                canvas.Children.Remove(pet.NameLabel);
            }
        }

        _simulationEngine.RemovePet(pet);

        var settings = _settingsManager.CurrentSettings;
        settings.Pets.RemoveAll(p => p.Id == pet.Id);
        _settingsManager.Save(settings);

        _trayManager?.RefreshPetMenu();
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
