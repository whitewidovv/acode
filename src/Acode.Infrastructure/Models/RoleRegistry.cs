using Acode.Application.Models;
using Acode.Domain.Models.Routing;
using Microsoft.Extensions.Logging;

namespace Acode.Infrastructure.Models;

/// <summary>
/// Provides access to role definitions and tracks the current active role.
/// </summary>
public sealed class RoleRegistry : IRoleRegistry
{
    private readonly ILogger<RoleRegistry> _logger;
    private readonly Dictionary<AgentRole, RoleDefinition> _roles;
    private AgentRole _currentRole = AgentRole.Default;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoleRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for role transitions.</param>
    public RoleRegistry(ILogger<RoleRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;

        // Initialize with the four core role definitions
        _roles = new Dictionary<AgentRole, RoleDefinition>
        {
            [AgentRole.Default] = new RoleDefinition
            {
                Role = AgentRole.Default,
                Name = "default",
                Description = "General-purpose role for non-specialized interactions",
                Capabilities = new List<string>
                {
                    "answer_questions",
                    "explain_code",
                    "provide_guidance",
                },
                Constraints = Array.Empty<string>(),
            },
            [AgentRole.Planner] = new RoleDefinition
            {
                Role = AgentRole.Planner,
                Name = "planner",
                Description = "Task decomposition and planning phase",
                Capabilities = new List<string>
                {
                    "plan_tasks",
                    "decompose_requirements",
                    "identify_dependencies",
                    "estimate_complexity",
                },
                Constraints = new List<string>
                {
                    "cannot_modify_files",
                    "read_only",
                },
            },
            [AgentRole.Coder] = new RoleDefinition
            {
                Role = AgentRole.Coder,
                Name = "coder",
                Description = "Code implementation phase with strict minimal diff",
                Capabilities = new List<string>
                {
                    "write_file",
                    "edit_file",
                    "create_file",
                    "delete_file",
                    "run_tests",
                },
                Constraints = new List<string>
                {
                    "strict_minimal_diff",
                    "follow_plan",
                },
            },
            [AgentRole.Reviewer] = new RoleDefinition
            {
                Role = AgentRole.Reviewer,
                Name = "reviewer",
                Description = "Code review and quality assurance phase",
                Capabilities = new List<string>
                {
                    "analyze_diff",
                    "identify_issues",
                    "verify_correctness",
                    "suggest_improvements",
                },
                Constraints = new List<string>
                {
                    "read_only",
                    "cannot_modify_files",
                },
            },
        };
    }

    /// <inheritdoc/>
    public RoleDefinition GetRole(AgentRole role)
    {
        if (_roles.TryGetValue(role, out var definition))
        {
            return definition;
        }

        throw new InvalidOperationException($"Role '{role}' is not registered.");
    }

    /// <inheritdoc/>
    public IReadOnlyList<RoleDefinition> ListRoles()
    {
        // Return roles ordered by enum value (Default=0, Planner=1, Coder=2, Reviewer=3)
        return _roles.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value).ToList();
    }

    /// <inheritdoc/>
    public AgentRole GetCurrentRole()
    {
        return _currentRole;
    }

    /// <inheritdoc/>
    public void SetCurrentRole(AgentRole role, string reason)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        // Verify role is registered
        if (!_roles.ContainsKey(role))
        {
            throw new InvalidOperationException($"Role '{role}' is not registered.");
        }

        var previousRole = _currentRole;
        _currentRole = role;

        _logger.LogInformation(
            "Role transition: {PreviousRole} → {NewRole}. Reason: {Reason}",
            previousRole,
            role,
            reason);
    }
}
