// src/Acode.Infrastructure/Sync/SqliteOutboxRepository.cs
namespace Acode.Infrastructure.Sync;

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Acode.Application.Sync;
using Acode.Domain.Sync;

/// <summary>
/// SQLite implementation of the outbox repository.
/// </summary>
public sealed class SqliteOutboxRepository : IOutboxRepository
{
    private readonly IDbConnection _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteOutboxRepository"/> class.
    /// </summary>
    /// <param name="connection">Database connection.</param>
    public SqliteOutboxRepository(IDbConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    /// <inheritdoc/>
    public Task AddAsync(OutboxEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            INSERT INTO outbox (
                id, idempotency_key, entity_type, entity_id, operation, payload,
                status, retry_count, next_retry_at, processing_started_at,
                completed_at, created_at, last_error
            ) VALUES (
                @id, @idempotency_key, @entity_type, @entity_id, @operation, @payload,
                @status, @retry_count, @next_retry_at, @processing_started_at,
                @completed_at, @created_at, @last_error
            )";

        AddParameter(command, "@id", entry.Id);
        AddParameter(command, "@idempotency_key", entry.IdempotencyKey);
        AddParameter(command, "@entity_type", entry.EntityType);
        AddParameter(command, "@entity_id", entry.EntityId);
        AddParameter(command, "@operation", entry.Operation);
        AddParameter(command, "@payload", entry.Payload);
        AddParameter(command, "@status", (int)entry.Status);
        AddParameter(command, "@retry_count", entry.RetryCount);
        AddParameter(command, "@next_retry_at", FormatDateTimeOffset(entry.NextRetryAt));
        AddParameter(command, "@processing_started_at", FormatDateTimeOffset(entry.ProcessingStartedAt));
        AddParameter(command, "@completed_at", FormatDateTimeOffset(entry.CompletedAt));
        AddParameter(command, "@created_at", FormatDateTimeOffset(entry.CreatedAt));
        AddParameter(command, "@last_error", entry.LastError);

        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<OutboxEntry?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, idempotency_key, entity_type, entity_id, operation, payload,
                   status, retry_count, next_retry_at, processing_started_at,
                   completed_at, created_at, last_error
            FROM outbox
            WHERE id = @id";

        AddParameter(command, "@id", id);

        using var reader = command.ExecuteReader();
        if (reader.Read())
        {
            return Task.FromResult<OutboxEntry?>(MapOutboxEntry(reader));
        }

        return Task.FromResult<OutboxEntry?>(null);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<OutboxEntry>> GetPendingAsync(int limit, CancellationToken cancellationToken = default)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = @"
            SELECT id, idempotency_key, entity_type, entity_id, operation, payload,
                   status, retry_count, next_retry_at, processing_started_at,
                   completed_at, created_at, last_error
            FROM outbox
            WHERE status = @status
              AND (next_retry_at IS NULL OR next_retry_at <= @now)
            ORDER BY created_at ASC
            LIMIT @limit";

        AddParameter(command, "@status", (int)OutboxStatus.Pending);
        AddParameter(command, "@now", FormatDateTimeOffset(DateTimeOffset.UtcNow));
        AddParameter(command, "@limit", limit);

        var entries = new List<OutboxEntry>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            entries.Add(MapOutboxEntry(reader));
        }

        return Task.FromResult<IReadOnlyList<OutboxEntry>>(entries);
    }

    /// <inheritdoc/>
    public Task UpdateAsync(OutboxEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);

        using var command = _connection.CreateCommand();
        command.CommandText = @"
            UPDATE outbox
            SET status = @status,
                retry_count = @retry_count,
                next_retry_at = @next_retry_at,
                processing_started_at = @processing_started_at,
                completed_at = @completed_at,
                last_error = @last_error
            WHERE id = @id";

        AddParameter(command, "@status", (int)entry.Status);
        AddParameter(command, "@retry_count", entry.RetryCount);
        AddParameter(command, "@next_retry_at", FormatDateTimeOffset(entry.NextRetryAt));
        AddParameter(command, "@processing_started_at", FormatDateTimeOffset(entry.ProcessingStartedAt));
        AddParameter(command, "@completed_at", FormatDateTimeOffset(entry.CompletedAt));
        AddParameter(command, "@last_error", entry.LastError);
        AddParameter(command, "@id", entry.Id);

        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var command = _connection.CreateCommand();
        command.CommandText = "DELETE FROM outbox WHERE id = @id";
        AddParameter(command, "@id", id);
        command.ExecuteNonQuery();

        return Task.CompletedTask;
    }

    private static OutboxEntry MapOutboxEntry(IDataReader reader)
    {
        var id = reader.GetString(0);
        var idempotencyKey = reader.GetString(1);
        var entityType = reader.GetString(2);
        var entityId = reader.GetString(3);
        var operation = reader.GetString(4);
        var payload = reader.GetString(5);
        var status = (OutboxStatus)reader.GetInt32(6);
        var retryCount = reader.GetInt32(7);
        var nextRetryAt = ParseDateTimeOffset(reader.IsDBNull(8) ? null : reader.GetString(8));
        var processingStartedAt = ParseDateTimeOffset(reader.IsDBNull(9) ? null : reader.GetString(9));
        var completedAt = ParseDateTimeOffset(reader.IsDBNull(10) ? null : reader.GetString(10));
        var createdAt = ParseDateTimeOffset(reader.GetString(11))!.Value;
        var lastError = reader.IsDBNull(12) ? null : reader.GetString(12);

        // Use reflection to reconstruct OutboxEntry with private setters
        var entry = OutboxEntry.Create(entityType, entityId, operation, payload);

        SetProperty(entry, "Id", id);
        SetProperty(entry, "IdempotencyKey", idempotencyKey);
        SetProperty(entry, "Status", status);
        SetProperty(entry, "RetryCount", retryCount);
        SetProperty(entry, "NextRetryAt", nextRetryAt);
        SetProperty(entry, "ProcessingStartedAt", processingStartedAt);
        SetProperty(entry, "CompletedAt", completedAt);
        SetProperty(entry, "CreatedAt", createdAt);
        SetProperty(entry, "LastError", lastError);

        return entry;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName);
        property?.SetValue(obj, value);
    }

    private static void AddParameter(IDbCommand command, string name, object? value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value ?? DBNull.Value;
        command.Parameters.Add(parameter);
    }

    private static string? FormatDateTimeOffset(DateTimeOffset? value)
    {
        return value?.ToString("o", CultureInfo.InvariantCulture);
    }

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
    }
}
