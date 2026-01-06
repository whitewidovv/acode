using Acode.Domain.Models.Routing;
using Acode.Infrastructure.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Acode.Infrastructure.Tests.Models;

/// <summary>
/// Tests for <see cref="RoleRegistry"/>.
/// </summary>
public class RoleRegistryTests
{
    [Fact]
    public void GetRole_ForPlanner_ReturnsCorrectDefinition()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var definition = registry.GetRole(AgentRole.Planner);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Planner);
        definition.Name.Should().Be("planner");
        definition.Description.Should().Contain("planning");
        definition.Capabilities.Should().NotBeEmpty();
    }

    [Fact]
    public void GetRole_ForCoder_ReturnsCorrectDefinition()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var definition = registry.GetRole(AgentRole.Coder);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Coder);
        definition.Name.Should().Be("coder");
        definition.Description.Should().Contain("implementation");
        definition.Capabilities.Should().Contain("write_file");
    }

    [Fact]
    public void GetRole_ForReviewer_ReturnsCorrectDefinition()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var definition = registry.GetRole(AgentRole.Reviewer);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Reviewer);
        definition.Name.Should().Be("reviewer");
        definition.Description.Should().Contain("review");
        definition.Capabilities.Should().Contain("analyze_diff");
    }

    [Fact]
    public void GetRole_ForDefault_ReturnsCorrectDefinition()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var definition = registry.GetRole(AgentRole.Default);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Default);
        definition.Name.Should().Be("default");
    }

    [Fact]
    public void ListRoles_ReturnsAllFourRoles()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var roles = registry.ListRoles();

        // Assert
        roles.Should().HaveCount(4);
        roles.Select(r => r.Role).Should().Contain(new[]
        {
            AgentRole.Default,
            AgentRole.Planner,
            AgentRole.Coder,
            AgentRole.Reviewer,
        });
    }

    [Fact]
    public void ListRoles_IsOrderedByRoleEnumValue()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var roles = registry.ListRoles();

        // Assert
        roles[0].Role.Should().Be(AgentRole.Default);
        roles[1].Role.Should().Be(AgentRole.Planner);
        roles[2].Role.Should().Be(AgentRole.Coder);
        roles[3].Role.Should().Be(AgentRole.Reviewer);
    }

    [Fact]
    public void GetCurrentRole_Initially_ReturnsDefault()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var current = registry.GetCurrentRole();

        // Assert
        current.Should().Be(AgentRole.Default);
    }

    [Fact]
    public void SetCurrentRole_UpdatesCurrentRole()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        registry.SetCurrentRole(AgentRole.Planner, "Starting planning phase");
        var current = registry.GetCurrentRole();

        // Assert
        current.Should().Be(AgentRole.Planner);
    }

    [Fact]
    public void SetCurrentRole_MultipleTimes_ReturnsLatestRole()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        registry.SetCurrentRole(AgentRole.Planner, "Planning");
        registry.SetCurrentRole(AgentRole.Coder, "Coding");
        registry.SetCurrentRole(AgentRole.Reviewer, "Reviewing");
        var current = registry.GetCurrentRole();

        // Assert
        current.Should().Be(AgentRole.Reviewer);
    }

    [Fact]
    public void RoleRegistry_PlannerHasConstraintCannotModifyFiles()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var planner = registry.GetRole(AgentRole.Planner);

        // Assert
        planner.Constraints.Should().Contain("cannot_modify_files");
    }

    [Fact]
    public void RoleRegistry_CoderHasStrictMinimalDiffConstraint()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var coder = registry.GetRole(AgentRole.Coder);

        // Assert
        coder.Constraints.Should().Contain("strict_minimal_diff");
    }

    [Fact]
    public void RoleRegistry_ReviewerHasReadOnlyConstraint()
    {
        // Arrange
        var registry = new RoleRegistry(NullLogger<RoleRegistry>.Instance);

        // Act
        var reviewer = registry.GetRole(AgentRole.Reviewer);

        // Assert
        reviewer.Constraints.Should().Contain("read_only");
    }
}
