# Architecture and Implementation Specification: Desktop Pets

## 1. Product Vision & Execution Hierarchy

The core product principle is **“cute, lightweight, zero-friction.”** The application must remain almost invisible from a resource and UX perspective, acting as a native part of the desktop environment.

When facing implementation trade-offs, strictly adhere to this priority hierarchy:

1. Low CPU usage
2. Low memory usage
3. Smooth animation
4. Simple architecture
5. Cute interactions
6. Visual fidelity (choose the cheaper technical implementation if high fidelity spikes CPU)

## 2. Technology Stack & Rendering Strategy

* **Framework:** C# with WPF (.NET 10).
* **Window Style:** `WS_EX_TOPMOST` and `WS_EX_LAYERED` for a click-through, transparent overlay.
* **Rendering:** Use standard WPF `Image` controls coupled with cached bitmap sources. **Do not** prematurely optimize with `WriteableBitmap`. Keep the rendering path simple; only drop down closer to the metal if performance profiling proves it necessary.
* **Coordinate System:** Strictly use Windows' DPI-aware APIs and device-independent coordinates. The renderer must handle the conversion between logical WPF coordinates and physical pixels transparently.

## 3. Application Lifecycle & UX

The primary control surface is a **System Tray (NotifyIcon)** menu. The application launches minimized to the tray.

**System Tray Menu Requirements:**

* Show/Hide Pets
* Enable/Disable individual pets
* Global Pet Size (Slider/Options)
* Sound On/Off
* Pause/Resume
* Settings
* Exit

**Context Menu (Right-Click on Pet):**

* Pause This Pet
* Hide This Pet
* Change Size
* Remove / Disable

**Configuration & Scale:**

* **Scale:** User-configurable global multiplier (Default: `1.0x`, Range: `0.5x` to `2.0x`).
* The scale multiplier must universally apply to sprite dimensions, hitboxes, movement speeds, and collision boundaries.
* Avoid requiring the user to edit `appsettings.json` manually for standard UX.

## 4. Multi-Monitor & Environmental Physics

Treat every connected monitor as an independent playable surface.

* **Monitor Boundaries:** Pets are constrained to their current monitor. When a pet reaches the physical edge of a screen, it must trigger a turnaround state. Do not attempt cross-monitor gap traversal for V1.
* **The Floor (Work Area):** The default floor is the usable monitor work area (e.g., `SystemParameters.WorkArea`), not the absolute physical bottom pixel.
* **Taskbar Handling:** Pets must never disappear behind or get trapped under the Windows Taskbar. If the taskbar is at the bottom, the floor rests above it; if at the top, the ceiling rests below it.

## 5. Software Architecture

Do not force traditional WPF MVVM onto the simulation engine. Use MVVM solely for the Settings UI. The simulation requires a lightweight Object-Oriented Entity and Finite State Machine (FSM) architecture. Avoid heavy ECS frameworks.

**Core Hierarchy:**

```text
Application
│
├── OverlayManager
├── MonitorManager
├── SimulationEngine
│   ├── PetEntity
│   ├── Physics
│   ├── Collision
│   └── FSM
│
├── SpriteManager
├── AudioManager
├── SettingsManager
└── TrayManager

```

**Entity Model:**
Each pet owns its behavioral state.

```text
DesktopPet
 ├── Transform
 ├── PhysicsBody
 ├── Animator
 ├── Collider
 └── StateMachine

```

**Update Loop:**
Keep the simulation deterministic and decoupled from the WPF rendering cycle:

* **Simulation Tick:** ~60 Hz (Updates state, physics, transforms)
* **Animation Tick:** 10 FPS (Hardcoded based on the 100ms duration per frame in the JSON assets).


* **Rendering:** Handled natively by WPF based on transform updates.

## 6. Pet Entities & Behaviors (Bunny MVP)

Implement specific behaviors using an inheritance model (`DesktopPet` base class -> `BunnyEntity`). The base hit-test boundaries should match the sprite dimensions (32x32 base logical units).

**Target Behaviors (FSM States):**

* **Idle State (`BunnyLieDown`):** Play the 2-frame lie-down animation on a continuous loop while stationary.


* **Roam State (`BunnyRun`):** Periodically pick a random horizontal direction and play the 5-frame run animation. If moving left, flip the sprite rendering (`ScaleTransform.ScaleX = -1`).


* **Hover / Flinch State (`BunnyJump`):** Attach a `MouseEnter` hit-test. When the cursor hovers over the bunny, interrupt the current state, transition immediately to the 18-frame jump animation, and apply a small vertical arc offset to the physics position before landing back down into the Idle state.



## 7. Asset Pipeline

* **Atlas Format:** Aseprite JSON Array/Hash format. The assets are provided as individual horizontal sprite strips (e.g., `BunnyRun-Sheet.png`) with a corresponding JSON metadata file.


* **Parsing Logic:** Use `System.Text.Json` to deserialize lightweight C# records mapping the JSON schema.


* **Frame Slicing:** The `SpriteManager` must load each PNG as a `BitmapImage`, slice the frames using `CroppedBitmap` based on the JSON `x, y, w, h` frame coordinates (all uniformly 32x32), and cache them as arrays of `BitmapSource`.


* **Timing:** The `SpriteManager` should read the `duration: 100` parameter from the JSON to drive the timing of the animation loop.



## 8. Audio System

Audio is optional flavor, not a core dependency.

* **Lazy Loading:** Audio assets must be loaded lazily and cached after the first use.
* **Profile:** Sounds should be short, low-volume, and trigger with appropriate cooldowns (e.g., soft bunny hops, chewing/nibbling, quick squeaks).
* **Global Limiter:** Implement a strict global audio limit (Maximum simultaneous pet sounds = 2) to prevent overlapping audio spam.