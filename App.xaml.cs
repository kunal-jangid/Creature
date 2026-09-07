using System;
using System.IO;
using System.Linq;
using System.Windows;
using Creature.Entities;
using Creature.Entities.Bunny;
using Creature.Entities.Gorgon;
using Creature.Entities.Werewolf;
using Creature.Graphics;
using Creature.Settings;
using Creature.Simulation;
using Creature.SystemIntegration;
using Creature.Tray;
using Creature.UI;
using Creature.Windowing;

namespace Creature;

public partial class App : System.Windows.Application
{
    private MonitorManager? _monitorManager;
    private OverlayManager? _overlayManager;
    private SpriteManager? _spriteManager;
    private SettingsManager? _settingsManager;
    private SimulationEngine? _simulationEngine;
    private TrayManager? _trayManager;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _settingsManager = new SettingsManager();
        StartupManager.SetStartup(_settingsManager.CurrentSettings.StartWithWindows);

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
            OnAddPetSpecies = AddPetWithPrompt,
            OnRenamePet = RenamePetWithPrompt,
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

        DesktopPet pet;
        switch (profile.Species.ToLowerInvariant())
        {
            case "gorgon":
                pet = new GorgonEntity(profile.Id, _spriteManager, primaryMonitor.WorkingArea, profile.Name);
                break;
            case "werewolf":
                pet = new WerewolfEntity(profile.Id, _spriteManager, primaryMonitor.WorkingArea, profile.Name);
                break;
            case "bunny":
            default:
                pet = new BunnyEntity(profile.Id, _spriteManager, primaryMonitor.WorkingArea, profile.Name);
                break;
        }

        pet.Transform.Scale = _settingsManager.CurrentSettings.GlobalScale;
        pet.SetShowName(_settingsManager.CurrentSettings.ShowPetNames);
        pet.UpdateFloor();

        // Stagger spawn positions so pets do not overlap directly on top of each other
        var random = new Random();
        var spawnX = primaryMonitor.WorkingArea.Left + random.NextDouble() * Math.Max(100, primaryMonitor.WorkingArea.Width - pet.Transform.ScaledWidth);
        pet.Transform.Position = new Core.Physics.Vector2D(spawnX, pet.Physics.FloorY);
        pet.SyncVisualTransform();

        canvas.Children.Add(pet.VisualElement);
        canvas.Children.Add(pet.NameLabel);
        _simulationEngine.AddPet(pet);
    }

    private void AddPetWithPrompt(string species)
    {
        if (_settingsManager == null || _simulationEngine == null) return;

        if (_settingsManager.CurrentSettings.Pets.Count >= 4)
        {
            System.Windows.MessageBox.Show("Maximum of 4 pets reached!", "Adopt Pet", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var nextIndex = _settingsManager.CurrentSettings.Pets.Count(p => p.Species.Equals(species, StringComparison.OrdinalIgnoreCase)) + 1;
        var defaultName = species.ToLowerInvariant() switch
        {
            "gorgon" => $"Medusa {nextIndex}",
            "werewolf" => $"Fang {nextIndex}",
            _ => $"Bunny {nextIndex}"
        };

        var petName = PetNameDialog.Prompt(species, defaultName);
        if (string.IsNullOrWhiteSpace(petName)) return;

        var newProfile = new PetProfile
        {
            Id = $"{species.ToLowerInvariant()}-{Guid.NewGuid():N}",
            Name = petName,
            Species = species
        };

        var settings = _settingsManager.CurrentSettings;
        settings.Pets.Add(newProfile);
        _settingsManager.Save(settings);

        SpawnPet(newProfile);
        _trayManager?.RefreshPetMenu();
    }

    private void RenamePetWithPrompt(DesktopPet pet)
    {
        if (_settingsManager == null) return;

        var species = pet.GetType().Name.Replace("Entity", "");
        var newName = PetNameDialog.Prompt(species, pet.Name, title: "Rename Pet", customPrompt: $"Enter a new name for {pet.Name}:");
        if (string.IsNullOrWhiteSpace(newName)) return;

        pet.SetName(newName);
        pet.SyncVisualTransform();

        var settings = _settingsManager.CurrentSettings;
        var profile = settings.Pets.FirstOrDefault(p => p.Id == pet.Id);
        if (profile != null)
        {
            profile.Name = newName;
            _settingsManager.Save(settings);
        }

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
        var baseDir = Path.Combine(AppContext.BaseDirectory, "Assets");

        // Bunny
        _spriteManager!.LoadAnimation("BunnyLieDown", Path.Combine(baseDir, "BunnyLieDown.json"), Path.Combine(baseDir, "BunnyLieDown.png"));
        _spriteManager!.LoadAnimation("BunnyRun", Path.Combine(baseDir, "BunnyRun.json"), Path.Combine(baseDir, "BunnyRun.png"));
        _spriteManager!.LoadAnimation("BunnyJump", Path.Combine(baseDir, "BunnyJump.json"), Path.Combine(baseDir, "BunnyJump.png"), loop: false);

        // Gorgon
        var gorgonDir = Path.Combine(baseDir, "Gorgon");
        if (Directory.Exists(gorgonDir))
        {
            _spriteManager.LoadAnimation("GorgonIdle", Path.Combine(gorgonDir, "Idle.json"), Path.Combine(gorgonDir, "Idle-sheet.png"));
            _spriteManager.LoadAnimation("GorgonRun", Path.Combine(gorgonDir, "Run.json"), Path.Combine(gorgonDir, "Run-sheet.png"));
            _spriteManager.LoadAnimation("GorgonSpecial", Path.Combine(gorgonDir, "Special.json"), Path.Combine(gorgonDir, "Special-sheet.png"), loop: false);
        }

        // Werewolf
        var werewolfDir = Path.Combine(baseDir, "Werewolf");
        if (Directory.Exists(werewolfDir))
        {
            _spriteManager.LoadAnimation("WerewolfIdle", Path.Combine(werewolfDir, "Idle.json"), Path.Combine(werewolfDir, "Idle-sheet.png"));
            _spriteManager.LoadAnimation("WerewolfRun", Path.Combine(werewolfDir, "Run.json"), Path.Combine(werewolfDir, "Run-sheet.png"));
            _spriteManager.LoadAnimation("WerewolfFlinch", Path.Combine(werewolfDir, "Dead.json"), Path.Combine(werewolfDir, "Dead-sheet.png"), loop: false);
        }
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
