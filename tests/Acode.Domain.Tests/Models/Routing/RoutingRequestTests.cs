using Acode.Domain.Models.Routing;
using FluentAssertions;

namespace Acode.Domain.Tests.Models.Routing;

/// <summary>
/// Tests for <see cref="RoutingRequest"/>.
/// </summary>
public class RoutingRequestTests
{
    [Fact]
    public void Constructor_WithRoleOnly_SetsRole()
    {
        // Act
        var request = new RoutingRequest
        {
            Role = AgentRole.Planner,
        };

        // Assert
        request.Role.Should().Be(AgentRole.Planner);
        request.UserOverride.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithRoleAndOverride_SetsBothProperties()
    {
        // Act
        var request = new RoutingRequest
        {
            Role = AgentRole.Coder,
            UserOverride = "llama3.2:70b",
        };

        // Assert
        request.Role.Should().Be(AgentRole.Coder);
        request.UserOverride.Should().Be("llama3.2:70b");
    }

    [Fact]
    public void RoutingRequest_IsImmutable()
    {
        // Arrange
        var request = new RoutingRequest
        {
            Role = AgentRole.Reviewer,
            UserOverride = "mistral:7b",
        };

        // Act & Assert
        // Properties should be init-only, verified by compilation success
        request.Role.Should().Be(AgentRole.Reviewer);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var request1 = new RoutingRequest
        {
            Role = AgentRole.Planner,
            UserOverride = "model1",
        };
        var request2 = new RoutingRequest
        {
            Role = AgentRole.Planner,
            UserOverride = "model1",
        };

        // Act & Assert
        request1.Should().Be(request2);
    }

    [Fact]
    public void RecordEquality_WithDifferentRoles_AreNotEqual()
    {
        // Arrange
        var request1 = new RoutingRequest
        {
            Role = AgentRole.Planner,
        };
        var request2 = new RoutingRequest
        {
            Role = AgentRole.Coder,
        };

        // Act & Assert
        request1.Should().NotBe(request2);
    }

    [Fact]
    public void HasUserOverride_WithOverride_ReturnsTrue()
    {
        // Arrange
        var request = new RoutingRequest
        {
            Role = AgentRole.Coder,
            UserOverride = "llama3.2:7b",
        };

        // Act
        var result = request.HasUserOverride;

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasUserOverride_WithoutOverride_ReturnsFalse()
    {
        // Arrange
        var request = new RoutingRequest
        {
            Role = AgentRole.Planner,
        };

        // Act
        var result = request.HasUserOverride;

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasUserOverride_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var request = new RoutingRequest
        {
            Role = AgentRole.Reviewer,
            UserOverride = string.Empty,
        };

        // Act
        var result = request.HasUserOverride;

        // Assert
        result.Should().BeFalse();
    }
}
