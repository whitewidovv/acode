namespace Acode.Infrastructure.Roles;

using Acode.Application.Roles;
using Acode.Domain.Roles;
using Microsoft.Extensions.Logging;

/// <summary>
/// Production implementation of <see cref="IRoleRegistry"/>.
/// Manages role definitions, current role state, and role transitions with validation.
/// </summary>
/// <remarks>
/// AC-035 to AC-040: State management for role tracking and transitions.
/// </remarks>
public sealed class RoleRegistry : IRoleRegistry
{
    /// <summary>
    /// Valid role transitions. Key is (from, to), value is whether the transition is allowed.
    /// </summary>
    private static readonly HashSet<(AgentRole From, AgentRole To)> ValidTransitions = new()
    {
        (AgentRole.Default, AgentRole.Planner),
        (AgentRole.Planner, AgentRole.Coder),
        (AgentRole.Coder, AgentRole.Reviewer),
        (AgentRole.Reviewer, AgentRole.Coder),
        (AgentRole.Reviewer, AgentRole.Default),
        (AgentRole.Planner, AgentRole.Default),
        (AgentRole.Coder, AgentRole.Default),
    };

    private readonly ILogger<RoleRegistry> _logger;
    private readonly Dictionary<AgentRole, RoleDefinition> _roleDefinitions;
    private readonly List<RoleTransitionEntry> _transitionHistory;
    private readonly object _lock = new();
    private AgentRole _currentRole;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRegistry"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when logger is null.</exception>
    public RoleRegistry(ILogger<RoleRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // Load core role definitions
        _roleDefinitions = RoleDefinitionProvider.GetCoreRoles().ToDictionary(r => r.Role, r => r);

        // Initialize state - AC-036: Initial is Default
        _currentRole = AgentRole.Default;
        _transitionHistory = new List<RoleTransitionEntry>
        {
            new()
            {
                FromRole = null,
                ToRole = AgentRole.Default,
                Reason = "Initial state",
                Timestamp = DateTime.UtcNow,
            },
        };
    }

    /// <inheritdoc />
    public RoleDefinition GetRole(AgentRole role)
    {
        if (!_roleDefinitions.TryGetValue(role, out var definition))
        {
            _logger.LogWarning("Unknown role requested: {Role}. Returning Default.", role);
            return _roleDefinitions[AgentRole.Default];
        }

        return definition;
    }

    /// <inheritdoc />
    public IReadOnlyList<RoleDefinition> ListRoles()
    {
        return _roleDefinitions.Values.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public AgentRole GetCurrentRole()
    {
        lock (_lock)
        {
            return _currentRole;
        }
    }

    /// <inheritdoc />
    public void SetCurrentRole(AgentRole role, string reason)
    {
        ArgumentNullException.ThrowIfNull(reason);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Transition reason cannot be empty", nameof(reason));
        }

        lock (_lock)
        {
            var fromRole = _currentRole;

            // Check if transition is valid (only validate if actually changing roles)
            if (fromRole != role)
            {
                ValidateTransition(fromRole, role, reason);
            }

            // Perform transition
            _currentRole = role;

            // Record transition
            var entry = new RoleTransitionEntry
            {
                FromRole = fromRole,
                ToRole = role,
                Reason = reason,
                Timestamp = DateTime.UtcNow,
            };

            _transitionHistory.Add(entry);

            // Log the transition (AC-039: Logged on change)
            _logger.LogInformation(
                "Role transition: {From} → {To}. Reason: {Reason}",
                fromRole.ToDisplayString(),
                role.ToDisplayString(),
                reason
            );
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<RoleTransitionEntry> GetRoleHistory()
    {
        lock (_lock)
        {
            return _transitionHistory.ToList().AsReadOnly();
        }
    }

    private void ValidateTransition(AgentRole fromRole, AgentRole toRole, string reason)
    {
        // Any role can transition to Default (reset/cancel)
        if (toRole == AgentRole.Default)
        {
            return;
        }

        // Check if this transition is in the valid transitions set
        if (!ValidTransitions.Contains((fromRole, toRole)))
        {
            var errorMessage =
                $"Cannot transition from {fromRole.ToDisplayString()} to {toRole.ToDisplayString()}. This transition is not allowed.";
            _logger.LogWarning("{Message} Reason: {Reason}", errorMessage, reason);
            throw new InvalidRoleTransitionException(fromRole, toRole, errorMessage);
        }
    }
}
