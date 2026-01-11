using Acode.Application.Fallback;
using Acode.Application.Routing;
using Acode.Cli.Commands;
using FluentAssertions;
using NSubstitute;

namespace Acode.Cli.Tests.Commands;

/// <summary>
/// Tests for FallbackCommand.
/// </summary>
/// <remarks>
/// AC-044 to AC-054: CLI command tests for fallback management.
/// </remarks>
public sealed class FallbackCommandTests
{
    private readonly IFallbackHandler _mockHandler;
    private readonly IFallbackConfiguration _mockConfig;
    private readonly FallbackCommand _command;

    public FallbackCommandTests()
    {
        _mockHandler = Substitute.For<IFallbackHandler>();
        _mockConfig = Substitute.For<IFallbackConfiguration>();
        _command = new FallbackCommand(_mockHandler, _mockConfig);
    }

    [Fact]
    public void Name_Should_Return_Fallback()
    {
        _command.Name.Should().Be("fallback");
    }

    [Fact]
    public void Aliases_Should_Include_Fb()
    {
        _command.Aliases.Should().Contain("fb");
    }

    [Fact]
    public void Description_Should_Be_Meaningful()
    {
        _command.Description.Should().NotBeNullOrEmpty();
        _command.Description.Should().Contain("fallback");
    }

