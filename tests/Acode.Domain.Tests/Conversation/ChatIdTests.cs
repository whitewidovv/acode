// tests/Acode.Domain.Tests/Conversation/ChatIdTests.cs
namespace Acode.Domain.Tests.Conversation;

using System;
using Acode.Domain.Conversation;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for ChatId value object.
/// Verifies ULID format validation and immutability.
/// </summary>
public sealed class ChatIdTests
{
    [Fact]
    public void NewId_GeneratesValidUlid()
    {
        // Act
        var chatId = ChatId.NewId();

        // Assert
        chatId.Value.Should().NotBeNullOrEmpty();
        chatId.Value.Should().HaveLength(26);
        chatId.Value.Should().MatchRegex(@"^[0-9A-Z]{26}$");
    }

    [Fact]
    public void From_WithValidUlid_CreatesChatId()
    {
        // Arrange
        var validUlid = "01HKABC1234567890ABCDEFGHI";

        // Act
        var chatId = ChatId.From(validUlid);

        // Assert
        chatId.Value.Should().Be(validUlid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithNullOrWhiteSpace_ThrowsArgumentException(string? invalidValue)
    {
        // Act
        var act = () => ChatId.From(invalidValue!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ChatId*");
    }

    [Fact]
    public void From_WithInvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var invalidUlid = "SHORT";

        // Act
        var act = () => ChatId.From(invalidUlid);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*26 characters*");
    }

    [Fact]
    public void Empty_ReturnsZeroUlid()
    {
        // Act
        var empty = ChatId.Empty;

        // Assert
        empty.Value.Should().Be("00000000000000000000000000");
    }

    [Fact]
    public void TryParse_WithValidUlid_ReturnsTrue()
    {
        // Arrange
        var validUlid = "01HKABC1234567890ABCDEFGHI";

        // Act
        var success = ChatId.TryParse(validUlid, out var chatId);

        // Assert
        success.Should().BeTrue();
        chatId.Value.Should().Be(validUlid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("SHORT")]
    public void TryParse_WithInvalidValue_ReturnsFalse(string? invalidValue)
    {
        // Act
        var success = ChatId.TryParse(invalidValue, out var chatId);

        // Assert
        success.Should().BeFalse();
        chatId.Should().Be(ChatId.Empty);
    }

    [Fact]
    public void Equality_WithSameValue_ReturnsTrue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var id1 = ChatId.From(ulid);
        var id2 = ChatId.From(ulid);

        // Act & Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValue_ReturnsFalse()
    {
        // Arrange
        var id1 = ChatId.From("01HKABC1234567890ABCDEFGHI");
        var id2 = ChatId.From("01HKDEF1234567890ABCDEFGHI");

        // Act & Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
    }

    [Fact]
    public void ImplicitConversion_ToString_ReturnsValue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var chatId = ChatId.From(ulid);

        // Act
        string value = chatId;

        // Assert
        value.Should().Be(ulid);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        // Arrange
        var ulid = "01HKABC1234567890ABCDEFGHI";
        var chatId = ChatId.From(ulid);

        // Act
        var result = chatId.ToString();

        // Assert
        result.Should().Be(ulid);
    }

    [Fact]
    public void CompareTo_WithSmallerValue_ReturnsPositive()
    {
        // Arrange
        var id1 = ChatId.From("01HKABC1234567890ABCDEFGHZ");
        var id2 = ChatId.From("01HKABC1234567890ABCDEFGHA");

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
        var id1 = ChatId.From(ulid);
        var id2 = ChatId.From(ulid);

        // Act
        var result = id1.CompareTo(id2);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void NewId_GeneratesUniqueIds()
    {
        // Act
        var id1 = ChatId.NewId();
        var id2 = ChatId.NewId();

        // Assert
        id1.Should().NotBe(id2);
    }
}
