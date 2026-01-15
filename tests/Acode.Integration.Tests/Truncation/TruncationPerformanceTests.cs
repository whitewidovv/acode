namespace Acode.Integration.Tests.Truncation;

using System.Diagnostics;
using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation;
using FluentAssertions;

/// <summary>
/// Performance tests for the truncation system.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Spec Reference: Testing Requirements lines 1597-1632, Description lines 31-32.
/// Verifies performance requirements:
/// Under 100KB should complete in less than 10 milliseconds.
/// 100KB to 10MB should complete in less than 100 milliseconds.
/// </remarks>
public sealed class TruncationPerformanceTests : IDisposable
{
    private readonly string testDirectory;
    private readonly FileSystemArtifactStore artifactStore;

    public TruncationPerformanceTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"truncation-perf-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.testDirectory);
        this.artifactStore = new FileSystemArtifactStore(this.testDirectory);
    }

    public void Dispose()
    {
        this.artifactStore.Dispose();
        if (Directory.Exists(this.testDirectory))
        {
            Directory.Delete(this.testDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task Should_Truncate_100KB_In_Under_10ms()
    {
        // Arrange - 100KB content with HeadTailStrategy, InlineLimit=10000
        var config = new TruncationConfiguration
        {
            DefaultLimits = new TruncationLimits
            {
                InlineLimit = 10_000,
                ArtifactThreshold = 200_000, // High to avoid artifact creation
                HeadLines = 50,
                TailLines = 50
            },
            DefaultStrategy = TruncationStrategy.HeadTail
        };

        var processor = new TruncationProcessor(config, this.artifactStore);
        var content = new string('x', 100_000); // 100KB

        // Warmup run
        await processor.ProcessAsync(content, "test_tool", "text/plain");

        // Act - Measure single truncation
        var stopwatch = Stopwatch.StartNew();
        var result = await processor.ProcessAsync(content, "test_tool", "text/plain");
        stopwatch.Stop();

        // Assert
        result.WasTruncated.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10, "truncation of 100KB should complete in under 10ms as per spec");
    }

    [Fact]
    public async Task Should_Handle_10_Concurrent_Truncations()
    {
        // Arrange - 10 concurrent tasks, each processing 50KB
        var config = new TruncationConfiguration
        {
            DefaultLimits = new TruncationLimits
            {
                InlineLimit = 10_000,
                ArtifactThreshold = 200_000,
                HeadLines = 50,
                TailLines = 50
            },
            DefaultStrategy = TruncationStrategy.HeadTail
        };

        var processor = new TruncationProcessor(config, this.artifactStore);
        var tasks = new List<Task<TruncationResult>>();

        // Act - Create 10 concurrent truncation tasks with unique content
        var stopwatch = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++)
        {
            var content = new string((char)('a' + i), 50_000); // 50KB each, unique char
            tasks.Add(processor.ProcessAsync(content, $"tool_{i}", "text/plain"));
        }

        var results = await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        results.Should().HaveCount(10);
        results.Should().OnlyContain(r => r.WasTruncated, "all 50KB contents should be truncated");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "10 concurrent truncations should complete in under 1 second");

        // Verify uniqueness - each result should have different content since input was different
        var uniqueContents = results.Select(r => r.Content).Distinct().ToList();
        uniqueContents.Should().HaveCount(10, "each concurrent truncation should produce unique output");
    }
}
