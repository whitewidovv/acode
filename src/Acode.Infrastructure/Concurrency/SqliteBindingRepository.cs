// src/Acode.Infrastructure/Concurrency/SqliteBindingRepository.cs
namespace Acode.Infrastructure.Concurrency;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Concurrency;
using Acode.Application.Database;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using Microsoft.Extensions.Logging;

/// <summary>
/// SQLite implementation of worktree-to-chat binding persistence.
/// Enforces one-to-one relationship between worktrees and chats.
/// </summary>
public sealed class SqliteBindingRepository : IBindingRepository
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<SqliteBindingRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteBindingRepository"/> class.
    /// </summary>
    /// <param name="connectionFactory">The database connection factory.</param>
    /// <param name="logger">The logger instance.</param>
    public SqliteBindingRepository(
        IConnectionFactory connectionFactory,
        ILogger<SqliteBindingRepository> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<WorktreeBinding?> GetByWorktreeAsync(
        WorktreeId worktreeId,
        CancellationToken ct)
    {
#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        const string sql = @"
            SELECT worktree_id AS WorktreeId, chat_id AS ChatId, created_at AS CreatedAt
            FROM worktree_bindings
            WHERE worktree_id = @WorktreeId";

        var rows = await connection.QueryAsync<BindingRow>(
            sql,
            new { WorktreeId = worktreeId.Value }).ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        if (row is null)
        {
            return null;
        }

        return WorktreeBinding.Reconstitute(
            WorktreeId.From(row.WorktreeId),
            ChatId.From(row.ChatId),
            DateTimeOffset.Parse(row.CreatedAt));
    }

    /// <inheritdoc />
    public async Task<WorktreeBinding?> GetByChatAsync(
        ChatId chatId,
        CancellationToken ct)
    {
#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        const string sql = @"
            SELECT worktree_id AS WorktreeId, chat_id AS ChatId, created_at AS CreatedAt
            FROM worktree_bindings
            WHERE chat_id = @ChatId";

        var rows = await connection.QueryAsync<BindingRow>(
            sql,
            new { ChatId = chatId.Value }).ConfigureAwait(false);

        var row = rows.FirstOrDefault();
        if (row is null)
        {
            return null;
        }

        return WorktreeBinding.Reconstitute(
            WorktreeId.From(row.WorktreeId),
            ChatId.From(row.ChatId),
            DateTimeOffset.Parse(row.CreatedAt));
    }

    /// <inheritdoc />
    public async Task CreateAsync(WorktreeBinding binding, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(binding);

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        // Check for existing binding (enforce one-to-one)
        var existing = await GetByWorktreeAsync(binding.WorktreeId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"Worktree {binding.WorktreeId} is already bound to chat {existing.ChatId}");
        }

        const string sql = @"
            INSERT INTO worktree_bindings (worktree_id, chat_id, created_at)
            VALUES (@WorktreeId, @ChatId, @CreatedAt)";

        await connection.ExecuteAsync(sql, new
        {
            WorktreeId = binding.WorktreeId.Value,
            ChatId = binding.ChatId.Value,
            CreatedAt = binding.CreatedAt,
        }).ConfigureAwait(false);

        _logger.LogInformation(
            "Binding created: Worktree={WorktreeId}, Chat={ChatId}",
            binding.WorktreeId,
            binding.ChatId);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(WorktreeId worktreeId, CancellationToken ct)
    {
#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        const string sql = "DELETE FROM worktree_bindings WHERE worktree_id = @WorktreeId";
        await connection.ExecuteAsync(sql, new { WorktreeId = worktreeId.Value }).ConfigureAwait(false);

        _logger.LogInformation("Binding deleted for worktree {WorktreeId}", worktreeId);
    }

    /// <inheritdoc />
    public async Task DeleteByChatAsync(ChatId chatId, CancellationToken ct)
    {
#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        const string sql = "DELETE FROM worktree_bindings WHERE chat_id = @ChatId";
        await connection.ExecuteAsync(sql, new { ChatId = chatId.Value }).ConfigureAwait(false);

        _logger.LogInformation("Binding deleted for chat {ChatId}", chatId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WorktreeBinding>> ListAllAsync(CancellationToken ct)
    {
#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var connection = await _connectionFactory.CreateAsync(ct).ConfigureAwait(false);
#pragma warning restore CA2007

        const string sql = @"
            SELECT worktree_id AS WorktreeId, chat_id AS ChatId, created_at AS CreatedAt
            FROM worktree_bindings
            ORDER BY created_at DESC";

        var rows = await connection.QueryAsync<BindingRow>(sql).ConfigureAwait(false);

        return rows.Select(r => WorktreeBinding.Reconstitute(
            WorktreeId.From(r.WorktreeId),
            ChatId.From(r.ChatId),
            DateTimeOffset.Parse(r.CreatedAt))).ToList();
    }

    /// <summary>
    /// DTO for binding row data from database.
    /// </summary>
    /// <param name="WorktreeId">The worktree ID value.</param>
    /// <param name="ChatId">The chat ID value.</param>
    /// <param name="CreatedAt">The creation timestamp as ISO string.</param>
    private sealed record BindingRow(
        string WorktreeId,
        string ChatId,
        string CreatedAt);
}
