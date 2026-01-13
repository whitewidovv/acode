namespace Acode.Domain.Tests.Common;

using Acode.Domain.Common;
using FluentAssertions;

/// <summary>
/// Tests for Base62Encoder utility following TDD (RED phase).
/// </summary>
public class Base62EncoderTests
{
    [Fact]
    public void Encode_WithValidGuid_ReturnsBase62String()
    {
        // Arrange
        var guid = Guid.Parse("00000000-0000-0000-0000-000000000001");

        // Act
        var result = Base62Encoder.Encode(guid);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().MatchRegex("^[0-9A-Za-z]+$");
    }

    [Fact]
    public void Encode_WithZeroGuid_ReturnsZero()
    {
        // Arrange
        var guid = Guid.Empty;

        // Act
        var result = Base62Encoder.Encode(guid);

        // Assert
        result.Should().Be("0");
    }

    [Fact]
    public void Encode_SameGuid_ReturnsSameEncoding()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result1 = Base62Encoder.Encode(guid);
        var result2 = Base62Encoder.Encode(guid);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void Encode_DifferentGuids_ReturnsDifferentEncodings()
    {
        // Arrange
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        // Act
        var result1 = Base62Encoder.Encode(guid1);
        var result2 = Base62Encoder.Encode(guid2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void Encode_OnlyUsesBase62Alphabet()
    {
        // Arrange
        for (int i = 0; i < 100; i++)
        {
            var guid = Guid.NewGuid();

            // Act
            var result = Base62Encoder.Encode(guid);

            // Assert
            result.Should().MatchRegex("^[0-9A-Za-z]+$", "Base62 only uses 0-9, A-Z, a-z");
        }
    }

    [Fact]
    public void Encode_ProducesReasonableLength()
    {
        // Arrange
        var guid = Guid.NewGuid();

        // Act
        var result = Base62Encoder.Encode(guid);

        // Assert
        // 128-bit GUID encoded in base62 should be ~22 characters
        result.Length.Should().BeInRange(20, 25);
    }
}
