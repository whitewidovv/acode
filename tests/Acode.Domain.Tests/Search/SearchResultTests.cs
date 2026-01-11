#pragma warning disable IDE0005 // Using directive is unnecessary

using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using FluentAssertions;
using Xunit;

namespace Acode.Domain.Tests.Search;

public class SearchResultTests
{
    [Fact]
    public void TotalPages_WithExactDivision_CalculatesCorrectly()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.TotalPages.Should().Be(5);
    }

    [Fact]
    public void TotalPages_WithRemainder_RoundsUp()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 95,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.TotalPages.Should().Be(5); // Ceiling(95/20) = 5
    }

    [Fact]
    public void HasNextPage_WhenNotLastPage_ReturnsTrue()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 3,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public void HasNextPage_WhenLastPage_ReturnsFalse()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 5,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasNextPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenFirstPage_ReturnsFalse()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 1,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasPreviousPage.Should().BeFalse();
    }

    [Fact]
    public void HasPreviousPage_WhenNotFirstPage_ReturnsTrue()
    {
        // Arrange
        var results = new SearchResults
        {
            Results = Array.Empty<SearchResult>(),
            TotalCount = 100,
            PageSize = 20,
            PageNumber = 3,
            QueryTimeMs = 100
        };

        // Act & Assert
        results.HasPreviousPage.Should().BeTrue();
    }

    [Fact]
    public void SearchResult_WithAllProperties_IsValid()
    {
        // Arrange & Act
        var result = new SearchResult
        {
            MessageId = MessageId.NewId(),
            ChatId = ChatId.NewId(),
            ChatTitle = "Test Chat",
            Role = MessageRole.User,
            CreatedAt = DateTime.UtcNow,
            Snippet = "This is a <mark>test</mark> snippet",
            Score = 12.34,
            Matches = new[]
            {
                new MatchLocation { Field = "content", StartOffset = 10, Length = 4 }
            }
        };

        // Assert
        result.MessageId.Should().NotBe(default(MessageId));
        result.Snippet.Should().Contain("<mark>");
        result.Matches.Should().HaveCount(1);
    }

    [Fact]
    public void MatchLocation_StoresFieldAndOffsets()
    {
        // Arrange & Act
        var match = new MatchLocation
        {
            Field = "content",
            StartOffset = 42,
            Length = 10
        };

        // Assert
        match.Field.Should().Be("content");
        match.StartOffset.Should().Be(42);
        match.Length.Should().Be(10);
    }
}
