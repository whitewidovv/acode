using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Search;

public class SearchQueryTests
{
    [Fact]
    public void Validate_WithEmptyQueryText_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = string.Empty };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("empty") || e.Contains("Query text"));
    }

    [Fact]
    public void Validate_WithNullQueryText_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = null! };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public void Validate_WithQueryTextTooLong_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = new string('a', 201) };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("200"));
    }

    [Fact]
    public void Validate_WithInvalidPageSize_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test", PageSize = 0 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Page size"));
    }

    [Fact]
    public void Validate_WithPageSizeTooLarge_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test", PageSize = 101 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Page size"));
    }

    [Fact]
    public void Validate_WithSinceAfterUntil_ReturnsFailure()
    {
        // Arrange
        var query = new SearchQuery
        {
            QueryText = "test",
            Since = DateTime.UtcNow,
            Until = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("Since") || e.Contains("before"));
    }

    [Fact]
    public void Validate_WithValidQuery_ReturnsSuccess()
    {
        // Arrange
        var query = new SearchQuery { QueryText = "test query", PageSize = 20 };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithAllFilters_ReturnsSuccess()
    {
        // Arrange
        var query = new SearchQuery
        {
            QueryText = "test",
            ChatId = ChatId.NewId(),
            Since = DateTime.UtcNow.AddDays(-7),
            Until = DateTime.UtcNow,
            RoleFilter = MessageRole.User,
            PageSize = 10,
            PageNumber = 2
        };

        // Act
        var result = query.Validate();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PageSize_DefaultsTo20()
    {
        // Arrange & Act
        var query = new SearchQuery { QueryText = "test" };

        // Assert
        query.PageSize.Should().Be(20);
    }

    [Fact]
    public void PageNumber_DefaultsTo1()
    {
        // Arrange & Act
        var query = new SearchQuery { QueryText = "test" };

        // Assert
        query.PageNumber.Should().Be(1);
    }

    [Fact]
    public void SortBy_DefaultsToRelevance()
    {
        // Arrange & Act
        var query = new SearchQuery { QueryText = "test" };

        // Assert
        query.SortBy.Should().Be(SortOrder.Relevance);
    }
}
