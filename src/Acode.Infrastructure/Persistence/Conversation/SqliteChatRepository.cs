// src/Acode.Infrastructure/Persistence/Conversation/SqliteChatRepository.cs
namespace Acode.Infrastructure.Persistence.Conversation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Dapper;
using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite implementation of IChatRepository using Dapper.
/// Provides conversation data persistence with optimistic concurrency support.
/// </summary>
public sealed class SqliteChatRepository : IChatRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteChatRepository"/> class.
    /// </summary>
    /// <param name="databasePath">The path to the SQLite database file.</param>
    public SqliteChatRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
    }

    /// <inheritdoc/>
    public async Task<ChatId> CreateAsync(Chat chat, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(chat);

        const string sql = @"
            INSERT INTO conv_chats (id, title, tags, worktree_id, is_deleted, deleted_at,
                              sync_status, version, created_at, updated_at)
            VALUES (@Id, @Title, @Tags, @worktree_id, @IsDeleted, @DeletedAt,
                   @SyncStatus, @Version, @CreatedAt, @UpdatedAt)";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = chat.Id.Value,
                Title = chat.Title,
                Tags = JsonSerializer.Serialize(chat.Tags),
                worktree_id = chat.WorktreeBinding?.Value,
                IsDeleted = chat.IsDeleted ? 1 : 0,
                DeletedAt = chat.DeletedAt?.ToString("O"),
                SyncStatus = chat.SyncStatus.ToString(),
                Version = chat.Version,
                CreatedAt = chat.CreatedAt.ToString("O"),
                UpdatedAt = chat.UpdatedAt.ToString("O"),
            },
            cancellationToken: ct)).ConfigureAwait(false);

        return chat.Id;
    }

    /// <inheritdoc/>
    public async Task<Chat?> GetByIdAsync(ChatId id, bool includeRuns = false, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT id, title, tags, worktree_id AS WorktreeId,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   sync_status AS SyncStatus, version,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM conv_chats
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var row = await conn.QueryFirstOrDefaultAsync<ChatRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct)).ConfigureAwait(false);

        if (row == null)
        {
            return null;
        }

        return MapToChat(row);

        // Note: includeRuns functionality would require IRunRepository - out of scope for Chat repository
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Chat chat, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(chat);

        const string sql = @"
            UPDATE conv_chats
            SET title = @Title,
                tags = @Tags,
                worktree_id = @worktree_id,
                is_deleted = @IsDeleted,
                deleted_at = @DeletedAt,
                sync_status = @SyncStatus,
                version = @Version,
                updated_at = @UpdatedAt
            WHERE id = @Id AND version = @ExpectedVersion";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var rowsAffected = await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = chat.Id.Value,
                Title = chat.Title,
                Tags = JsonSerializer.Serialize(chat.Tags),
                worktree_id = chat.WorktreeBinding?.Value,
                IsDeleted = chat.IsDeleted ? 1 : 0,
                DeletedAt = chat.DeletedAt?.ToString("O"),
                SyncStatus = chat.SyncStatus.ToString(),
                Version = chat.Version,
                UpdatedAt = chat.UpdatedAt.ToString("O"),
                ExpectedVersion = chat.Version - 1,
            },
            cancellationToken: ct)).ConfigureAwait(false);

        if (rowsAffected == 0)
        {
            throw new ConcurrencyException(
                $"Chat {chat.Id} was modified by another process. Expected version {chat.Version - 1} but entity has different version.");
        }
    }

    /// <inheritdoc/>
    public async Task SoftDeleteAsync(ChatId id, CancellationToken ct)
    {
        const string sql = @"
            UPDATE conv_chats
            SET is_deleted = 1,
                deleted_at = @DeletedAt,
                updated_at = @UpdatedAt,
                version = version + 1,
                sync_status = 'Pending'
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = id.Value,
                DeletedAt = DateTimeOffset.UtcNow.ToString("O"),
                UpdatedAt = DateTimeOffset.UtcNow.ToString("O"),
            },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<PagedResult<Chat>> ListAsync(ChatFilter filter, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var whereClause = "WHERE 1=1";
        var parameters = new DynamicParameters();

        if (!filter.IncludeDeleted)
        {
            whereClause += " AND is_deleted = 0";
        }

        if (filter.WorktreeId.HasValue)
        {
            whereClause += " AND worktree_id = @WorktreeId";
            parameters.Add("WorktreeId", filter.WorktreeId.Value.Value);
        }

        if (filter.CreatedAfter.HasValue)
        {
            whereClause += " AND created_at >= @CreatedAfter";
            parameters.Add("CreatedAfter", filter.CreatedAfter.Value.ToString("O"));
        }

        if (filter.CreatedBefore.HasValue)
        {
            whereClause += " AND created_at <= @CreatedBefore";
            parameters.Add("CreatedBefore", filter.CreatedBefore.Value.ToString("O"));
        }

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // Get total count
        var countSql = $"SELECT COUNT(*) FROM conv_chats {whereClause}";
        var totalCount = await conn.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: ct)).ConfigureAwait(false);

        // Get paged results
        var dataSql = $@"
                SELECT id, title, tags, worktree_id AS WorktreeId,
                       is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                       sync_status AS SyncStatus, version,
                       created_at AS CreatedAt, updated_at AS UpdatedAt
                FROM conv_chats {whereClause}
                ORDER BY updated_at DESC
                LIMIT @PageSize OFFSET @Offset";

        parameters.Add("PageSize", filter.PageSize);
        parameters.Add("Offset", filter.Page * filter.PageSize);

        var rows = await conn.QueryAsync<ChatRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: ct)).ConfigureAwait(false);

        var chats = rows.Select(MapToChat).ToList();

        return new PagedResult<Chat>(chats, totalCount, filter.Page, filter.PageSize);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Chat>> GetByWorktreeAsync(WorktreeId worktreeId, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, title, tags, worktree_id AS WorktreeId,
                   is_deleted AS IsDeleted, deleted_at AS DeletedAt,
                   sync_status AS SyncStatus, version,
                   created_at AS CreatedAt, updated_at AS UpdatedAt
            FROM conv_chats
            WHERE worktree_id = @WorktreeId AND is_deleted = 0
            ORDER BY updated_at DESC";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var rows = await conn.QueryAsync<ChatRow>(
            new CommandDefinition(sql, new { WorktreeId = worktreeId.Value }, cancellationToken: ct)).ConfigureAwait(false);

        return rows.Select(MapToChat).ToList();
    }

    /// <inheritdoc/>
    public async Task<int> PurgeDeletedAsync(DateTimeOffset before, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM conv_chats
            WHERE is_deleted = 1 AND deleted_at < @Before";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var rowsDeleted = await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Before = before.ToString("O") }, cancellationToken: ct)).ConfigureAwait(false);

        return rowsDeleted;
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(ChatId id, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(id);

        const string sql = @"
            DELETE FROM conv_chats
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private static Chat MapToChat(ChatRow row)
    {
        var tags = string.IsNullOrWhiteSpace(row.Tags)
            ? Enumerable.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(row.Tags) ?? Enumerable.Empty<string>();

        var createdAt = string.IsNullOrWhiteSpace(row.CreatedAt)
            ? DateTimeOffset.UtcNow
            : DateTimeOffset.Parse(row.CreatedAt);

        var updatedAt = string.IsNullOrWhiteSpace(row.UpdatedAt)
            ? DateTimeOffset.UtcNow
            : DateTimeOffset.Parse(row.UpdatedAt);

        return Chat.Reconstitute(
            ChatId.From(row.Id),
            row.Title,
            tags,
            string.IsNullOrWhiteSpace(row.WorktreeId) ? null : WorktreeId.From(row.WorktreeId),
            row.IsDeleted == 1,
            string.IsNullOrWhiteSpace(row.DeletedAt) ? null : DateTimeOffset.Parse(row.DeletedAt),
            Enum.Parse<SyncStatus>(row.SyncStatus),
            row.Version,
            createdAt,
            updatedAt);
    }

    private sealed class ChatRow
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Tags { get; set; }

        public string? WorktreeId { get; set; }

        public int IsDeleted { get; set; }

        public string? DeletedAt { get; set; }

        public string SyncStatus { get; set; } = "Pending";

        public int Version { get; set; }

        public string CreatedAt { get; set; } = string.Empty;

        public string UpdatedAt { get; set; } = string.Empty;
    }
}
