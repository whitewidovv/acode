// tests/Acode.Domain.Tests/Concurrency/WorktreeBindingTests.cs
namespace Acode.Domain.Tests.Concurrency;

using System;
using Acode.Domain.Concurrency;
using Acode.Domain.Conversation;
using Acode.Domain.Worktree;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for WorktreeBinding domain entity.
/// Verifies binding creation, reconstitution, and immutability.
/// </summary>
public sealed class WorktreeBindingTests
{
    [Fact]
    public void Create_WithValidIds_CreatesBinding()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");

        // Act
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Assert
        binding.WorktreeId.Should().Be(worktreeId);
        binding.ChatId.Should().Be(chatId);
        binding.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Reconstitute_WithStoredValues_RecreatesBinding()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/payments");
        var chatId = ChatId.From("01HKDEF1234567890ABCDEFGHI");
        var createdAt = DateTimeOffset.UtcNow.AddDays(-7);

        // Act
        var binding = WorktreeBinding.Reconstitute(worktreeId, chatId, createdAt);

        // Assert
        binding.WorktreeId.Should().Be(worktreeId);
        binding.ChatId.Should().Be(chatId);
        binding.CreatedAt.Should().Be(createdAt);
    }

    [Fact]
    public void WorktreeId_IsReadOnly()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act & Assert
        // Attempting to set would cause compile error - verify it's a get-only property
        var retrievedWorktreeId = binding.WorktreeId;
        retrievedWorktreeId.Should().Be(worktreeId);
    }

    [Fact]
    public void ChatId_IsReadOnly()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act & Assert
        var retrievedChatId = binding.ChatId;
        retrievedChatId.Should().Be(chatId);
    }

    [Fact]
    public void CreatedAt_IsReadOnly()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Act & Assert
        var createdAt = binding.CreatedAt;
        createdAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Create_SetsCreatedAtToCurrentTime()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var binding = WorktreeBinding.Create(worktreeId, chatId);

        // Assert
        var afterCreate = DateTimeOffset.UtcNow;
        binding.CreatedAt.Should().BeOnOrAfter(beforeCreate);
        binding.CreatedAt.Should().BeOnOrBefore(afterCreate);
    }

    [Fact]
    public void Reconstitute_PreservesOriginalCreatedAt()
    {
        // Arrange
        var worktreeId = WorktreeId.FromPath("/home/user/project/feature/auth");
        var chatId = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var originalCreatedAt = DateTimeOffset.UtcNow.AddMonths(-3);

        // Act
        var binding = WorktreeBinding.Reconstitute(worktreeId, chatId, originalCreatedAt);

        // Assert
        binding.CreatedAt.Should().Be(originalCreatedAt, "reconstitute should preserve exact original timestamp");
    }
}
