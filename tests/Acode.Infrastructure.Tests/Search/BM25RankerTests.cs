using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using Acode.Infrastructure.Search;
using FluentAssertions;
using Xunit;

namespace Acode.Infrastructure.Tests.Search;

public class BM25RankerTests
{
    [Fact]
    public void CalculateScore_WithExactMatch_ReturnsHighScore()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test query";
        var content = "This is a test query example";
        var createdAt = DateTime.UtcNow.AddDays(-5); // Recent

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateScore_WithNoMatch_ReturnsZero()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test query";
        var content = "This content has no matching terms";
        var createdAt = DateTime.UtcNow;

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateScore_WithPartialMatch_ReturnsIntermediateScore()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test query example";
        var content = "This is a test example"; // Matches "test" and "example", not "query"
        var createdAt = DateTime.UtcNow;

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().BeGreaterThan(0);
        score.Should().BeLessThan(ranker.CalculateScore(query, "test query example", createdAt));
    }

    [Fact]
    public void CalculateScore_WithRecentMessage_AppliesRecencyBoost()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = "test content";
        var recentDate = DateTime.UtcNow.AddDays(-5); // <7 days = 1.5x boost
        var olderDate = DateTime.UtcNow.AddDays(-15); // 7-30 days = 1.0x boost

        // Act
        var recentScore = ranker.CalculateScore(query, content, recentDate);
        var olderScore = ranker.CalculateScore(query, content, olderDate);

        // Assert
        recentScore.Should().BeGreaterThan(olderScore);
        (recentScore / olderScore).Should().BeApproximately(1.5, 0.1);
    }

    [Fact]
    public void CalculateScore_WithOldMessage_AppliesPenalty()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = "test content";
        var normalDate = DateTime.UtcNow.AddDays(-15); // 7-30 days = 1.0x
        var oldDate = DateTime.UtcNow.AddDays(-60); // >30 days = 0.8x

        // Act
        var normalScore = ranker.CalculateScore(query, content, normalDate);
        var oldScore = ranker.CalculateScore(query, content, oldDate);

        // Assert
        oldScore.Should().BeLessThan(normalScore);
        (oldScore / normalScore).Should().BeApproximately(0.8, 0.1);
    }

    [Fact]
    public void CalculateScore_IsCaseInsensitive()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "TEST Query";
        var content = "test query example";
        var createdAt = DateTime.UtcNow;

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CalculateScore_WithEmptyQuery_ReturnsZero()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = string.Empty;
        var content = "test content";
        var createdAt = DateTime.UtcNow;

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateScore_WithEmptyContent_ReturnsZero()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = string.Empty;
        var createdAt = DateTime.UtcNow;

        // Act
        var score = ranker.CalculateScore(query, content, createdAt);

        // Assert
        score.Should().Be(0);
    }

    [Fact]
    public void CalculateScore_WithMultipleTerms_ConsidersAllTerms()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "alpha beta gamma";
        var contentWithAll = "alpha beta gamma delta";
        var contentWithTwo = "alpha beta epsilon";
        var createdAt = DateTime.UtcNow;

        // Act
        var scoreAll = ranker.CalculateScore(query, contentWithAll, createdAt);
        var scoreTwo = ranker.CalculateScore(query, contentWithTwo, createdAt);

        // Assert
        scoreAll.Should().BeGreaterThan(scoreTwo);
    }

    [Fact]
    public void CalculateScore_WithRepeatedTerms_HandlesCorrectly()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var contentOnce = "test content";
        var contentMultiple = "test test test content";
        var createdAt = DateTime.UtcNow;

        // Act
        var scoreOnce = ranker.CalculateScore(query, contentOnce, createdAt);
        var scoreMultiple = ranker.CalculateScore(query, contentMultiple, createdAt);

        // Assert
        scoreMultiple.Should().BeGreaterThan(scoreOnce);
    }

    [Fact]
    public void RankResults_SortsByScoreDescending()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var results = new List<SearchResult>
        {
            new SearchResult
            {
                MessageId = MessageId.NewId(),
                ChatId = ChatId.NewId(),
                ChatTitle = "Chat 1",
                Role = MessageRole.User,
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                Snippet = "some content",
                Score = 5.0,
                Matches = Array.Empty<MatchLocation>()
            },
            new SearchResult
            {
                MessageId = MessageId.NewId(),
                ChatId = ChatId.NewId(),
                ChatTitle = "Chat 2",
                Role = MessageRole.Assistant,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                Snippet = "test test test",
                Score = 15.0,
                Matches = Array.Empty<MatchLocation>()
            },
            new SearchResult
            {
                MessageId = MessageId.NewId(),
                ChatId = ChatId.NewId(),
                ChatTitle = "Chat 3",
                Role = MessageRole.User,
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                Snippet = "test content",
                Score = 10.0,
                Matches = Array.Empty<MatchLocation>()
            }
        };

        // Act
        var ranked = ranker.RankResults(results);

        // Assert
        ranked.Should().HaveCount(3);
        ranked[0].Score.Should().Be(15.0);
        ranked[1].Score.Should().Be(10.0);
        ranked[2].Score.Should().Be(5.0);
    }

    [Fact]
    public void RankResults_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var results = new List<SearchResult>();

        // Act
        var ranked = ranker.RankResults(results);

        // Assert
        ranked.Should().BeEmpty();
    }
}
