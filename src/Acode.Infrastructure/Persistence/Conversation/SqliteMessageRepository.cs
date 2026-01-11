// src/Acode.Infrastructure/Persistence/Conversation/SqliteMessageRepository.cs
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
/// SQLite implementation of IMessageRepository using Dapper.
/// Provides Message entity persistence with ordered retrieval and cascade delete.
/// </summary>
public sealed class SqliteMessageRepository : IMessageRepository
{
    private readonly string _connectionString;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteMessageRepository"/> class.
    /// </summary>
    /// <param name="databasePath">The path to the SQLite database file.</param>
    public SqliteMessageRepository(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _connectionString = $"Data Source={databasePath};Mode=ReadWriteCreate";
    }

    /// <inheritdoc/>
    public async Task<MessageId> CreateAsync(Message message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        const string sql = @"
            INSERT INTO conv_messages (id, run_id, role, content, tool_calls, created_at, sequence_number, sync_status)
            VALUES (@Id, @RunId, @Role, @Content, @ToolCalls, @CreatedAt, @SequenceNumber, @SyncStatus)";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(new CommandDefinition(
            sql,
            new
            {
                Id = message.Id.Value,
                RunId = message.RunId.Value,
                Role = message.Role,
                Content = message.Content,
                ToolCalls = message.GetToolCallsJson(),
                CreatedAt = message.CreatedAt.ToString("O"),
                SequenceNumber = message.SequenceNumber,
                SyncStatus = message.SyncStatus.ToString(),
            },
            cancellationToken: ct)).ConfigureAwait(false);

        return message.Id;
    }

    /// <inheritdoc/>
    public async Task<Message?> GetByIdAsync(MessageId id, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, run_id AS RunId, role AS Role, content AS Content,
                   tool_calls AS ToolCalls, created_at AS CreatedAt,
                   sequence_number AS SequenceNumber, sync_status AS SyncStatus
            FROM conv_messages
            WHERE id = @Id";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var row = await conn.QueryFirstOrDefaultAsync<MessageRow>(
            new CommandDefinition(sql, new { Id = id.Value }, cancellationToken: ct)).ConfigureAwait(false);

        if (row == null)
        {
            return null;
        }

        return MapToMessage(row);
    }

    /// <inheritdoc/>
    public async Task UpdateAsync(Message message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        const string sql = @"
            UPDATE conv_messages
            SET tool_calls = @ToolCalls,
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
                Id = message.Id.Value,
                ToolCalls = message.GetToolCallsJson(),
                SyncStatus = message.SyncStatus.ToString(),
            },
            cancellationToken: ct)).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Message>> ListByRunAsync(RunId runId, CancellationToken ct)
    {
        const string sql = @"
            SELECT id, run_id AS RunId, role AS Role, content AS Content,
                   tool_calls AS ToolCalls, created_at AS CreatedAt,
                   sequence_number AS SequenceNumber, sync_status AS SyncStatus
            FROM conv_messages
            WHERE run_id = @RunId
            ORDER BY sequence_number";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        var rows = await conn.QueryAsync<MessageRow>(
            new CommandDefinition(sql, new { RunId = runId.Value }, cancellationToken: ct)).ConfigureAwait(false);

        return rows.Select(MapToMessage).ToList();
    }

    /// <inheritdoc/>
    public async Task DeleteByRunAsync(RunId runId, CancellationToken ct)
    {
        const string sql = @"
            DELETE FROM conv_messages
            WHERE run_id = @RunId";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        await conn.ExecuteAsync(
            new CommandDefinition(sql, new { RunId = runId.Value }, cancellationToken: ct)).ConfigureAwait(false);
    }

    public async Task<MessageId> AppendAsync(RunId runId, Message message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        // Get next sequence number for this run
        const string getSeqSql = @"
            SELECT COALESCE(MAX(sequence_number), 0) + 1
            FROM conv_messages
            WHERE run_id = @RunId";

        const string insertSql = @"
            INSERT INTO conv_messages (id, run_id, role, content, tool_calls, created_at, sequence_number, sync_status)
            VALUES (@Id, @RunId, @Role, @Content, @ToolCalls, @CreatedAt, @SequenceNumber, @SyncStatus)";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // Get next sequence number
        var nextSeq = await conn.QuerySingleAsync<int>(
            new CommandDefinition(getSeqSql, new { RunId = runId.Value }, cancellationToken: ct)).ConfigureAwait(false);

        // Set sequence number on the message
        var messageWithSeq = Message.Reconstitute(
            message.Id,
            message.RunId,
            message.Role.ToString(),
            message.Content,
            message.ToolCalls,
            message.CreatedAt,
            nextSeq,
            message.SyncStatus);

        // Insert with assigned sequence number
        var toolCallsJson = messageWithSeq.ToolCalls?.Any() == true
            ? System.Text.Json.JsonSerializer.Serialize(messageWithSeq.ToolCalls)
            : null;

        await conn.ExecuteAsync(
            new CommandDefinition(
                insertSql,
                new
                {
                    Id = messageWithSeq.Id.Value,
                    RunId = messageWithSeq.RunId.Value,
                    Role = messageWithSeq.Role.ToString(),
                    messageWithSeq.Content,
                    ToolCalls = toolCallsJson,
                    CreatedAt = messageWithSeq.CreatedAt.ToString("O"),
                    SequenceNumber = nextSeq,
                    SyncStatus = messageWithSeq.SyncStatus.ToString()
                },
                cancellationToken: ct)).ConfigureAwait(false);

        return messageWithSeq.Id;
    }

    public async Task BulkCreateAsync(IEnumerable<Message> messages, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var messageList = messages.ToList();
        if (!messageList.Any())
        {
            return;
        }

        const string getSeqSql = @"
            SELECT COALESCE(MAX(sequence_number), 0) + 1
            FROM conv_messages
            WHERE run_id = @RunId";

        const string insertSql = @"
            INSERT INTO conv_messages (id, run_id, role, content, tool_calls, created_at, sequence_number, sync_status)
            VALUES (@Id, @RunId, @Role, @Content, @ToolCalls, @CreatedAt, @SequenceNumber, @SyncStatus)";

#pragma warning disable CA2007 // Async disposal doesn't require ConfigureAwait for database connections
        await using var conn = new SqliteConnection(_connectionString);
#pragma warning restore CA2007
        await conn.OpenAsync(ct).ConfigureAwait(false);

        // Group messages by RunId to get correct sequence numbers
        var messagesByRun = messageList.GroupBy(m => m.RunId.Value);

        foreach (var runGroup in messagesByRun)
        {
            var runId = runGroup.Key;

            // Get next sequence number for this run
            var nextSeq = await conn.QuerySingleAsync<int>(
                new CommandDefinition(getSeqSql, new { RunId = runId }, cancellationToken: ct)).ConfigureAwait(false);

            // Assign sequence numbers and prepare batch insert
            var parameters = new List<object>();
            var seqCounter = nextSeq;

            foreach (var message in runGroup)
            {
                var toolCallsJson = message.ToolCalls?.Any() == true
                    ? System.Text.Json.JsonSerializer.Serialize(message.ToolCalls)
                    : null;

                parameters.Add(new
                {
                    Id = message.Id.Value,
                    RunId = message.RunId.Value,
                    Role = message.Role.ToString(),
                    message.Content,
                    ToolCalls = toolCallsJson,
                    CreatedAt = message.CreatedAt.ToString("O"),
                    SequenceNumber = seqCounter++,
                    SyncStatus = message.SyncStatus.ToString()
                });
            }

            // Bulk insert all messages for this run
            await conn.ExecuteAsync(
                new CommandDefinition(insertSql, parameters, cancellationToken: ct)).ConfigureAwait(false);
        }
    }

    private static Message MapToMessage(MessageRow row)
    {
        IEnumerable<ToolCall>? toolCalls = null;
        if (!string.IsNullOrWhiteSpace(row.ToolCalls))
        {
            toolCalls = System.Text.Json.JsonSerializer.Deserialize<ToolCall[]>(row.ToolCalls);
        }

        return Message.Reconstitute(
            MessageId.From(row.Id),
            RunId.From(row.RunId),
            row.Role,
            row.Content,
            toolCalls,
            DateTimeOffset.Parse(row.CreatedAt),
            row.SequenceNumber,
            Enum.Parse<SyncStatus>(row.SyncStatus));
    }

    private sealed class MessageRow
    {
        public string Id { get; set; } = string.Empty;

        public string RunId { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string? ToolCalls { get; set; }

        public string CreatedAt { get; set; } = string.Empty;

        public int SequenceNumber { get; set; }

        public string SyncStatus { get; set; } = "Pending";
    }
}
