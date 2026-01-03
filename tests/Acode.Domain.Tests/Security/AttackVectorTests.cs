namespace Acode.Domain.Tests.Security;

using Acode.Domain.Security;
using FluentAssertions;
using Xunit;

public class AttackVectorTests
{
    [Fact]
    public void AttackVector_ShouldBeCreatableWithRequiredFields()
    {
        // Arrange & Act
        var vector = new AttackVector
        {
            VectorId = "VEC-001",
            Description = "Path traversal attack",
            ThreatActor = ThreatActor.MaliciousInput,
            Boundary = TrustBoundary.FileSystem
        };

        // Assert
        vector.VectorId.Should().Be("VEC-001");
        vector.Description.Should().Be("Path traversal attack");
        vector.ThreatActor.Should().Be(ThreatActor.MaliciousInput);
        vector.Boundary.Should().Be(TrustBoundary.FileSystem);
    }

    [Fact]
    public void AttackVector_ShouldBeImmutable()
    {
        // Arrange
        var vector = new AttackVector
        {
            VectorId = "VEC-002",
            Description = "SQL injection",
            ThreatActor = ThreatActor.User,
            Boundary = TrustBoundary.UserInput
        };

        // Act & Assert
        // Records are immutable by default - with expression creates new instance
        var modified = vector with { Description = "Modified" };

        vector.Description.Should().Be("SQL injection");
        modified.Description.Should().Be("Modified");
        modified.VectorId.Should().Be(vector.VectorId);
    }

    [Fact]
    public void AttackVector_ShouldSupportValueEquality()
    {
        // Arrange
        var vector1 = new AttackVector
        {
            VectorId = "VEC-003",
            Description = "Command injection",
            ThreatActor = ThreatActor.Process,
            Boundary = TrustBoundary.ProcessExecution
        };

        var vector2 = new AttackVector
        {
            VectorId = "VEC-003",
            Description = "Command injection",
            ThreatActor = ThreatActor.Process,
            Boundary = TrustBoundary.ProcessExecution
        };

        // Act & Assert
        vector1.Should().Be(vector2);
        (vector1 == vector2).Should().BeTrue();
        vector1.GetHashCode().Should().Be(vector2.GetHashCode());
    }

    [Fact]
    public void AttackVector_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var vector1 = new AttackVector
        {
            VectorId = "VEC-004",
            Description = "XSS attack",
            ThreatActor = ThreatActor.MaliciousInput,
            Boundary = TrustBoundary.LlmOutput
        };

        var vector2 = new AttackVector
        {
            VectorId = "VEC-005",
            Description = "XSS attack",
            ThreatActor = ThreatActor.MaliciousInput,
            Boundary = TrustBoundary.LlmOutput
        };

        // Act & Assert
        vector1.Should().NotBe(vector2);
        (vector1 == vector2).Should().BeFalse();
    }

    [Fact]
    public void AttackVector_ToString_ShouldIncludeKeyFields()
    {
        // Arrange
        var vector = new AttackVector
        {
            VectorId = "VEC-006",
            Description = "Data exfiltration",
            ThreatActor = ThreatActor.Network,
            Boundary = TrustBoundary.Network
        };

        // Act
        var result = vector.ToString();

        // Assert
        result.Should().Contain("VEC-006");
        result.Should().Contain("Data exfiltration");
    }
}
