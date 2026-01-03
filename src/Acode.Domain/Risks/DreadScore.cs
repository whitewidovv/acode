namespace Acode.Domain.Risks;

/// <summary>
/// DREAD risk scoring value object.
/// DREAD = Damage, Reproducibility, Exploitability, Affected users, Discoverability.
/// Each component scored 1-10, average determines severity.
/// </summary>
public sealed record DreadScore
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DreadScore"/> class.
    /// </summary>
    /// <param name="damage">Damage potential (1-10).</param>
    /// <param name="reproducibility">How easy to reproduce (1-10).</param>
    /// <param name="exploitability">How easy to exploit (1-10).</param>
    /// <param name="affectedUsers">Number of affected users (1-10).</param>
    /// <param name="discoverability">How easy to discover (1-10).</param>
    public DreadScore(
        int damage,
        int reproducibility,
        int exploitability,
        int affectedUsers,
        int discoverability)
    {
        ValidateScore(damage, nameof(damage));
        ValidateScore(reproducibility, nameof(reproducibility));
        ValidateScore(exploitability, nameof(exploitability));
        ValidateScore(affectedUsers, nameof(affectedUsers));
        ValidateScore(discoverability, nameof(discoverability));

        Damage = damage;
        Reproducibility = reproducibility;
        Exploitability = exploitability;
        AffectedUsers = affectedUsers;
        Discoverability = discoverability;

        Average = (damage + reproducibility + exploitability + affectedUsers + discoverability) / 5.0;
        Severity = CalculateSeverity(Average);
    }

    /// <summary>
    /// Gets the damage potential score (1-10).
    /// </summary>
    public int Damage { get; }

    /// <summary>
    /// Gets the reproducibility score (1-10).
    /// </summary>
    public int Reproducibility { get; }

    /// <summary>
    /// Gets the exploitability score (1-10).
    /// </summary>
    public int Exploitability { get; }

    /// <summary>
    /// Gets the affected users score (1-10).
    /// </summary>
    public int AffectedUsers { get; }

    /// <summary>
    /// Gets the discoverability score (1-10).
    /// </summary>
    public int Discoverability { get; }

    /// <summary>
    /// Gets the calculated average DREAD score.
    /// </summary>
    public double Average { get; }

    /// <summary>
    /// Gets the severity level based on average score.
    /// </summary>
    public Severity Severity { get; }

    private static void ValidateScore(int score, string paramName)
    {
        if (score < 1 || score > 10)
        {
            throw new ArgumentOutOfRangeException(
                paramName,
                score,
                "DREAD scores must be between 1 and 10");
        }
    }

    private static Severity CalculateSeverity(double average) => average switch
    {
        <= 4.0 => Severity.Low,
        <= 7.0 => Severity.Medium,
        <= 9.0 => Severity.High,
        _ => Severity.Critical
    };
}
