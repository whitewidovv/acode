// src/Acode.Infrastructure/Persistence/Conversation/SqliteRunRepository.cs
namespace Acode.Infrastructure.Persistence.Conversation;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Conversation.Persistence;
using Acode.Domain.Conversation;
using Dapper;
using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite implementation of IRunRepository using Dapper.
/// Provides Run entity persistence with ordered retrieval.
/// </summary>
public sealed class SqliteRunRepository : IRunRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteRunRepository"/> class.
    /// </summary>
    /// <param name="databasePath">The path to the SQLite database file.</param>
    public SqliteRunRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
    }

    /// <inheritdoc/>
    public async Task<RunId> CreateAsync(Run run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = @"
            INSERT INTO conv_runs (id, chat_id, model_id, status, started_at, completed_at,
                             tokens_in, tokens_out, sequence_number, error_message, sync_status)
            VALUES (@Id, @ChatId, @ModelId, @Status, @StartedAt, @CompletedAt,
                   @TokensIn, @TokensOut, @SequenceNumber, @ErrorMessage, @SyncStatus)";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = run.Id.Value,
                ChatId = run.ChatId.Value,
                ModelId = run.ModelId,
                Status = run.Status.ToString(),
                StartedAt = run.StartedAt.ToString("O"),
                CompletedAt = run.CompletedAt?.ToString("O"),
                TokensIn = run.TokensIn,
                TokensOut = run.TokensOut,
                SequenceNumber = run.SequenceNumber,
                ErrorMessage = run.ErrorMessage,
                SyncStatus = run.SyncStatus.ToString(),
            },
            cancellationToken: ct)).ConfigureAwait(false);

        return run.Id;
    }

    /// <inheritdoc/>
    public async Task<Run?> GetByIdAsync(RunId id, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, chat_id AS ChatId, model_id AS ModelId, status AS Status,
                   started_at AS StartedAt, completed_at AS CompletedAt,
                   tokens_in AS TokensIn, tokens_out AS TokensOut,
                   sequence_number AS SequenceNumber, error_message AS ErrorMessage,
                   sync_status AS SyncStatus
            FROM runs
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var row = await conn.QueryFirstOrDefaultAsync<RunRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct)).ConfigureAwait(false);

        if (row == null)
        {
            return null;
        }

        return MapToRun(row);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Run run, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(run);

        const string sql = @"
            UPDATE runs
            SET model_id = @ModelId,
                status = @Status,
                completed_at = @CompletedAt,
                tokens_in = @TokensIn,
                tokens_out = @TokensOut,
                error_message = @ErrorMessage,
                sync_status = @SyncStatus
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = run.Id.Value,
                ModelId = run.ModelId,
                Status = run.Status.ToString(),
                CompletedAt = run.CompletedAt?.ToString("O"),
                TokensIn = run.TokensIn,
                TokensOut = run.TokensOut,
                ErrorMessage = run.ErrorMessage,
                SyncStatus = run.SyncStatus.ToString(),
            },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Run>> ListByChatAsync(ChatId chatId, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, chat_id AS ChatId, model_id AS ModelId, status AS Status,
                   started_at AS StartedAt, completed_at AS CompletedAt,
                   tokens_in AS TokensIn, tokens_out AS TokensOut,
                   sequence_number AS SequenceNumber, error_message AS ErrorMessage,
                   sync_status AS SyncStatus
            FROM runs
            WHERE chat_id = @ChatId
            ORDER BY sequence_number";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var rows = await conn.QueryAsync<RunRow>(
            new CommandDefinition(sql, new { ChatId = chatId.Value }, cancellationToken: ct)).ConfigureAwait(false);

        return rows.Select(MapToRun).ToList();
    }

    /// <inheritdoc/>
    public async Task<Run?> GetLatestAsync(ChatId chatId, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, chat_id AS ChatId, model_id AS ModelId, status AS Status,
                   started_at AS StartedAt, completed_at AS CompletedAt,
                   tokens_in AS TokensIn, tokens_out AS TokensOut,
                   sequence_number AS SequenceNumber, error_message AS ErrorMessage,
                   sync_status AS SyncStatus
            FROM runs
            WHERE chat_id = @ChatId
            ORDER BY sequence_number DESC
            LIMIT 1";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var row = await conn.QueryFirstOrDefaultAsync<RunRow>(
            new CommandDefinition(sql, new { ChatId = chatId.Value }, cancellationToken: ct)).ConfigureAwait(false);

        if (row == null)
        {
            return null;
        }

        return MapToRun(row);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(RunId id, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM runs
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct)).ConfigureAwait(false);
    }

    private static Run MapToRun(RunRow row)
    {
        return Run.Reconstitute(
            RunId.From(row.Id),
            ChatId.From(row.ChatId),
            row.ModelId,
            Enum.Parse<RunStatus>(row.Status),
            DateTimeOffset.Parse(row.StartedAt),
            string.IsNullOrWhiteSpace(row.CompletedAt) ? null : DateTimeOffset.Parse(row.CompletedAt),
            row.TokensIn,
            row.TokensOut,
            row.SequenceNumber,
            row.ErrorMessage,
            Enum.Parse<SyncStatus>(row.SyncStatus));
    }

    private sealed class RunRow
    {
        public string Id { get; set; } = string.Empty;

        public string ChatId { get; set; } = string.Empty;

        public string ModelId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string StartedAt { get; set; } = string.Empty;

        public string? CompletedAt { get; set; }

        public int TokensIn { get; set; }

        public int TokensOut { get; set; }

        public int SequenceNumber { get; set; }

        public string? ErrorMessage { get; set; }

        public string SyncStatus { get; set; } = "Pending";
    }
}
