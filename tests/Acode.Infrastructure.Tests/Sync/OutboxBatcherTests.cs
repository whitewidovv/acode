// tests/Acode.Infrastructure.Tests/Sync/OutboxBatcherTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code

namespace Acode.Infrastructure.Tests.Sync;

using System.Linq;
using Acode.Domain.Sync;
using Acode.Infrastructure.Sync;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for OutboxBatcher.
/// Verifies batching logic for outbox entries based on size and byte limits.
/// </summary>
public sealed class OutboxBatcherTests
{
    [Fact]
    public void Should_Batch_Items()
    {
        // Arrange
        var entries = Enumerable.Range(1, 75)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(2, "75 items should create 2 batches with max size 50");
        batches[0].Should().HaveCount(50);
        batches[1].Should().HaveCount(25);
    }

    [Fact]
    public void Should_Respect_Size_Limit()
    {
        // Arrange
        var entries = Enumerable.Range(1, 100)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 30, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(4, "100 items with max 30 per batch should create 4 batches");
        batches.Should().AllSatisfy(b => b.Count.Should().BeLessOrEqualTo(30));
    }

    [Fact]
    public void Should_Respect_Byte_Limit()
    {
        // Arrange
        var largePayload = new string('a', 100_000);  // 100KB payload
        var entries = Enumerable.Range(1, 15)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", largePayload))
            .ToList();

        // Max batch: 1MB = 10 x 100KB payloads
        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(2, "15 x 100KB entries should create 2 batches under 1MB limit");
        batches.Should().AllSatisfy(batch =>
        {
            var totalBytes = batch.Sum(e => System.Text.Encoding.UTF8.GetByteCount(e.Payload));
            totalBytes.Should().BeLessOrEqualTo(1_000_000);
        });
    }

    [Fact]
    public void Should_Handle_Single_Large_Item()
    {
        // Arrange
        var hugePayload = new string('a', 2_000_000);  // 2MB payload (exceeds 1MB limit)
        var entry = OutboxEntry.Create("Chat", "chat-1", "Insert", hugePayload);

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(new[] { entry });

        // Assert
        batches.Should().HaveCount(1, "single item exceeding limit should still create one batch");
        batches[0].Should().HaveCount(1);
    }

    [Fact]
    public void Should_Handle_Empty_Input()
    {
        // Arrange
        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(new List<OutboxEntry>());

        // Assert
        batches.Should().BeEmpty("empty input should produce no batches");
    }

    [Fact]
    public void Should_Create_Single_Batch_For_Small_Count()
    {
        // Arrange
        var entries = Enumerable.Range(1, 10)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(1, "10 items under limit should create single batch");
        batches[0].Should().HaveCount(10);
    }

    [Fact]
    public void Should_Enforce_Minimum_Batch_Size()
    {
        // Arrange
        var entries = Enumerable.Range(1, 5)
            .Select(i => OutboxEntry.Create("Chat", $"chat-{i}", "Insert", "{}"))
            .ToList();

        var batcher = new OutboxBatcher(maxBatchSize: 1, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(5, "max batch size of 1 should create 5 batches for 5 items");
        batches.Should().AllSatisfy(b => b.Should().HaveCount(1));
    }

    [Fact]
    public void Should_Handle_Mixed_Sizes()
    {
        // Arrange
        var entries = new List<OutboxEntry>
        {
            OutboxEntry.Create("Chat", "chat-1", "Insert", new string('a', 500_000)),  // 500KB
            OutboxEntry.Create("Chat", "chat-2", "Insert", new string('b', 400_000)),  // 400KB
            OutboxEntry.Create("Chat", "chat-3", "Insert", new string('c', 300_000)),  // 300KB
            OutboxEntry.Create("Chat", "chat-4", "Insert", new string('d', 100_000)),  // 100KB
        };

        var batcher = new OutboxBatcher(maxBatchSize: 50, maxBatchBytes: 1_000_000);

        // Act
        var batches = batcher.CreateBatches(entries);

        // Assert
        batches.Should().HaveCount(2, "mixed sizes should create 2 batches");

        // Batch 1: 500KB + 400KB = 900KB (fits)
        batches[0].Should().HaveCount(2);
        var batch1Bytes = batches[0].Sum(e => System.Text.Encoding.UTF8.GetByteCount(e.Payload));
        batch1Bytes.Should().BeLessOrEqualTo(1_000_000);

        // Batch 2: 300KB + 100KB = 400KB
        batches[1].Should().HaveCount(2);
        var batch2Bytes = batches[1].Sum(e => System.Text.Encoding.UTF8.GetByteCount(e.Payload));
        batch2Bytes.Should().BeLessOrEqualTo(1_000_000);
    }
}
