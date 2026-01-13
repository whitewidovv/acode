namespace Acode.Domain.Tests.Audit;

using Acode.Domain.Audit;
using FluentAssertions;

/// <summary>
/// Tests for SpanId value object.
/// Spec requirement: SpanId.Value must match pattern ^span_[a-zA-Z0-9]+$.
/// </summary>
public class SpanIdTests
{
    [Fact]
    public void New_ShouldGenerateUniqueSpanIds()
    {
        // Arrange & Act
        var spanId1 = SpanId.New();
        var spanId2 = SpanId.New();

        // Assert
        spanId1.Value.Should().NotBe(
            spanId2.Value,
            because: "each SpanId must be unique");
    }

    [Fact]
    public void New_ShouldMatchRequiredFormat()
    {
        // Arrange & Act
        var spanId = SpanId.New();

        // Assert - From spec line 1017: must match ^span_[a-zA-Z0-9]+$
        spanId.Value.Should().MatchRegex(
            @"^span_[a-zA-Z0-9]+$",
            because: "SpanId must follow span_xxx format per FR-003c spec line 1017");
    }

    [Fact]
    public void New_ShouldHaveSpanPrefix()
    {
        // Arrange & Act
        var spanId = SpanId.New();

        // Assert
        spanId.Value.Should().StartWith(
            "span_",
            because: "SpanId must have span_ prefix");
    }

    [Fact]
    public void Constructor_ShouldAcceptValidFormat()
    {
        // Arrange
        var validValue = "span_abc123XYZ";

        // Act
        var spanId = new SpanId(validValue);

        // Assert
        spanId.Value.Should().Be(validValue);
    }

    [Fact]
    public void Constructor_ShouldRejectNullOrWhitespace()
    {
        // Arrange & Act & Assert
        Action nullAction = () => new SpanId(null!);
        Action emptyAction = () => new SpanId(string.Empty);
        Action whitespaceAction = () => new SpanId("   ");

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
            "span",                   // Missing suffix
            "span_",                  // Empty suffix
            "sp_abc123",              // Wrong prefix
            "abc123",                 // No prefix
            "span abc123",            // Space instead of underscore
            "span_abc 123",           // Space in suffix
            "span_abc-123",           // Dash not allowed
        };

        // Act & Assert
        foreach (var invalid in invalidFormats)
        {
            Action action = () => new SpanId(invalid);
            action.Should().Throw<ArgumentException>(
                because: $"'{invalid}' does not match required format span_[a-zA-Z0-9]+");
        }
    }

    [Fact]
    public void ToString_ShouldReturnValue()
    {
        // Arrange
        var spanId = new SpanId("span_test123");

        // Act
        var result = spanId.ToString();

        // Assert
        result.Should().Be("span_test123");
    }

    [Fact]
    public void ValueObjects_ShouldBeEqual_WhenValuesAreEqual()
    {
        // Arrange
        var id1 = new SpanId("span_test123");
        var id2 = new SpanId("span_test123");

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
        var id1 = new SpanId("span_test123");
        var id2 = new SpanId("span_test456");

        // Act & Assert
        id1.Should().NotBe(
            id2,
            because: "records with different values should not be equal");
        (id1 != id2).Should().BeTrue();
    }
}
