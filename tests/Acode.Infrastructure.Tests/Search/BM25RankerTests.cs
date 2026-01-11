using Acode.Domain.Configuration;
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
    public void CalculateScore_WithVeryRecentMessage_Applies1_5xBoost()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = "test content";
        var veryRecentDate = DateTime.UtcNow.AddHours(-12); // <24 hours = 1.5x boost
        var olderDate = DateTime.UtcNow.AddDays(-10); // >7 days = 1.0x (no boost)

        // Act
        var veryRecentScore = ranker.CalculateScore(query, content, veryRecentDate);
        var olderScore = ranker.CalculateScore(query, content, olderDate);

        // Assert
        veryRecentScore.Should().BeGreaterThan(olderScore);
        (veryRecentScore / olderScore).Should().BeApproximately(1.5, 0.1);
    }

    [Fact]
    public void CalculateScore_WithWeekOldMessage_Applies1_2xBoost()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = "test content";
        var weekOldDate = DateTime.UtcNow.AddDays(-5); // ≤7 days = 1.2x boost
        var olderDate = DateTime.UtcNow.AddDays(-30); // >7 days = 1.0x (no boost)

        // Act
        var weekOldScore = ranker.CalculateScore(query, content, weekOldDate);
        var olderScore = ranker.CalculateScore(query, content, olderDate);

        // Assert
        weekOldScore.Should().BeGreaterThan(olderScore);
        (weekOldScore / olderScore).Should().BeApproximately(1.2, 0.1);
    }

    [Fact]
    public void CalculateScore_WithOldMessage_AppliesNoBoostOrPenalty()
    {
        // Arrange
        var ranker = new BM25Ranker();
        var query = "test";
        var content = "test content";
        var oldDate = DateTime.UtcNow.AddDays(-60); // >7 days = 1.0x (no boost, no penalty)
        var veryOldDate = DateTime.UtcNow.AddDays(-365); // >7 days = 1.0x (no penalty)

        // Act
        var oldScore = ranker.CalculateScore(query, content, oldDate);
        var veryOldScore = ranker.CalculateScore(query, content, veryOldDate);

        // Assert - Both should have same boost (1.0x)
        (oldScore / veryOldScore).Should().BeApproximately(1.0, 0.01);
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

    [Fact]
    public void CalculateScore_WithCustomRecencyBoostSettings_UsesConfiguredValues()
    {
        // Arrange - AC-054: Custom boost multipliers
        var customSettings = new SearchSettings
        {
            RecencyBoostEnabled = true,
            RecencyBoost24Hours = 2.0,  // Custom: 2.0x boost (vs default 1.5x)
            RecencyBoost7Days = 1.5,    // Custom: 1.5x boost (vs default 1.2x)
            RecencyBoostDefault = 1.0
        };
        var ranker = new BM25Ranker(customSettings);
        var query = "test";
        var content = "test content";
        var veryRecentDate = DateTime.UtcNow.AddHours(-12); // <24 hours
        var weekOldDate = DateTime.UtcNow.AddDays(-5); // ≤7 days
        var oldDate = DateTime.UtcNow.AddDays(-30); // >7 days

        // Act
        var veryRecentScore = ranker.CalculateScore(query, content, veryRecentDate);
        var weekOldScore = ranker.CalculateScore(query, content, weekOldDate);
        var oldScore = ranker.CalculateScore(query, content, oldDate);

        // Assert - Custom multipliers applied
        (veryRecentScore / oldScore).Should().BeApproximately(2.0, 0.1);
        (weekOldScore / oldScore).Should().BeApproximately(1.5, 0.1);
    }

    [Fact]
    public void CalculateScore_WithRecencyBoostDisabled_ReturnsBaseScoreWithoutBoost()
    {
        // Arrange - AC-055: Recency boost can be disabled
        var disabledSettings = new SearchSettings
        {
            RecencyBoostEnabled = false,
            RecencyBoost24Hours = 1.5,
            RecencyBoost7Days = 1.2,
            RecencyBoostDefault = 1.0
        };
        var ranker = new BM25Ranker(disabledSettings);
        var query = "test";
        var content = "test content";
        var veryRecentDate = DateTime.UtcNow.AddHours(-12); // <24 hours
        var weekOldDate = DateTime.UtcNow.AddDays(-5); // ≤7 days
        var oldDate = DateTime.UtcNow.AddDays(-30); // >7 days

        // Act
        var veryRecentScore = ranker.CalculateScore(query, content, veryRecentDate);
        var weekOldScore = ranker.CalculateScore(query, content, weekOldDate);
        var oldScore = ranker.CalculateScore(query, content, oldDate);

        // Assert - All scores identical (no recency boost applied)
        veryRecentScore.Should().BeApproximately(weekOldScore, 0.01);
        weekOldScore.Should().BeApproximately(oldScore, 0.01);
    }

    [Fact]
    public void CalculateScore_WithTitleMatch_Applies2xBoost()
    {
        // Arrange - AC-048: Title matches weighted 2x over body matches
        var ranker = new BM25Ranker();
        var query = "authentication";
        var createdAt = DateTime.UtcNow.AddDays(-10);

        // Same term in title vs body
        var title = "authentication guide";
        var contentWithoutTerm = "This is a guide to securing your application";

        // Act - Calculate score with title match
        var scoreWithTitleMatch = ranker.CalculateScore(query, title, contentWithoutTerm, createdAt);

        // Calculate score with same term only in body
        var titleWithoutTerm = "Security guide";
        var contentWithTerm = "authentication guide for your application";
        var scoreWithBodyMatch = ranker.CalculateScore(query, titleWithoutTerm, contentWithTerm, createdAt);

        // Assert - Title match should be approximately 2x body match
        (scoreWithTitleMatch / scoreWithBodyMatch).Should().BeApproximately(2.0, 0.2);
    }

    [Fact]
    public void CalculateScore_WithBothTitleAndBodyMatch_CombinesScores()
    {
        // Arrange - AC-048: Title and body matches should combine
        var ranker = new BM25Ranker();
        var query = "authentication";
        var createdAt = DateTime.UtcNow.AddDays(-10);

        // Both title and body have the term
        var title = "authentication guide";
        var content = "This authentication system is secure";

        // Only body has the term
        var titleWithoutTerm = "Security guide";
        var contentWithTerm = "This authentication system is secure";

        // Act
        var scoreWithBoth = ranker.CalculateScore(query, title, content, createdAt);
        var scoreWithBodyOnly = ranker.CalculateScore(query, titleWithoutTerm, contentWithTerm, createdAt);

        // Assert - Combined score should be higher than body-only
        scoreWithBoth.Should().BeGreaterThan(scoreWithBodyOnly);
    }
}
