namespace Acode.Infrastructure.Tests.Heuristics;

using Acode.Application.Routing;
using Acode.Domain.Modes;
using Acode.Infrastructure.Heuristics;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

/// <summary>
/// Unit tests for <see cref="OverrideResolver"/>.
/// Tests AC-026 to AC-040: Override precedence and validation.
/// </summary>
public sealed class OverrideResolverTests
{
    private readonly ILogger<OverrideResolver> _logger = NullLogger<OverrideResolver>.Instance;

    /// <summary>
    /// Test that request override has highest precedence.
    /// AC-026: Request override highest.
    /// </summary>
    [Fact]
    public void Should_Apply_Request_Override_With_Highest_Precedence()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = "llama3.2:70b",
            SessionOverride = "llama3.2:7b",
            ConfigOverride = "mistral:7b",
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be("llama3.2:70b");
        result.Source.Should().Be(OverrideSource.Request);
    }

    /// <summary>
    /// Test that session override is used when no request override.
    /// AC-027: Session overrides config.
    /// </summary>
    [Fact]
    public void Should_Apply_Session_Override_When_No_Request_Override()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = null,
            SessionOverride = "llama3.2:7b",
            ConfigOverride = "mistral:7b",
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be("llama3.2:7b");
        result.Source.Should().Be(OverrideSource.Session);
    }

    /// <summary>
    /// Test that config override is used when no request or session.
    /// AC-028: Config overrides heuristics.
    /// </summary>
    [Fact]
    public void Should_Apply_Config_Override_When_No_Request_Or_Session()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = null,
            SessionOverride = null,
            ConfigOverride = "mistral:7b",
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be("mistral:7b");
        result.Source.Should().Be(OverrideSource.Config);
    }

    /// <summary>
    /// Test that null is returned when no overrides present.
    /// AC-029: Heuristics lowest (no override).
    /// </summary>
    [Fact]
    public void Should_Return_Null_When_No_Overrides_Present()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = null,
            SessionOverride = null,
            ConfigOverride = null,
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().BeNull();
    }

    /// <summary>
    /// Test that empty strings are treated as no override (skipped).
    /// Empty string is semantically equivalent to null for override purposes.
    /// </summary>
    [Fact]
    public void Should_Treat_Empty_Strings_As_No_Override()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = string.Empty,
            SessionOverride = string.Empty,
            ConfigOverride = string.Empty,
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().BeNull("empty strings should be treated as 'no override'");
    }

    /// <summary>
    /// Test that invalid model ID format is rejected.
    /// AC-032, AC-067: Validate model ID format.
    /// </summary>
    [Fact]
    public void Should_Reject_Invalid_Model_ID()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext { RequestOverride = "invalid-model-no-colon" };

        // Act
        var action = () => resolver.Resolve(context);

        // Assert
        action.Should().Throw<RoutingException>().Which.ErrorCode.Should().Be("ACODE-HEU-001");
    }

    /// <summary>
    /// Test that cloud models are rejected in LocalOnly mode.
    /// AC-066: Validate mode compatibility.
    /// </summary>
    [Fact]
    public void Should_Reject_Model_Incompatible_With_Operating_Mode()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext { RequestOverride = "gpt-4:latest" };

        // Act
        var action = () => resolver.Resolve(context);

        // Assert
        action.Should().Throw<RoutingException>().Which.ErrorCode.Should().Be("ACODE-HEU-002");
    }

    /// <summary>
    /// Test that cloud models are accepted in Burst mode.
    /// </summary>
    [Fact]
    public void Should_Accept_Cloud_Model_In_Burst_Mode()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.Burst, _logger);
        var context = new OverrideContext { RequestOverride = "gpt-4:latest" };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be("gpt-4:latest");
    }

    /// <summary>
    /// Test that override is logged.
    /// AC-034, AC-040: Log override application.
    /// </summary>
    [Fact]
    public void Should_Log_Override_Application()
    {
        // Arrange
        var mockLogger = Substitute.For<ILogger<OverrideResolver>>();
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, mockLogger);
        var context = new OverrideContext { RequestOverride = "llama3.2:70b" };

        // Act
        resolver.Resolve(context);

        // Assert - LogInformation was called
        mockLogger
            .ReceivedWithAnyArgs(1)
            .Log(
                LogLevel.Information,
                Arg.Any<EventId>(),
                Arg.Any<object>(),
                null,
                Arg.Any<Func<object, Exception?, string>>()
            );
    }

    /// <summary>
    /// Test that null context throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Should_Throw_For_Null_Context()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);

        // Act
        var action = () => resolver.Resolve(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Test that empty string overrides are treated as no override.
    /// </summary>
    [Fact]
    public void Should_Treat_Empty_String_As_No_Override()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = string.Empty,
            SessionOverride = string.Empty,
            ConfigOverride = "mistral:7b",
        };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be("mistral:7b");
        result.Source.Should().Be(OverrideSource.Config);
    }

    /// <summary>
    /// Test that override chain description can be retrieved.
    /// </summary>
    [Fact]
    public void Should_Return_Override_Chain_Description()
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext
        {
            RequestOverride = "llama3.2:70b",
            SessionOverride = "llama3.2:7b",
            ConfigOverride = null,
        };

        // Act
        var description = resolver.GetOverrideChainDescription(context);

        // Assert
        description.Should().Contain("Request: llama3.2:70b [ACTIVE]");
        description.Should().Contain("Session: llama3.2:7b");
        description.Should().Contain("Config: (none)");
    }

    /// <summary>
    /// Test various valid model ID formats.
    /// </summary>
    /// <param name="modelId">The model ID to test.</param>
    [Theory]
    [InlineData("llama3.2:7b")]
    [InlineData("llama3.2:70b")]
    [InlineData("mistral:7b-instruct")]
    [InlineData("codellama:34b")]
    [InlineData("phi:latest")]
    public void Should_Accept_Valid_Model_ID_Formats(string modelId)
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext { RequestOverride = modelId };

        // Act
        var result = resolver.Resolve(context);

        // Assert
        result.Should().NotBeNull();
        result!.ModelId.Should().Be(modelId);
    }

    /// <summary>
    /// Test various invalid model ID formats.
    /// </summary>
    /// <param name="modelId">The invalid model ID to test.</param>
    [Theory]
    [InlineData("nocolon")]
    [InlineData("   ")]
    public void Should_Reject_Invalid_Model_ID_Formats(string modelId)
    {
        // Arrange
        var resolver = new OverrideResolver(OperatingMode.LocalOnly, _logger);
        var context = new OverrideContext { RequestOverride = modelId };

        // Act
        var action = () => resolver.Resolve(context);

        // Assert
        action.Should().Throw<RoutingException>();
    }
}
