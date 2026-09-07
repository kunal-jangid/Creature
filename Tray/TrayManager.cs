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
