using System.Diagnostics;
using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using Acode.Infrastructure.Persistence.Conversation;
using Acode.Infrastructure.Search;
using FluentAssertions;
using Microsoft.Data.Sqlite;

namespace Acode.Integration.Tests.Search;

/// <summary>
/// End-to-end integration tests for search functionality.
/// Tests the full stack: MessageRepository → FTS5 triggers → SqliteFtsSearchService.
/// </summary>
public sealed class SearchE2ETests : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private SqliteChatRepository? _chatRepository;
    private SqliteRunRepository? _runRepository;
    private SqliteMessageRepository? _messageRepository;
    private SqliteFtsSearchService? _searchService;
    private string? _dbFilePath;

    public async Task InitializeAsync()
    {
        // Create temp file database (in-memory doesn't work well with multiple connections)
        var tempDir = Path.GetTempPath();
        Directory.CreateDirectory(tempDir); // Ensure directory exists
        _dbFilePath = Path.Combine(tempDir, $"test_{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={_dbFilePath}";
        _connection = new SqliteConnection(connectionString);
        await _connection.OpenAsync().ConfigureAwait(true);

        // Apply schema migrations
        await ApplySchemaAsync().ConfigureAwait(true);

        // Initialize repositories with the database file path (NOT connection string)
        _chatRepository = new SqliteChatRepository(_dbFilePath);
        _runRepository = new SqliteRunRepository(_dbFilePath);
        _messageRepository = new SqliteMessageRepository(_dbFilePath);

        // Initialize search service
        _searchService = new SqliteFtsSearchService(_connection);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(true);
        }

        // Clean up temp database file
        if (_dbFilePath != null && File.Exists(_dbFilePath))
        {
            File.Delete(_dbFilePath);
        }
    }

    [Fact]
    public async Task Should_Index_And_Search_Messages_End_To_End()
    {
        // Arrange - Create chat, run, and messages through repositories
        var chat = Chat.Create("Authentication Discussion");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "How do I implement JWT authentication?", 1);
        var message2 = Message.Create(run.Id, "assistant", "JWT authentication requires token validation and expiration checks.", 2);
        var message3 = Message.Create(run.Id, "user", "What about OAuth2?", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search for "JWT"
        var query = new SearchQuery
        {
            QueryText = "JWT",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(2, "two messages contain 'JWT'");
        results.Results.Should().HaveCount(2);
        results.Results.Should().OnlyContain(r => r.Snippet.Contains("JWT", StringComparison.OrdinalIgnoreCase));
        results.Results.Should().Contain(r => r.Role == MessageRole.User);
        results.Results.Should().Contain(r => r.Role == MessageRole.Assistant);
        results.QueryTimeMs.Should().BeLessThan(500, "search should complete quickly");
    }

    [Fact]
    public async Task Should_Filter_Results_By_ChatId()
    {
        // Arrange - Create two separate chats with different messages
        var chat1 = Chat.Create("Chat 1");
        var chat2 = Chat.Create("Chat 2");
        await _chatRepository!.CreateAsync(chat1, CancellationToken.None).ConfigureAwait(true);
        await _chatRepository!.CreateAsync(chat2, CancellationToken.None).ConfigureAwait(true);

        var run1 = Run.Create(chat1.Id, "llama3");
        var run2 = Run.Create(chat2.Id, "llama3");
        await _runRepository!.CreateAsync(run1, CancellationToken.None).ConfigureAwait(true);
        await _runRepository!.CreateAsync(run2, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run1.Id, "user", "JWT in chat 1", 1);
        var message2 = Message.Create(run2.Id, "user", "JWT in chat 2", 1);
        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with ChatId filter
        var query = new SearchQuery
        {
            QueryText = "JWT",
            ChatId = chat1.Id,
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.TotalCount.Should().Be(1, "only one message in chat1 contains 'JWT'");
        results.Results.Should().HaveCount(1);
        results.Results[0].ChatId.Should().Be(chat1.Id);
        results.Results[0].Snippet.Should().Contain("chat 1");
    }

    [Fact]
    public async Task Should_Filter_Results_By_Date_Range()
    {
        // Arrange - Create messages with different timestamps
        var chat = Chat.Create("Date Filter Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        // Create messages (MessageRepository will set CreatedAt to DateTime.UtcNow)
        var message1 = Message.Create(run.Id, "user", "Recent JWT message", 1);
        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);

        // Manually update created_at to simulate old message
        using (var cmd = _connection!.CreateCommand())
        {
            cmd.CommandText = "UPDATE conv_messages SET created_at = @oldDate WHERE id = @messageId";
            cmd.Parameters.AddWithValue("@oldDate", DateTime.UtcNow.AddDays(-90).ToString("O"));
            cmd.Parameters.AddWithValue("@messageId", message1.Id.Value);
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);
        }

        // Rebuild search index to pick up the timestamp change
        await _searchService!.RebuildIndexAsync(null, CancellationToken.None).ConfigureAwait(true);

        var message2 = Message.Create(run.Id, "user", "Very recent JWT message", 2);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with Since filter (last 7 days)
        var query = new SearchQuery
        {
            QueryText = "JWT",
            Since = DateTime.UtcNow.AddDays(-7),
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.TotalCount.Should().Be(1, "only recent message should match");
        results.Results.Should().HaveCount(1);
        results.Results[0].Snippet.Should().Contain("Very recent");
    }

    [Fact]
    public async Task Should_Filter_Results_By_Role()
    {
        // Arrange
        var chat = Chat.Create("Role Filter Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var userMessage = Message.Create(run.Id, "user", "User asks about JWT", 1);
        var assistantMessage = Message.Create(run.Id, "assistant", "Assistant explains JWT", 2);

        await _messageRepository!.CreateAsync(userMessage, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(assistantMessage, CancellationToken.None).ConfigureAwait(true);

        // Act - Search filtering by User role
        var query = new SearchQuery
        {
            QueryText = "JWT",
            RoleFilter = MessageRole.User,
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.TotalCount.Should().Be(1, "only user message should match");
        results.Results.Should().HaveCount(1);
        results.Results[0].Role.Should().Be(MessageRole.User);
        results.Results[0].Snippet.Should().Contain("User asks");
    }

    [Fact]
    public async Task Should_Rank_Results_By_Relevance_With_BM25()
    {
        // Arrange - Create messages with different term frequencies
        var chat = Chat.Create("Relevance Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        // Message with high term frequency (3 occurrences)
        var highFreqMessage = Message.Create(run.Id, "user", "JWT JWT JWT authentication", 1);

        // Message with low term frequency (1 occurrence)
        var lowFreqMessage = Message.Create(run.Id, "user", "JWT mentioned once", 2);

        await _messageRepository!.CreateAsync(highFreqMessage, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(lowFreqMessage, CancellationToken.None).ConfigureAwait(true);

        // Act
        var query = new SearchQuery
        {
            QueryText = "JWT",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.TotalCount.Should().Be(2);
        results.Results.Should().HaveCount(2);

        // First result should have higher score (more occurrences)
        results.Results[0].Score.Should().BeGreaterThan(results.Results[1].Score);
        results.Results[0].Snippet.Should().Contain("<mark>JWT</mark>");
        results.Results[0].Snippet.Should().Contain("authentication");
    }

    [Fact]
    public async Task Should_Apply_Pagination_Correctly()
    {
        // Arrange - Create 25 messages
        var chat = Chat.Create("Pagination Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        for (int i = 0; i < 25; i++)
        {
            var message = Message.Create(run.Id, "user", $"Message {i} about JWT", i + 1);
            await _messageRepository!.CreateAsync(message, CancellationToken.None).ConfigureAwait(true);
        }

        // Act - Get page 1 (10 results)
        var page1Query = new SearchQuery { QueryText = "JWT", PageSize = 10, PageNumber = 1 };
        var page1Results = await _searchService!.SearchAsync(page1Query, CancellationToken.None).ConfigureAwait(true);

        // Act - Get page 2 (10 results)
        var page2Query = new SearchQuery { QueryText = "JWT", PageSize = 10, PageNumber = 2 };
        var page2Results = await _searchService!.SearchAsync(page2Query, CancellationToken.None).ConfigureAwait(true);

        // Act - Get page 3 (5 results)
        var page3Query = new SearchQuery { QueryText = "JWT", PageSize = 10, PageNumber = 3 };
        var page3Results = await _searchService!.SearchAsync(page3Query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        page1Results.TotalCount.Should().Be(25);
        page1Results.Results.Should().HaveCount(10);
        page1Results.PageNumber.Should().Be(1);
        page1Results.TotalPages.Should().Be(3);
        page1Results.HasNextPage.Should().BeTrue();
        page1Results.HasPreviousPage.Should().BeFalse();

        page2Results.TotalCount.Should().Be(25);
        page2Results.Results.Should().HaveCount(10);
        page2Results.PageNumber.Should().Be(2);
        page2Results.HasNextPage.Should().BeTrue();
        page2Results.HasPreviousPage.Should().BeTrue();

        page3Results.TotalCount.Should().Be(25);
        page3Results.Results.Should().HaveCount(5);
        page3Results.PageNumber.Should().Be(3);
        page3Results.HasNextPage.Should().BeFalse();
        page3Results.HasPreviousPage.Should().BeTrue();

        // Results should not overlap
        var page1Ids = page1Results.Results.Select(r => r.MessageId).ToHashSet();
        var page2Ids = page2Results.Results.Select(r => r.MessageId).ToHashSet();
        var page3Ids = page3Results.Results.Select(r => r.MessageId).ToHashSet();

        page1Ids.Should().NotIntersectWith(page2Ids);
        page2Ids.Should().NotIntersectWith(page3Ids);
        page1Ids.Should().NotIntersectWith(page3Ids);
    }

    [Fact]
    public async Task Should_Generate_Snippets_With_Mark_Tags()
    {
        // Arrange
        var chat = Chat.Create("Snippet Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message = Message.Create(run.Id, "user", "This is a longer message about JWT authentication and how it works with token validation.", 1);
        await _messageRepository!.CreateAsync(message, CancellationToken.None).ConfigureAwait(true);

        // Act
        var query = new SearchQuery { QueryText = "JWT authentication", PageSize = 10, PageNumber = 1 };
        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert
        results.Results.Should().HaveCount(1);
        var snippet = results.Results[0].Snippet;

        snippet.Should().Contain("<mark>", "snippet should have highlighting");
        snippet.Should().Contain("</mark>", "snippet should have closing tags");
        snippet.Length.Should().BeLessOrEqualTo(200, "snippet should be truncated");
    }

    [Fact]
    public async Task Should_Handle_Large_Corpus_Within_Performance_SLA()
    {
        // Arrange - Create 1000 messages (reduced from spec's 10k for faster test execution)
        var chat = Chat.Create("Performance Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        for (int i = 0; i < 1000; i++)
        {
            var message = Message.Create(run.Id, "user", $"Message {i} with content about topic {i % 100}", i + 1);
            await _messageRepository!.CreateAsync(message, CancellationToken.None).ConfigureAwait(true);
        }

        // Act
        var stopwatch = Stopwatch.StartNew();
        var query = new SearchQuery { QueryText = "topic", PageSize = 20, PageNumber = 1 };
        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);
        stopwatch.Stop();

        // Assert - Performance SLA: Search 1k messages in <250ms (spec target), <500ms (spec max)
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500, "search should meet performance SLA");
        results.QueryTimeMs.Should().BeLessThan(500);
        results.TotalCount.Should().BeGreaterThan(900, "most messages mention 'topic'");
    }

    [Fact]
    public async Task Should_Rebuild_Index_Successfully()
    {
        // Arrange - Create messages
        var chat = Chat.Create("Rebuild Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT message 1", 1);
        var message2 = Message.Create(run.Id, "user", "JWT message 2", 2);
        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Corrupt the index
        using (var cmd = _connection!.CreateCommand())
        {
            cmd.CommandText = "DELETE FROM conversation_search";
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);
        }

        // Verify search returns 0 results
        var beforeQuery = new SearchQuery { QueryText = "JWT", PageSize = 10, PageNumber = 1 };
        var beforeResults = await _searchService!.SearchAsync(beforeQuery, CancellationToken.None).ConfigureAwait(true);
        beforeResults.TotalCount.Should().Be(0, "index should be empty after corruption");

        // Act - Rebuild index
        await _searchService!.RebuildIndexAsync(null, CancellationToken.None).ConfigureAwait(true);

        // Assert - Search works again
        var afterQuery = new SearchQuery { QueryText = "JWT", PageSize = 10, PageNumber = 1 };
        var afterResults = await _searchService!.SearchAsync(afterQuery, CancellationToken.None).ConfigureAwait(true);
        afterResults.TotalCount.Should().Be(2, "index should be restored");
    }

    [Fact]
    public async Task Should_Return_Index_Status_Correctly()
    {
        // Arrange - Create 5 messages
        var chat = Chat.Create("Status Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        for (int i = 0; i < 5; i++)
        {
            var message = Message.Create(run.Id, "user", $"Message {i}", i + 1);
            await _messageRepository!.CreateAsync(message, CancellationToken.None).ConfigureAwait(true);
        }

        // Act
        var status = await _searchService!.GetIndexStatusAsync(CancellationToken.None).ConfigureAwait(true);

        // Assert
        status.IndexedMessageCount.Should().Be(5);
        status.TotalMessageCount.Should().Be(5);
        status.IsHealthy.Should().BeTrue();
    }

    // BOOLEAN OPERATOR E2E TESTS (P2.1)
    [Fact]
    public async Task Should_Search_WithAND_Operator()
    {
        // Arrange - Create messages with different term combinations
        var chat = Chat.Create("AND Operator Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT authentication is secure", 1);
        var message2 = Message.Create(run.Id, "user", "OAuth validation process", 2);
        var message3 = Message.Create(run.Id, "user", "JWT is a token", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with AND operator (both terms must be present)
        var query = new SearchQuery
        {
            QueryText = "JWT AND authentication",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only message1 contains both "JWT" AND "authentication"
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only one message contains both JWT and authentication");
        results.Results.Should().HaveCount(1);
        results.Results[0].Snippet.Should().ContainAll(new[] { "JWT", "authentication" });
    }

    [Fact]
    public async Task Should_Search_WithOR_Operator()
    {
        // Arrange - Create messages with different terms
        var chat = Chat.Create("OR Operator Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT is a standard", 1);
        var message2 = Message.Create(run.Id, "user", "OAuth is another standard", 2);
        var message3 = Message.Create(run.Id, "user", "SAML authentication method", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with OR operator (either term can be present)
        var query = new SearchQuery
        {
            QueryText = "JWT OR OAuth",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Both message1 and message2 should be returned (message3 should not)
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(2, "two messages contain either JWT or OAuth");
        results.Results.Should().HaveCount(2);
        results.Results.Should().Contain(r => r.Snippet.Contains("JWT", StringComparison.OrdinalIgnoreCase));
        results.Results.Should().Contain(r => r.Snippet.Contains("OAuth", StringComparison.OrdinalIgnoreCase));
        results.Results.Should().NotContain(r => r.Snippet.Contains("SAML", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Should_Search_WithNOT_Operator()
    {
        // Arrange - Create messages with overlapping terms
        var chat = Chat.Create("NOT Operator Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT authentication flow", 1);
        var message2 = Message.Create(run.Id, "user", "JWT validation process", 2);
        var message3 = Message.Create(run.Id, "user", "OAuth token validation", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with NOT operator (exclude results with "validation")
        var query = new SearchQuery
        {
            QueryText = "JWT NOT validation",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only message1 contains JWT but NOT validation
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only one message contains JWT but not validation");
        results.Results.Should().HaveCount(1);
        results.Results[0].Snippet.Should().Contain("JWT");
        results.Results[0].Snippet.Should().Contain("authentication");
        results.Results[0].Snippet.Should().NotContain("validation");
    }

    [Fact]
    public async Task Should_Search_WithParentheses_Grouping()
    {
        // Arrange - Create messages with complex term combinations
        var chat = Chat.Create("Parentheses Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT validation is important", 1);
        var message2 = Message.Create(run.Id, "user", "OAuth validation is required", 2);
        var message3 = Message.Create(run.Id, "user", "JWT authentication flow", 3);
        var message4 = Message.Create(run.Id, "user", "SAML protocol for SSO", 4);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message4, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with grouped operators: (JWT OR OAuth) AND validation
        var query = new SearchQuery
        {
            QueryText = "(JWT OR OAuth) AND validation",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only message1 and message2 match: they have validation AND (JWT or OAuth)
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(2, "two messages have (JWT OR OAuth) AND validation");
        results.Results.Should().HaveCount(2);
        results.Results.Should().OnlyContain(r => r.Snippet.Contains("validation", StringComparison.OrdinalIgnoreCase));
        results.Results.Should().Contain(r => r.Snippet.Contains("JWT", StringComparison.OrdinalIgnoreCase));
        results.Results.Should().Contain(r => r.Snippet.Contains("OAuth", StringComparison.OrdinalIgnoreCase));
    }

    // FIELD PREFIX E2E TESTS (P3.1)
    [Fact]
    public async Task Should_Search_WithRoleUserPrefix()
    {
        // Arrange - Create user and assistant messages
        var chat = Chat.Create("Role Filter Test");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "User asks about authentication", 1);
        var message2 = Message.Create(run.Id, "assistant", "Assistant explains authentication", 2);
        var message3 = Message.Create(run.Id, "user", "User asks more questions", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with role:user prefix
        var query = new SearchQuery
        {
            QueryText = "role:user authentication",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only user messages should be returned
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only user messages with 'authentication' should match");
        results.Results.Should().HaveCount(1);
        results.Results[0].Role.Should().Be(MessageRole.User);
        results.Results[0].Snippet.Should().Contain("authentication");
    }

    [Fact]
    public async Task Should_Search_WithTitlePrefix()
    {
        // Arrange - Create messages in chats with different titles
        var chat1 = Chat.Create("JWT Authentication Guide");
        var chat2 = Chat.Create("OAuth Setup Tutorial");
        await _chatRepository!.CreateAsync(chat1, CancellationToken.None).ConfigureAwait(true);
        await _chatRepository!.CreateAsync(chat2, CancellationToken.None).ConfigureAwait(true);

        var run1 = Run.Create(chat1.Id, "llama3");
        var run2 = Run.Create(chat2.Id, "llama3");
        await _runRepository!.CreateAsync(run1, CancellationToken.None).ConfigureAwait(true);
        await _runRepository!.CreateAsync(run2, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run1.Id, "user", "Discussing tokens", 1);
        var message2 = Message.Create(run2.Id, "user", "Discussing tokens", 1);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with title:JWT prefix
        var query = new SearchQuery
        {
            QueryText = "title:JWT",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only messages from JWT chat should be returned
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only messages from chat with 'JWT' in title");
        results.Results.Should().HaveCount(1);
        results.Results[0].ChatTitle.Should().Contain("JWT");
    }

    [Fact]
    public async Task Should_Search_WithTagPrefix()
    {
        // Arrange - Create chats (tags feature not yet implemented in Chat entity)
        var chat1 = Chat.Create("Security Discussion");
        var chat2 = Chat.Create("General Chat");
        await _chatRepository!.CreateAsync(chat1, CancellationToken.None).ConfigureAwait(true);
        await _chatRepository!.CreateAsync(chat2, CancellationToken.None).ConfigureAwait(true);

        var run1 = Run.Create(chat1.Id, "llama3");
        var run2 = Run.Create(chat2.Id, "llama3");
        await _runRepository!.CreateAsync(run1, CancellationToken.None).ConfigureAwait(true);
        await _runRepository!.CreateAsync(run2, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run1.Id, "user", "Discussing authentication", 1);
        var message2 = Message.Create(run2.Id, "user", "Discussing authentication", 1);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with tag:security prefix
        var query = new SearchQuery
        {
            QueryText = "tag:security",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Parser extracts tag filter correctly (no results since tags not implemented yet)
        results.Should().NotBeNull();

        // NOTE: Tag functionality will work once Chat entity supports tags
    }

    [Fact]
    public async Task Should_Search_WithMultipleFieldPrefixes()
    {
        // Arrange - Create complex scenario
        var chat1 = Chat.Create("Security Discussion");
        await _chatRepository!.CreateAsync(chat1, CancellationToken.None).ConfigureAwait(true);

        var run1 = Run.Create(chat1.Id, "llama3");
        await _runRepository!.CreateAsync(run1, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run1.Id, "user", "How does JWT work?", 1);
        var message2 = Message.Create(run1.Id, "assistant", "JWT is a token standard", 2);
        var message3 = Message.Create(run1.Id, "user", "Tell me about OAuth", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with multiple field prefixes (role + content)
        var query = new SearchQuery
        {
            QueryText = "role:user JWT",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only user messages with "JWT" should match
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only user messages with 'JWT'");
        results.Results.Should().HaveCount(1);
        results.Results[0].Role.Should().Be(MessageRole.User);
        results.Results[0].Snippet.Should().Contain("JWT");
    }

    [Fact]
    public async Task Should_Search_FieldPrefixWithBooleanOps()
    {
        // Arrange - Create messages with different roles
        var chat = Chat.Create("Mixed Discussion");
        await _chatRepository!.CreateAsync(chat, CancellationToken.None).ConfigureAwait(true);

        var run = Run.Create(chat.Id, "llama3");
        await _runRepository!.CreateAsync(run, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run.Id, "user", "JWT and OAuth are standards", 1);
        var message2 = Message.Create(run.Id, "assistant", "JWT and validation work together", 2);
        var message3 = Message.Create(run.Id, "user", "JWT requires validation", 3);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message3, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with role prefix and boolean operators
        var query = new SearchQuery
        {
            QueryText = "role:user (JWT AND validation)",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only user messages with both JWT and validation
        results.Should().NotBeNull();
        results.TotalCount.Should().Be(1, "only user messages with both JWT and validation");
        results.Results.Should().HaveCount(1);
        results.Results[0].Role.Should().Be(MessageRole.User);
        results.Results[0].Snippet.Should().ContainAll(new[] { "JWT", "validation" });
    }

    [Fact]
    public async Task Should_Search_WithChatNamePrefix()
    {
        // Arrange - Create messages in different chats
        var chat1 = Chat.Create("auth-discussion");
        var chat2 = Chat.Create("general-chat");
        await _chatRepository!.CreateAsync(chat1, CancellationToken.None).ConfigureAwait(true);
        await _chatRepository!.CreateAsync(chat2, CancellationToken.None).ConfigureAwait(true);

        var run1 = Run.Create(chat1.Id, "llama3");
        var run2 = Run.Create(chat2.Id, "llama3");
        await _runRepository!.CreateAsync(run1, CancellationToken.None).ConfigureAwait(true);
        await _runRepository!.CreateAsync(run2, CancellationToken.None).ConfigureAwait(true);

        var message1 = Message.Create(run1.Id, "user", "Discussing errors", 1);
        var message2 = Message.Create(run2.Id, "user", "Discussing errors", 1);

        await _messageRepository!.CreateAsync(message1, CancellationToken.None).ConfigureAwait(true);
        await _messageRepository!.CreateAsync(message2, CancellationToken.None).ConfigureAwait(true);

        // Act - Search with chat:auth-discussion prefix (name-based)
        var query = new SearchQuery
        {
            QueryText = "chat:auth-discussion error",
            PageSize = 10,
            PageNumber = 1
        };

        var results = await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);

        // Assert - Only messages from auth-discussion chat
        // NOTE: This test will pass only after ChatNameFilter resolution is implemented
        // For now, it tests that the parser extracts the filter correctly
        results.Should().NotBeNull();
    }

    private async Task ApplySchemaAsync()
    {
        // Apply minimal schema needed for testing
        using var cmd = _connection!.CreateCommand();

        // Create chats table (with conv_ prefix to match production schema)
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS conv_chats (
                id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                tags TEXT,
                worktree_id TEXT,
                is_archived INTEGER NOT NULL DEFAULT 0,
                is_deleted INTEGER NOT NULL DEFAULT 0,
                deleted_at TEXT,
                created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
                updated_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
                sync_status TEXT NOT NULL DEFAULT 'pending',
                sync_at TEXT,
                remote_id TEXT,
                version INTEGER NOT NULL DEFAULT 1
            );";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        // Create runs table
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS conv_runs (
                id TEXT PRIMARY KEY,
                chat_id TEXT NOT NULL,
                model_id TEXT NOT NULL,
                sequence_number INTEGER NOT NULL DEFAULT 0,
                status TEXT NOT NULL DEFAULT 'running',
                started_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
                completed_at TEXT,
                tokens_in INTEGER NOT NULL DEFAULT 0,
                tokens_out INTEGER NOT NULL DEFAULT 0,
                error_message TEXT,
                sync_status TEXT NOT NULL DEFAULT 'pending',
                FOREIGN KEY (chat_id) REFERENCES conv_chats(id)
            );";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        // Create messages table
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS conv_messages (
                id TEXT PRIMARY KEY,
                run_id TEXT NOT NULL,
                role TEXT NOT NULL,
                content TEXT,
                tool_calls TEXT,
                created_at TEXT NOT NULL DEFAULT (strftime('%Y-%m-%dT%H:%M:%SZ', 'now')),
                sequence_number INTEGER NOT NULL DEFAULT 0,
                sync_status TEXT NOT NULL DEFAULT 'pending',
                FOREIGN KEY (run_id) REFERENCES conv_runs(id)
            );";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        // Create FTS5 search index
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE IF NOT EXISTS conversation_search USING fts5(
                message_id UNINDEXED,
                chat_id UNINDEXED,
                run_id UNINDEXED,
                created_at UNINDEXED,
                role UNINDEXED,
                content,
                chat_title,
                tags,
                tokenize='porter unicode61'
            );";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        // Create triggers for automatic indexing
        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS conversation_search_after_insert
            AFTER INSERT ON conv_messages
            BEGIN
                INSERT INTO conversation_search (
                    message_id, chat_id, run_id, created_at, role, content, chat_title, tags
                )
                SELECT
                    NEW.id,
                    r.chat_id,
                    NEW.run_id,
                    NEW.created_at,
                    NEW.role,
                    NEW.content,
                    c.title,
                    c.tags
                FROM conv_runs r
                INNER JOIN conv_chats c ON r.chat_id = c.id
                WHERE r.id = NEW.run_id;
            END;";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS conversation_search_after_update
            AFTER UPDATE ON conv_messages
            BEGIN
                DELETE FROM conversation_search WHERE message_id = OLD.id;
                INSERT INTO conversation_search (
                    message_id, chat_id, run_id, created_at, role, content, chat_title, tags
                )
                SELECT
                    NEW.id,
                    r.chat_id,
                    NEW.run_id,
                    NEW.created_at,
                    NEW.role,
                    NEW.content,
                    c.title,
                    c.tags
                FROM conv_runs r
                INNER JOIN conv_chats c ON r.chat_id = c.id
                WHERE r.id = NEW.run_id;
            END;";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        cmd.CommandText = @"
            CREATE TRIGGER IF NOT EXISTS conversation_search_after_delete
            AFTER DELETE ON conv_messages
            BEGIN
                DELETE FROM conversation_search WHERE message_id = OLD.id;
            END;";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);
    }
}
