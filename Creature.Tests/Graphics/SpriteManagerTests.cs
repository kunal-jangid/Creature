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
    public void LoadAnimation_WithBunnyLieDown_ShouldLoadFourFramesWith100msDuration()
    {
        // Note: BunnyLieDown.json defines 4 frames (128x32 sheet / 32x32 frames)
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");

        File.Exists(jsonPath).Should().BeTrue();
        File.Exists(pngPath).Should().BeTrue();

        var spriteManager = new SpriteManager();
        var clip = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);

        clip.Should().NotBeNull();
        clip.Name.Should().Be("BunnyLieDown");
        clip.Frames.Length.Should().Be(4);
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

    [StaFact]
    public void GetClip_WhenAnimationNotLoaded_ShouldReturnNull()
    {
        var spriteManager = new SpriteManager();
        var clip = spriteManager.GetClip("NonExistent");

        clip.Should().BeNull();
    }

    [StaFact]
    public void GetClip_WhenAnimationLoaded_ShouldReturnSameClip()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");

        var spriteManager = new SpriteManager();
        var loaded = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
        var retrieved = spriteManager.GetClip("BunnyLieDown");

        retrieved.Should().BeSameAs(loaded);
    }

    [StaFact]
    public void LoadAnimation_CalledMultipleTimes_ShouldReturnCachedInstance()
    {
        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.json");
        var pngPath = Path.Combine(AppContext.BaseDirectory, "Assets", "BunnyLieDown.png");

        var spriteManager = new SpriteManager();
        var first = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);
        var second = spriteManager.LoadAnimation("BunnyLieDown", jsonPath, pngPath);

        second.Should().BeSameAs(first);
    }
}
