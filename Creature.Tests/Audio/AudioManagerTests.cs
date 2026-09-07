using Creature.Audio;
using FluentAssertions;
using Xunit;

namespace Creature.Tests.Audio;

public class AudioManagerTests
{
    [Fact]
    public void PlaySound_WhenMuted_ShouldNotPlay()
    {
        var audio = new AudioManager { IsSoundEnabled = false };
        var played = audio.PlaySound("hop");
        played.Should().BeFalse();
        audio.ActiveVoiceCount.Should().Be(0);
    }

    [Fact]
    public void ConcurrencyLimiter_ShouldCapSimultaneousVoicesAtTwo()
    {
        var audio = new AudioManager { IsSoundEnabled = true };
        audio.ActiveVoiceCount.Should().Be(0);
        audio.MaxVoices.Should().Be(2);
    }

    [Fact]
    public void PlaySound_WhenActiveVoicesExceedMaxVoices_ShouldRejectAdditionalPlayback()
    {
        var audio = new AudioManager { IsSoundEnabled = true, MaxVoices = 2 };
        var first = audio.PlaySound("hop");
        var second = audio.PlaySound("hop");
        var third = audio.PlaySound("hop");

        first.Should().BeTrue();
        second.Should().BeTrue();
        third.Should().BeFalse();
        audio.ActiveVoiceCount.Should().Be(2);
    }
}
