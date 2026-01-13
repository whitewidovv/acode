namespace Acode.Domain.Tests.Audit;

using Acode.Domain.Audit;
using FluentAssertions;

/// <summary>
/// Tests for CorrelationId value object.
/// Spec requirement: CorrelationId.Value must match pattern ^corr_[a-zA-Z0-9]+$.
/// </summary>
public class CorrelationIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueCorrelationIds()
    {
        // Arrange & Act
        var corrId1 = CorrelationId.New();
        var corrId2 = CorrelationId.New();

        // Assert
        corrId1.Value.Should().NotBe(
            corrId2.Value,
            because: "each CorrelationId must be unique");
    }

    [Fact]
    public void New_ShouldMatchRequiredFormat()
    {
        // Arrange & Act
        var corrId = CorrelationId.New();

        // Assert - From spec line 930-931: must match ^corr_[a-zA-Z0-9]+$
        corrId.Value.Should().MatchRegex(
            @"^corr_[a-zA-Z0-9]+$",
            because: "CorrelationId must follow corr_xxx format per FR-003c spec line 930-931");
    }

    [Fact]
    public void New_ShouldHaveCorrPrefix()
    {
        // Arrange & Act
        var corrId = CorrelationId.New();

        // Assert
        corrId.Value.Should().StartWith(
            "corr_",
            because: "CorrelationId must have corr_ prefix");
    }

    [Fact]
    public void Constructor_ShouldAcceptValidFormat()
    {
        // Arrange
        var validValue = "corr_abc123XYZ";

        // Act
        var corrId = new CorrelationId(validValue);

        // Assert
        corrId.Value.Should().Be(validValue);
    }

    [Fact]
    public void Constructor_ShouldRejectNullOrWhitespace()
    {
        // Arrange & Act & Assert
        Action nullAction = () => new CorrelationId(null!);
        Action emptyAction = () => new CorrelationId(string.Empty);
        Action whitespaceAction = () => new CorrelationId("   ");

        nullAction.Should().Throw<ArgumentException>();
        emptyAction.Should().Throw<ArgumentException>();
        whitespaceAction.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_ShouldRejectInvalidFormat()
    {
        // Arrange - various invalid formats
        var invalidFormats = new[]
        {
            "corr",                   // Missing suffix
            "corr_",                  // Empty suffix
            "correlation_abc123",     // Wrong prefix
            "abc123",                 // No prefix
            "corr abc123",            // Space instead of underscore
            "corr_abc 123",           // Space in suffix
            "corr_abc-123",           // Dash not allowed
        };

        // Act & Assert
        foreach (var invalid in invalidFormats)
        {
            Action action = () => new CorrelationId(invalid);
            action.Should().Throw<ArgumentException>(
                because: $"'{invalid}' does not match required format corr_[a-zA-Z0-9]+");
        }
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var corrId = new CorrelationId("corr_test123");

        // Act
        var result = corrId.ToString();

        // Assert
        result.Should().Be("corr_test123");
    }

    [Fact]
    public void ValueObjects_ShouldBeEqual_WhenValuesAreEqual()
    {
        // Arrange
        var id1 = new CorrelationId("corr_test123");
        var id2 = new CorrelationId("corr_test123");

        // Act & Assert
        id1.Should().Be(
            id2,
            because: "records with same value should be equal");
        (id1 == id2).Should().BeTrue();
    }

    [Fact]
    public void ValueObjects_ShouldNotBeEqual_WhenValuesDiffer()
    {
        // Arrange
        var id1 = new CorrelationId("corr_test123");
        var id2 = new CorrelationId("corr_test456");

        // Act & Assert
        id1.Should().NotBe(
            id2,
            because: "records with different values should not be equal");
        (id1 != id2).Should().BeTrue();
    }
}