    [Fact]
    public void GetHelp_Should_Include_Subcommands()
    {
        var help = _command.GetHelp();

        help.Should().Contain("status");
        help.Should().Contain("reset");
        help.Should().Contain("test");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoSubcommand_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext([]);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownSubcommand_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(["unknown"]);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_Status_ShowsGlobalChain()
    {
        // Arrange
        _mockConfig
            .GetGlobalChain()
            .Returns(new List<string> { "llama3.2:70b", "mistral:22b", "llama3.2:7b" });
        _mockConfig.GetRoleChain(Arg.Any<AgentRole>()).Returns(new List<string>());
        _mockConfig.Policy.Returns(EscalationPolicy.RetryThenFallback);
        _mockConfig.FailureThreshold.Returns(5);
        _mockConfig.CoolingPeriod.Returns(TimeSpan.FromSeconds(60));
        _mockConfig.RetryCount.Returns(2);
        _mockConfig.TimeoutMs.Returns(60000);
        _mockHandler.GetAllCircuitStates().Returns(new Dictionary<string, CircuitStateInfo>());

        var output = new StringWriter();
        var context = CreateContext(["status"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Global Fallback Chain");
        outputText.Should().Contain("llama3.2:70b");
        outputText.Should().Contain("mistral:22b");
    }

    [Fact]
    public async Task ExecuteAsync_Status_ShowsCircuitStates()
    {
        // Arrange
        _mockConfig.GetGlobalChain().Returns(new List<string>());
        _mockConfig.GetRoleChain(Arg.Any<AgentRole>()).Returns(new List<string>());
        _mockConfig.Policy.Returns(EscalationPolicy.CircuitBreaker);
        _mockConfig.FailureThreshold.Returns(5);
        _mockConfig.CoolingPeriod.Returns(TimeSpan.FromSeconds(60));
        _mockConfig.RetryCount.Returns(2);
        _mockConfig.TimeoutMs.Returns(60000);

        var circuitStates = new Dictionary<string, CircuitStateInfo>
        {
            ["llama3.2:70b"] = new CircuitStateInfo
            {
                ModelId = "llama3.2:70b",
                State = CircuitState.Open,
                FailureCount = 5,
                LastFailureTime = DateTimeOffset.UtcNow.AddMinutes(-1),
                NextRetryTime = DateTimeOffset.UtcNow.AddSeconds(30),
            },
        };
        _mockHandler.GetAllCircuitStates().Returns(circuitStates);

        var output = new StringWriter();
        var context = CreateContext(["status"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        var outputText = output.ToString();
        outputText.Should().Contain("Circuit Breaker States");
        outputText.Should().Contain("llama3.2:70b");
        outputText.Should().Contain("OPEN");
    }

    [Fact]
    public async Task ExecuteAsync_Reset_WithoutArgs_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(["reset"]);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_Reset_WithAll_ResetsAllCircuits()
    {
        // Arrange
        var output = new StringWriter();
        var context = CreateContext(["reset", "--all"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        _mockHandler.Received(1).ResetAllCircuits();
        output.ToString().Should().Contain("All circuit breakers have been reset");
    }

    [Fact]
    public async Task ExecuteAsync_Reset_WithModel_ResetsSpecificCircuit()
    {
        // Arrange
        var output = new StringWriter();
        var context = CreateContext(["reset", "--model", "llama3.2:70b"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        _mockHandler.Received(1).ResetCircuit("llama3.2:70b");
        output.ToString().Should().Contain("llama3.2:70b");
    }

    [Fact]
    public async Task ExecuteAsync_Test_WithNoChain_ReturnsConfigurationError()
    {
        // Arrange
        _mockConfig.GetGlobalChain().Returns(new List<string>());
        var context = CreateContext(["test"]);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.ConfigurationError);
    }

    [Fact]
    public async Task ExecuteAsync_Test_WithHealthyChain_ReturnsSuccess()
    {
        // Arrange
        _mockConfig.GetGlobalChain().Returns(new List<string> { "llama3.2:70b", "mistral:22b" });
        _mockHandler
            .GetCircuitState(Arg.Any<string>())
            .Returns(
                new CircuitStateInfo
                {
                    ModelId = "test",
                    State = CircuitState.Closed,
                    FailureCount = 0,
                }
            );

        var output = new StringWriter();
        var context = CreateContext(["test"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        output.ToString().Should().Contain("Chain is healthy");
    }

    [Fact]
    public async Task ExecuteAsync_Test_WithOpenCircuit_ReturnsRuntimeError()
    {
        // Arrange
        _mockConfig.GetGlobalChain().Returns(new List<string> { "llama3.2:70b" });
        _mockHandler
            .GetCircuitState("llama3.2:70b")
            .Returns(
                new CircuitStateInfo
                {
                    ModelId = "llama3.2:70b",
                    State = CircuitState.Open,
                    FailureCount = 5,
                    NextRetryTime = DateTimeOffset.UtcNow.AddSeconds(30),
                }
            );

        var output = new StringWriter();
        var context = CreateContext(["test"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.RuntimeError);
        output.ToString().Should().Contain("Chain has issues");
    }

    [Fact]
    public async Task ExecuteAsync_Test_WithRole_UsesRoleChain()
    {
        // Arrange
        _mockConfig.GetRoleChain(AgentRole.Planner).Returns(new List<string> { "llama3.2:70b" });
        _mockHandler
            .GetCircuitState(Arg.Any<string>())
            .Returns(
                new CircuitStateInfo
                {
                    ModelId = "test",
                    State = CircuitState.Closed,
                    FailureCount = 0,
                }
            );

        var output = new StringWriter();
        var context = CreateContext(["test", "--role", "planner"], output);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.Success);
        output.ToString().Should().Contain("Planner");
    }

    [Fact]
    public async Task ExecuteAsync_Test_WithInvalidRole_ReturnsInvalidArguments()
    {
        // Arrange
        var context = CreateContext(["test", "--role", "invalid"]);

        // Act
        var result = await _command.ExecuteAsync(context).ConfigureAwait(true);

        // Assert
        result.Should().Be(ExitCode.InvalidArguments);
    }

    [Fact]
    public async Task ExecuteAsync_NullContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _command.ExecuteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().ConfigureAwait(true);
    }

    [Fact]
    public void Constructor_NullHandler_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FallbackCommand(null!, _mockConfig);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("fallbackHandler");
    }

    [Fact]
    public void Constructor_NullConfig_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new FallbackCommand(_mockHandler, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    private static CommandContext CreateContext(string[] args, TextWriter? output = null)
    {
        output ??= new StringWriter();
        return new CommandContext
        {
            Configuration = new Dictionary<string, object>(),
            Args = args,
            Formatter = new ConsoleFormatter(output, enableColors: false),
            Output = output,
            CancellationToken = CancellationToken.None,
        };
    }
}
