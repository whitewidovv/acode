namespace Acode.Integration.Tests.Truncation;

using Acode.Application.Truncation;
using Acode.Infrastructure.Truncation;
using FluentAssertions;

/// <summary>
/// Integration tests for the truncation pipeline.
/// </summary>
/// <remarks>
/// Task-007c: Truncation + Artifact Attachment Rules.
/// Spec Reference: Testing Requirements lines 1540-1592.
/// Tests full truncation flow from configuration to output.
/// </remarks>
public sealed class TruncationIntegrationTests : IDisposable
{
    private readonly string testDirectory;
    private readonly FileSystemArtifactStore artifactStore;

    public TruncationIntegrationTests()
    {
        this.testDirectory = Path.Combine(Path.GetTempPath(), $"truncation-integration-test-{Guid.NewGuid():N}");
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
    public async Task Should_Truncate_Large_Command_Output()
    {
        // Arrange - TruncationConfiguration with Tail strategy for execute_command
        // InlineLimit is set high enough to accommodate TailLines without character truncation
        // ArtifactThreshold is set high enough to not trigger artifact creation
        var config = new TruncationConfiguration
        {
            DefaultLimits = new TruncationLimits
            {
                InlineLimit = 5000, // High enough for 50 lines
                ArtifactThreshold = 100_000,
                TailLines = 50
            },
            DefaultStrategy = TruncationStrategy.Tail
        };
        config.ToolStrategies["execute_command"] = TruncationStrategy.Tail;
        config.ToolLimits["execute_command"] = new TruncationLimits
        {
            InlineLimit = 5000, // High enough for 50 lines
            ArtifactThreshold = 100_000,
            TailLines = 50
        };

        var processor = new TruncationProcessor(config, this.artifactStore);

        // Create 500 lines of log output (~27KB, under artifact threshold)
        var lines = new List<string>();
        for (int i = 1; i <= 500; i++)
        {
            lines.Add($"Log line {i}: Processing item with some additional text");
        }

        var content = string.Join('\n', lines);

        // Act
        var result = await processor.ProcessAsync(content, "execute_command", "text/plain");

        // Assert
        result.WasTruncated.Should().BeTrue();

        // Tail strategy should keep the last TailLines (50) lines (lines 451-500)
        result.Content.Should().Contain("Log line 451");
        result.Content.Should().Contain("Log line 500");
        result.Content.Should().NotContain("Log line 1:");
        result.Metadata.StrategyUsed.Should().Be(TruncationStrategy.Tail);
    }

    [Fact]
    public async Task Should_Create_Artifact_For_Massive_Content()
    {
        // Arrange - InlineLimit=1000, ArtifactThreshold=5000
        var config = new TruncationConfiguration
        {
            DefaultLimits = new TruncationLimits
            {
                InlineLimit = 1000,
                ArtifactThreshold = 5000
            }
        };

        var processor = new TruncationProcessor(config, this.artifactStore);

        // Create 100KB content (exceeds ArtifactThreshold)
        var content = new string('x', 100_000);

        // Act
        var result = await processor.ProcessAsync(content, "test_tool", "text/plain");

        // Assert
        result.WasTruncated.Should().BeTrue();
        result.ArtifactId.Should().NotBeNullOrEmpty();
        result.Content.Should().Contain("[Artifact:");
        result.Content.Should().Contain(result.ArtifactId!);
        result.Metadata.ArtifactCreated.Should().BeTrue();

        // Verify artifact was stored correctly
        var retrievedContent = await this.artifactStore.GetContentAsync(result.ArtifactId!);
        retrievedContent.Should().NotBeNull();
        retrievedContent!.Length.Should().Be(100_000);
    }
}
