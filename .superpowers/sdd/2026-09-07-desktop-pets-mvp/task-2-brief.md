# Task 2: Graphics & Asset Pipeline (`SpriteManager`)

**Files:**
- Create: `Graphics/AsepriteMetadata.cs`
- Create: `Graphics/AnimationClip.cs`
- Create: `Graphics/SpriteManager.cs`
- Test: `Creature.Tests/Graphics/SpriteManagerTests.cs`

**Interfaces:**
- Produces:
  - `AsepriteMetadata`, `AsepriteFrameEntry`, `AsepriteRect`, `AsepriteMeta`, `AsepriteSize` records for JSON deserialization
  - `AnimationClip(string Name, BitmapSource[] Frames, int FrameDurationMs, bool Loop)`
  - `SpriteManager.LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true) -> AnimationClip`
  - `SpriteManager.GetClip(string name) -> AnimationClip?`

## Steps to Execute:

- [ ] **Step 1: Write failing tests for `SpriteManager`**

Create `Creature.Tests/Graphics/SpriteManagerTests.cs`:
```csharp
using System;
using System.IO;
using System.Windows.Media.Imaging;
using Creature.Graphics;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Graphics;

public class SpriteManagerTests
{
    [StaFact]
    public void LoadAnimation_WithBunnyLieDown_ShouldLoadTwoFramesWith100msDuration()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");

        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(pngPath).Should().BeTrue();

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);

        clip.Should().NotBeNull();
        clip.Name.Should().Be("BunnyLieDown");
        clip.Frames.Length.Should().Be(2);
        clip.FrameDurationMs.Should().Be(100);
        clip.Frames[0].PixelWidth.Should().Be(32);
        clip.Frames[0].PixelHeight.Should().Be(32);
    }

    [StaFact]
    public void LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.png");

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyRun", jsonPath, pngPath);

        clip.Frames.Length.Should().Be(5);
        clip.FrameDurationMs.Should().Be(100);
    }

    [StaFact]
    public void LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.png");

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyJump", jsonPath, pngPath, loop: false);

        clip.Frames.Length.Should().Be(18);
        clip.Loop.Should().BeFalse();
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run: `dotnet test --filter "FullyQualifiedName~SpriteManagerTests"`
Expected: FAIL.

- [ ] **Step 3: Implement `AsepriteMetadata.cs`, `AnimationClip.cs`, and `SpriteManager.cs`**

Create `Graphics/AsepriteMetadata.cs`:
```csharp
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Creature.Graphics;

public record AsepriteMetadata(
    [property: JsonPropertyName("frames")] Dictionary<string, AsepriteFrameEntry> Frames,
    [property: JsonPropertyName("meta")] AsepriteMeta? Meta
);

public record AsepriteFrameEntry(
    [property: JsonPropertyName("frame")] AsepriteRect Frame,
    [property: JsonPropertyName("duration")] int Duration
);

public record AsepriteRect(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);

public record AsepriteMeta(
    [property: JsonPropertyName("size")] AsepriteSize? Size
);

public record AsepriteSize(
    [property: JsonPropertyName("w")] int W,
    [property: JsonPropertyName("h")] int H
);
```

Create `Graphics/AnimationClip.cs`:
```csharp
using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class AnimationClip
{
    public string Name { get; }
    public BitmapSource[] Frames { get; }
    public int FrameDurationMs { get; }
    public bool Loop { get; }

    public AnimationClip(string name, BitmapSource[] frames, int frameDurationMs, bool loop = true)
    {
        Name = name;
        Frames = frames;
        FrameDurationMs = frameDurationMs > 0 ? frameDurationMs : 100;
        Loop = loop;
    }
}
```

Create `Graphics/SpriteManager.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Creature.Graphics;

public class SpriteManager
{
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);

    public AnimationClip LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true)
    {
        if (_clips.TryGetValue(name, out var cached))
            return cached;

        var jsonText = File.ReadAllText(jsonPath);
        var metadata = JsonSerializer.Deserialize<AsepriteMetadata>(jsonText, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException($"Failed to deserialize Aseprite JSON: {jsonPath}");

        var masterBitmap = new BitmapImage();
        masterBitmap.BeginInit();
        masterBitmap.UriSource = new Uri(Path.GetFullPath(pngPath), UriKind.Absolute);
        masterBitmap.CacheOption = BitmapCacheOption.OnLoad;
        masterBitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
        masterBitmap.EndInit();
        masterBitmap.Freeze();

        var frames = new List<BitmapSource>();
        var frameDuration = 100;

        foreach (var (_, entry) in metadata.Frames)
        {
            frameDuration = entry.Duration;
            var rect = new Int32Rect(entry.Frame.X, entry.Frame.Y, entry.Frame.W, entry.Frame.H);
            var cropped = new CroppedBitmap(masterBitmap, rect);
            cropped.Freeze();
            frames.Add(cropped);
        }

        var clip = new AnimationClip(name, frames.ToArray(), frameDuration, loop);
        _clips[name] = clip;
        return clip;
    }

    public AnimationClip? GetClip(string name)
    {
        _clips.TryGetValue(name, out var clip);
        return clip;
    }
}
```

- [ ] **Step 4: Run `dotnet test` to verify passing**

Run: `dotnet test`
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add Graphics/ Creature.Tests/
git commit -m "feat: implement Aseprite JSON parsing and CroppedBitmap sprite slicing"
```
