<p align="center">
  <img src="Assets/app.png" alt="Creature App Logo" width="128" height="128" />
</p>

<h1 align="center">Creature - Desktop Pets</h1>

<p align="center">
  <b>A lightweight, zero-friction, multi-pet desktop companion for Windows built with .NET 10 & WPF.</b>
</p>

---

## ? Features

- ?? **Multiple Pet Species & Custom Names**
  - **Bunny**: Hops and roams gracefully along the floor.
  - **Gorgon**: Slithers, idles, and casts stone stares.
  - **Werewolf**: Wanders the desktop and performs agile jumps and flinches.
  - Supports up to 4 simultaneous desktop pets with customizable hover name labels.

- ?? **Simpness Factor & Intelligent Cursor Interaction**
  - Unique cursor attraction dynamics per species.
  - Pets naturally face your cursor when nearby and dynamically choose whether to follow or mind their own business.
  - Interactive cursor hover triggers playful state transitions.

- ??? **Multi-Monitor Transparent Overlay with Zero Click Friction**
  - Native Win32 `WM_NCHITTEST` hit-test routing ensures that empty canvas areas return `HTTRANSPARENT` (-1), letting all mouse clicks pass straight through to your desktop, IDE, or games.
  - Only the bounding boxes of your pets interact with the cursor.

- ?? **Pixel-Perfect Scaling**
  - Native `NearestNeighbor` interpolation ensures crisp, high-quality pixel art at any scale (1.0x, 1.5x, 2.0x Default, 2.5x, 3.0x, 4.0x).

- ?? **Local Storage & Boot Persistence**
  - All pet configurations, custom names, active pets, and scaling presets are automatically stored in `%APPDATA%\Creature\settings.json`.
  - Seamless "Start with Windows" support so your pets greet you right upon boot.

- ??? **System Tray Controls**
  - Right-click the tray icon to pause/resume simulation, toggle pet names, change pet sizes, adopt/rename/remove pets, and configure startup behavior.

---

## ??? Architecture & Tech Stack

- **Framework**: .NET 10.0 (`net10.0-windows`)
- **UI & Graphics**: WPF (Windows Presentation Foundation) with direct `CroppedBitmap` frame slicing from Aseprite JSON metadata.
- **Windowing & Interop**: Win32 P/Invoke (`User32.dll`) transparent overlays with `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TRANSPARENT`.
- **Game Loop**: Low CPU `CompositionTarget.Rendering` ~60Hz simulation loop with delta-time clamping and zero garbage collection churn.
- **Testing**: Comprehensive xUnit + FluentAssertions STA test suite.

---

## ?? Getting Started

### Prerequisites
- [Windows 10 / 11](https://www.microsoft.com/windows)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Building and Running
```bash
# Clone the repository
git clone https://github.com/kunal-jangid/Creature.git
cd Creature

# Run unit tests
dotnet test

# Build and run
dotnet run --project Creature.csproj
```

### Publishing Standalone Binary
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

---

## ?? License
MIT License. Free for personal and commercial use.

