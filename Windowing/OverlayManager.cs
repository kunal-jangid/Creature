using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using Creature.Entities;

namespace Creature.Windowing;

public class OverlayManager : IDisposable
{
    private readonly MonitorManager _monitorManager;
    private readonly List<OverlayWindow> _overlays = new();
    private Func<System.Windows.Point, DesktopPet?>? _petHitTester;

    public Func<System.Windows.Point, DesktopPet?>? PetHitTester
    {
        get => _petHitTester;
        set
        {
            _petHitTester = value;
            foreach (var overlay in _overlays)
            {
                overlay.PetHitTester = value;
            }
        }
    }

    public IReadOnlyList<OverlayWindow> Overlays => _overlays;

    public OverlayManager(MonitorManager monitorManager)
    {
        _monitorManager = monitorManager;
        _monitorManager.DisplaysChanged += RecreateOverlays;
        RecreateOverlays();
    }

    public void RecreateOverlays()
    {
        if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(RecreateOverlays);
            return;
        }

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
                PetHitTester = _petHitTester
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
