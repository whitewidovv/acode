namespace Acode.Domain.Tests.Risks;

using Acode.Domain.Risks;
using FluentAssertions;

public class MitigationIdTests
{
    [Theory]
    [InlineData("MIT-001")]
    [InlineData("MIT-042")]
    [InlineData("MIT-124")]
    [InlineData("MIT-999")]
    public void Should_Accept_Valid_Mitigation_IDs(string mitId)
    {
        // Act
        var id = new MitigationId(mitId);

        // Assert
        id.Should().NotBeNull();
        id.Value.Should().Be(mitId);
    }

    [Theory]
    [InlineData("MIT-01")] // Too few digits
    [InlineData("MIT-1000")] // Too many digits
    [InlineData("mit-001")] // Lowercase
    [InlineData("RISK-001")] // Wrong prefix
    [InlineData("MIT_001")] // Wrong delimiter
    [InlineData("M-001")] // Wrong prefix
    public void Should_Reject_Invalid_Mitigation_IDs(string mitId)
    {
        // Act
        Action act = () => new MitigationId(mitId);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Reject_Null_Mitigation_ID()
    {
        // Act
        Action act = () => new MitigationId(null!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Reject_Empty_Mitigation_ID()
    {
        // Act
        Action act = () => new MitigationId(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Should_Reject_Whitespace_Mitigation_ID()
    {
        // Act
        Action act = () => new MitigationId("   ");

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SequenceNumber_Should_Parse_Correctly()
    {
        // Arrange & Act
        var id1 = new MitigationId("MIT-001");
        var id2 = new MitigationId("MIT-042");
        var id3 = new MitigationId("MIT-124");

        // Assert
        id1.SequenceNumber.Should().Be(1);
        id2.SequenceNumber.Should().Be(42);
        id3.SequenceNumber.Should().Be(124);
    }

    [Fact]
    public void ToString_Should_Return_Value()
    {
        // Arrange
        var id = new MitigationId("MIT-042");

        // Act
        var result = id.ToString();

        // Assert
        result.Should().Be("MIT-042");
    }

    [Fact]
    public void Should_Be_Equal_When_Values_Match()
    {
        // Arrange
        var id1 = new MitigationId("MIT-001");
        var id2 = new MitigationId("MIT-001");

        // Assert
        id1.Should().Be(id2);
        (id1 == id2).Should().BeTrue();
        (id1 != id2).Should().BeFalse();
    }

    [Fact]
    public void Should_Not_Be_Equal_When_Values_Differ()
    {
        // Arrange
        var id1 = new MitigationId("MIT-001");
        var id2 = new MitigationId("MIT-002");

        // Assert
        id1.Should().NotBe(id2);
        (id1 == id2).Should().BeFalse();
        (id1 != id2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_Should_Be_Same_For_Equal_Values()
    {
        // Arrange
        var id1 = new MitigationId("MIT-001");
        var id2 = new MitigationId("MIT-001");

        // Assert
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }
}
