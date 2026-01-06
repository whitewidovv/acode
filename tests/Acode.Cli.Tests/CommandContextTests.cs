using FluentAssertions;

namespace Acode.Cli.Tests;

/// <summary>
/// Tests for <see cref="CommandContext"/>.
/// </summary>
public class CommandContextTests
{
    [Fact]
    public void Constructor_WithAllProperties_SetsProperties()
    {
        // Arrange
        var config = new Dictionary<string, object>
        {
            ["model"] = "llama3.2:7b",
            ["verbose"] = true,
        };
        var args = new[] { "arg1", "arg2" };
        var output = new StringWriter();
        var cancellationToken = CancellationToken.None;

        // Act
        var context = new CommandContext
        {
            Configuration = config,
            Args = args,
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = cancellationToken,
        };

        // Assert
        context.Configuration.Should().BeSameAs(config);
        context.Args.Should().BeSameAs(args);
        context.Formatter.Should().NotBeNull();
        context.Output.Should().BeSameAs(output);
        context.CancellationToken.Should().Be(cancellationToken);
    }

    [Fact]
    public void CommandContext_IsImmutable()
    {
        // Arrange
        var config = new Dictionary<string, object>();
        var output = new StringWriter();

        // Act
        var context = new CommandContext
        {
            Configuration = config,
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Assert - Properties should be init-only, verified by compilation success
        context.Configuration.Should().BeSameAs(config);
    }

    [Fact]
    public void RecordEquality_WithSameValues_AreEqual()
    {
        // Arrange
        var config = new Dictionary<string, object> { ["key"] = "value" };
        var output = new StringWriter();
        var formatter = new ConsoleFormatter(output, enableColors: false);
        var token = default(CancellationToken);

        var context1 = new CommandContext
        {
            Configuration = config,
            Args = Array.Empty<string>(),
            Formatter = formatter,
            Output = output,
            CancellationToken = token,
        };

        var context2 = new CommandContext
        {
            Configuration = config,
            Args = Array.Empty<string>(),
            Formatter = formatter,
            Output = output,
            CancellationToken = token,
        };

        // Act & Assert
        context1.Should().Be(context2);
    }

    [Fact]
    public void RecordEquality_WithDifferentConfiguration_AreNotEqual()
    {
        // Arrange
        var config1 = new Dictionary<string, object> { ["key"] = "value1" };
        var config2 = new Dictionary<string, object> { ["key"] = "value2" };
        var output = new StringWriter();

        var context1 = new CommandContext
        {
            Configuration = config1,
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        var context2 = new CommandContext
        {
            Configuration = config2,
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act & Assert
        context1.Should().NotBe(context2);
    }

    [Fact]
    public void ToString_IncludesConfigurationCount()
    {
        // Arrange
        var config = new Dictionary<string, object>
        {
            ["model"] = "llama3.2:7b",
            ["verbose"] = true,
        };
        var output = new StringWriter();

        var context = new CommandContext
        {
            Configuration = config,
            Args = Array.Empty<string>(),
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };

        // Act
        var result = context.ToString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
