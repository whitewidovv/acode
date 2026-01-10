using System.Diagnostics;
using Acode.Application.Interfaces;
using Acode.Domain.Conversation;
using Acode.Domain.Models.Inference;
using Acode.Domain.Search;
using Microsoft.Data.Sqlite;

namespace Acode.Infrastructure.Search;

/// <summary>
/// SQLite FTS5-based implementation of ISearchService.
/// </summary>
public sealed class SqliteFtsSearchService : ISearchService
{
    private readonly SqliteConnection _connection;
    private readonly BM25Ranker _ranker;
    private readonly SnippetGenerator _snippetGenerator;
    private readonly SafeQueryParser _queryParser;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFtsSearchService"/> class.
    /// </summary>
    /// <param name="connection">The SQLite database connection.</param>
    public SqliteFtsSearchService(SqliteConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _ranker = new BM25Ranker();
        _snippetGenerator = new SnippetGenerator();
        _queryParser = new SafeQueryParser();
    }

    /// <inheritdoc />
    public async Task<SearchResults> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validationResult = query.Validate();
        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid query: {string.Join(", ", validationResult.Errors)}");
        }

        var stopwatch = Stopwatch.StartNew();

        // Parse and sanitize query
        var safeQuery = _queryParser.ParseQuery(query.QueryText);
        if (string.IsNullOrWhiteSpace(safeQuery))
        {
            return new SearchResults
            {
                Results = Array.Empty<SearchResult>(),
                TotalCount = 0,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize,
                QueryTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }

        // Build SQL query
        var sql = BuildSearchQuery(query, safeQuery, out var parameters);

        // Execute search
        var allResults = new List<SearchResult>();

        using (var cmd = _connection.CreateCommand())
        {
            cmd.CommandText = sql;
            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }

            using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var messageId = MessageId.From(reader.GetString(0));
                var chatId = ChatId.From(reader.GetString(1));
                var chatTitle = reader.GetString(2);
                var role = Enum.Parse<MessageRole>(reader.GetString(3));
                var createdAt = DateTime.Parse(reader.GetString(4));
                var content = reader.GetString(5);

                // Calculate BM25 score with recency boost
                var score = _ranker.CalculateScore(query.QueryText, content, createdAt);

                // Generate snippet with highlighted terms
                var snippet = _snippetGenerator.GenerateSnippet(content, query.QueryText);

                allResults.Add(new SearchResult
                {
                    MessageId = messageId,
                    ChatId = chatId,
                    ChatTitle = chatTitle,
                    Role = role,
                    CreatedAt = createdAt,
                    Snippet = snippet,
                    Score = score,
                    Matches = Array.Empty<MatchLocation>()
                });
            }
        }

        // Rank results
        var rankedResults = _ranker.RankResults(allResults);

        // Apply sorting if not relevance-based
        if (query.SortBy != SortOrder.Relevance)
        {
            rankedResults = query.SortBy == SortOrder.DateDescending
                ? rankedResults.OrderByDescending(r => r.CreatedAt).ToList()
                : rankedResults.OrderBy(r => r.CreatedAt).ToList();
        }

        // Get total count
        var totalCount = rankedResults.Count;

        // Apply pagination
        var skip = (query.PageNumber - 1) * query.PageSize;
        var pagedResults = rankedResults.Skip(skip).Take(query.PageSize).ToList();

        stopwatch.Stop();

        return new SearchResults
        {
            Results = pagedResults,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize,
            QueryTimeMs = stopwatch.Elapsed.TotalMilliseconds
        };
    }

    /// <inheritdoc />
    public async Task IndexMessageAsync(Message message, CancellationToken cancellationToken)
    {
        // Note: In production, this would typically be handled by triggers.
        // This method is for manual indexing if needed.
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateMessageIndexAsync(Message message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM conversation_search WHERE message_id = @messageId;
            INSERT INTO conversation_search (message_id, chat_id, run_id, created_at, role, content, chat_title, tags)
            SELECT @messageId, r.chat_id, @runId, @createdAt, @role, @content, c.title, c.tags
            FROM conv_runs r
            INNER JOIN conv_chats c ON r.chat_id = c.id
            WHERE r.id = @runId;
        ";

        cmd.Parameters.AddWithValue("@messageId", message.Id.Value);
        cmd.Parameters.AddWithValue("@runId", message.RunId.Value);
        cmd.Parameters.AddWithValue("@createdAt", message.CreatedAt.ToString("O"));
        cmd.Parameters.AddWithValue("@role", message.Role.ToString());
        cmd.Parameters.AddWithValue("@content", message.Content);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveFromIndexAsync(MessageId messageId, CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM conversation_search WHERE message_id = @messageId";
        cmd.Parameters.AddWithValue("@messageId", messageId.Value);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IndexStatus> GetIndexStatusAsync(CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            SELECT
                (SELECT COUNT(*) FROM conversation_search) AS indexed_count,
                (SELECT COUNT(*) FROM conv_messages) AS total_count
        ";

        using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var indexedCount = reader.GetInt32(0);
            var totalCount = reader.GetInt32(1);

            return new IndexStatus
            {
                IndexedMessageCount = indexedCount,
                TotalMessageCount = totalCount,
                IsHealthy = indexedCount == totalCount
            };
        }

        return new IndexStatus
        {
            IndexedMessageCount = 0,
            TotalMessageCount = 0,
            IsHealthy = true
        };
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(IProgress<int>? progress, CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            DELETE FROM conversation_search;

            INSERT INTO conversation_search (message_id, chat_id, run_id, created_at, role, content, chat_title, tags)
            SELECT m.id, r.chat_id, m.run_id, m.created_at, m.role, m.content, c.title, c.tags
            FROM conv_messages m
            INNER JOIN conv_runs r ON m.run_id = r.id
            INNER JOIN conv_chats c ON r.chat_id = c.id;
        ";

        var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        progress?.Report(rowsAffected);
    }

    /// <summary>
    /// Builds the FTS5 search query SQL with filters.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="safeQuery">The sanitized FTS5 query text.</param>
    /// <param name="parameters">Output parameters for the query.</param>
    /// <returns>The SQL query string.</returns>
    private static string BuildSearchQuery(
        SearchQuery query,
        string safeQuery,
        out List<(string Name, object Value)> parameters)
    {
        parameters = new List<(string Name, object Value)>();

        var sql = @"
            SELECT
                cs.message_id,
                cs.chat_id,
                cs.chat_title,
                cs.role,
                cs.created_at,
                m.content
            FROM conversation_search cs
            INNER JOIN conv_messages m ON cs.message_id = m.id
            WHERE conversation_search MATCH @query
        ";

        parameters.Add(("@query", safeQuery));

        // Apply ChatId filter
        if (query.ChatId.HasValue)
        {
            sql += " AND cs.chat_id = @chatId";
            parameters.Add(("@chatId", query.ChatId.Value.Value));
        }

        // Apply RoleFilter
        if (query.RoleFilter.HasValue)
        {
            sql += " AND cs.role = @role";
            parameters.Add(("@role", query.RoleFilter.Value.ToString()));
        }

        // Apply Since date filter
        if (query.Since.HasValue)
        {
            sql += " AND cs.created_at >= @since";
            parameters.Add(("@since", query.Since.Value.ToString("O")));
        }

        // Apply Until date filter
        if (query.Until.HasValue)
        {
            sql += " AND cs.created_at <= @until";
            parameters.Add(("@until", query.Until.Value.ToString("O")));
        }

        return sql;
    }
}
