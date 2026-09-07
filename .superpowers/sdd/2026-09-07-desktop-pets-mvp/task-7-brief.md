# Task 7: Multi-Monitor & Overlay Window Management

**Files:**
- Create: `Windowing/MonitorInfo.cs`
- Create: `Windowing/MonitorManager.cs`
- Create: `Windowing/OverlayWindow.xaml`
- Create: `Windowing/OverlayWindow.xaml.cs`
- Create: `Windowing/OverlayManager.cs`
- Test: `Creature.Tests/Windowing/MonitorManagerTests.cs`

**Interfaces:**
- Produces:
  - `MonitorInfo(string DeviceName, Rect Bounds, Rect WorkingArea, bool IsPrimary)`
  - `MonitorManager : IDisposable` with `GetMonitors() -> List<MonitorInfo>`, `event Action? DisplaysChanged`
  - `OverlayWindow : Window` with `MonitorInfo Monitor`, `PetCanvas` (`Canvas`), `Func<Point, DesktopPet?>? PetHitTester`, and `WM_NCHITTEST` WndProc hook returning `HTCLIENT` (`1`) on pet hit or `HTTRANSPARENT` (`-1`) on background
  - `OverlayManager : IDisposable` with `RecreateOverlays()`, `GetCanvasForMonitor(string deviceName) -> Canvas?`, `PetHitTester` property

## Steps to Execute:

- [ ] **Step 1: Write test for `MonitorManager`**

Create `Creature.Tests/Windowing/MonitorManagerTests.cs`:
```csharp
using System.Windows;
using Creature.Windowing;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Windowing;

public class MonitorManagerTests
{
    [Fact]
    public void GetMonitors_ShouldReturnAtLeastOneActiveMonitor()
    {
        using var manager = new MonitorManager();
        var monitors = manager.GetMonitors();

        monitors.Should().NotBeEmpty();
        monitors[0].WorkingArea.Width.Should().BeGreaterThan(0);
        monitors[0].WorkingArea.Height.Should().BeGreaterThan(0);
        monitors.Should().Contain(m => m.IsPrimary);
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~MonitorManagerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `MonitorInfo.cs`, `MonitorManager.cs`, `OverlayWindow.xaml`, `OverlayWindow.xaml.cs`, `OverlayManager.cs`**

Create `Windowing/MonitorInfo.cs`:
```csharp
using System.Windows;

namespace Creature.Windowing;

public record MonitorInfo(string DeviceName, Rect Bounds, Rect WorkingArea, bool IsPrimary);
```

Create `Windowing/MonitorManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Windows;
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
using System;
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
