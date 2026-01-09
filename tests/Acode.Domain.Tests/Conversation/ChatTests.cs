// tests/Acode.Domain.Tests/Conversation/ChatTests.cs
#pragma warning disable CA2007 // ConfigureAwait not required in test code
namespace Acode.Domain.Tests.Conversation;

using System;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for Chat aggregate root.
/// Verifies chat creation, modification, soft delete, and version tracking.
/// </summary>
public sealed class ChatTests
{
    [Fact]
    public void Create_WithValidTitle_CreatesChat()
    {
        // Arrange
        var title = "Feature: Add Authentication";
        var worktreeId = WorktreeId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        var chat = Chat.Create(title, worktreeId);

        // Assert
        chat.Should().NotBeNull();
        chat.Id.Should().NotBe(ChatId.Empty);
        chat.Title.Should().Be(title);
        chat.WorktreeBinding.Should().Be(worktreeId);
        chat.IsDeleted.Should().BeFalse();
        chat.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        chat.UpdatedAt.Should().Be(chat.CreatedAt);
        chat.Version.Should().Be(1);
        chat.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTitle_ThrowsArgumentException(string? invalidTitle)
    {
        // Arrange
        var worktreeId = WorktreeId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        var act = () => Chat.Create(invalidTitle!, worktreeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*title*");
    }

    [Fact]
    public void Create_WithTitleExceeding500Chars_ThrowsArgumentException()
    {
        // Arrange
        var longTitle = new string('a', 501);
        var worktreeId = WorktreeId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        var act = () => Chat.Create(longTitle, worktreeId);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*500 characters*");
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_UpdatesTitleAndVersion()
    {
        // Arrange
        var chat = Chat.Create("Original Title", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        var originalVersion = chat.Version;

        // Act
        chat.UpdateTitle("Updated Title");

        // Assert
        chat.Title.Should().Be("Updated Title");
        chat.Version.Should().Be(originalVersion + 1);
        chat.UpdatedAt.Should().BeAfter(chat.CreatedAt);
        chat.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Fact]
    public void UpdateTitle_OnDeletedChat_ThrowsInvalidOperationException()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.Delete();

        // Act
        var act = () => chat.UpdateTitle("New Title");

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*deleted*");
    }

    [Fact]
    public void Delete_SetsIsDeletedAndDeletedAt()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        chat.Delete();

        // Assert
        chat.IsDeleted.Should().BeTrue();
        chat.DeletedAt.Should().NotBeNull();
        chat.DeletedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        chat.Version.Should().Be(2);
        chat.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Fact]
    public void Delete_OnAlreadyDeletedChat_IsIdempotent()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.Delete();
        var versionAfterFirstDelete = chat.Version;
        var deletedAtAfterFirstDelete = chat.DeletedAt;

        // Act
        chat.Delete();

        // Assert
        chat.IsDeleted.Should().BeTrue();
        chat.Version.Should().Be(versionAfterFirstDelete);
        chat.DeletedAt.Should().Be(deletedAtAfterFirstDelete);
    }

    [Fact]
    public void Restore_OnDeletedChat_ClearsDeletedState()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.Delete();

        // Act
        chat.Restore();

        // Assert
        chat.IsDeleted.Should().BeFalse();
        chat.DeletedAt.Should().BeNull();
        chat.Version.Should().Be(3);
    }

    [Fact]
    public void Restore_OnNonDeletedChat_ThrowsInvalidOperationException()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        var act = () => chat.Restore();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*not deleted*");
    }

    [Fact]
    public void AddTag_AddsTagToCollection()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        chat.AddTag("bug-fix");
        chat.AddTag("priority-high");

        // Assert
        chat.Tags.Should().HaveCount(2);
        chat.Tags.Should().Contain(new[] { "bug-fix", "priority-high" });
        chat.Version.Should().Be(3); // 1 (create) + 2 (add tag x2)
    }

    [Fact]
    public void AddTag_NormalizesCaseAndTrims()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        chat.AddTag("  BUG-FIX  ");

        // Assert
        chat.Tags.Should().Contain("bug-fix");
    }

    [Fact]
    public void AddTag_IgnoresDuplicates()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.AddTag("bug-fix");
        var versionAfterFirst = chat.Version;

        // Act
        chat.AddTag("bug-fix");

        // Assert
        chat.Tags.Should().HaveCount(1);
        chat.Version.Should().Be(versionAfterFirst); // No version increment for duplicate
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AddTag_WithInvalidTag_ThrowsArgumentException(string? invalidTag)
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        var act = () => chat.AddTag(invalidTag!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*tag*");
    }

    [Fact]
    public void RemoveTag_RemovesTagFromCollection()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.AddTag("bug-fix");
        chat.AddTag("priority-high");

        // Act
        var removed = chat.RemoveTag("bug-fix");

        // Assert
        removed.Should().BeTrue();
        chat.Tags.Should().HaveCount(1);
        chat.Tags.Should().Contain("priority-high");
        chat.Version.Should().Be(4); // 1 (create) + 2 (add) + 1 (remove)
    }

    [Fact]
    public void RemoveTag_WithNonExistentTag_ReturnsFalse()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        var versionBefore = chat.Version;

        // Act
        var removed = chat.RemoveTag("non-existent");

        // Assert
        removed.Should().BeFalse();
        chat.Version.Should().Be(versionBefore); // No version increment
    }

    [Fact]
    public void BindToWorktree_SetsWorktreeBinding()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", null);
        var worktreeId = WorktreeId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        chat.BindToWorktree(worktreeId);

        // Assert
        chat.WorktreeBinding.Should().Be(worktreeId);
        chat.Version.Should().Be(2);
        chat.SyncStatus.Should().Be(SyncStatus.Pending);
    }

    [Fact]
    public void MarkSynced_SetsSyncStatusToSynced()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));
        chat.SyncStatus.Should().Be(SyncStatus.Pending);

        // Act
        chat.MarkSynced();

        // Assert
        chat.SyncStatus.Should().Be(SyncStatus.Synced);
    }

    [Fact]
    public void MarkConflict_SetsSyncStatusToConflict()
    {
        // Arrange
        var chat = Chat.Create("Test Chat", WorktreeId.From("01HKABC1234567890ABCDEFGHI"));

        // Act
        chat.MarkConflict();

        // Assert
        chat.SyncStatus.Should().Be(SyncStatus.Conflict);
    }
}
