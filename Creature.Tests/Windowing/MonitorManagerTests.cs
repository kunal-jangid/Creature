using System;
using System.Windows;
using Creature.Entities;
using Creature.Graphics;
using Creature.Windowing;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Windowing;

public class MonitorManagerTests
{
    private class DummyPet : DesktopPet
    {
        public bool Hovered { get; private set; }

        public DummyPet(System.Windows.Rect area) : base("dummy", new SpriteManager(), area)
        {
        }

        public override void OnCursorHover()
        {
            Hovered = true;
        }
    }

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

    [Fact]
    public void MonitorInfo_Properties_ShouldBeCorrectlyInitialized()
    {
        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
        var workArea = new System.Windows.Rect(0, 0, 1920, 1040);
        var info = new MonitorInfo(@"\\.\DISPLAY1", bounds, workArea, true);

        info.DeviceName.Should().Be(@"\\.\DISPLAY1");
        info.Bounds.Should().Be(bounds);
        info.WorkingArea.Should().Be(workArea);
        info.IsPrimary.Should().BeTrue();
    }

    [StaFact]
    public void OverlayWindow_WndProc_WhenHitPet_ShouldReturnHTCLIENT_AndTriggerHover()
    {
        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
        var overlay = new OverlayWindow(monitor);
        try
        {
            var pet = new DummyPet(bounds);
            pet.Transform.Position = new Creature.Core.Physics.Vector2D(100, 100);
            pet.Transform.Scale = 1.0;

            overlay.PetHitTester = pt => pet.HitTest(pt) ? pet : null;

            bool handled = false;
            // Screen point (110, 110) hits pet at (100, 100)
            long lParam = (110L << 16) | (110 & 0xFFFF);
            var result = overlay.WndProc(IntPtr.Zero, OverlayWindow.WM_NCHITTEST, IntPtr.Zero, (IntPtr)lParam, ref handled);

            handled.Should().BeTrue();
            result.Should().Be((IntPtr)OverlayWindow.HTCLIENT);
            pet.Hovered.Should().BeTrue();
        }
        finally
        {
            overlay.Close();
        }
    }

    [StaFact]
    public void OverlayWindow_WndProc_WhenNoHit_ShouldReturnHTTRANSPARENT()
    {
        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
        var overlay = new OverlayWindow(monitor);
        try
        {
            var pet = new DummyPet(bounds);
            pet.Transform.Position = new Creature.Core.Physics.Vector2D(100, 100);
            overlay.PetHitTester = pt => pet.HitTest(pt) ? pet : null;

            bool handled = false;
            // Screen point (500, 500) misses pet
            long lParam = (500L << 16) | (500 & 0xFFFF);
            var result = overlay.WndProc(IntPtr.Zero, OverlayWindow.WM_NCHITTEST, IntPtr.Zero, (IntPtr)lParam, ref handled);

            handled.Should().BeTrue();
            result.Should().Be((IntPtr)OverlayWindow.HTTRANSPARENT);
            pet.Hovered.Should().BeFalse();
        }
        finally
        {
            overlay.Close();
        }
    }

    [StaFact]
    public void OverlayWindow_WndProc_WhenOtherMessage_ShouldReturnZeroAndUnhandled()
    {
        var bounds = new System.Windows.Rect(0, 0, 1920, 1080);
        var monitor = new MonitorInfo(@"\\.\DISPLAY1", bounds, bounds, true);
        var overlay = new OverlayWindow(monitor);
        try
        {
            bool handled = false;
            var result = overlay.WndProc(IntPtr.Zero, 0x0001 /* WM_CREATE */, IntPtr.Zero, IntPtr.Zero, ref handled);

            handled.Should().BeFalse();
            result.Should().Be(IntPtr.Zero);
        }
        finally
        {
            overlay.Close();
        }
    }

    [StaFact]
    public void OverlayManager_Lifecycle_ShouldManageOverlays()
    {
        using var monitorManager = new MonitorManager();
        using var overlayManager = new OverlayManager(monitorManager);

        var monitors = monitorManager.GetMonitors();
        overlayManager.Overlays.Should().HaveCount(monitors.Count);

        var canvas = overlayManager.GetCanvasForMonitor(monitors[0].DeviceName);
        canvas.Should().NotBeNull();

        overlayManager.PetHitTester = pt => null;
        foreach (var overlay in overlayManager.Overlays)
        {
            overlay.PetHitTester.Should().NotBeNull();
        }
    }

    [StaFact]
    public void OverlayManager_GetCanvasForMonitor_WhenNotFound_ShouldFallbackToFirst()
    {
        using var monitorManager = new MonitorManager();
        using var overlayManager = new OverlayManager(monitorManager);

        var canvas = overlayManager.GetCanvasForMonitor("NON_EXISTENT_DEVICE");
        canvas.Should().NotBeNull();
        canvas.Should().Be(overlayManager.Overlays[0].PetCanvas);
    }

    [StaFact]
    public void OverlayManager_RecreateOverlays_ShouldUpdateExistingOverlays()
    {
        using var monitorManager = new MonitorManager();
        using var overlayManager = new OverlayManager(monitorManager);

        var originalCount = overlayManager.Overlays.Count;
        overlayManager.RecreateOverlays();

        overlayManager.Overlays.Should().HaveCount(originalCount);
    }
}
