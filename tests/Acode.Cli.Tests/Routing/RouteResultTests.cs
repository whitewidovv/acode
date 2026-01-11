namespace Acode.Cli.Tests.Routing;

using Acode.Cli.Routing;
using FluentAssertions;
using NSubstitute;

/// <summary>
/// Tests for <see cref="RouteResult"/>.
/// </summary>
public sealed class RouteResultTests
{
    [Fact]
    public void Success_ShouldCreateValidResult()
    {
        // Arrange.
        var command = Substitute.For<ICommand>();
        command.Name.Returns("test");
        var remainingArgs = new[] { "--flag", "value" };

        // Act.
        var result = RouteResult.Success(command, remainingArgs);

        // Assert.
        result.Command.Should().Be(command);
        result.RemainingArgs.Should().BeEquivalentTo(remainingArgs);
        result.IsUnknown.Should().BeFalse();
        result.UnknownName.Should().BeNull();
    }

    [Fact]
    public void Success_WithEmptyArgs_ShouldWork()
    {
        // Arrange.
        var command = Substitute.For<ICommand>();
        var remainingArgs = Array.Empty<string>();

        // Act.
        var result = RouteResult.Success(command, remainingArgs);

        // Assert.
        result.Command.Should().Be(command);
        result.RemainingArgs.Should().BeEmpty();
        result.IsUnknown.Should().BeFalse();
    }

    [Fact]
    public void Unknown_ShouldCreateUnknownResult()
    {
        // Arrange.
        const string unknownCommand = "unknowncmd";

        // Act.
        var result = RouteResult.Unknown(unknownCommand);

        // Assert.
        result.Command.Should().BeNull();
        result.RemainingArgs.Should().BeEmpty();
        result.IsUnknown.Should().BeTrue();
        result.UnknownName.Should().Be(unknownCommand);
    }

    [Fact]
    public void RecordEquality_ShouldWorkCorrectly()
    {
        // Arrange.
        var command = Substitute.For<ICommand>();
        command.Name.Returns("test");
        var args = new[] { "--flag" };

        // Act.
        var result1 = RouteResult.Success(command, args);
        var result2 = RouteResult.Success(command, args);

        // Assert - records with same values should be equal.
        result1.Should().Be(result2);
    }

    [Fact]
    public void Unknown_WithEmptyName_ShouldWork()
    {
        // Act.
        var result = RouteResult.Unknown(string.Empty);

        // Assert.
        result.IsUnknown.Should().BeTrue();
        result.UnknownName.Should().BeEmpty();
    }

    [Fact]
    public void RouteResult_ShouldBeImmutable()
    {
        // Arrange.
        var command = Substitute.For<ICommand>();
        var result = RouteResult.Success(command, new[] { "arg1" });

        // Act - create a copy with different args.
        var modifiedResult = result with
        {
            RemainingArgs = new[] { "arg2" },
        };

        // Assert - original should be unchanged.
        result.RemainingArgs.Should().ContainSingle().Which.Should().Be("arg1");
        modifiedResult.RemainingArgs.Should().ContainSingle().Which.Should().Be("arg2");
    }
}
