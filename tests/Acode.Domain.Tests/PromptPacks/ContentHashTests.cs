using Acode.Domain.PromptPacks;
using FluentAssertions;

namespace Acode.Domain.Tests.PromptPacks;

/// <summary>
/// Tests for <see cref="ContentHash"/> value object.
/// </summary>
public class ContentHashTests
{
    [Fact]
    public void Constructor_ValidHash_ShouldSucceed()
    {
        // Arrange
        var validHash = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";

        // Act
        var hash = new ContentHash(validHash);

        // Assert
        hash.Value.Should().Be(validHash);
    }

    [Fact]
    public void Constructor_InvalidLength_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidHash = "abc123"; // Too short

        // Act
        var act = () => new ContentHash(invalidHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*64 characters*");
    }

    [Fact]
    public void Constructor_InvalidCharacters_ShouldThrowArgumentException()
    {
        // Arrange
        var invalidHash = "xyz2c3d4e5f6789012345678901234567890123456789012345678901234abcd"; // Contains 'xyz'

        // Act
        var act = () => new ContentHash(invalidHash);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*hexadecimal*");
    }

    [Fact]
    public void Constructor_UppercaseHash_ShouldConvertToLowercase()
    {
        // Arrange
        var uppercaseHash = "A1B2C3D4E5F6789012345678901234567890123456789012345678901234ABCD";
        var expectedLowercase = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";

        // Act
        var hash = new ContentHash(uppercaseHash);

        // Assert
        hash.Value.Should().Be(expectedLowercase);
    }

    [Fact]
    public void Equality_SameHash_ShouldBeEqual()
    {
        // Arrange
        var hash1 = new ContentHash("a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd");
        var hash2 = new ContentHash("a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd");

        // Act & Assert
        hash1.Should().Be(hash2);
        (hash1 == hash2).Should().BeTrue();
        (hash1 != hash2).Should().BeFalse();
    }

    [Fact]
    public void Equality_DifferentHash_ShouldNotBeEqual()
    {
        // Arrange
        var hash1 = new ContentHash("a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd");
        var hash2 = new ContentHash("b2c3d4e5f6789012345678901234567890123456789012345678901234abcdef");

        // Act & Assert
        hash1.Should().NotBe(hash2);
        (hash1 == hash2).Should().BeFalse();
        (hash1 != hash2).Should().BeTrue();
    }

    [Fact]
    public void ToString_ShouldReturnLowercaseHex()
    {
        // Arrange
        var hashValue = "a1b2c3d4e5f6789012345678901234567890123456789012345678901234abcd";
        var hash = new ContentHash(hashValue);

        // Act
        var result = hash.ToString();

        // Assert
        result.Should().Be(hashValue);
    }
}
