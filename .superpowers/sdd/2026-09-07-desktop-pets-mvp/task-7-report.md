# Task 7 Report: Multi-Monitor & Overlay Window Management

**Date:** 2026-09-07  
**Status:** Completed  
**Commit:** `f9038bc63884893755277b88d0a1078900688f07` (`feat: implement MonitorManager and OverlayWindow with WM_NCHITTEST pass-through`)

---

## 1. Summary of Work

Implemented multi-monitor detection, fullscreen transparent overlay windows, and Win32 message routing for mouse pass-through hit testing:
- **`Windowing/MonitorInfo.cs`**: Immutable record storing monitor metadata (`DeviceName`, `System.Windows.Rect Bounds`, `System.Windows.Rect WorkingArea`, `bool IsPrimary`).
- **`Windowing/MonitorManager.cs`**: Discovers active screens using `System.Windows.Forms.Screen.AllScreens`, translates display bounds/working areas to WPF DIP rects (`System.Windows.Rect`), and hooks `Microsoft.Win32.SystemEvents.DisplaySettingsChanged` to notify consumers when displays are added, removed, or resized.
- **`Windowing/OverlayWindow.xaml` & `.xaml.cs`**: Transparent, borderless, topmost, un-activatable window (`AllowsTransparency="True"`, `WindowStyle="None"`, `ShowActivated="False"`, `ShowInTaskbar="False"`, `ResizeMode="NoResize"`) containing a transparent `Canvas` (`PetCanvas`). In `OnSourceInitialized`, applies `WS_EX_TOOLWINDOW` to hide from Alt+Tab and registers a Win32 `HwndSource` hook to intercept `WM_NCHITTEST` (0x0084). Returns `HTCLIENT` (1) when the cursor intersects a pet's bounding box and triggers `hitPet.OnCursorHover()`; returns `HTTRANSPARENT` (-1) otherwise to pass clicks through to underlying desktop windows.
- **`Windowing/OverlayManager.cs`**: Coordinates overlay window instances for each active display. Subscribes to `MonitorManager.DisplaysChanged` to automatically recreate overlays when display configurations shift. Dispatches UI updates through the WPF Dispatcher when required, provides `GetCanvasForMonitor(string deviceName)` with fallback to the primary/first monitor, and synchronizes `PetHitTester` across all active overlay instances.
- **`Creature.Tests/Windowing/MonitorManagerTests.cs`**: Unit tests verifying monitor enumeration, primary monitor detection, `MonitorInfo` initialization, `OverlayWindow` `WM_NCHITTEST` routing (verifying `HTCLIENT` return + hover trigger on pet hit, `HTTRANSPARENT` return on background miss, unhandled return on non-hit-test messages), and `OverlayManager` lifecycle / recreation / canvas resolution fallback.

---

## 2. TDD Workflow Verification

1. **RED Phase**:
   - Authored test cases in `Creature.Tests/Windowing/MonitorManagerTests.cs`.
   - Executed `dotnet test --filter "FullyQualifiedName~MonitorManagerTests"`.
   - Verified compilation failure (`error CS0234: The type or namespace name 'Windowing' does not exist in the namespace 'Creature'`).
2. **GREEN Phase**:
   - Implemented `MonitorInfo.cs`, `MonitorManager.cs`, `OverlayWindow.xaml`, `OverlayWindow.xaml.cs`, and `OverlayManager.cs`.
   - Used explicit namespaces (`System.Windows.Rect`, `System.Windows.Point`, `System.Windows.Application`) to prevent namespace collisions between WPF and Windows Forms.
   - Executed `dotnet test`.
   - All 36 tests across the entire solution passed with zero errors or warnings.
3. **COMMIT**:
   - Committed changes via `git commit -m "feat: implement MonitorManager and OverlayWindow with WM_NCHITTEST pass-through"` (`f9038bc`).

---

## 3. Key Technical Decisions & Observations

- **Win32 `WM_NCHITTEST` & Transparency**: By returning `HTTRANSPARENT` (-1) from `WndProc` when the cursor is over empty canvas space, Windows natively routes mouse events (clicks, drags, scrolls) directly to whatever window or desktop is beneath the overlay. When hovering/clicking over a pet bounding box, returning `HTCLIENT` (1) and invoking `OnCursorHover()` provides instant responsiveness without blocking the user's OS interactions elsewhere.
- **DPI and PresentationSource Resilience**: In `OverlayWindow.WndProc`, if `PresentationSource.FromVisual(this)` is available, `PointFromScreen` translates screen coordinates to device-independent window coordinates. If invoked in headless test contexts before presentation source attachment, it falls back to `(screenX - Left, screenY - Top)`.
- **Property Forwarding on `OverlayManager.PetHitTester`**: Setting `overlayManager.PetHitTester` forwards the delegate to all currently active `OverlayWindow` instances and stores it for future windows created during `RecreateOverlays()`.
- **Thread-Affinity & Multi-Test STA Safety**: `Xunit.StaFact` runs tests on distinct STA threads; avoiding persistent global `Application.Current` creation across isolated test methods prevents thread affinity conflicts during rapid parallel test runs.

---

## 4. Test Results

```
Test run for D:\projects\Creature\Creature.Tests\bin\Debug\net10.0-windows\Creature.Tests.dll (.NETCoreApp,Version=v10.0)
A total of 1 test files matched the specified pattern.

Passed!  - Failed:     0, Passed:    36, Skipped:     0, Total:    36, Duration: 620 ms - Creature.Tests.dll (net10.0)
```
