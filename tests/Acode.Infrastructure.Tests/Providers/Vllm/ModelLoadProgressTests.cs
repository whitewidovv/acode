using Acode.Domain.Providers.Vllm;
using FluentAssertions;

namespace Acode.Infrastructure.Tests.Providers.Vllm;

/// <summary>
/// Tests for ModelLoadProgress class.
/// </summary>
public class ModelLoadProgressTests
{
    [Fact]
    public void Test_ModelLoadProgress_FromDownloading_CreatesProgress()
    {
        // Arrange & Act
        var progress = ModelLoadProgress.FromDownloading(
            modelId: "meta-llama/Llama-2-7b",
            downloaded: 5_000_000_000,
            total: 10_000_000_000,
            status: "downloading");

        // Assert
        progress.ModelId.Should().Be("meta-llama/Llama-2-7b");
        progress.BytesDownloaded.Should().Be(5_000_000_000);
        progress.TotalBytes.Should().Be(10_000_000_000);
        progress.ProgressPercent.Should().BeApproximately(50.0, 0.1);
        progress.Status.Should().Be("downloading");
        progress.IsProgressKnown.Should().BeTrue();
        progress.IsComplete.Should().BeFalse();
        progress.CompletedAt.Should().BeNull();
    }

    [Fact]
    public void Test_ModelLoadProgress_FromComplete_CreatesCompleted()
    {
        // Arrange & Act
        var beforeCreation = DateTime.UtcNow;
        var progress = ModelLoadProgress.FromComplete("meta-llama/Llama-2-7b");
        var afterCreation = DateTime.UtcNow.AddMilliseconds(1);

        // Assert
        progress.ModelId.Should().Be("meta-llama/Llama-2-7b");
        progress.ProgressPercent.Should().Be(100.0);
        progress.Status.Should().Be("loaded");
        progress.BytesDownloaded.Should().BeNull();
        progress.TotalBytes.Should().BeNull();
        progress.IsProgressKnown.Should().BeFalse();
        progress.IsComplete.Should().BeTrue();
        progress.CompletedAt.Should().NotBeNull();
        progress.CompletedAt.Should().BeOnOrAfter(beforeCreation).And.BeOnOrBefore(afterCreation);
    }

    [Fact]
    public void Test_ModelLoadProgress_Properties_Immutable()
    {
        // Arrange & Act
        var progress = ModelLoadProgress.FromDownloading(
            modelId: "original-model",
            downloaded: 100,
            total: 200);

        // Assert - All property values should be accessible and correct
        progress.ModelId.Should().Be("original-model");
        progress.BytesDownloaded.Should().Be(100);
        progress.TotalBytes.Should().Be(200);
        progress.Status.Should().Be("downloading");

        // Assert - Computed properties should be readable
        progress.IsProgressKnown.Should().BeTrue();
        progress.IsComplete.Should().BeFalse();

        // Additional check - create instance with init values to verify structure
        var newProgress = new ModelLoadProgress
        {
            ModelId = "new-model",
            ProgressPercent = 50,
            Status = "loading",
        };
        newProgress.ModelId.Should().Be("new-model");
        newProgress.ProgressPercent.Should().Be(50);
    }

    [Fact]
    public void Test_ModelLoadProgress_IsProgressKnown_False_WhenNoBytes()
    {
        // Arrange & Act
        var progress = new ModelLoadProgress
        {
            ModelId = "model",
            Status = "loading",
        };

        // Assert
        progress.IsProgressKnown.Should().BeFalse("progress is unknown when bytes/total are null");
    }

    [Fact]
    public void Test_ModelLoadProgress_IsProgressKnown_True_WhenBothSet()
    {
        // Arrange & Act
        var progress = new ModelLoadProgress
        {
            ModelId = "model",
            BytesDownloaded = 100,
            TotalBytes = 200,
            Status = "downloading",
        };

        // Assert
        progress.IsProgressKnown.Should().BeTrue("progress is known when both bytes and total are set");
    }

    [Fact]
    public void Test_ModelLoadProgress_ProgressPercent_Calculated()
    {
        // Arrange & Act
        var progress = ModelLoadProgress.FromDownloading(
            modelId: "model",
            downloaded: 25_000_000,
            total: 100_000_000);

        // Assert
        progress.ProgressPercent.Should().BeApproximately(25.0, 0.1);
    }

    [Fact]
    public void Test_ModelLoadProgress_ProgressPercent_SafeWhenZeroTotal()
    {
        // Arrange & Act
        var progress = ModelLoadProgress.FromDownloading(
            modelId: "model",
            downloaded: 100,
            total: 0);

        // Assert
        progress.ProgressPercent.Should().Be(0.0, "percentage should be 0 when total is 0 to avoid divide by zero");
    }

    [Fact]
    public void Test_ModelLoadProgress_CompletedAt_SetOnCompletion()
    {
        // Arrange & Act
        var progress = ModelLoadProgress.FromComplete("model");

        // Assert
        progress.CompletedAt.Should().NotBeNull("CompletedAt should be set when using FromComplete factory");
        progress.IsComplete.Should().BeTrue("IsComplete should reflect CompletedAt being set");
    }

    [Fact]
    public void Test_ModelLoadProgress_StartedAt_Tracked()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var progress = ModelLoadProgress.FromDownloading(
            modelId: "model",
            downloaded: 100,
            total: 200);
        var afterCreation = DateTime.UtcNow.AddMilliseconds(1);

        // Assert
        progress.StartedAt.Should().BeOnOrAfter(beforeCreation);
        progress.StartedAt.Should().BeOnOrBefore(afterCreation);
    }
}
