namespace Acode.Domain.Tests.Models.Inference;

using System.Text.Json;
using Acode.Domain.Models.Inference;
using FluentAssertions;

/// <summary>
/// Tests for ContentFilterResult record.
/// FR-004b-089 to FR-004b-095: Content filter type for moderation results.
/// </summary>
public sealed class ContentFilterResultTests
{
    [Theory]
    [InlineData(FilterCategory.Sexual)]
    [InlineData(FilterCategory.Violence)]
    [InlineData(FilterCategory.Hate)]
    [InlineData(FilterCategory.SelfHarm)]
    public void Should_Have_Category(FilterCategory category)
    {
        // Act
        var result = new ContentFilterResult
        {
            Category = category,
            Severity = FilterSeverity.Safe,
            Filtered = false,
        };

        // Assert
        result.Category.Should().Be(category);
    }

    [Theory]
    [InlineData(FilterSeverity.Safe)]
    [InlineData(FilterSeverity.Low)]
    [InlineData(FilterSeverity.Medium)]
    [InlineData(FilterSeverity.High)]
    public void Should_Have_Severity(FilterSeverity severity)
    {
        // Act
        var result = new ContentFilterResult
        {
            Category = FilterCategory.Violence,
            Severity = severity,
            Filtered = severity == FilterSeverity.High,
        };

        // Assert
        result.Severity.Should().Be(severity);
    }

    [Fact]
    public void Should_Have_Filtered_Flag()
    {
        // Arrange
        var filtered = new ContentFilterResult
        {
            Category = FilterCategory.Violence,
            Severity = FilterSeverity.High,
            Filtered = true,
        };

        var notFiltered = new ContentFilterResult
        {
            Category = FilterCategory.Violence,
            Severity = FilterSeverity.Low,
            Filtered = false,
        };

        // Assert
        filtered.Filtered.Should().BeTrue();
        notFiltered.Filtered.Should().BeFalse();
    }

    [Fact]
    public void Should_Support_Optional_Reason()
    {
        // Arrange
        var withReason = new ContentFilterResult
        {
            Category = FilterCategory.Violence,
            Severity = FilterSeverity.High,
            Filtered = true,
            Reason = "Graphic violence detected",
        };

        var withoutReason = new ContentFilterResult
        {
            Category = FilterCategory.Violence,
            Severity = FilterSeverity.Safe,
            Filtered = false,
        };

        // Assert
        withReason.Reason.Should().Be("Graphic violence detected");
        withoutReason.Reason.Should().BeNull();
    }

    [Fact]
    public void Should_Serialize_To_Json()
    {
        // Arrange
        var result = new ContentFilterResult
        {
            Category = FilterCategory.Hate,
            Severity = FilterSeverity.Medium,
            Filtered = false,
            Reason = "Borderline content",
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        // Act
        var json = JsonSerializer.Serialize(result, options);

        // Assert
        json.Should().Contain("\"category\"");
        json.Should().Contain("\"severity\"");
        json.Should().Contain("\"filtered\":false");
    }

    [Fact]
    public void Should_Deserialize_From_Json()
    {
        // Arrange
        var json = """
        {
            "category": "violence",
            "severity": "low",
            "filtered": false
        }
        """;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };

        // Act
        var result = JsonSerializer.Deserialize<ContentFilterResult>(json, options);

        // Assert
        result.Should().NotBeNull();
        result!.Category.Should().Be(FilterCategory.Violence);
        result.Severity.Should().Be(FilterSeverity.Low);
    }
}
