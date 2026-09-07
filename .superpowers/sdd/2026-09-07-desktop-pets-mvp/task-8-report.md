# Task 8 Report: Simulation Engine, Tray Management & App Bootstrap

**Date:** 2026-09-08  
**Status:** Completed  
**Commit:** `4079b72c297e5dae597583d0ae7f9fe89d175152` (`feat: implement SimulationEngine, TrayManager, and zero-window bootstrap`)

---

## 1. Summary of Work

Implemented the final MVP orchestration layer connecting the simulation loop, system tray controls, desktop overlay rendering, and zero-window application lifecycle:
- **`Simulation/SimulationEngine.cs`**:
  - Encapsulates ~60Hz simulation loop driven by `CompositionTarget.Rendering` with elapsed delta-time calculation and lag spike clamping (`dt <= 0.05s`).
  - Supports `Start()`, `Stop()`, and `IsPaused` toggling.
  - Implements `AddPet()`, `RemovePet()`, and collection exposure via `Pets`.
  - Implements `HitTest(Point localPoint)` mapping to pets' bounding boxes.
  - Implements `ApplyGlobalScale(double scale)` updating pet scale, recalculating monitor floor bounds, and resynchronizing visual transforms.
- **`Tray/TrayManager.cs`**:
  - Implements `IDisposable` wrapper around WinForms `NotifyIcon`.
  - Context menu items for:
    - **Pause Simulation** (toggles `SimulationEngine.IsPaused` and persists to `UserSettings`).
    - **Sound Enabled** (toggles sound and persists to `UserSettings`).
    - **Global Pet Size** (sub-menu with preset scale factors: 0.5x, 0.75x, 1.0x, 1.25x, 1.5x, 2.0x, updating engine scale and settings).
    - **Exit** (invokes graceful application shutdown).
- **`Entities/DesktopPet.cs` & `BunnyEntityTests.cs`**:
  - Fixed visual scale compounding in `SyncVisualTransform()`: sets `VisualElement.Width` & `VisualElement.Height` to `Transform.ScaledWidth` & `Transform.ScaledHeight`, while restricting `RenderTransform` strictly to `new ScaleTransform(Transform.IsFacingLeft ? -1.0 : 1.0, 1.0)` for horizontal flipping without re-multiplying scale.
  - Aligned unit tests in `BunnyEntityTests.cs` to assert `ScaleX == -1.0` and `ScaleY == 1.0`.
- **`App.xaml` & `App.xaml.cs`**:
  - Converted `App.xaml` to zero-window startup with `ShutdownMode="OnExplicitShutdown"` and removed `StartupUri`.
  - Implemented `App.xaml.cs` lifecycle orchestration: initializes `SettingsManager`, `AudioManager`, `SpriteManager` (loading animations with fallback paths), `MonitorManager`, `SimulationEngine`, and `OverlayManager`.
  - Hooks `PetHitTester` delegate into `OverlayManager`.
  - Spawns the primary Bunny MVP entity on the primary monitor canvas.
  - Initializes `TrayManager` and starts the simulation engine.
  - Implements `OnExit` teardown, disposing engine, tray icon, overlay manager, and monitor manager.
- **Removal of Boilerplate**:
  - Deleted `MainWindow.xaml` and `MainWindow.xaml.cs`.
- **Unit Tests**:
  - Authored `Creature.Tests/Simulation/SimulationEngineTests.cs` covering hit testing, global scaling, pet collection management, pause state, and start/stop idempotency.

---

## 2. TDD Workflow Verification

1. **RED Phase**:
   - Authored `Creature.Tests/Simulation/SimulationEngineTests.cs`.
   - Executed `dotnet test --filter "FullyQualifiedName~SimulationEngineTests"`.
   - Verified compilation failure (`error CS0234: The type or namespace name 'Simulation' does not exist in the namespace 'Creature'`).
2. **GREEN Phase**:
   - Created `Simulation/SimulationEngine.cs` and `Tray/TrayManager.cs`.
   - Disambiguated `Point` between `System.Windows.Point` and `System.Drawing.Point`.
   - Updated `DesktopPet.cs` scale transform and updated `BunnyEntityTests.cs`.
   - Updated `App.xaml` and `App.xaml.cs`, deleted `MainWindow.xaml` and `MainWindow.xaml.cs`.
   - Executed `dotnet test` and `dotnet build`.
   - All 41 tests across the entire test suite passed with zero errors or warnings.
3. **COMMIT Phase**:
   - Committed changes via `git commit -m "feat: implement SimulationEngine, TrayManager, and zero-window bootstrap"` (`4079b72`).

---

## 3. Test Results

```
Passed!  - Failed:     0, Passed:    41, Skipped:     0, Total:    41, Duration: 599 ms - Creature.Tests.dll (net10.0)
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## 4. Observations & Status

- **Compounding Scale Fix**: The previous `SyncVisualTransform()` multiplied `Width`/`Height` by scale and simultaneously applied `ScaleTransform(scaleX * Scale, Scale)`, effectively squaring the scale. The fix cleanly separates visual geometry sizing from flip transforms.
- **Ambiguity Resolution**: With `<UseWindowsForms>true</UseWindowsForms>` and `<UseWPF>true</UseWPF>`, `Point` and `Application` require explicit aliasing (`using Point = System.Windows.Point;` and `System.Windows.Application`).
- Zero known regressions; all 41 test cases pass.
