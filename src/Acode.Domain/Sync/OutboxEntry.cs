// src/Acode.Domain/Sync/OutboxEntry.cs
namespace Acode.Domain.Sync;

/// <summary>
/// Represents an outbox entry for reliable sync delivery.
/// Uses the outbox pattern to ensure changes are eventually synced to remote storage.
/// </summary>
public sealed class OutboxEntry
{
    private OutboxEntry()
    {
        // Private constructor for EF Core
        Id = string.Empty;
        IdempotencyKey = string.Empty;
        EntityType = string.Empty;
        EntityId = string.Empty;
        Operation = string.Empty;
        Payload = string.Empty;
    }

    /// <summary>
    /// Gets the unique identifier for this outbox entry.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// Gets the ULID-format idempotency key for deduplication.
    /// </summary>
    public string IdempotencyKey { get; private set; }

    /// <summary>
    /// Gets the entity type (e.g., "Chat", "Message").
    /// </summary>
    public string EntityType { get; private set; }

    /// <summary>
    /// Gets the entity identifier.
    /// </summary>
    public string EntityId { get; private set; }

    /// <summary>
    /// Gets the operation type (e.g., "Insert", "Update", "Delete").
    /// </summary>
    public string Operation { get; private set; }

    /// <summary>
    /// Gets the JSON payload containing the entity data.
    /// </summary>
    public string Payload { get; private set; }

    /// <summary>
    /// Gets the current status of the outbox entry.
    /// </summary>
    public OutboxStatus Status { get; private set; }

    /// <summary>
    /// Gets the number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    /// <summary>
    /// Gets the timestamp when retry should be attempted.
    /// </summary>
    public DateTimeOffset? NextRetryAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when processing started.
    /// </summary>
    public DateTimeOffset? ProcessingStartedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when entry was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>
    /// Gets the timestamp when entry was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>
    /// Gets the last error message if any.
    /// </summary>
    public string? LastError { get; private set; }

    /// <summary>
    /// Creates a new outbox entry.
    /// </summary>
    /// <param name="entityType">Entity type.</param>
    /// <param name="entityId">Entity identifier.</param>
    /// <param name="operation">Operation type.</param>
    /// <param name="payload">JSON payload.</param>
    /// <returns>A new outbox entry.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are null or empty.</exception>
    public static OutboxEntry Create(string entityType, string entityId, string operation, string payload)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            throw new ArgumentException("Entity type cannot be null or empty.", nameof(entityType));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException("Entity ID cannot be null or empty.", nameof(entityId));
        }

        if (string.IsNullOrWhiteSpace(operation))
        {
            throw new ArgumentException("Operation cannot be null or empty.", nameof(operation));
        }

        if (string.IsNullOrWhiteSpace(payload))
        {
            throw new ArgumentException("Payload cannot be null or empty.", nameof(payload));
        }

        var entry = new OutboxEntry
        {
            Id = Guid.NewGuid().ToString(),
            IdempotencyKey = GenerateUlid(),
            EntityType = entityType,
            EntityId = entityId,
            Operation = operation,
            Payload = payload,
            Status = OutboxStatus.Pending,
            RetryCount = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return entry;
    }

    /// <summary>
    /// Marks the entry as currently processing.
    /// </summary>
    public void MarkAsProcessing()
    {
        Status = OutboxStatus.Processing;
        ProcessingStartedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the entry as completed successfully.
    /// </summary>
    public void MarkAsCompleted()
    {
        Status = OutboxStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Marks the entry as failed and increments retry count.
    /// Entry returns to Pending status for retry.
    /// </summary>
    /// <param name="errorMessage">Error message.</param>
    public void MarkAsFailed(string errorMessage)
    {
        Status = OutboxStatus.Pending;
        RetryCount++;
        LastError = errorMessage;
    }

    /// <summary>
    /// Schedules the next retry attempt.
    /// </summary>
    /// <param name="delay">Delay before next retry.</param>
    public void ScheduleRetry(TimeSpan delay)
    {
        NextRetryAt = DateTimeOffset.UtcNow.Add(delay);
    }

    /// <summary>
    /// Marks the entry as dead letter after exceeding max retries.
    /// </summary>
    /// <param name="errorMessage">Final error message.</param>
    public void MarkAsDeadLetter(string errorMessage)
    {
        Status = OutboxStatus.DeadLetter;
        LastError = errorMessage;
    }

    /// <summary>
    /// Generates a ULID-format string (26 characters, base32).
    /// ULID = Universally Unique Lexicographically Sortable Identifier.
    /// Format: 10 characters timestamp + 16 characters randomness = 26 characters total.
    /// </summary>
    /// <returns>ULID string.</returns>
    private static string GenerateUlid()
    {
        // ULID encoding uses Crockford's Base32 alphabet: 0-9 A-Z excluding I, L, O, U
        const string base32Chars = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        // Get timestamp (milliseconds since Unix epoch)
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Get random bytes
        var randomBytes = new byte[10];
        System.Security.Cryptography.RandomNumberGenerator.Fill(randomBytes);

        // Encode timestamp (48 bits = 10 base32 chars)
        var ulid = new char[26];
        for (int i = 9; i >= 0; i--)
        {
            ulid[i] = base32Chars[(int)(timestamp & 0x1F)];
            timestamp >>= 5;
        }

        // Encode randomness (80 bits = 16 base32 chars)
        long randomValue = 0;
        int bitsAvailable = 0;
        int outputIndex = 10;

        foreach (var b in randomBytes)
        {
            randomValue = (randomValue << 8) | b;
            bitsAvailable += 8;

            while (bitsAvailable >= 5 && outputIndex < 26)
            {
                bitsAvailable -= 5;
                ulid[outputIndex++] = base32Chars[(int)((randomValue >> bitsAvailable) & 0x1F)];
            }
        }

        return new string(ulid);
    }
}
