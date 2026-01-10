// tests/Acode.Domain.Tests/Worktree/WorktreeIdTests.cs
namespace Acode.Domain.Tests.Worktree;

using System;
using Acode.Domain.Worktree;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for WorktreeId value object.
/// Verifies ULID format validation and immutability.
/// </summary>
public sealed class WorktreeIdTests
{
    [Fact]
    public void From_WithValidUlid_CreatesWorktreeId()
    {
        // Arrange
        var validUlid = "01HKABC1234567890ABCDEFGHI";

        // Act
        var worktreeId = WorktreeId.From(validUlid);

        // Assert
        worktreeId.Value.Should().Be(validUlid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhiteSpace_ThrowsArgumentException(string? invalidValue)
    {
        // Act
        var act = () => WorktreeId.From(invalidValue!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*WorktreeId*");
    }

    [Fact]
    public void From_WithInvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var invalidUlid = "SHORT";

        // Act
        var act = () => WorktreeId.From(invalidUlid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*26 characters*");
    }

    [Fact]
    public void Equality_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var id1 = WorktreeId.From(ulid);
        var id2 = WorktreeId.From(ulid);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = WorktreeId.From("01HKABC1234567890ABCDEFGHI");
        var id2 = WorktreeId.From("01HKDEF1234567890ABCDEFGHI");

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var worktreeId = WorktreeId.From(ulid);

        // Act
        string value = worktreeId;

        // Assert
        value.Should().Be(ulid);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var worktreeId = WorktreeId.From(ulid);

        // Act
        var result = worktreeId.ToString();

        // Assert
        result.Should().Be(ulid);
    }

    [Fact]
    public void CompareTo_WithSmallerValue_ReturnsPositive()
    {
        // Arrange
        var id1 = WorktreeId.From("01HKABC1234567890ABCDEFGHZ");
        var id2 = WorktreeId.From("01HKABC1234567890ABCDEFGHA");

        // Act
        var result = id1.CompareTo(id2);

        // Assert
        result.Should().BeGreaterThan(0);
    }

    [Fact]
    public void CompareTo_WithSameValue_ReturnsZero()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var id1 = WorktreeId.From(ulid);
        var id2 = WorktreeId.From(ulid);

        // Act
        var result = id1.CompareTo(id2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void FromPath_WithSamePath_ProducesSameId()
    {
        // Arrange
        var path = "/home/user/project/feature/auth";

        // Act
        var id1 = WorktreeId.FromPath(path);
        var id2 = WorktreeId.FromPath(path);

        // Assert
        id1.Should().Be(id2, "same path should produce same deterministic ID");
        id1.Value.Should().Be(id2.Value);
    }

    [Fact]
    public void FromPath_WithDifferentPaths_ProducesDifferentIds()
    {
        // Arrange
        var path1 = "/home/user/project/feature/auth";
        var path2 = "/home/user/project/feature/payments";

        // Act
        var id1 = WorktreeId.FromPath(path1);
        var id2 = WorktreeId.FromPath(path2);

        // Assert
        id1.Should().NotBe(id2, "different paths should produce different IDs");
        id1.Value.Should().NotBe(id2.Value);
    }

    [Fact]
    public void FromPath_ProducesValidUlidFormat()
    {
        // Arrange
        var path = "/home/user/project/feature/auth";

        // Act
        var id = WorktreeId.FromPath(path);

        // Assert
        id.Value.Should().HaveLength(26, "deterministic ID should match ULID format length");
        id.Value.Should().MatchRegex("^[0-9A-HJKMNP-TV-Z]{26}$", "should be valid Base32 ULID format");
    }

    [Fact]
    public void FromPath_WithNormalizedPaths_ProducesSameId()
    {
        // Arrange
        var path1 = "/home/user/project/feature/auth";
        var path2 = "/home/user/project/feature/auth/";  // Trailing slash

        // Act
        var id1 = WorktreeId.FromPath(path1);
        var id2 = WorktreeId.FromPath(path2);

        // Assert
        id1.Should().Be(id2, "normalized paths should produce same ID");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void FromPath_WithNullOrWhiteSpace_ThrowsArgumentException(string? invalidPath)
    {
        // Act
        var act = () => WorktreeId.FromPath(invalidPath!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*path*");
    }
}
