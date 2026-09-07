# Architectural Design Specification: Desktop Pets (.NET 10 WPF)

## 1. Overview & Vision
Desktop Pets is a cute, lightweight, zero-friction desktop companion application built in C# with WPF on .NET 10. The application runs smoothly across multiple monitors with minimal CPU and memory usage, rendering animated pixel-art pets (starting with the Bunny MVP) that walk along the taskbar/work area, jump when hovered over, and pass through all background mouse interactions to underlying windows.

---

## 2. Technology Stack & Key Libraries
* **Framework:** C# / WPF on .NET 10 (`net10.0-windows`)
* **OS Platform:** Windows 10/11 (x64 / ARM64)
* **Window Styling:** `WS_EX_TOPMOST` and `WS_EX_LAYERED` borderless transparent overlay
* **Interactivity:** Win32 `WM_NCHITTEST` message hook via `HwndSource`
* **Asset Format:** Aseprite horizontal PNG sprite sheets + JSON metadata
* **Rendering & Scaling:** WPF `Image` controls inside per-monitor `Canvas`, with `BitmapScalingMode.NearestNeighbor` for crisp pixel-art scaling
* **Testing:** xUnit + FluentAssertions for unit and simulation math testing

---

## 3. System Architecture & Components

```
Creature
│
├── Assets/                        # Embedded / content sprite sheets and JSON
│   ├── BunnyJump.png / .json
│   ├── BunnyLieDown.png / .json
│   └── BunnyRun.png / .json
│
├── Core/
│   ├── Models/                    # Data models, Rects, Config records
│   ├── Physics/                   # PhysicsBody, Vector2D, Transform2D
│   └── StateMachine/              # Generic FSM state interfaces & StateMachine<T>
│
├── Entities/
│   ├── DesktopPet.cs              # Base pet entity (Transform, Physics, Animator, Collider)
│   └── BunnyEntity.cs             # Bunny implementation & specific states
│       ├── BunnyIdleState.cs      # LieDown animation loop
│       ├── BunnyRoamState.cs      # Run animation & horizontal traversal
│       └── BunnyJumpState.cs      # Hover flinch arc & jump animation
│
├── Graphics/
│   ├── AsepriteMetadata.cs        # JSON records for System.Text.Json
│   ├── AnimationClip.cs           # Sliced frame sequence & duration
│   └── SpriteManager.cs           # Slices PNGs via CroppedBitmap & caches BitmapSource[]
│
├── Windowing/
│   ├── MonitorInfo.cs             # Monitor bounds & working area tracker
│   ├── MonitorManager.cs          # Multi-monitor enumeration & DisplaySettingsChanged listener
│   ├── OverlayWindow.xaml         # Transparent topmost overlay window with Canvas
│   ├── OverlayWindow.xaml.cs      # WM_NCHITTEST WndProc hook returning HTTRANSPARENT/HTCLIENT
│   └── OverlayManager.cs          # Manages one OverlayWindow per monitor
│
├── Audio/
│   └── AudioManager.cs            # Sound pool with 2-voice concurrent limiter
│
├── Settings/
│   ├── UserSettings.cs            # Schema for scale, sound, pet configurations
│   └── SettingsManager.cs         # JSON persistence to %APPDATA%\Creature\settings.json
│
├── Tray/
│   └── TrayManager.cs             # NotifyIcon tray menu & pet right-click context menu
│
├── Simulation/
│   └── SimulationEngine.cs        # 60Hz tick loop, physics integration & entity coordination
│
└── App.xaml / App.xaml.cs         # Application bootstrap & lifecycle
```

---

## 4. Subsystem Details

### 4.1 Overlay & Win32 Interop (`OverlayManager`, `OverlayWindow`)
* One `OverlayWindow` per connected display screen.
* Attributes: `WindowStyle="None"`, `AllowsTransparency="True"`, `Background="Transparent"`, `Topmost="True"`, `ShowInTaskbar="False"`.
* Windows API `WM_NCHITTEST` Hook:
  * Intercepts `0x0084` (`WM_NCHITTEST`).
  * Converts cursor screen position to local overlay coordinates.
  * Queries `SimulationEngine.HitTest(localPoint)`:
    * If inside any active pet's bounding box: returns `HTCLIENT` (`1`), allowing clicks and `MouseEnter` events to reach the pet visual.
    * Otherwise: returns `HTTRANSPARENT` (`-1`), causing Windows to route all mouse events straight through to the underlying desktop/apps.
* `MonitorManager` listens to `SystemEvents.DisplaySettingsChanged` to adjust or recreate overlays when monitor configurations change.

### 4.2 Asset Pipeline (`SpriteManager`)
* Deserializes Aseprite JSON metadata using `System.Text.Json`.
* Loads PNG strips into a master `BitmapImage` with `BitmapCacheOption.OnLoad`.
* Slices 32x32 frames using `CroppedBitmap` into cached `BitmapSource[]`.
* Applies `RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor)` to prevent blurring when scaling up.
* Exposes `AnimationClip` objects with frame durations parsed from the JSON metadata (`duration: 100` ms per frame).

### 4.3 Simulation & Entity FSM (`SimulationEngine`, `DesktopPet`, `BunnyEntity`)
* **Tick Loop:** 60 Hz simulation update decoupled from animation duration.
* **Physics & Transform:**
  * Logical coordinate system `(X, Y)`.
  * `FloorY = WorkingArea.Bottom - (BaseHeight * GlobalScale)`.
  * Gravity `g` and jump impulse `Vy` integrated smoothly during jumps.
* **Bunny FSM States:**
  * `BunnyIdleState`: Plays `BunnyLieDown` (2 frames, 100ms/frame looping). Stationary for a randomized interval (3–7s).
  * `BunnyRoamState`: Plays `BunnyRun` (5 frames, 100ms/frame looping). Moves left or right at 50 units/sec. Flips `ScaleTransform.ScaleX` (-1 for left, +1 for right). Turns around when reaching `WorkingArea.Left` or `WorkingArea.Right`.
  * `BunnyJumpState`: Triggered immediately on cursor hover. Plays `BunnyJump` (18 frames, 100ms/frame non-looping) with a parabolic vertical jump arc before landing back on the floor and transitioning to Idle.

### 4.4 Audio, Settings & Tray UX
* **Settings:** Persisted to `%APPDATA%\Creature\settings.json`. Controls `GlobalScale` (0.5x to 2.0x), `IsSoundEnabled`, and `IsPaused`.
* **Audio:** `AudioManager` enforces a 2-voice maximum simultaneous playback pool to prevent audio clutter.
* **Tray Icon:** Provides options to Show/Hide Pets, Change Scale, Toggle Sound, Pause/Resume, and Exit.
* **Pet Context Menu:** Right-clicking directly on a pet provides pet-specific controls (Pause, Hide, Scale, Remove).

---

## 5. Testing & Verification
* **Unit Tests (`Creature.Tests`):**
  * `AsepriteParserTests`: Validates frame slicing coordinates, frame count, and durations.
  * `PhysicsTests`: Validates gravity integration, floor clamping, and parabolic jump trajectories.
  * `FSMTests`: Validates state timeouts, hover interruptions, and edge turnarounds.
  * `SettingsTests`: Validates default fallback and serialization round-trips.
* **Manual Verification:**
  * Verify full desktop click-through passes to desktop icons and browser windows underneath.
  * Verify hover triggers jump and right-click opens context menu.
  * Verify nearest-neighbor pixel art stays crisp at 0.5x, 1.0x, 1.5x, and 2.0x scales.
