using Acode.Application.Fallback;
using Acode.Application.Routing;

namespace Acode.Cli.Commands;

/// <summary>
/// Command for managing fallback chains and circuit breakers.
/// </summary>
/// <remarks>
/// <para>AC-044 to AC-054: CLI commands for fallback management.</para>
/// <para>Subcommands: status, reset, test.</para>
/// </remarks>
public sealed class FallbackCommand : ICommand
{
    private readonly IFallbackHandler _fallbackHandler;
    private readonly IFallbackConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackCommand"/> class.
    /// </summary>
    /// <param name="fallbackHandler">The fallback handler.</param>
    /// <param name="configuration">The fallback configuration.</param>
    public FallbackCommand(IFallbackHandler fallbackHandler, IFallbackConfiguration configuration)
    {
        _fallbackHandler =
            fallbackHandler ?? throw new ArgumentNullException(nameof(fallbackHandler));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <inheritdoc/>
    public string Name => "fallback";

    /// <inheritdoc/>
    public string[]? Aliases => ["fb"];

    /// <inheritdoc/>
    public string Description => "Manage model fallback chains and circuit breakers";

    /// <inheritdoc/>
    public async Task<ExitCode> ExecuteAsync(CommandContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Args.Length == 0)
        {
            await context
                .Output.WriteLineAsync(
                    "Error: Missing subcommand. Use 'acode fallback status', 'acode fallback reset', or 'acode fallback test'."
                )
                .ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        var subcommand = context.Args[0];

        return subcommand.ToLowerInvariant() switch
        {
            "status" => await ShowStatusAsync(context).ConfigureAwait(false),
            "reset" => await ResetCircuitsAsync(context).ConfigureAwait(false),
            "test" => await TestChainAsync(context).ConfigureAwait(false),
            _ => await WriteUnknownSubcommandAsync(context, subcommand).ConfigureAwait(false),
        };
    }

    /// <inheritdoc/>
    public string GetHelp()
    {
        return @"Usage: acode fallback <subcommand> [options]

Subcommands:
  status    Show fallback chains and circuit breaker states
  reset     Reset circuit breakers
  test      Test fallback chain availability

Options:
  --model <id>    Specify a model ID (for reset/test)
  --all           Reset all circuit breakers
  --role <role>   Specify agent role (for test)

Examples:
  acode fallback status
  acode fallback reset --model llama3.2:70b
  acode fallback reset --all
  acode fallback test --role planner";
    }

    private static async Task<ExitCode> WriteUnknownSubcommandAsync(
        CommandContext context,
        string subcommand
    )
    {
        await context
            .Output.WriteLineAsync($"Error: Unknown subcommand '{subcommand}'.")
            .ConfigureAwait(false);
        await context
            .Output.WriteLineAsync(
                "Use 'acode fallback status', 'acode fallback reset', or 'acode fallback test'."
            )
            .ConfigureAwait(false);
        return ExitCode.InvalidArguments;
    }

    private static string? GetArgValue(string[] args, string key)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    /// <summary>
    /// Shows fallback chains and circuit breaker states (AC-044 to AC-046, AC-049, AC-050).
    /// </summary>
    private async Task<ExitCode> ShowStatusAsync(CommandContext context)
    {
        await context.Output.WriteLineAsync("Fallback Status").ConfigureAwait(false);
        await context.Output.WriteLineAsync(new string('=', 50)).ConfigureAwait(false);
        await context.Output.WriteLineAsync().ConfigureAwait(false);

        // Show global chain
        await context.Output.WriteLineAsync("Global Fallback Chain:").ConfigureAwait(false);
        var globalChain = _configuration.GetGlobalChain();
        if (globalChain.Count == 0)
        {
            await context.Output.WriteLineAsync("  (none configured)").ConfigureAwait(false);
        }
        else
        {
            for (int i = 0; i < globalChain.Count; i++)
            {
                await context
                    .Output.WriteLineAsync($"  {i + 1}. {globalChain[i]}")
                    .ConfigureAwait(false);
            }
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);

        // Show per-role chains
        await context.Output.WriteLineAsync("Per-Role Fallback Chains:").ConfigureAwait(false);
        var roles = new[] { AgentRole.Planner, AgentRole.Coder, AgentRole.Reviewer };
        var hasRoleChains = false;

        foreach (var role in roles)
        {
            var roleChain = _configuration.GetRoleChain(role);
            if (roleChain.Count > 0)
            {
                hasRoleChains = true;
                await context.Output.WriteLineAsync($"  {role}:").ConfigureAwait(false);
                for (int i = 0; i < roleChain.Count; i++)
                {
                    await context
                        .Output.WriteLineAsync($"    {i + 1}. {roleChain[i]}")
                        .ConfigureAwait(false);
                }
            }
        }

        if (!hasRoleChains)
        {
            await context
                .Output.WriteLineAsync("  (none configured, using global chain)")
                .ConfigureAwait(false);
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);

        // Show circuit breaker states
        await context.Output.WriteLineAsync("Circuit Breaker States:").ConfigureAwait(false);
        var circuitStates = _fallbackHandler.GetAllCircuitStates();

        if (circuitStates.Count == 0)
        {
            await context.Output.WriteLineAsync("  (no circuits tracked)").ConfigureAwait(false);
        }
        else
        {
            foreach (var kvp in circuitStates)
            {
                var state = kvp.Value;
                var stateDisplay = state.State switch
                {
                    CircuitState.Closed => "CLOSED (healthy)",
                    CircuitState.Open => "OPEN (disabled)",
                    CircuitState.HalfOpen => "HALF-OPEN (testing)",
                    _ => state.State.ToString(),
                };

                await context.Output.WriteLineAsync($"  {kvp.Key}:").ConfigureAwait(false);
                await context
                    .Output.WriteLineAsync($"    State: {stateDisplay}")
                    .ConfigureAwait(false);
                await context
                    .Output.WriteLineAsync($"    Failures: {state.FailureCount}")
                    .ConfigureAwait(false);

                if (state.LastFailureTime.HasValue)
                {
                    await context
                        .Output.WriteLineAsync(
                            $"    Last Failure: {state.LastFailureTime.Value:yyyy-MM-dd HH:mm:ss}"
                        )
                        .ConfigureAwait(false);
                }

                if (state.NextRetryTime.HasValue && state.State == CircuitState.Open)
                {
                    await context
                        .Output.WriteLineAsync(
                            $"    Next Retry: {state.NextRetryTime.Value:yyyy-MM-dd HH:mm:ss}"
                        )
                        .ConfigureAwait(false);
                }

                await context
                    .Output.WriteLineAsync(
                        $"    Allowing Requests: {(state.IsAllowingRequests ? "yes" : "no")}"
                    )
                    .ConfigureAwait(false);
            }
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);

        // Show configuration
        await context.Output.WriteLineAsync("Configuration:").ConfigureAwait(false);
        await context
            .Output.WriteLineAsync($"  Policy: {_configuration.Policy}")
            .ConfigureAwait(false);
        await context
            .Output.WriteLineAsync($"  Failure Threshold: {_configuration.FailureThreshold}")
            .ConfigureAwait(false);
        await context
            .Output.WriteLineAsync(
                $"  Cooling Period: {_configuration.CoolingPeriod.TotalSeconds}s"
            )
            .ConfigureAwait(false);
        await context
            .Output.WriteLineAsync($"  Retry Count: {_configuration.RetryCount}")
            .ConfigureAwait(false);
        await context
            .Output.WriteLineAsync($"  Timeout: {_configuration.TimeoutMs}ms")
            .ConfigureAwait(false);

        return ExitCode.Success;
    }

    /// <summary>
    /// Resets circuit breakers (AC-047, AC-051, AC-052).
    /// </summary>
    private async Task<ExitCode> ResetCircuitsAsync(CommandContext context)
    {
        var modelId = GetArgValue(context.Args, "--model");
        var resetAll = context.Args.Contains("--all", StringComparer.OrdinalIgnoreCase);

        if (!resetAll && string.IsNullOrEmpty(modelId))
        {
            await context
                .Output.WriteLineAsync(
                    "Error: Specify --model <id> or --all to reset circuit breakers."
                )
                .ConfigureAwait(false);
            return ExitCode.InvalidArguments;
        }

        if (resetAll)
        {
            _fallbackHandler.ResetAllCircuits();
            await context
                .Output.WriteLineAsync("All circuit breakers have been reset.")
                .ConfigureAwait(false);
        }
        else if (!string.IsNullOrEmpty(modelId))
        {
            _fallbackHandler.ResetCircuit(modelId);
            await context
                .Output.WriteLineAsync($"Circuit breaker for '{modelId}' has been reset.")
                .ConfigureAwait(false);
        }

        return ExitCode.Success;
    }

    /// <summary>
    /// Tests fallback chain availability (AC-048, AC-053).
    /// </summary>
    private async Task<ExitCode> TestChainAsync(CommandContext context)
    {
        var roleName = GetArgValue(context.Args, "--role");

        AgentRole? role = null;
        if (!string.IsNullOrEmpty(roleName))
        {
            if (!Enum.TryParse<AgentRole>(roleName, ignoreCase: true, out var parsedRole))
            {
                await context
                    .Output.WriteLineAsync(
                        $"Error: Unknown role '{roleName}'. Use: planner, coder, reviewer, executor."
                    )
                    .ConfigureAwait(false);
                return ExitCode.InvalidArguments;
            }

            role = parsedRole;
        }

        await context
            .Output.WriteLineAsync(
                role.HasValue
                    ? $"Testing Fallback Chain for {role.Value}"
                    : "Testing Global Fallback Chain"
            )
            .ConfigureAwait(false);
        await context.Output.WriteLineAsync(new string('=', 50)).ConfigureAwait(false);
        await context.Output.WriteLineAsync().ConfigureAwait(false);

        var chain = role.HasValue
            ? _configuration.GetRoleChain(role.Value)
            : _configuration.GetGlobalChain();

        if (chain.Count == 0)
        {
            if (role.HasValue)
            {
                chain = _configuration.GetGlobalChain();
                await context
                    .Output.WriteLineAsync($"No chain for {role.Value}, using global chain.")
                    .ConfigureAwait(false);
                await context.Output.WriteLineAsync().ConfigureAwait(false);
            }
            else
            {
                await context
                    .Output.WriteLineAsync("No fallback chain configured.")
                    .ConfigureAwait(false);
                return ExitCode.ConfigurationError;
            }
        }

        var allHealthy = true;
        foreach (var modelId in chain)
        {
            var circuitState = _fallbackHandler.GetCircuitState(modelId);
            var isCircuitOpen = circuitState.State == CircuitState.Open;

            var status = isCircuitOpen ? "CIRCUIT OPEN" : "OK";
            var symbol = isCircuitOpen ? "✗" : "✓";

            await context
                .Output.WriteLineAsync($"  {symbol} {modelId}: {status}")
                .ConfigureAwait(false);

            if (isCircuitOpen)
            {
                allHealthy = false;
                await context
                    .Output.WriteLineAsync(
                        $"      Failures: {circuitState.FailureCount}, Retry at: {circuitState.NextRetryTime:HH:mm:ss}"
                    )
                    .ConfigureAwait(false);
            }
        }

        await context.Output.WriteLineAsync().ConfigureAwait(false);
        await context
            .Output.WriteLineAsync(allHealthy ? "Chain is healthy." : "Chain has issues.")
            .ConfigureAwait(false);

        return allHealthy ? ExitCode.Success : ExitCode.RuntimeError;
    }
}
