# 🛠️ Desktop Pets — Technical Architecture & Developer Guide

**Author**: Kunal Jangid  
**Publication**: Private Publication Application  
**Copyright**: Copyright © 2026 Kunal Jangid. All rights reserved.

This document contains deep technical specifications, design patterns, physics simulation models, Win32 interop details, and developer workflows for the **Desktop Pets** application ecosystem.

---

## 🏛️ System Architecture

Desktop Pets is structured into decoupled subsystems targeting high framerates (~60Hz), sub-1% CPU usage, and zero memory allocation churn during the active rendering loop.

```
┌─────────────────────────────────────────────────────────────┐
│                      App Bootstrap                          │
│               (App.xaml / App.xaml.cs)                      │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
       ┌───────▼────────┐              ┌───────▼────────┐
       │ SettingsManager│              │ MonitorManager │
       └───────┬────────┘              └───────┬────────┘
               │                               │
       ┌───────▼────────┐              ┌───────▼────────┐
       │ TrayManager    │              │ OverlayManager │
       └────────────────┘              │ (Transparent)  │
                                       └───────┬────────┘
                                               │
                                       ┌───────▼────────┐
                                       │SimulationEngine│
                                       └───────┬────────┘
                                               │
                         ┌─────────────────────┴─────────────────────┐
                         │                                           │
                 ┌───────▼────────┐                         ┌────────▼────────┐
                 │ DesktopPet     │                         │ SpriteManager   │
                 │ (FSM & Physics)│                         │ (Aseprite JSON) │
                 └────────────────┘                         └─────────────────┘
```

---

## 🪟 1. Zero-Friction Click-Through Windowing

### Win32 Interop & Extended Styles
Full-screen borderless windows are spawned across all attached monitors via `MonitorManager`.
To ensure pets can be hovered over and dragged while empty screen space remains 100% click-through:
- Extended styles applied: `WS_EX_TOOLWINDOW` (hides from Alt+Tab) | `WS_EX_NOACTIVATE` (prevents stealing window focus) | `WS_EX_TRANSPARENT`.
- `HwndSource` hooks native Win32 messages via `WndProc`.
- On `WM_NCHITTEST` (0x0084), screen coordinates `(lParam)` are mapped to WPF canvas coordinates.
- If the point hits an active pet bounding box, it returns `HTCLIENT` (`0x0001`), capturing mouse interactions.
- If the point misses all pets, it returns `HTTRANSPARENT` (`-0x0001`), passing all clicks directly to underlying applications (browsers, IDEs, desktop).

```csharp
private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
{
    if (msg == WM_NCHITTEST)
    {
        var screenX = (short)(lParam.ToInt64() & 0xFFFF);
        var screenY = (short)((lParam.ToInt64() >> 16) & 0xFFFF);
        var clientPoint = PointFromScreen(new Point(screenX, screenY));

        if (PetHitTester != null && PetHitTester(clientPoint))
        {
            handled = true;
            return new IntPtr(HTCLIENT);
        }

        handled = true;
        return new IntPtr(HTTRANSPARENT);
    }
    return IntPtr.Zero;
}
```

---

## ⚡ 2. Game Simulation Loop & Physics

### CompositionTarget.Rendering
Instead of high-overhead timers (`DispatcherTimer`), updates are tied to `CompositionTarget.Rendering` (~60 FPS refresh rate):
- **Delta-Time Clamping**: Elapsed time `dt` is clamped to `[0.001s, 0.1s]` to prevent physics blowup during system sleep or stalls.
- **Zero-Allocation Hot Path**: Update calculations use mutating `Vector2D` structs and pre-allocated arrays.
- **PhysicsBody Integration**:
  - `Position += Velocity * dt`
  - `Velocity.Y += Gravity * dt`
  - Floor clamping at `FloorY` with landing callbacks.
  - Horizontal screen boundary reflection with directional sprite flips (`ScaleTransform(IsFacingLeft ? -1 : 1, 1)`).

---

## 🤖 3. Finite State Machines & Pet Species

Each pet extends `DesktopPet` and is governed by a typed `StateMachine<T>`.

### Species Specifications & Simpness Factor

| Species | Base Size | Simpness Factor | Unique Behaviors |
| :--- | :--- | :--- | :--- |
| **Bunny** | 32×32 | 0.85 | Hopping, floor roam, cursor jump reaction |
| **Gorgon** | 128×128 | 0.30 | Stone stare special, slow glide, aloof wandering |
| **Werewolf** | 128×128 | 0.90 | Aggressive sprint, jump attack, flinch recovery |

### Simpness Factor Dynamics
- If `SimpnessFactor > 0.7`: When the cursor is within range (350px), the pet frequently moves toward the cursor (`RoamState` targets cursor X coordinate).
- If `SimpnessFactor <= 0.7`: The pet ignores cursor movement but will still face the cursor when nearby.

---

## 🎨 4. Sprite Slicing & Nearest-Neighbor Rendering

- **Aseprite Integration**: JSON metadata (`FrameTag`, `Duration`, `Dimensions`) is parsed via `System.Text.Json`.
- **CroppedBitmap**: Slices sprite sheets into individual frame buffers cached as frozen `BitmapSource[]`.
- **NearestNeighbor Scaling**: Visual elements apply `RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor)` to prevent blurriness when rendered at 2.0x, 3.0x, or 4.0x.

---

## 💾 5. Settings Persistence & Startup Registry

- **Settings Storage**: JSON serialized to `%APPDATA%\DesktopPets\settings.json` (with automatic fallback migration from legacy `%APPDATA%\Creature\settings.json`).
  - Remembers: `GlobalScale`, `ShowPetNames`, `StartWithWindows`, `IsPaused`, and pet lists (`Id`, `Name`, `Species`).
- **Start with Windows**: Modifies `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` with `"Desktop Pets" = "\"[ProcessPath]\""`.

---

## 🧪 6. Testing & Quality Assurance

- **Framework**: xUnit 2.9 + FluentAssertions 8.1 + `Xunit.StaFact` (for STA WPF thread execution).
- **Run Tests**:
  ```bash
  dotnet test
  ```
- **Test Coverage**:
  - Physics vector math & gravity integration.
  - State machine transitions & elapsed time timers.
  - Aseprite frame slicing and dimension validation.
  - Settings loading, fallback resilience, and persistence.
  - Multi-monitor overlay initialization and hit-testing.
  - Windows registry startup toggle operations.

---

## 🔨 7. Build & Deployment Pipelines

### Build Configurations
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Self-contained single-file publish
dotnet publish Creature.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish

# Build Windows Setup Installer locally (requires Inno Setup)
powershell -ExecutionPolicy Bypass -File installer/build_installer.ps1 -Version 1.0.0
```

### GitHub Actions CI/CD
Defined in [`.github/workflows/build-release.yml`](.github/workflows/build-release.yml):
- Manually triggerable via `workflow_dispatch`.
- Automatically computes semantic version tags (`v1.0.0` -> `v1.1.0` -> `v2.0.0`) or accepts custom version inputs.
- Compiles, runs full test suite, generates the Inno Setup Windows Installer (`DesktopPets-Setup-vX.X.X.exe`), packages the portable zip (`DesktopPets-win-x64-vX.X.X.zip`), and Authenticode-signs the binaries with Kunal Jangid publisher credentials.
- Publishes all assets to GitHub Releases and automatically submits the package update to the official Microsoft `winget-pkgs` repository (when `WINGET_TOKEN` secret is configured).