using Acode.Application.Security;
using Acode.Domain.Risks;
using Acode.Domain.Security.PathProtection;
using Acode.Infrastructure.Security;

namespace Acode.Cli.Commands;

/// <summary>
/// Command for security-related operations.
/// </summary>
public sealed class SecurityCommand
{
    private readonly IProtectedPathValidator _pathValidator;
    private readonly IRiskRegister? _riskRegister;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityCommand"/> class.
    /// </summary>
    public SecurityCommand()
    {
        // Create dependencies for ProtectedPathValidator
        var pathMatcher = new GlobMatcher(caseSensitive: false); // Case-insensitive for security
        var pathNormalizer = new PathNormalizer();
        var symlinkResolver = new SymlinkResolver();

        _pathValidator = new ProtectedPathValidator(pathMatcher, pathNormalizer, symlinkResolver);

        // Try to load risk register
        try
        {
            var riskRegisterPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "docs",
                "security",
                "risk-register.yaml");
            if (File.Exists(riskRegisterPath))
            {
                _riskRegister = new YamlRiskRegisterRepository(riskRegisterPath);
            }
        }
        catch
        {
            // Risk register optional - commands will check for null
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityCommand"/> class with dependencies.
    /// </summary>
    /// <param name="riskRegister">Optional risk register.</param>
    public SecurityCommand(IRiskRegister? riskRegister)
    {
        // Create dependencies for ProtectedPathValidator
        var pathMatcher = new GlobMatcher(caseSensitive: false); // Case-insensitive for security
        var pathNormalizer = new PathNormalizer();
        var symlinkResolver = new SymlinkResolver();

        _pathValidator = new ProtectedPathValidator(pathMatcher, pathNormalizer, symlinkResolver);
        _riskRegister = riskRegister;
    }

    /// <summary>
    /// Shows current security status.
    /// </summary>
    /// <returns>Exit code (0 = success).</returns>
    public int ShowStatus()
    {
        Console.WriteLine("Security Status:");
        Console.WriteLine("  Operating Mode: LocalOnly (default)");
        Console.WriteLine("  Protected Paths: Enforced");
        Console.WriteLine("  Secret Redaction: Enabled");
        Console.WriteLine("  Audit Logging: Enabled");
        Console.WriteLine($"  Denylist Entries: {DefaultDenylist.Entries.Count}");
        return 0;
    }

    /// <summary>
    /// Shows all denylist entries.
    /// </summary>
    /// <returns>Exit code (0 = success).</returns>
    public int ShowDenylist()
    {
        Console.WriteLine("Protected Paths Denylist:");
        Console.WriteLine();

        foreach (var entry in DefaultDenylist.Entries)
        {
            Console.WriteLine($"  Pattern:  {entry.Pattern}");
            Console.WriteLine($"  Category: {entry.Category}");
            Console.WriteLine($"  Risk ID:  {entry.RiskId}");
            Console.WriteLine($"  Reason:   {entry.Reason}");
            Console.WriteLine();
        }

        Console.WriteLine($"Total: {DefaultDenylist.Entries.Count} protected patterns");
        return 0;
    }

    /// <summary>
    /// Checks if a path is protected.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <param name="operation">Optional file operation to validate against.</param>
    /// <returns>Exit code (0 = allowed, 1 = blocked).</returns>
    public int CheckPath(string path, FileOperation? operation = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, nameof(path));

        var result = operation.HasValue
            ? _pathValidator.Validate(path, operation.Value)
            : _pathValidator.Validate(path);

        if (result.IsProtected)
        {
            Console.WriteLine($"BLOCKED: {path}");
            Console.WriteLine($"  Error Code: {result.Error?.ErrorCode ?? "UNKNOWN"}");
            Console.WriteLine($"  Pattern:    {result.MatchedPattern}");
            Console.WriteLine($"  Category:   {result.Category}");
            Console.WriteLine($"  Risk ID:    {result.RiskId}");
            Console.WriteLine($"  Reason:     {result.Reason}");
            return 1;
        }

        Console.WriteLine($"ALLOWED: {path}");
        return 0;
    }

