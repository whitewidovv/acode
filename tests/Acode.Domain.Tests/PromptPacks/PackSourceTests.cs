using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="PackSource"/> enum.
/// </summary>
public class PackSourceTests
{
    [Fact]
    public void AllSources_ShouldBeDefined()
    {
        // Arrange & Act
        var sources = Enum.GetValues<PackSource>();

        // Assert
        sources.Should().Contain(PackSource.BuiltIn);
        sources.Should().Contain(PackSource.User);
    }

    [Fact]
    public void BuiltIn_ShouldHaveValue()
    {
        // Act
        var value = (int)PackSource.BuiltIn;

        // Assert
        value.Should().Be(0);
    }

    [Fact]
    public void User_ShouldHaveValue()
    {
        // Act
        var value = (int)PackSource.User;

        // Assert
        value.Should().Be(1);
    }
}
