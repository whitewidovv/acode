using Acode.Domain.Commands;
using FluentAssertions;

namespace Acode.Domain.Tests.Commands;

/// <summary>
/// Tests for CommandGroup enum.
/// Verifies all six command groups are defined per Task 002.c.
/// </summary>
public class CommandGroupTests
{
    [Fact]
    public void CommandGroup_ShouldHaveExactlySixGroups()
    {
        // Arrange & Act
        var groups = Enum.GetValues<CommandGroup>();

        // Assert
        groups.Should().HaveCount(6, "per FR-002c-01 and FR-002c-02");
    }

    [Theory]
    [InlineData(CommandGroup.Setup)]
    [InlineData(CommandGroup.Build)]
    [InlineData(CommandGroup.Test)]
    [InlineData(CommandGroup.Lint)]
    [InlineData(CommandGroup.Format)]
    [InlineData(CommandGroup.Start)]
    public void CommandGroup_ShouldHaveAllRequiredGroups(CommandGroup group)
    {
        // Arrange & Act
        var groupName = group.ToString();

        // Assert - all required groups must be defined
        groupName.Should().BeOneOf("Setup", "Build", "Test", "Lint", "Format", "Start");
    }

    [Fact]
    public void CommandGroup_Setup_ShouldBeFirstGroup()
    {
        // Arrange & Act
        var setup = CommandGroup.Setup;

        // Assert
        setup.Should().Be(CommandGroup.Setup);
        ((int)setup).Should().Be(0, "setup should be the first group");
    }

    [Fact]
    public void CommandGroup_ToStringGivesGroupName()
    {
        // Arrange & Act
        var buildName = CommandGroup.Build.ToString();
        var testName = CommandGroup.Test.ToString();

        // Assert
        buildName.Should().Be("Build");
        testName.Should().Be("Test");
    }
}
