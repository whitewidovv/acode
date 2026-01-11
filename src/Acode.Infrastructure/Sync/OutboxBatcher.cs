// src/Acode.Infrastructure/Sync/OutboxBatcher.cs
namespace Acode.Infrastructure.Sync;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Acode.Domain.Sync;

/// <summary>
/// Batches outbox entries based on count and byte size limits.
/// </summary>
public sealed class OutboxBatcher
{
    private readonly int _maxBatchSize;
    private readonly int _maxBatchBytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxBatcher"/> class.
    /// </summary>
    /// <param name="maxBatchSize">Maximum number of entries per batch.</param>
    /// <param name="maxBatchBytes">Maximum total bytes per batch.</param>
    public OutboxBatcher(int maxBatchSize, int maxBatchBytes)
    {
        if (maxBatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBatchSize), "Max batch size must be greater than zero");
        }

        if (maxBatchBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxBatchBytes), "Max batch bytes must be greater than zero");
        }

        _maxBatchSize = maxBatchSize;
        _maxBatchBytes = maxBatchBytes;
    }

    /// <summary>
    /// Creates batches from a collection of outbox entries.
    /// Respects both count and byte size limits.
    /// </summary>
    /// <param name="entries">The outbox entries to batch.</param>
    /// <returns>A list of batches, where each batch is a list of outbox entries.</returns>
    public IReadOnlyList<IReadOnlyList<OutboxEntry>> CreateBatches(IEnumerable<OutboxEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var batches = new List<IReadOnlyList<OutboxEntry>>();
        var currentBatch = new List<OutboxEntry>();
        var currentBatchBytes = 0;

        foreach (var entry in entries)
        {
            var entryBytes = Encoding.UTF8.GetByteCount(entry.Payload);

            // Check if adding this entry would exceed limits
            var wouldExceedCount = currentBatch.Count >= _maxBatchSize;
            var wouldExceedBytes = currentBatch.Count > 0 && (currentBatchBytes + entryBytes) > _maxBatchBytes;

            if (wouldExceedCount || wouldExceedBytes)
            {
                // Finalize current batch and start new one
                batches.Add(currentBatch.ToList());
                currentBatch = new List<OutboxEntry>();
                currentBatchBytes = 0;
            }

            // Add entry to current batch
            // Note: Even if entry exceeds byte limit, we still add it
            // (handles single large items that exceed the limit)
            currentBatch.Add(entry);
            currentBatchBytes += entryBytes;
        }

        // Add final batch if not empty
        if (currentBatch.Count > 0)
        {
            batches.Add(currentBatch.ToList());
        }

        return batches;
    }
}
