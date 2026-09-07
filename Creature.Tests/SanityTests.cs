using FluentAssertions;
using Xunit;

namespace Creature.Tests;

public class SanityTests
{
    [Fact]
    public void Environment_ShouldTargetNet10()
    {
        Environment.Version.Major.Should().BeGreaterThanOrEqualTo(10);
    }
}