    /// <summary>
    /// Shows all risks, optionally filtered by category or severity.
    /// </summary>
    /// <param name="category">Optional STRIDE category filter.</param>
    /// <param name="severity">Optional severity filter.</param>
    /// <returns>Exit code (0 = success, 1 = error).</returns>
    public async Task<int> ShowRisksAsync(RiskCategory? category = null, Severity? severity = null)
    {
        if (_riskRegister == null)
        {
            Console.WriteLine("Error: Risk register not available");
            return 1;
        }

        try
        {
            IReadOnlyList<Risk> risks = category.HasValue
                ? await _riskRegister.GetRisksByCategoryAsync(category.Value).ConfigureAwait(false)
                : await _riskRegister.GetAllRisksAsync().ConfigureAwait(false);
            if (severity.HasValue)
            {
                risks = risks.Where(r => r.Severity == severity.Value).ToList();
            }

            Console.WriteLine($"Risk Register (Version {_riskRegister.Version}, Updated {_riskRegister.LastUpdated:yyyy-MM-dd})");
            Console.WriteLine();
            Console.WriteLine($"Total Risks: {risks.Count}");
            Console.WriteLine();
            Console.WriteLine("ID             | Category              | Severity | Title");
            Console.WriteLine("---------------|----------------------|----------|----------------------------------------");

            foreach (var risk in risks.OrderBy(r => r.RiskId.Value))
            {
                Console.WriteLine($"{risk.RiskId.Value,-14} | {risk.Category,-20} | {risk.Severity,-8} | {risk.Title}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading risks: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Shows detailed information for a specific risk.
    /// </summary>
    /// <param name="riskId">Risk identifier.</param>
    /// <returns>Exit code (0 = success, 1 = not found/error).</returns>
    public async Task<int> ShowRiskDetailAsync(string riskId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(riskId, nameof(riskId));

        if (_riskRegister == null)
        {
            Console.WriteLine("Error: Risk register not available");
            return 1;
        }

        try
        {
            var id = new RiskId(riskId);
            var risk = await _riskRegister.GetRiskAsync(id).ConfigureAwait(false);

            if (risk == null)
            {
                Console.WriteLine($"Risk not found: {riskId}");
                return 1;
            }

            Console.WriteLine($"Risk ID:      {risk.RiskId.Value}");
            Console.WriteLine($"Category:     {risk.Category}");
            Console.WriteLine($"Title:        {risk.Title}");
            Console.WriteLine($"Severity:     {risk.Severity}");
            Console.WriteLine($"Status:       {risk.Status}");
            Console.WriteLine($"Owner:        {risk.Owner}");
            Console.WriteLine($"Created:      {risk.Created:yyyy-MM-dd}");
            Console.WriteLine($"Last Review:  {risk.LastReview:yyyy-MM-dd}");
            Console.WriteLine();
            Console.WriteLine("Description:");
            Console.WriteLine($"  {risk.Description}");
            Console.WriteLine();
            Console.WriteLine("DREAD Score:");
            Console.WriteLine($"  Damage:          {risk.DreadScore.Damage}/10");
            Console.WriteLine($"  Reproducibility: {risk.DreadScore.Reproducibility}/10");
            Console.WriteLine($"  Exploitability:  {risk.DreadScore.Exploitability}/10");
            Console.WriteLine($"  Affected Users:  {risk.DreadScore.AffectedUsers}/10");
            Console.WriteLine($"  Discoverability: {risk.DreadScore.Discoverability}/10");
            Console.WriteLine($"  Average:         {risk.DreadScore.Average:F1}");
            Console.WriteLine();

            if (risk.AttackVectors?.Any() == true)
            {
                Console.WriteLine("Attack Vectors:");
                foreach (var vector in risk.AttackVectors)
                {
                    Console.WriteLine($"  - {vector}");
                }

                Console.WriteLine();
            }

            if (risk.Mitigations.Any())
            {
                Console.WriteLine($"Mitigations ({risk.Mitigations.Count}):");
                foreach (var mitigation in risk.Mitigations)
                {
                    Console.WriteLine($"  {mitigation.Id.Value}: {mitigation.Title} [{mitigation.Status}]");
                }

                Console.WriteLine();
            }

            if (risk.ResidualRisk != null)
            {
                Console.WriteLine("Residual Risk:");
                Console.WriteLine($"  {risk.ResidualRisk}");
            }

            return 0;
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"Invalid risk ID format: {ex.Message}");
            return 1;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading risk: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Shows all mitigations.
    /// </summary>
    /// <returns>Exit code (0 = success, 1 = error).</returns>
    public async Task<int> ShowMitigationsAsync()
    {
        if (_riskRegister == null)
        {
            Console.WriteLine("Error: Risk register not available");
            return 1;
        }

        try
        {
            var mitigations = await _riskRegister.GetAllMitigationsAsync().ConfigureAwait(false);

            Console.WriteLine($"Mitigations (Version {_riskRegister.Version})");
            Console.WriteLine();
            Console.WriteLine($"Total Mitigations: {mitigations.Count}");
            Console.WriteLine();
            Console.WriteLine("ID       | Status        | Title");
            Console.WriteLine("---------|---------------|------------------------------------------------");

            foreach (var mitigation in mitigations.OrderBy(m => m.Id.Value))
            {
                Console.WriteLine($"{mitigation.Id.Value,-8} | {mitigation.Status,-13} | {mitigation.Title}");
            }

            Console.WriteLine();
            Console.WriteLine("Status Summary:");
            var statusGroups = mitigations.GroupBy(m => m.Status).OrderBy(g => g.Key);
            foreach (var group in statusGroups)
            {
                Console.WriteLine($"  {group.Key,-13}: {group.Count()}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading mitigations: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Verifies mitigation implementation status.
    /// </summary>
    /// <returns>Exit code (0 = all verified, 1 = failures found).</returns>
    public async Task<int> VerifyMitigationsAsync()
    {
        if (_riskRegister == null)
        {
            Console.WriteLine("Error: Risk register not available");
            return 1;
        }

        try
        {
            var mitigations = await _riskRegister.GetAllMitigationsAsync().ConfigureAwait(false);

            Console.WriteLine("Mitigation Verification Report");
            Console.WriteLine($"Generated: {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            var implemented = mitigations.Where(m => m.Status == MitigationStatus.Implemented).ToList();
            var inProgress = mitigations.Where(m => m.Status == MitigationStatus.InProgress).ToList();
            var pending = mitigations.Where(m => m.Status == MitigationStatus.Pending).ToList();
            var notApplicable = mitigations.Where(m => m.Status == MitigationStatus.NotApplicable).ToList();

            Console.WriteLine($"✅ Implemented:    {implemented.Count,3}");
            Console.WriteLine($"🔄 In Progress:    {inProgress.Count,3}");
            Console.WriteLine($"⏸️  Pending:        {pending.Count,3}");
            Console.WriteLine($"⊘  Not Applicable: {notApplicable.Count,3}");
            Console.WriteLine($"   Total:          {mitigations.Count,3}");
            Console.WriteLine();

            if (implemented.Any())
            {
                Console.WriteLine("Implemented Mitigations:");
                foreach (var mitigation in implemented.OrderBy(m => m.Id.Value))
                {
                    Console.WriteLine($"  {mitigation.Id.Value}: {mitigation.Title}");
                    if (!string.IsNullOrEmpty(mitigation.VerificationTest))
                    {
                        Console.WriteLine($"    Verification: {mitigation.VerificationTest}");
                    }

                    Console.WriteLine($"    Last Verified: {mitigation.LastVerified:yyyy-MM-dd}");
                }

                Console.WriteLine();
            }

            if (inProgress.Any())
            {
                Console.WriteLine("In Progress:");
                foreach (var mitigation in inProgress.OrderBy(m => m.Id.Value))
                {
                    Console.WriteLine($"  {mitigation.Id.Value}: {mitigation.Title}");
                }

                Console.WriteLine();
            }

            if (pending.Any())
            {
                Console.WriteLine("Pending Implementation:");
                foreach (var mitigation in pending.OrderBy(m => m.Id.Value))
                {
                    Console.WriteLine($"  {mitigation.Id.Value}: {mitigation.Title}");
                }

                Console.WriteLine();
            }

            var needsAttention = inProgress.Count + pending.Count;
            if (needsAttention > 0)
            {
                Console.WriteLine($"⚠️  {needsAttention} mitigation(s) need attention");
                return 1;
            }

            Console.WriteLine("✅ All mitigations verified");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verifying mitigations: {ex.Message}");
            return 1;
        }
    }
}
