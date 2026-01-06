namespace Acode.Domain.Tests.Risks;

using Acode.Domain.Risks;
using FluentAssertions;

public class RiskIdTests
{
    [Theory]
    [InlineData("RISK-S-001")]
    [InlineData("RISK-T-042")]
    [InlineData("RISK-R-999")]
    [InlineData("RISK-I-123")]
    [InlineData("RISK-D-007")]
    [InlineData("RISK-E-100")]
    public void RiskId_WithValidFormat_ShouldCreate(string validId)
    {
        // Arrange & Act
        var riskId = new RiskId(validId);

        // Assert
        riskId.Value.Should().Be(validId);
    }

    [Theory]
    [InlineData("RISK-S-001", RiskCategory.Spoofing)]
    [InlineData("RISK-T-042", RiskCategory.Tampering)]
    [InlineData("RISK-R-999", RiskCategory.Repudiation)]
    [InlineData("RISK-I-123", RiskCategory.InformationDisclosure)]
    [InlineData("RISK-D-007", RiskCategory.DenialOfService)]
    [InlineData("RISK-E-100", RiskCategory.ElevationOfPrivilege)]
    public void RiskId_ShouldExtractCategoryFromId(string id, RiskCategory expectedCategory)
    {
        // Arrange & Act
        var riskId = new RiskId(id);

        // Assert
        riskId.Category.Should().Be(expectedCategory);
    }

    [Theory]
    [InlineData("RISK-S-001", 1)]
    [InlineData("RISK-T-042", 42)]
    [InlineData("RISK-R-999", 999)]
    [InlineData("RISK-I-007", 7)]
    public void RiskId_ShouldExtractSequenceNumber(string id, int expectedNumber)
    {
        // Arrange & Act
        var riskId = new RiskId(id);

        // Assert
        riskId.SequenceNumber.Should().Be(expectedNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RiskId_WithNullOrWhitespace_ShouldThrow(string? invalidId)
    {
        // Arrange & Act & Assert
        var act = () => new RiskId(invalidId!);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("INVALID")]
    [InlineData("RISK-X-001")] // Invalid category
    [InlineData("RISK-S-")] // Missing number
    [InlineData("RISK-S-ABC")] // Non-numeric
    [InlineData("RISK-S")] // Incomplete
    [InlineData("S-001")] // Missing RISK prefix
    public void RiskId_WithInvalidFormat_ShouldThrow(string invalidId)
    {
        // Arrange & Act & Assert
        var act = () => new RiskId(invalidId);
        act.Should().Throw<ArgumentException>()
            .WithMessage("*format*");
    }

    [Fact]
    public void RiskId_ShouldSupportValueEquality()
    {
        // Arrange
        var id1 = new RiskId("RISK-S-001");
        var id2 = new RiskId("RISK-S-001");

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void RiskId_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var id1 = new RiskId("RISK-S-001");
        var id2 = new RiskId("RISK-S-002");

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void RiskId_ToString_ShouldReturnValue()
    {
        // Arrange
        var riskId = new RiskId("RISK-T-042");

        // Act
        var result = riskId.ToString();

        // Assert
        result.Should().Be("RISK-T-042");
    }

    [Fact]
    public void RiskId_ShouldBeImmutable()
    {
        // Arrange
        var riskId = new RiskId("RISK-E-123");

        // Act & Assert - Value should be read-only
        riskId.Value.Should().Be("RISK-E-123");

        // No setter should exist - verified by compilation
    }
}
