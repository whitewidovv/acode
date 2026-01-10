using Acode.Domain.Search;
using Acode.Infrastructure.Search;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Xunit;

namespace Acode.Infrastructure.Tests.Search;

/// <summary>
/// Unit tests for SqliteFtsSearchService error handling and validation.
/// </summary>
public sealed class SqliteFtsSearchServiceTests : IAsyncLifetime
{
    private SqliteConnection? _connection;
    private SqliteFtsSearchService? _searchService;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync().ConfigureAwait(true);

        // Create minimal FTS5 table for testing
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE VIRTUAL TABLE conversation_search USING fts5(
                message_id UNINDEXED,
                chat_id UNINDEXED,
                run_id UNINDEXED,
                created_at UNINDEXED,
                role UNINDEXED,
                content,
                chat_title,
                tags,
                tokenize='porter unicode61'
            );
        ";
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(true);

        _searchService = new SqliteFtsSearchService(_connection);
    }

    public async Task DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task SearchAsync_WithInvalidBooleanSyntax_ThrowsSearchException()
    {
        // Arrange - Query with leading operator (invalid syntax)
        var query = new SearchQuery
        {
            QueryText = "AND invalid",
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SearchException>(async () =>
        {
            await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);
        }).ConfigureAwait(true);

        exception.ErrorCode.Should().Be(SearchErrorCodes.InvalidQuerySyntax);
        exception.Message.Should().Contain("Invalid query syntax");
        exception.Message.Should().Contain("cannot start with");
        exception.Remediation.Should().Contain("balanced parentheses");
        exception.Remediation.Should().Contain("valid operators");
    }

    [Fact]
    public async Task SearchAsync_WithTooManyOperators_ThrowsSearchException()
    {
        // Arrange - Query with 6 operators (max is 5)
        var query = new SearchQuery
        {
            QueryText = "a AND b OR c AND d NOT e OR f AND g",
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SearchException>(async () =>
        {
            await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);
        }).ConfigureAwait(true);

        exception.ErrorCode.Should().Be(SearchErrorCodes.InvalidQuerySyntax);
        exception.Message.Should().Contain("Invalid query syntax");
        exception.Message.Should().Contain("6 operators");
        exception.Message.Should().Contain("maximum 5");
        exception.Remediation.Should().Contain("operator limit");
    }

    [Fact]
    public async Task SearchAsync_WithUnbalancedParentheses_ThrowsSearchException()
    {
        // Arrange - Query with unbalanced parentheses
        var query = new SearchQuery
        {
            QueryText = "(JWT AND validation",
            PageSize = 10,
            PageNumber = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<SearchException>(async () =>
        {
            await _searchService!.SearchAsync(query, CancellationToken.None).ConfigureAwait(true);
        }).ConfigureAwait(true);

        exception.ErrorCode.Should().Be(SearchErrorCodes.InvalidQuerySyntax);
        exception.Message.Should().Contain("Invalid query syntax");
        exception.Message.Should().Contain("unbalanced");
        exception.Remediation.Should().Contain("balanced parentheses");
    }
}
