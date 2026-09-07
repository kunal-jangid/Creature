using System;
using System.Collections.Generic;
using System.Windows.Forms;
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
        foreach (var screen in Screen.AllScreens)
        {
            var bounds = new System.Windows.Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
            var workArea = new System.Windows.Rect(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
            list.Add(new MonitorInfo(screen.DeviceName, bounds, workArea, screen.Primary));
        }
        return list;
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }
}
