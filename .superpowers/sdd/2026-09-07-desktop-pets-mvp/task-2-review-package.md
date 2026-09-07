# Review Package for Task 2

## Commits
`
2cf4af9 feat: implement Aseprite JSON parsing and CroppedBitmap sprite slicing

`

## Diff Stat
`
 Creature.Tests/Creature.Tests.csproj          |  1 +
 Creature.Tests/Graphics/SpriteManagerTests.cs | 93 +++++++++++++++++++++++++++
 Graphics/AnimationClip.cs                     | 19 ++++++
 Graphics/AsepriteMetadata.cs                  | 30 +++++++++
 Graphics/SpriteManager.cs                     | 55 ++++++++++++++++
 5 files changed, 198 insertions(+)

`

## Full Diff
`diff
diff --git a/Creature.Tests/Creature.Tests.csproj b/Creature.Tests/Creature.Tests.csproj
index 423d831..5fc7327 100644
--- a/Creature.Tests/Creature.Tests.csproj
+++ b/Creature.Tests/Creature.Tests.csproj
@@ -10,20 +10,21 @@
   </PropertyGroup>
 
   <ItemGroup>
     <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
     <PackageReference Include="xunit" Version="2.9.3" />
     <PackageReference Include="xunit.runner.visualstudio" Version="3.0.2">
       <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
       <PrivateAssets>all</PrivateAssets>
     </PackageReference>
     <PackageReference Include="FluentAssertions" Version="8.1.1" />
+    <PackageReference Include="Xunit.StaFact" Version="1.1.11" />
   </ItemGroup>
 
   <ItemGroup>
     <ProjectReference Include="..\Creature.csproj" />
   </ItemGroup>
 
   <ItemGroup>
     <Content Include="..\docs\assets\**\*.*">
       <Link>Assets\%(RecursiveDir)%(Filename)%(Extension)</Link>
       <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
diff --git a/Creature.Tests/Graphics/SpriteManagerTests.cs b/Creature.Tests/Graphics/SpriteManagerTests.cs
new file mode 100644
index 0000000..ebe5833
--- /dev/null
+++ b/Creature.Tests/Graphics/SpriteManagerTests.cs
@@ -0,0 +1,93 @@
+using System;
+using System.IO;
+using System.Windows.Media.Imaging;
+using Creature.Graphics;
+using FluentAssertions;
+using Xunit;
+
+namespace Creature.Tests.Graphics;
+
+public class SpriteManagerTests
+{
+    [StaFact]
+    public void LoadAnimation_WithBunnyLieDown_ShouldLoadFourFramesWith100msDuration()
+    {
+        // Note: BunnyLieDown.json defines 4 frames (128x32 sheet / 32x32 frames)
+        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
+        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");
+
+        File.Exists(jsonPath).Should().BeTrue();
+        File.Exists(pngPath).Should().BeTrue();
+
+        var spriteManager = new SpriteManager();
+        var clip = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
+
+        clip.Should().NotBeNull();
+        clip.Name.Should().Be("BunnyLieDown");
+        clip.Frames.Length.Should().Be(4);
+        clip.FrameDurationMs.Should().Be(100);
+        clip.Frames[0].PixelWidth.Should().Be(32);
+        clip.Frames[0].PixelHeight.Should().Be(32);
+    }
+
+    [StaFact]
+    public void LoadAnimation_WithBunnyRun_ShouldLoadFiveFrames()
+    {
+        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.json");
+        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyRun.png");
+
+        var spriteManager = new SpriteManager();
+        var clip = spriteManager.LoadAnimation("BunnyRun", jsonPath, pngPath);
+
+        clip.Frames.Length.Should().Be(5);
+        clip.FrameDurationMs.Should().Be(100);
+    }
+
+    [StaFact]
+    public void LoadAnimation_WithBunnyJump_ShouldLoadEighteenFrames()
+    {
+        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.json");
+        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyJump.png");
+
+        var spriteManager = new SpriteManager();
+        var clip = spriteManager.LoadAnimation("BunnyJump", jsonPath, pngPath, loop: false);
+
+        clip.Frames.Length.Should().Be(18);
+        clip.Loop.Should().BeFalse();
+    }
+
+    [StaFact]
+    public void GetClip_WhenAnimationNotLoaded_ShouldReturnNull()
+    {
+        var spriteManager = new SpriteManager();
+        var clip = spriteManager.GetClip("NonExistent");
+
+        clip.Should().BeNull();
+    }
+
+    [StaFact]
+    public void GetClip_WhenAnimationLoaded_ShouldReturnSameClip()
+    {
+        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
+        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");
+
+        var spriteManager = new SpriteManager();
+        var loaded = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
+        var retrieved = spriteManager.GetClip("BunnyLieDown");
+
+        retrieved.Should().BeSameAs(loaded);
+    }
+
+    [StaFact]
+    public void LoadAnimation_CalledMultipleTimes_ShouldReturnCachedInstance()
+    {
+        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
+        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");
+
+        var spriteManager = new SpriteManager();
+        var first = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
+        var second = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
+
+        second.Should().BeSameAs(first);
+    }
+}
diff --git a/Graphics/AnimationClip.cs b/Graphics/AnimationClip.cs
new file mode 100644
index 0000000..0a81a0e
--- /dev/null
+++ b/Graphics/AnimationClip.cs
@@ -0,0 +1,19 @@
+using System.Windows.Media.Imaging;
+
+namespace Creature.Graphics;
+
+public class AnimationClip
+{
+    public string Name { get; }
+    public BitmapSource[] Frames { get; }
+    public int FrameDurationMs { get; }
+    public bool Loop { get; }
+
+    public AnimationClip(string name, BitmapSource[] frames, int frameDurationMs, bool loop = true)
+    {
+        Name = name;
+        Frames = frames;
+        FrameDurationMs = frameDurationMs > 0 ? frameDurationMs : 100;
+        Loop = loop;
+    }
+}
diff --git a/Graphics/AsepriteMetadata.cs b/Graphics/AsepriteMetadata.cs
new file mode 100644
index 0000000..47bbed9
--- /dev/null
+++ b/Graphics/AsepriteMetadata.cs
@@ -0,0 +1,30 @@
+using System.Collections.Generic;
+using System.Text.Json.Serialization;
+
+namespace Creature.Graphics;
+
+public record AsepriteMetadata(
+    [property: JsonPropertyName("frames")] Dictionary<string, AsepriteFrameEntry> Frames,
+    [property: JsonPropertyName("meta")] AsepriteMeta? Meta
+);
+
+public record AsepriteFrameEntry(
+    [property: JsonPropertyName("frame")] AsepriteRect Frame,
+    [property: JsonPropertyName("duration")] int Duration
+);
+
+public record AsepriteRect(
+    [property: JsonPropertyName("x")] int X,
+    [property: JsonPropertyName("y")] int Y,
+    [property: JsonPropertyName("w")] int W,
+    [property: JsonPropertyName("h")] int H
+);
+
+public record AsepriteMeta(
+    [property: JsonPropertyName("size")] AsepriteSize? Size
+);
+
+public record AsepriteSize(
+    [property: JsonPropertyName("w")] int W,
+    [property: JsonPropertyName("h")] int H
+);
diff --git a/Graphics/SpriteManager.cs b/Graphics/SpriteManager.cs
new file mode 100644
index 0000000..37c12ad
--- /dev/null
+++ b/Graphics/SpriteManager.cs
@@ -0,0 +1,55 @@
+using System;
+using System.Collections.Generic;
+using System.IO;
+using System.Text.Json;
+using System.Windows;
+using System.Windows.Media.Imaging;
+
+namespace Creature.Graphics;
+
+public class SpriteManager
+{
+    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);
+
+    public AnimationClip LoadAnimation(string name, string jsonPath, string pngPath, bool loop = true)
+    {
+        if (_clips.TryGetValue(name, out var cached))
+            return cached;
+
+        var jsonText = File.ReadAllText(jsonPath);
+        var metadata = JsonSerializer.Deserialize<AsepriteMetadata>(jsonText, new JsonSerializerOptions
+        {
+            PropertyNameCaseInsensitive = true
+        }) ?? throw new InvalidDataException($"Failed to deserialize Aseprite JSON: {jsonPath}");
+
+        var masterBitmap = new BitmapImage();
+        masterBitmap.BeginInit();
+        masterBitmap.UriSource = new Uri(Path.GetFullPath(pngPath), UriKind.Absolute);
+        masterBitmap.CacheOption = BitmapCacheOption.OnLoad;
+        masterBitmap.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
+        masterBitmap.EndInit();
+        masterBitmap.Freeze();
+
+        var frames = new List<BitmapSource>();
+        var frameDuration = 100;
+
+        foreach (var (_, entry) in metadata.Frames)
+        {
+            frameDuration = entry.Duration;
+            var rect = new Int32Rect(entry.Frame.X, entry.Frame.Y, entry.Frame.W, entry.Frame.H);
+            var cropped = new CroppedBitmap(masterBitmap, rect);
+            cropped.Freeze();
+            frames.Add(cropped);
+        }
+
+        var clip = new AnimationClip(name, frames.ToArray(), frameDuration, loop);
+        _clips[name] = clip;
+        return clip;
+    }
+
+    public AnimationClip? GetClip(string name)
+    {
+        _clips.TryGetValue(name, out var clip);
+        return clip;
+    }
+}

`
