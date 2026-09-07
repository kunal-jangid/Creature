using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Creature.Entities;
using Creature.Settings;
using Creature.Simulation;

namespace Creature.Tray;

public class TrayManager : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsManager _settingsManager;
    private readonly SimulationEngine _simulationEngine;
    private readonly Action _onExit;

    public Action? OnAddPet { get; set; }
    public Action<DesktopPet>? OnRemovePet { get; set; }

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

    public void RefreshPetMenu()
    {
        _notifyIcon.ContextMenuStrip = BuildContextMenu();
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

        var addPetItem = new ToolStripMenuItem("Add Bunny", null, (s, e) =>
        {
            OnAddPet?.Invoke();
        });
        menu.Items.Add(addPetItem);

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
                var removeSpecificItem = new ToolStripMenuItem($"{pet.Name} ({pet.Id})", null, (s, e) =>
                {
                    OnRemovePet?.Invoke(targetPet);
                });
                removePetSubMenu.DropDownItems.Add(removeSpecificItem);
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
