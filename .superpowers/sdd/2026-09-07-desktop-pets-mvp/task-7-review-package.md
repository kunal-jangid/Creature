# Review Package for Task 7

## Commits
`
f9038bc feat: implement MonitorManager and OverlayWindow with WM_NCHITTEST pass-through

`

## Diff Stat
`
 Creature.Tests/Windowing/MonitorManagerTests.cs | 169 ++++++++++++++++++++++++
 Windowing/MonitorInfo.cs                        |   3 +
 Windowing/MonitorManager.cs                     |  38 ++++++
 Windowing/OverlayManager.cs                     |  78 +++++++++++
 Windowing/OverlayWindow.xaml                    |  13 ++
 Windowing/OverlayWindow.xaml.cs                 |  78 +++++++++++
 6 files changed, 379 insertions(+)

`

## Full Diff
`diff
diff --git a/Creature.Tests/Windowing/MonitorManagerTests.cs b/Creature.Tests/Windowing/MonitorManagerTests.cs
new file mode 100644
index 0000000..a89c951
--- /dev/null
+++ b/Creature.Tests/Windowing/MonitorManagerTests.cs
@@ -0,0 +1,169 @@
+using System;
+using System.Windows;
+using Creature.Entities;
+using Creature.Graphics;
+using Creature.Windowing;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Windowing;
+
+public class MonitorManagerTests
+{
+    private class DummyPet : DesktopPet
+    {
+        public bool Hovered { get; private set; }
+
+        public DummyPet(System.Windows.Rect area) : base("dummy", new SpriteManager(), area)
+        {
+        }
+
+        public override void OnCursorHover()
+        {
+            Hovered = true;
+        }
+    }
+
+    [Fact]
+    public void GetMonitors_ShouldReturnAtLeastOneActiveMonitor()
+    {
+        using var manager = new MonitorManager();
+        var monitors = manager.GetMonitors();
+
+        monitors.Should().NotBeEmpty();
+        monitors[0].WorkingArea.Width.Should().BeGreaterThan(0);
+        monitors[0].WorkingArea.Height.Should().BeGreaterThan(0);
+        monitors.Should().Contain(m => m.IsPrimary);
+    }
+
+    [Fact]
+    public void MonitorInfo_Properties_ShouldBeCorrectlyInitialized()
+    {
+        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
+        var workArea = new System.Windows.Rect(0, 0, 1920, 1040);
+        var info = new MonitorInfo(@"\\.\DISPLAY1", bounds, workArea, true);
+
+        info.DeviceName.Should().Be(@"\\.\DISPLAY1");
+        info.Bounds.Should().Be(bounds);
+        info.WorkingArea.Should().Be(workArea);
+        info.IsPrimary.Should().BeTrue();
+    }
+
+    [StaFact]
+    public void OverlayWindow_WndProc_WhenHitPet_ShouldReturnHTCLIENT_AndTriggerHover()
+    {
+        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
+        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
+        var overlay = new OverlayWindow(monitor);
+        try
+        {
+            var pet = new DummyPet(bounds);
+            pet.Transform.Position = new Creature.Core.Physics.Vector2D(100, 100);
+            pet.Transform.Scale = 1.0;
+
+            overlay.PetHitTester = pt => pet.HitTest(pt) ? pet : null;
+
+            bool handled = false;
+            // Screen point (110, 110) hits pet at (100, 100)
+            long lParam = (110L << 16) | (110 & 0xFFFF);
+            var result = overlay.WndProc(IntPtr.Zero, OverlayWindow.WM_NCHITTEST, IntPtr.Zero, (IntPtr)lParam, ref handled);
+
+            handled.Should().BeTrue();
+            result.Should().Be((IntPtr)OverlayWindow.HTCLIENT);
+            pet.Hovered.Should().BeTrue();
+        }
+        finally
+        {
+            overlay.Close();
+        }
+    }
+
+    [StaFact]
+    public void OverlayWindow_WndProc_WhenNoHit_ShouldReturnHTTRANSPARENT()
+    {
+        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
+        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
+        var overlay = new OverlayWindow(monitor);
+        try
+        {
+            var pet = new DummyPet(bounds);
+            pet.Transform.Position = new Creature.Core.Physics.Vector2D(100, 100);
+            overlay.PetHitTester = pt => pet.HitTest(pt) ? pet : null;
+
+            bool handled = false;
+            // Screen point (500, 500) misses pet
+            long lParam = (500L << 16) | (500 & 0xFFFF);
+            var result = overlay.WndProc(IntPtr.Zero, OverlayWindow.WM_NCHITTEST, IntPtr.Zero, (IntPtr)lParam, ref handled);
+
+            handled.Should().BeTrue();
+            result.Should().Be((IntPtr)OverlayWindow.HTTRANSPARENT);
+            pet.Hovered.Should().BeFalse();
+        }
+        finally
+        {
+            overlay.Close();
+        }
+    }
+
+    [StaFact]
+    public void OverlayWindow_WndProc_WhenOtherMessage_ShouldReturnZeroAndUnhandled()
+    {
+        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
+        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
+        var overlay = new OverlayWindow(monitor);
+        try
+        {
+            bool handled = false;
+            var result = overlay.WndProc(IntPtr.Zero, 0x0001 /* WM_CREATE */, IntPtr.Zero, IntPtr.Zero, ref handled);
+
+            handled.Should().BeFalse();
+            result.Should().Be(IntPtr.Zero);
+        }
+        finally
+        {
+            overlay.Close();
+        }
+    }
+
+    [StaFact]
+    public void OverlayManager_Lifecycle_ShouldManageOverlays()
+    {
+        using var monitorManager = new MonitorManager();
+        using var overlayManager = new OverlayManager(monitorManager);
+
+        var monitors = monitorManager.GetMonitors();
+        overlayManager.Overlays.Should().HaveCount(monitors.Count);
+
+        var canvas = overlayManager.GetCanvasForMonitor(monitors[0].DeviceName);
+        canvas.Should().NotBeNull();
+
+        overlayManager.PetHitTester = pt => null;
+        foreach (var overlay in overlayManager.Overlays)
+        {
+            overlay.PetHitTester.Should().NotBeNull();
+        }
+    }
+
+    [StaFact]
+    public void OverlayManager_GetCanvasForMonitor_WhenNotFound_ShouldFallbackToFirst()
+    {
+        using var monitorManager = new MonitorManager();
+        using var overlayManager = new OverlayManager(monitorManager);
+
+        var canvas = overlayManager.GetCanvasForMonitor("NON_EXISTENT_DEVICE");
+        canvas.Should().NotBeNull();
+        canvas.Should().Be(overlayManager.Overlays[0].PetCanvas);
+    }
+
+    [StaFact]
+    public void OverlayManager_RecreateOverlays_ShouldUpdateExistingOverlays()
+    {
+        using var monitorManager = new MonitorManager();
+        using var overlayManager = new OverlayManager(monitorManager);
+
+        var originalCount = overlayManager.Overlays.Count;
+        overlayManager.RecreateOverlays();
+
+        overlayManager.Overlays.Should().HaveCount(originalCount);
+    }
+}
diff --git a/Windowing/MonitorInfo.cs b/Windowing/MonitorInfo.cs
new file mode 100644
index 0000000..a7c2ba8
--- /dev/null
+++ b/Windowing/MonitorInfo.cs
@@ -0,0 +1,3 @@
+namespace Creature.Windowing;
+
+public record MonitorInfo(string DeviceName, System.Windows.Rect Bounds, System.Windows.Rect WorkingArea, bool IsPrimary);
diff --git a/Windowing/MonitorManager.cs b/Windowing/MonitorManager.cs
new file mode 100644
index 0000000..3d5ff95
--- /dev/null
+++ b/Windowing/MonitorManager.cs
@@ -0,0 +1,38 @@
+using System;
+using System.Collections.Generic;
+using System.Windows.Forms;
+using Microsoft.Win32;
+
+namespace Creature.Windowing;
+
+public class MonitorManager : IDisposable
+{
+    public event Action? DisplaysChanged;
+
+    public MonitorManager()
+    {
+        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
+    }
+
+    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
+    {
+        DisplaysChanged?.Invoke();
+    }
+
+    public List<MonitorInfo> GetMonitors()
+    {
+        var list = new List<MonitorInfo>();
+        foreach (var screen in Screen.AllScreens)
+        {
+            var bounds = new System.Windows.Rect(screen.Bounds.X, screen.Bounds.Y, screen.Bounds.Width, screen.Bounds.Height);
+            var workArea = new System.Windows.Rect(screen.WorkingArea.X, screen.WorkingArea.Y, screen.WorkingArea.Width, screen.WorkingArea.Height);
+            list.Add(new MonitorInfo(screen.DeviceName, bounds, workArea, screen.Primary));
+        }
+        return list;
+    }
+
+    public void Dispose()
+    {
+        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
+    }
+}
diff --git a/Windowing/OverlayManager.cs b/Windowing/OverlayManager.cs
new file mode 100644
index 0000000..82424e7
--- /dev/null
+++ b/Windowing/OverlayManager.cs
@@ -0,0 +1,78 @@
+using System;
+using System.Collections.Generic;
+using System.Linq;
+using System.Windows.Controls;
+using Creature.Entities;
+
+namespace Creature.Windowing;
+
+public class OverlayManager : IDisposable
+{
+    private readonly MonitorManager _monitorManager;
+    private readonly List<OverlayWindow> _overlays = new();
+    private Func<System.Windows.Point, DesktopPet?>? _petHitTester;
+
+    public Func<System.Windows.Point, DesktopPet?>? PetHitTester
+    {
+        get => _petHitTester;
+        set
+        {
+            _petHitTester = value;
+            foreach (var overlay in _overlays)
+            {
+                overlay.PetHitTester = value;
+            }
+        }
+    }
+
+    public IReadOnlyList<OverlayWindow> Overlays => _overlays;
+
+    public OverlayManager(MonitorManager monitorManager)
+    {
+        _monitorManager = monitorManager;
+        _monitorManager.DisplaysChanged += RecreateOverlays;
+        RecreateOverlays();
+    }
+
+    public void RecreateOverlays()
+    {
+        if (System.Windows.Application.Current != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
+        {
+            System.Windows.Application.Current.Dispatcher.Invoke(RecreateOverlays);
+            return;
+        }
+
+        foreach (var overlay in _overlays)
+        {
+            overlay.Close();
+        }
+        _overlays.Clear();
+
+        var monitors = _monitorManager.GetMonitors();
+        foreach (var monitor in monitors)
+        {
+            var overlay = new OverlayWindow(monitor)
+            {
+                PetHitTester = _petHitTester
+            };
+            overlay.Show();
+            _overlays.Add(overlay);
+        }
+    }
+
+    public Canvas? GetCanvasForMonitor(string deviceName)
+    {
+        return _overlays.FirstOrDefault(o => o.Monitor.DeviceName == deviceName)?.PetCanvas 
+               ?? _overlays.FirstOrDefault()?.PetCanvas;
+    }
+
+    public void Dispose()
+    {
+        _monitorManager.DisplaysChanged -= RecreateOverlays;
+        foreach (var overlay in _overlays)
+        {
+            overlay.Close();
+        }
+        _overlays.Clear();
+    }
+}
diff --git a/Windowing/OverlayWindow.xaml b/Windowing/OverlayWindow.xaml
new file mode 100644
index 0000000..0d1b421
--- /dev/null
+++ b/Windowing/OverlayWindow.xaml
@@ -0,0 +1,13 @@
+<Window x:Class="Creature.Windowing.OverlayWindow"
+        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
+        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
+        Title="CreatureOverlay"
+        WindowStyle="None"
+        AllowsTransparency="True"
+        Background="Transparent"
+        Topmost="True"
+        ShowInTaskbar="False"
+        ShowActivated="False"
+        ResizeMode="NoResize">
+    <Canvas x:Name="PetCanvas" Background="Transparent" />
+</Window>
diff --git a/Windowing/OverlayWindow.xaml.cs b/Windowing/OverlayWindow.xaml.cs
new file mode 100644
index 0000000..920297c
--- /dev/null
+++ b/Windowing/OverlayWindow.xaml.cs
@@ -0,0 +1,78 @@
+using System;
+using System.Runtime.InteropServices;
+using System.Windows;
+using System.Windows.Interop;
+using Creature.Entities;
+
+namespace Creature.Windowing;
+
+public partial class OverlayWindow : Window
+{
+    public const int WM_NCHITTEST = 0x0084;
+    public const int HTTRANSPARENT = -1;
+    public const int HTCLIENT = 1;
+    private const int WS_EX_TOOLWINDOW = 0x00000080;
+    private const int GWL_EXSTYLE = -20;
+
+    [DllImport("user32.dll")]
+    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
+
+    [DllImport("user32.dll")]
+    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
+
+    public MonitorInfo Monitor { get; }
+    public Func<System.Windows.Point, DesktopPet?>? PetHitTester { get; set; }
+
+    public OverlayWindow(MonitorInfo monitor)
+    {
+        InitializeComponent();
+        Monitor = monitor;
+
+        Left = monitor.Bounds.Left;
+        Top = monitor.Bounds.Top;
+        Width = monitor.Bounds.Width;
+        Height = monitor.Bounds.Height;
+    }
+
+    protected override void OnSourceInitialized(EventArgs e)
+    {
+        base.OnSourceInitialized(e);
+        var hwnd = new WindowInteropHelper(this).Handle;
+        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
+        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
+
+        var source = HwndSource.FromHwnd(hwnd);
+        source?.AddHook(WndProc);
+    }
+
+    internal IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
+    {
+        if (msg == WM_NCHITTEST)
+        {
+            var screenX = unchecked((short)(long)lParam);
+            var screenY = unchecked((short)((long)lParam >> 16));
+            System.Windows.Point localPoint;
+            if (PresentationSource.FromVisual(this) != null)
+            {
+                localPoint = PointFromScreen(new System.Windows.Point(screenX, screenY));
+            }
+            else
+            {
+                localPoint = new System.Windows.Point(screenX - Left, screenY - Top);
+            }
+
+            var hitPet = PetHitTester?.Invoke(localPoint);
+            if (hitPet != null)
+            {
+                hitPet.OnCursorHover();
+                handled = true;
+                return (IntPtr)HTCLIENT;
+            }
+
+            handled = true;
+            return (IntPtr)HTTRANSPARENT;
+        }
+
+        return IntPtr.Zero;
+    }
+}

`
