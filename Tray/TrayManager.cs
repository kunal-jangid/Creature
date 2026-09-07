using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Creature.Entities;
using Creature.Settings;
using Creature.Simulation;
using Creature.SystemIntegration;

namespace Creature.Tray;

public class TrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsManager _settingsManager;
    private readonly SimulationEngine _simulationEngine;
    private readonly Action _onExit;

    public Action<string>? OnAddPetSpecies { get; set; }
    public Action? OnAddPet { get; set; }
    public Action<DesktopPet>? OnRenamePet { get; set; }
    public Action<DesktopPet>? OnRemovePet { get; set; }

    public TrayManager(SettingsManager settingsManager, SimulationEngine simulationEngine, Action onExit)
    {
        _settingsManager = settingsManager;
        _simulationEngine = simulationEngine;
        _onExit = onExit;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
        Icon icon = File.Exists(iconPath) ? new Icon(iconPath) : SystemIcons.Application;

        _notifyIcon = new NotifyIcon
        {
            Icon = icon,
            Text = "Desktop Pets",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };
    }

    public void RefreshPetMenu()
    {
        var oldMenu = _notifyIcon.ContextMenuStrip;
        _notifyIcon.ContextMenuStrip = BuildContextMenu();
        oldMenu?.Dispose();
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

        var namesItem = new ToolStripMenuItem("Show Pet Names", null, (s, e) =>
        {
            var item = (ToolStripMenuItem)s!;
            var settings = _settingsManager.CurrentSettings;
            settings.ShowPetNames = !settings.ShowPetNames;
            item.Checked = settings.ShowPetNames;
            _simulationEngine.SetShowPetNames(settings.ShowPetNames);
            _settingsManager.Save(settings);
        })
        {
            Checked = _settingsManager.CurrentSettings.ShowPetNames
        };
        menu.Items.Add(namesItem);

        var startWithWindowsItem = new ToolStripMenuItem("Start with Windows", null, (s, e) =>
        {
            var item = (ToolStripMenuItem)s!;
            var settings = _settingsManager.CurrentSettings;
            settings.StartWithWindows = !settings.StartWithWindows;
            item.Checked = settings.StartWithWindows;
            StartupManager.SetStartup(settings.StartWithWindows);
            _settingsManager.Save(settings);
        })
        {
            Checked = _settingsManager.CurrentSettings.StartWithWindows
        };
        menu.Items.Add(startWithWindowsItem);

        var scaleSubMenu = new ToolStripMenuItem("Global Pet Size");
        (double scale, string label)[] scales = [
            (1.0, "1.0x (Small - 32px)"),
            (1.5, "1.5x (Medium - 48px)"),
            (2.0, "2.0x (Default - 64px)"),
            (2.5, "2.5x (Large - 80px)"),
            (3.0, "3.0x (Extra Large - 96px)"),
            (4.0, "4.0x (Huge - 128px)")
        ];
        foreach (var (scale, label) in scales)
        {
            var scaleItem = new ToolStripMenuItem(label, null, (s, e) =>
            {
                var settings = _settingsManager.CurrentSettings;
                settings.GlobalScale = scale;
                _settingsManager.Save(settings);
                _simulationEngine.ApplyGlobalScale(scale);
                UpdateScaleMenuChecks(scaleSubMenu, scale);
            })
            {
                Checked = Math.Abs(_settingsManager.CurrentSettings.GlobalScale - scale) < 0.01,
                Tag = scale
            };
            scaleSubMenu.DropDownItems.Add(scaleItem);
        }
        menu.Items.Add(scaleSubMenu);

        menu.Items.Add(new ToolStripSeparator());

        var isMaxReached = _simulationEngine.Pets.Count >= 4;
        var addPetSubMenu = new ToolStripMenuItem(isMaxReached ? "Adopt Pet (Max 4)" : "Adopt Pet")
        {
            Enabled = !isMaxReached
        };

        string[] speciesList = ["Bunny", "Gorgon", "Werewolf"];
        foreach (var species in speciesList)
        {
            var sp = species;
            addPetSubMenu.DropDownItems.Add(new ToolStripMenuItem($"Adopt {sp}...", null, (s, e) =>
            {
                if (OnAddPetSpecies != null)
                {
                    OnAddPetSpecies(sp);
                }
                else
                {
                    OnAddPet?.Invoke();
                }
            }));
        }
        menu.Items.Add(addPetSubMenu);

        var renamePetSubMenu = new ToolStripMenuItem("Rename Pet");
        if (_simulationEngine.Pets.Count == 0)
        {
            renamePetSubMenu.DropDownItems.Add(new ToolStripMenuItem("No active pets") { Enabled = false });
        }
        else
        {
            foreach (var pet in _simulationEngine.Pets)
            {
                var targetPet = pet;
                renamePetSubMenu.DropDownItems.Add(new ToolStripMenuItem($"{pet.Name} ({pet.Id})", null, (s, e) =>
                {
                    OnRenamePet?.Invoke(targetPet);
                }));
            }
        }
        menu.Items.Add(renamePetSubMenu);

        var removePetSubMenu = new ToolStripMenuItem("Remove Pet");
        if (_simulationEngine.Pets.Count == 0)
        {
            removePetSubMenu.DropDownItems.Add(new ToolStripMenuItem("No active pets") { Enabled = false });
        }
        else
        {
            foreach (var pet in _simulationEngine.Pets)
            {
                var targetPet = pet;
                removePetSubMenu.DropDownItems.Add(new ToolStripMenuItem($"{pet.Name} ({pet.Id})", null, (s, e) =>
                {
                    OnRemovePet?.Invoke(targetPet);
                }));
            }
        }
        menu.Items.Add(removePetSubMenu);

        menu.Items.Add(new ToolStripSeparator());

        menu.Items.Add("Exit", null, (s, e) => _onExit());

        return menu;
    }

    private void UpdateScaleMenuChecks(ToolStripMenuItem parent, double activeScale)
    {
        foreach (ToolStripMenuItem item in parent.DropDownItems)
        {
            if (item.Tag is double scale)
            {
                item.Checked = Math.Abs(scale - activeScale) < 0.01;
            }
        }
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
