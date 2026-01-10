namespace Acode.Infrastructure.Tests.Roles;

using Acode.Application.Roles;
using Acode.Domain.Roles;
using Acode.Infrastructure.Roles;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Unit tests for the RoleRegistry implementation.
/// Tests AC-015 to AC-019 and AC-035 to AC-040 requirements.
/// </summary>
public class RoleRegistryTests
{
    private readonly ILogger<RoleRegistry> _logger = NullLogger<RoleRegistry>.Instance;

    /// <summary>
    /// Test: Should get role definition by enum.
    /// AC-016: GetRole method exists.
    /// </summary>
    [Fact]
    public void Should_Get_Role_Definition_By_Enum()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        var role = AgentRole.Planner;

        // Act
        var definition = registry.GetRole(role);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Planner);
        definition.Name.Should().Be("Planner");
        definition.Capabilities.Should().Contain("read_file");
        definition
            .Constraints.Should()
            .Contain(c => c.Contains("Cannot modify", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Test: Should list all roles.
    /// AC-017: ListRoles method exists.
    /// </summary>
    [Fact]
    public void Should_List_All_Roles()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);

        // Act
        var roles = registry.ListRoles();

        // Assert
        roles.Should().HaveCount(4);
        roles.Should().Contain(r => r.Role == AgentRole.Default);
        roles.Should().Contain(r => r.Role == AgentRole.Planner);
        roles.Should().Contain(r => r.Role == AgentRole.Coder);
        roles.Should().Contain(r => r.Role == AgentRole.Reviewer);
    }

    /// <summary>
    /// Test: Should track current role.
    /// AC-018, AC-035: GetCurrentRole tracks state.
    /// </summary>
    [Fact]
    public void Should_Track_Current_Role()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Starting planning");
        registry.SetCurrentRole(AgentRole.Coder, "Starting implementation");

        // Act
        var currentRole = registry.GetCurrentRole();

        // Assert
        currentRole.Should().Be(AgentRole.Coder);
    }

    /// <summary>
    /// Test: Should transition role successfully.
    /// AC-019, AC-037: SetCurrentRole exists and changes are explicit.
    /// </summary>
    [Fact]
    public void Should_Transition_Role_Successfully()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        var initialRole = AgentRole.Planner;
        var targetRole = AgentRole.Coder;
        var reason = "Plan complete, starting implementation";

        registry.SetCurrentRole(initialRole, "Initial planning");

        // Act
        registry.SetCurrentRole(targetRole, reason);

        // Assert
        registry.GetCurrentRole().Should().Be(targetRole);
    }

    /// <summary>
    /// Test: Should start with Default role.
    /// AC-036: Initial is Default.
    /// </summary>
    [Fact]
    public void Should_Start_With_Default_Role()
    {
        // Arrange
        var freshRegistry = new RoleRegistry(_logger);

        // Act
        var currentRole = freshRegistry.GetCurrentRole();

        // Assert
        currentRole.Should().Be(AgentRole.Default);
    }

    /// <summary>
    /// Test: Should throw on invalid role transition.
    /// AC-019: Invalid transitions are rejected.
    /// </summary>
    [Fact]
    public void Should_Throw_On_Invalid_Role_Transition()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Starting planning");

        // Act - Try to transition to Reviewer without implementing (invalid)
        Action act = () => registry.SetCurrentRole(AgentRole.Reviewer, "Skipping implementation");

        // Assert
        act.Should()
            .Throw<InvalidRoleTransitionException>()
            .Which.Message.Should()
            .Contain("not allowed");
    }

    /// <summary>
    /// Test: Should store role transition history.
    /// AC-038: Persists in session.
    /// </summary>
    [Fact]
    public void Should_Store_Role_Transition_History()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Planning phase");
        registry.SetCurrentRole(AgentRole.Coder, "Implementation phase");
        registry.SetCurrentRole(AgentRole.Reviewer, "Review phase");

        // Act
        var history = registry.GetRoleHistory();

        // Assert - Initial + 3 transitions = 4 entries
        history.Should().HaveCount(4);
        history[0].ToRole.Should().Be(AgentRole.Default);
        history[1].ToRole.Should().Be(AgentRole.Planner);
        history[2].ToRole.Should().Be(AgentRole.Coder);
        history[3].ToRole.Should().Be(AgentRole.Reviewer);
    }

    /// <summary>
    /// Test: Should allow Default to Planner transition.
    /// </summary>
    [Fact]
    public void Should_Allow_Default_To_Planner_Transition()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);

        // Act
        Action act = () => registry.SetCurrentRole(AgentRole.Planner, "Starting work");

        // Assert - Should not throw
        act.Should().NotThrow();
        registry.GetCurrentRole().Should().Be(AgentRole.Planner);
    }

    /// <summary>
    /// Test: Should allow Planner to Coder transition.
    /// </summary>
    [Fact]
    public void Should_Allow_Planner_To_Coder_Transition()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Planning");

        // Act
        Action act = () => registry.SetCurrentRole(AgentRole.Coder, "Plan complete");

        // Assert - Should not throw
        act.Should().NotThrow();
        registry.GetCurrentRole().Should().Be(AgentRole.Coder);
    }

    /// <summary>
    /// Test: Should allow Coder to Reviewer transition.
    /// </summary>
    [Fact]
    public void Should_Allow_Coder_To_Reviewer_Transition()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Planning");
        registry.SetCurrentRole(AgentRole.Coder, "Implementing");

        // Act
        Action act = () => registry.SetCurrentRole(AgentRole.Reviewer, "Implementation done");

        // Assert - Should not throw
        act.Should().NotThrow();
        registry.GetCurrentRole().Should().Be(AgentRole.Reviewer);
    }

    /// <summary>
    /// Test: Should allow Reviewer to Coder transition for revisions.
    /// </summary>
    [Fact]
    public void Should_Allow_Reviewer_To_Coder_Transition_For_Revisions()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        registry.SetCurrentRole(AgentRole.Planner, "Planning");
        registry.SetCurrentRole(AgentRole.Coder, "Implementing");
        registry.SetCurrentRole(AgentRole.Reviewer, "Reviewing");

        // Act
        Action act = () => registry.SetCurrentRole(AgentRole.Coder, "Revision requested");

        // Assert - Should not throw
        act.Should().NotThrow();
        registry.GetCurrentRole().Should().Be(AgentRole.Coder);
    }

    /// <summary>
    /// Test: Should allow any role to Default transition.
    /// </summary>
    [Fact]
    public void Should_Allow_Any_Role_To_Default_Transition()
    {
        // Arrange & Act & Assert
        var registry1 = new RoleRegistry(_logger);
        registry1.SetCurrentRole(AgentRole.Planner, "Planning");
        registry1.SetCurrentRole(AgentRole.Default, "Resetting");
        registry1.GetCurrentRole().Should().Be(AgentRole.Default);

        var registry2 = new RoleRegistry(_logger);
        registry2.SetCurrentRole(AgentRole.Planner, "Planning");
        registry2.SetCurrentRole(AgentRole.Coder, "Implementing");
        registry2.SetCurrentRole(AgentRole.Default, "Canceling");
        registry2.GetCurrentRole().Should().Be(AgentRole.Default);
    }

    /// <summary>
    /// Test: History should include transition reason.
    /// AC-039: Logged on change with reason.
    /// </summary>
    [Fact]
    public void History_Should_Include_Transition_Reason()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        var reason = "Plan contains 7 steps, moving to implementation";

        // Act
        registry.SetCurrentRole(AgentRole.Planner, reason);

        // Assert
        var history = registry.GetRoleHistory();
        history.Should().Contain(e => e.Reason == reason);
    }

    /// <summary>
    /// Test: Should return Default for unknown role.
    /// </summary>
    [Fact]
    public void Should_Return_Default_For_Unknown_Role()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);

        // Act - Try to get undefined role
        var definition = registry.GetRole((AgentRole)999);

        // Assert
        definition.Should().NotBeNull();
        definition.Role.Should().Be(AgentRole.Default);
    }

    /// <summary>
    /// Test: Transition history should include timestamps.
    /// </summary>
    [Fact]
    public void Transition_History_Should_Include_Timestamps()
    {
        // Arrange
        var registry = new RoleRegistry(_logger);
        var before = DateTime.UtcNow;

        // Act
        registry.SetCurrentRole(AgentRole.Planner, "Starting");

        // Assert
        var history = registry.GetRoleHistory();
        var lastEntry = history.Last();
        lastEntry.Timestamp.Should().BeOnOrAfter(before);
        lastEntry.Timestamp.Should().BeOnOrBefore(DateTime.UtcNow);
    }
}
