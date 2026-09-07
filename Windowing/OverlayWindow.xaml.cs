using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Creature.Entities;

namespace Creature.Windowing;

public partial class OverlayWindow : Window
{
    public const int WM_NCHITTEST = 0x0084;
    public const int HTTRANSPARENT = -1;
    public const int HTCLIENT = 1;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int GWL_EXSTYLE = -20;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    public MonitorInfo Monitor { get; }
    public Func<System.Windows.Point, DesktopPet?>? PetHitTester { get; set; }

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

    internal IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_NCHITTEST)
        {
            var screenX = unchecked((short)(long)lParam);
            var screenY = unchecked((short)((long)lParam >> 16));
            System.Windows.Point localPoint;
            if (PresentationSource.FromVisual(this) != null)
            {
                localPoint = PointFromScreen(new System.Windows.Point(screenX, screenY));
            }
            else
            {
                localPoint = new System.Windows.Point(screenX - Left, screenY - Top);
            }

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
