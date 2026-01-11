using System.Diagnostics;
using Acode.Application.Interfaces;
using Acode.Domain.Configuration;
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
    private readonly SearchSettings _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteFtsSearchService"/> class.
    /// </summary>
    /// <param name="connection">The SQLite database connection.</param>
    /// <param name="settings">Search settings for configurable behavior (optional, defaults applied if null).</param>
    public SqliteFtsSearchService(SqliteConnection connection, SearchSettings? settings = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _settings = settings ?? new SearchSettings();
        _ranker = new BM25Ranker(_settings);
        _snippetGenerator = new SnippetGenerator(_settings);
        _queryParser = new SafeQueryParser();
    }

    /// <inheritdoc />
    public async Task<SearchResults> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validationResult = query.Validate();
        if (!validationResult.IsValid)
        {
            // Check if error is date-related for specific error code (P4.3 - AC-123)
            var hasDateError = validationResult.Errors.Any(e =>
                e.Contains("date", StringComparison.OrdinalIgnoreCase) ||
                e.Contains("Since", StringComparison.Ordinal) ||
                e.Contains("Until", StringComparison.Ordinal));

            if (hasDateError)
            {
                throw new SearchException(
                    SearchErrorCodes.InvalidDateFilter,
                    $"Invalid date filter: {string.Join(", ", validationResult.Errors)}",
                    "Ensure dates are in the past and Since date is before Until date");
            }

            throw new ArgumentException($"Invalid query: {string.Join(", ", validationResult.Errors)}");
        }

        var stopwatch = Stopwatch.StartNew();

        // Create linked cancellation token with timeout (P4.2 - AC-122)
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(query.Timeout);

        // Parse and sanitize query
        var ftsQuery = _queryParser.ParseQuery(query.QueryText);
        if (!ftsQuery.IsValid)
        {
            // Check if error is role-related for specific error code (P4.4 - AC-124)
            var hasRoleError = ftsQuery.ErrorMessage != null &&
                (ftsQuery.ErrorMessage.Contains("role", StringComparison.OrdinalIgnoreCase) &&
                 ftsQuery.ErrorMessage.Contains("invalid", StringComparison.OrdinalIgnoreCase));

            if (hasRoleError)
            {
                throw new SearchException(
                    SearchErrorCodes.InvalidRoleFilter,
                    $"Invalid role filter: {ftsQuery.ErrorMessage}",
                    "Valid role values are: user, assistant, system, or tool");
            }

            throw new SearchException(
                SearchErrorCodes.InvalidQuerySyntax,
                $"Invalid query syntax: {ftsQuery.ErrorMessage}",
                "Check query for balanced parentheses, valid operators (AND/OR/NOT), and operator limit (max 5)");
        }

        // Merge field filters extracted from query text into SearchQuery
        // (role:, chat:, title:, tag: prefixes override CLI flags)
        if (ftsQuery.RoleFilter.HasValue)
        {
            query = query with { RoleFilter = ftsQuery.RoleFilter };
        }

        if (ftsQuery.ChatIdFilter.HasValue)
        {
            query = query with { ChatId = ChatId.From(ftsQuery.ChatIdFilter.Value.ToString()) };
        }

        // Note: ChatNameFilter would require IChatRepository.GetByNameAsync (not yet implemented)
        // Note: TitleTerms and TagFilter will be applied in BuildSearchQuery
        if (string.IsNullOrWhiteSpace(ftsQuery.Fts5Syntax) && ftsQuery.TitleTerms.Count == 0)
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

        // Build SQL query (includes title and tag filters from ftsQuery)
        var sql = BuildSearchQuery(query, ftsQuery, out var parameters);

        // Execute search with timeout enforcement (P4.2 - AC-122)
        var allResults = new List<SearchResult>();

        try
        {
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = sql;
                foreach (var (name, value) in parameters)
                {
                    cmd.Parameters.AddWithValue(name, value);
                }

                using var reader = await cmd.ExecuteReaderAsync(timeoutCts.Token).ConfigureAwait(false);
                while (await reader.ReadAsync(timeoutCts.Token).ConfigureAwait(false))
                {
                    var messageId = MessageId.From(reader.GetString(0));
                    var chatId = ChatId.From(reader.GetString(1));
                    var chatTitle = reader.GetString(2);
                    var role = Enum.Parse<MessageRole>(reader.GetString(3), ignoreCase: true);
                    var createdAt = DateTime.Parse(reader.GetString(4));
                    var content = reader.GetString(5);

                    // Calculate BM25 score with title boost (AC-048) and recency boost
                    var score = _ranker.CalculateScore(query.QueryText, chatTitle, content, createdAt);

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
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout occurred (not user cancellation)
            throw new SearchException(
                SearchErrorCodes.QueryTimeout,
                $"Search query exceeded timeout of {query.Timeout.TotalSeconds}s",
                "Simplify your query, add more specific terms, or use filters to narrow results");
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
        // P4.6: Check if conversation_search table exists (AC-126)
        using (var checkTableCmd = _connection.CreateCommand())
        {
            checkTableCmd.CommandText = @"
                SELECT COUNT(*)
                FROM sqlite_master
                WHERE type='table' AND name='conversation_search'
            ";

            var tableExists = Convert.ToInt32(await checkTableCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            if (tableExists == 0)
            {
                throw new SearchException(
                    SearchErrorCodes.IndexNotInitialized,
                    "Search index has not been initialized. The conversation_search table does not exist.",
                    "Run 'acode search rebuild' to initialize the search index");
            }
        }

        // P4.5: Check FTS5 index integrity (AC-125)
        using (var integrityCmd = _connection.CreateCommand())
        {
            integrityCmd.CommandText = "INSERT INTO conversation_search(conversation_search) VALUES('integrity-check')";

            try
            {
                await integrityCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            // SQLITE_CORRUPT = 11
            catch (SqliteException ex) when (ex.SqliteErrorCode == 11)
            {
                throw new SearchException(
                    SearchErrorCodes.IndexCorruption,
                    "Search index is corrupted and cannot be used.",
                    "Run 'acode search rebuild' to rebuild the corrupted index");
            }
        }

        // Get index statistics (P5.1 - AC-106, AC-107, AC-108)
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

            // Calculate index size in bytes
            long indexSizeBytes = 0;
            using (var sizeCmd = _connection.CreateCommand())
            {
                // Get approximate size from sqlite_master table
                sizeCmd.CommandText = @"
                    SELECT SUM(LENGTH(sql))
                    FROM sqlite_master
                    WHERE name LIKE 'conversation_search%'
                ";
                var sizeResult = await sizeCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (sizeResult != null && sizeResult != DBNull.Value)
                {
                    indexSizeBytes = Convert.ToInt64(sizeResult);
                }
            }

            // Get segment count (FTS5 specific)
            int segmentCount = 0;
            using (var segCmd = _connection.CreateCommand())
            {
                // FTS5 stores segment info in internal tables
                // This is an approximation - actual segment count requires FTS5 internals
                segCmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE name LIKE 'conversation_search%'";
                var segResult = await segCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (segResult != null && segResult != DBNull.Value)
                {
                    segmentCount = Math.Max(1, Convert.ToInt32(segResult) - 1);
                }
            }

            // Get last_optimized_at timestamp (P5.3 - AC-098)
            DateTime? lastOptimizedAt = null;
            await EnsureMetadataTableExistsAsync(cancellationToken).ConfigureAwait(false);
            using (var metaCmd = _connection.CreateCommand())
            {
                metaCmd.CommandText = "SELECT value FROM search_metadata WHERE key = 'last_optimized_at'";
                var metaResult = await metaCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                if (metaResult != null && metaResult != DBNull.Value)
                {
                    if (DateTime.TryParse(metaResult.ToString(), out var parsed))
                    {
                        lastOptimizedAt = parsed;
                    }
                }
            }

            return new IndexStatus
            {
                IndexedMessageCount = indexedCount,
                TotalMessageCount = totalCount,
                IsHealthy = indexedCount == totalCount,
                IndexSizeBytes = indexSizeBytes,
                SegmentCount = segmentCount,
                LastOptimizedAt = lastOptimizedAt
            };
        }

        return new IndexStatus
        {
            IndexedMessageCount = 0,
            TotalMessageCount = 0,
            IsHealthy = true,
            IndexSizeBytes = 0,
            SegmentCount = 0
        };
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(IProgress<int>? progress, CancellationToken cancellationToken)
    {
        // Get total message count first
        int totalMessages;
        using (var countCmd = _connection.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM conv_messages";
            totalMessages = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        // Delete existing index entries
        using (var deleteCmd = _connection.CreateCommand())
        {
            deleteCmd.CommandText = "DELETE FROM conversation_search";
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Process in batches of 100 for progress reporting
        const int batchSize = 100;
        int processedCount = 0;

        while (processedCount < totalMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO conversation_search (message_id, chat_id, run_id, created_at, role, content, chat_title, tags)
                SELECT m.id, r.chat_id, m.run_id, m.created_at, m.role, m.content, c.title, c.tags
                FROM conv_messages m
                INNER JOIN conv_runs r ON m.run_id = r.id
                INNER JOIN conv_chats c ON r.chat_id = c.id
                LIMIT @batchSize OFFSET @offset
            ";

            cmd.Parameters.AddWithValue("@batchSize", batchSize);
            cmd.Parameters.AddWithValue("@offset", processedCount);

            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            processedCount += rowsAffected;

            progress?.Report(processedCount);

            if (rowsAffected < batchSize)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public async Task RebuildIndexAsync(ChatId chatId, IProgress<int>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(chatId);

        // Get total message count for this chat
        int totalMessages;
        using (var countCmd = _connection.CreateCommand())
        {
            countCmd.CommandText = @"
                SELECT COUNT(*)
                FROM conv_messages m
                INNER JOIN conv_runs r ON m.run_id = r.id
                WHERE r.chat_id = @chatId
            ";
            countCmd.Parameters.AddWithValue("@chatId", chatId.Value);
            totalMessages = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
        }

        // Delete existing index entries for this chat
        using (var deleteCmd = _connection.CreateCommand())
        {
            deleteCmd.CommandText = "DELETE FROM conversation_search WHERE chat_id = @chatId";
            deleteCmd.Parameters.AddWithValue("@chatId", chatId.Value);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Process in batches of 100 for progress reporting
        const int batchSize = 100;
        int processedCount = 0;

        while (processedCount < totalMessages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using var cmd = _connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO conversation_search (message_id, chat_id, run_id, created_at, role, content, chat_title, tags)
                SELECT m.id, r.chat_id, m.run_id, m.created_at, m.role, m.content, c.title, c.tags
                FROM conv_messages m
                INNER JOIN conv_runs r ON m.run_id = r.id
                INNER JOIN conv_chats c ON r.chat_id = c.id
                WHERE r.chat_id = @chatId
                LIMIT @batchSize OFFSET @offset
            ";

            cmd.Parameters.AddWithValue("@chatId", chatId.Value);
            cmd.Parameters.AddWithValue("@batchSize", batchSize);
            cmd.Parameters.AddWithValue("@offset", processedCount);

            var rowsAffected = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            processedCount += rowsAffected;

            progress?.Report(processedCount);

            if (rowsAffected < batchSize)
            {
                break;
            }
        }
    }

    /// <inheritdoc />
    public async Task OptimizeIndexAsync(CancellationToken cancellationToken)
    {
        // Ensure metadata table exists
        await EnsureMetadataTableExistsAsync(cancellationToken).ConfigureAwait(false);

        // Run FTS5 optimize command to merge segments
        using (var optimizeCmd = _connection.CreateCommand())
        {
            optimizeCmd.CommandText = "INSERT INTO conversation_search(conversation_search) VALUES('optimize')";
            await optimizeCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // Update last_optimized_at timestamp
        using (var updateCmd = _connection.CreateCommand())
        {
            updateCmd.CommandText = @"
                INSERT OR REPLACE INTO search_metadata (key, value)
                VALUES ('last_optimized_at', @timestamp)
            ";
            updateCmd.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("O"));
            await updateCmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Builds the FTS5 search query SQL with filters.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <param name="ftsQuery">The parsed FTS query with field filters.</param>
    /// <param name="parameters">Output parameters for the query.</param>
    /// <returns>The SQL query string.</returns>
    private static string BuildSearchQuery(
        SearchQuery query,
        FtsQuery ftsQuery,
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
        ";

        // Build WHERE clause
        var whereConditions = new List<string>();

        // FTS5 query (if not empty)
        if (!string.IsNullOrWhiteSpace(ftsQuery.Fts5Syntax))
        {
            whereConditions.Add("conversation_search MATCH @query");
            parameters.Add(("@query", ftsQuery.Fts5Syntax));
        }

        // Apply ChatId filter
        if (query.ChatId.HasValue)
        {
            whereConditions.Add("cs.chat_id = @chatId");
            parameters.Add(("@chatId", query.ChatId.Value.Value));
        }

        // Apply RoleFilter (case-insensitive comparison)
        if (query.RoleFilter.HasValue)
        {
            whereConditions.Add("LOWER(cs.role) = LOWER(@role)");
            parameters.Add(("@role", query.RoleFilter.Value.ToString()));
        }

        // Apply Since date filter
        if (query.Since.HasValue)
        {
            whereConditions.Add("cs.created_at >= @since");
            parameters.Add(("@since", query.Since.Value.ToString("O")));
        }

        // Apply Until date filter
        if (query.Until.HasValue)
        {
            whereConditions.Add("cs.created_at <= @until");
            parameters.Add(("@until", query.Until.Value.ToString("O")));
        }

        // Apply title filter (from field prefix)
        if (ftsQuery.TitleTerms.Count > 0)
        {
            var titleConditions = new List<string>();
            for (int i = 0; i < ftsQuery.TitleTerms.Count; i++)
            {
                titleConditions.Add($"cs.chat_title LIKE @titleTerm{i}");
                parameters.Add(($"@titleTerm{i}", $"%{ftsQuery.TitleTerms[i]}%"));
            }

            whereConditions.Add($"({string.Join(" OR ", titleConditions)})");
        }

        // Apply tag filter (from field prefix)
        if (!string.IsNullOrWhiteSpace(ftsQuery.TagFilter))
        {
            whereConditions.Add("cs.tags LIKE @tagFilter");
            parameters.Add(("@tagFilter", $"%{ftsQuery.TagFilter}%"));
        }

        // Combine WHERE conditions
        if (whereConditions.Count > 0)
        {
            sql += " WHERE " + string.Join(" AND ", whereConditions);
        }

        return sql;
    }

    private async Task EnsureMetadataTableExistsAsync(CancellationToken cancellationToken)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS search_metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            )
        ";
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
