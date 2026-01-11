// tests/Acode.Domain.Tests/Conversation/RunTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code
namespace Acode.Domain.Tests.Conversation;

using System;
using Acode.Domain.Conversation;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for Run entity.
/// Verifies run creation, status transitions, token tracking, and duration calculation.
/// </summary>
public sealed class RunTests
{
    [Fact]
    public void Create_WithValidInputs_CreatesRun()
    {
        // Arrange
        var chatId = ChatId.NewId();
        var modelId = "claude-sonnet-4";

        // Act
        var run = Run.Create(chatId, modelId, sequenceNumber: 1);

        // Assert
        run.Should().NotBeNull();
        run.Id.Should().NotBe(RunId.Empty);
        run.ChatId.Should().Be(chatId);
        run.ModelId.Should().Be(modelId);
        run.SequenceNumber.Should().Be(1);
        run.Status.Should().Be(RunStatus.Running);
        run.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        run.CompletedAt.Should().BeNull();
        run.TokensIn.Should().Be(0);
        run.TokensOut.Should().Be(0);
        run.ErrorMessage.Should().BeNull();
        run.SyncStatus.Should().Be(SyncStatus.Pending);
        run.Messages.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithEmptyChatId_ThrowsArgumentException()
    {
        // Arrange & Act
        var act = () => Run.Create(ChatId.Empty, "claude-sonnet-4");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ChatId*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidModelId_ThrowsArgumentException(string? invalidModelId)
    {
        // Arrange & Act
        var act = () => Run.Create(ChatId.NewId(), invalidModelId!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ModelId*");
    }

    [Fact]
    public void Complete_WithValidTokenCounts_CompletesRun()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");
        var tokensIn = 150;
        var tokensOut = 230;

        // Act
        run.Complete(tokensIn, tokensOut);

        // Assert
        run.Status.Should().Be(RunStatus.Completed);
        run.CompletedAt.Should().NotBeNull();
        run.CompletedAt.Should().BeAfter(run.StartedAt);
        run.TokensIn.Should().Be(tokensIn);
        run.TokensOut.Should().Be(tokensOut);
        run.TotalTokens.Should().Be(380);
        run.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Theory]
    [InlineData(RunStatus.Completed)]
    [InlineData(RunStatus.Failed)]
    [InlineData(RunStatus.Cancelled)]
    public void Complete_WhenNotRunning_ThrowsInvalidOperationException(RunStatus status)
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Transition to non-Running state
        if (status == RunStatus.Completed)
        {
            run.Complete(100, 200);
        }
        else if (status == RunStatus.Failed)
        {
            run.Fail("Test error");
        }
        else if (status == RunStatus.Cancelled)
        {
            run.Cancel();
        }

        // Act
        var act = () => run.Complete(50, 100);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Fact]
    public void Fail_WithErrorMessage_FailsRun()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");
        var errorMessage = "API timeout after 30s";

        // Act
        run.Fail(errorMessage);

        // Assert
        run.Status.Should().Be(RunStatus.Failed);
        run.ErrorMessage.Should().Be(errorMessage);
        run.CompletedAt.Should().NotBeNull();
        run.CompletedAt.Should().BeAfter(run.StartedAt);
        run.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Fact]
    public void Fail_WithNullErrorMessage_UsesDefaultMessage()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Act
        run.Fail(null!);

        // Assert
        run.Status.Should().Be(RunStatus.Failed);
        run.ErrorMessage.Should().Be("Unknown error");
    }

    [Theory]
    [InlineData(RunStatus.Completed)]
    [InlineData(RunStatus.Failed)]
    [InlineData(RunStatus.Cancelled)]
    public void Fail_WhenNotRunning_ThrowsInvalidOperationException(RunStatus status)
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Transition to non-Running state
        if (status == RunStatus.Completed)
        {
            run.Complete(100, 200);
        }
        else if (status == RunStatus.Failed)
        {
            run.Fail("Test error");
        }
        else if (status == RunStatus.Cancelled)
        {
            run.Cancel();
        }

        // Act
        var act = () => run.Fail("Another error");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Fact]
    public void Cancel_CancelsRun()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Act
        run.Cancel();

        // Assert
        run.Status.Should().Be(RunStatus.Cancelled);
        run.CompletedAt.Should().NotBeNull();
        run.CompletedAt.Should().BeAfter(run.StartedAt);
        run.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Theory]
    [InlineData(RunStatus.Completed)]
    [InlineData(RunStatus.Failed)]
    [InlineData(RunStatus.Cancelled)]
    public void Cancel_WhenNotRunning_ThrowsInvalidOperationException(RunStatus status)
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Transition to non-Running state
        if (status == RunStatus.Completed)
        {
            run.Complete(100, 200);
        }
        else if (status == RunStatus.Failed)
        {
            run.Fail("Test error");
        }
        else if (status == RunStatus.Cancelled)
        {
            run.Cancel();
        }

        // Act
        var act = () => run.Cancel();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{status}*");
    }

    [Fact]
    public void Duration_WhenRunning_ReturnsNull()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Act
        var duration = run.Duration;

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void Duration_WhenCompleted_ReturnsTimeDifference()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");
        var startedAt = run.StartedAt;

        // Act
        run.Complete(100, 200);

        // Assert
        run.Duration.Should().NotBeNull();
        run.Duration.Should().BeGreaterThan(TimeSpan.Zero);
        run.Duration.Should().Be(run.CompletedAt!.Value - startedAt);
    }

    [Fact]
    public void TotalTokens_ReturnsSum()
    {
        // Arrange
        var run = Run.Create(ChatId.NewId(), "claude-sonnet-4");

        // Act
        run.Complete(tokensIn: 250, tokensOut: 380);

        // Assert
        run.TotalTokens.Should().Be(630);
    }

    [Fact]
    public void Reconstitute_CreatesRunFromPersistedData()
    {
        // Arrange
        var id = RunId.NewId();
        var chatId = ChatId.NewId();
        var modelId = "claude-sonnet-4";
        var status = RunStatus.Completed;
        var startedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        var completedAt = DateTimeOffset.UtcNow;
        var tokensIn = 150;
        var tokensOut = 230;
        var sequenceNumber = 3;
        var syncStatus = SyncStatus.Synced;

        // Act
        var run = Run.Reconstitute(
            id,
            chatId,
            modelId,
            status,
            startedAt,
            completedAt,
            tokensIn,
            tokensOut,
            sequenceNumber,
            errorMessage: null,
            syncStatus);

        // Assert
        run.Id.Should().Be(id);
        run.ChatId.Should().Be(chatId);
        run.ModelId.Should().Be(modelId);
        run.Status.Should().Be(status);
        run.StartedAt.Should().Be(startedAt);
        run.CompletedAt.Should().Be(completedAt);
        run.TokensIn.Should().Be(tokensIn);
        run.TokensOut.Should().Be(tokensOut);
        run.SequenceNumber.Should().Be(sequenceNumber);
        run.ErrorMessage.Should().BeNull();
        run.SyncStatus.Should().Be(syncStatus);
    }

    [Fact]
    public void Reconstitute_WithErrorMessage_RestoresError()
    {
        // Arrange
        var errorMessage = "Connection timeout";

        // Act
        var run = Run.Reconstitute(
            RunId.NewId(),
            ChatId.NewId(),
            "claude-sonnet-4",
            RunStatus.Failed,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            0,
            0,
            1,
            errorMessage,
            SyncStatus.Pending);

        // Assert
        run.ErrorMessage.Should().Be(errorMessage);
    }
}
