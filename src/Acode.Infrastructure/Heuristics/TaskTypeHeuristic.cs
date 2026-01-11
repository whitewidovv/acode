namespace Acode.Infrastructure.Heuristics;

using Acode.Application.Heuristics;
using Microsoft.Extensions.Logging;

/// <summary>
/// Task type categories for complexity scoring.
/// </summary>
public enum TaskType
{
    /// <summary>Unknown task type.</summary>
    Unknown,

    /// <summary>Bug fix task.</summary>
    Bug,

    /// <summary>Enhancement or improvement task.</summary>
    Enhancement,

    /// <summary>New feature implementation.</summary>
    Feature,

    /// <summary>Refactoring task.</summary>
    Refactor,
}

/// <summary>
/// Heuristic that scores complexity based on task type keywords.
/// Also detects security-critical keywords and forces conservative routing.
/// </summary>
/// <remarks>
/// <para>AC-022: TaskTypeHeuristic works.</para>
/// <para>AC-068: Security keywords force high scores.</para>
/// </remarks>
public sealed class TaskTypeHeuristic : IRoutingHeuristic
{
    /// <summary>
    /// Security-critical keywords that force high complexity.
    /// </summary>
    private static readonly HashSet<string> SecurityKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "authentication",
        "authorization",
        "security",
        "crypto",
        "encryption",
        "password",
        "token",
        "credentials",
        "permission",
        "access control",
        "sanitize",
        "validate",
        "xss",
        "sql injection",
        "csrf",
        "oauth",
        "saml",
        "jwt",
        "session",
        "cookie",
        "cors",
    };

    /// <summary>
    /// Task type keywords for classification.
    /// </summary>
    private static readonly Dictionary<TaskType, string[]> TaskKeywords = new()
    {
        [TaskType.Bug] = new[] { "fix", "bug", "issue", "crash", "error", "typo" },
        [TaskType.Enhancement] = new[] { "add", "enhance", "improve", "update", "upgrade" },
        [TaskType.Feature] = new[] { "implement", "new feature", "create", "develop" },
        [TaskType.Refactor] = new[] { "refactor", "restructure", "redesign", "migrate" },
    };

    private readonly ILogger<TaskTypeHeuristic> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskTypeHeuristic"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public TaskTypeHeuristic(ILogger<TaskTypeHeuristic> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public string Name => "TaskType";

    /// <inheritdoc />
    public int Priority => 2; // Run after FileCount

    /// <inheritdoc />
    public HeuristicResult Evaluate(HeuristicContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var description = context.TaskDescription;

        // Check for security keywords first (AC-068)
        var containsSecurityKeyword = SecurityKeywords.Any(keyword =>
            description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
        );

        if (containsSecurityKeyword)
        {
            _logger.LogWarning("Security-critical task detected. Forcing high complexity.");

            return new HeuristicResult
            {
                Score = 85,
                Confidence = 1.0,
                Reasoning = "Security-critical task detected. Conservative routing enforced.",
            };
        }

        // Detect task type from keywords
        var taskType = DetectTaskType(description);
        var (score, confidence) = GetScoreForTaskType(taskType);

        var reasoning = $"Detected task type: {taskType}";

        return new HeuristicResult
        {
            Score = score,
            Confidence = confidence,
            Reasoning = reasoning,
        };
    }

    private static TaskType DetectTaskType(string description)
    {
        foreach (var (type, keywords) in TaskKeywords)
        {
            if (
                keywords.Any(keyword =>
                    description.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                return type;
            }
        }

        return TaskType.Unknown;
    }

    private static (int Score, double Confidence) GetScoreForTaskType(TaskType type)
    {
        return type switch
        {
            TaskType.Bug => (20, 0.8),
            TaskType.Enhancement => (40, 0.75),
            TaskType.Feature => (60, 0.8),
            TaskType.Refactor => (80, 0.85),
            TaskType.Unknown => (50, 0.5), // Conservative default
            _ => (50, 0.5), // Handle any unnamed enum values
        };
    }
}
