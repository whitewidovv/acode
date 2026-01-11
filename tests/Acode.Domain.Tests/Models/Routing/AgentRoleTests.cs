using Acode.Domain.Models.Routing;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Routing;

/// <summary>
/// Tests for <see cref="AgentRole"/>.
/// </summary>
public class AgentRoleTests
{
    [Fact]
    public void AgentRole_HasDefaultValue()
    {
        // Arrange & Act
        var role = AgentRole.Default;

        // Assert
        role.Should().Be(AgentRole.Default);
        ((int)role).Should().Be(0);
    }

    [Fact]
    public void AgentRole_HasPlannerValue()
    {
        // Arrange & Act
        var role = AgentRole.Planner;

        // Assert
        role.Should().Be(AgentRole.Planner);
        ((int)role).Should().Be(1);
    }

    [Fact]
    public void AgentRole_HasCoderValue()
    {
        // Arrange & Act
        var role = AgentRole.Coder;

        // Assert
        role.Should().Be(AgentRole.Coder);
        ((int)role).Should().Be(2);
    }

    [Fact]
    public void AgentRole_HasReviewerValue()
    {
        // Arrange & Act
        var role = AgentRole.Reviewer;

        // Assert
        role.Should().Be(AgentRole.Reviewer);
        ((int)role).Should().Be(3);
    }

    [Fact]
    public void AgentRole_HasExactlyFourValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<AgentRole>();

        // Assert
        values.Should().HaveCount(4);
        values.Should().Contain(new[] { AgentRole.Default, AgentRole.Planner, AgentRole.Coder, AgentRole.Reviewer });
    }

    [Fact]
    public void AgentRole_CanConvertToString()
    {
        // Arrange
        var planner = AgentRole.Planner;

        // Act
        var result = planner.ToString();

        // Assert
        result.Should().Be("Planner");
    }

    [Fact]
    public void AgentRole_CanParseFromString()
    {
        // Arrange
        const string roleString = "Coder";

        // Act
        var parsed = Enum.Parse<AgentRole>(roleString);

        // Assert
        parsed.Should().Be(AgentRole.Coder);
    }
}
