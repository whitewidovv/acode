namespace Acode.Domain.Tests.Roles;

using Acode.Domain.Roles;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for the AgentRole enum.
/// Tests AC-001 to AC-007 requirements.
/// </summary>
public class AgentRoleTests
{
    /// <summary>
    /// Test: Should define all four core roles.
    /// AC-002 to AC-005: All role values exist.
    /// </summary>
    [Fact]
    public void Should_Define_All_Core_Roles()
    {
        // Arrange & Act
        var allRoles = Enum.GetValues<AgentRole>();

        // Assert
        allRoles.Should().Contain(AgentRole.Default);
        allRoles.Should().Contain(AgentRole.Planner);
        allRoles.Should().Contain(AgentRole.Coder);
        allRoles.Should().Contain(AgentRole.Reviewer);
        allRoles.Should().HaveCount(4, "MVP defines exactly 4 roles");
    }

    /// <summary>
    /// Test: Should convert role to string correctly.
    /// AC-006: String representations work.
    /// </summary>
    /// <param name="role">The role to convert.</param>
    /// <param name="expected">The expected string representation.</param>
    [Theory]
    [InlineData(AgentRole.Default, "Default")]
    [InlineData(AgentRole.Planner, "Planner")]
    [InlineData(AgentRole.Coder, "Coder")]
    [InlineData(AgentRole.Reviewer, "Reviewer")]
    public void Should_Convert_Role_To_String(AgentRole role, string expected)
    {
        // Arrange & Act
        var result = role.ToString();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Test: Should parse string to role correctly.
    /// AC-006: String representations work.
    /// </summary>
    /// <param name="input">The string to parse.</param>
    /// <param name="expected">The expected role.</param>
    [Theory]
    [InlineData("Default", AgentRole.Default)]
    [InlineData("Planner", AgentRole.Planner)]
    [InlineData("Coder", AgentRole.Coder)]
    [InlineData("Reviewer", AgentRole.Reviewer)]
    [InlineData("default", AgentRole.Default)]
    [InlineData("PLANNER", AgentRole.Planner)]
    public void Should_Parse_String_To_Role(string input, AgentRole expected)
    {
        // Arrange & Act
        var success = Enum.TryParse<AgentRole>(input, ignoreCase: true, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().Be(expected);
    }

    /// <summary>
    /// Test: Should return Default for unknown strings.
    /// AC-007: Unknown = Default.
    /// </summary>
    /// <param name="input">The invalid string to parse.</param>
    [Theory]
    [InlineData("InvalidRole")]
    [InlineData("Admin")]
    [InlineData("")]
    public void Should_Return_Default_For_Unknown_Strings(string input)
    {
        // Arrange & Act
        var result = AgentRoleExtensions.Parse(input);

        // Assert
        result.Should().Be(AgentRole.Default);
    }

    /// <summary>
    /// Test: ToDisplayString should work for all roles.
    /// </summary>
    [Fact]
    public void ToDisplayString_Should_Return_Correct_Value_For_All_Roles()
    {
        // Arrange & Act & Assert
        AgentRole.Default.ToDisplayString().Should().Be("Default");
        AgentRole.Planner.ToDisplayString().Should().Be("Planner");
        AgentRole.Coder.ToDisplayString().Should().Be("Coder");
        AgentRole.Reviewer.ToDisplayString().Should().Be("Reviewer");
    }

    /// <summary>
    /// Test: Parse extension method should work case-insensitively.
    /// </summary>
    [Fact]
    public void Parse_Should_Be_Case_Insensitive()
    {
        // Arrange & Act & Assert
        AgentRoleExtensions.Parse("planner").Should().Be(AgentRole.Planner);
        AgentRoleExtensions.Parse("CODER").Should().Be(AgentRole.Coder);
        AgentRoleExtensions.Parse("ReViEwEr").Should().Be(AgentRole.Reviewer);
    }
}
