namespace Acode.Infrastructure.Security;

using Acode.Application.Security;
using Acode.Domain.Risks;

/// <summary>
/// YAML file-based implementation of IRiskRegister.
/// Loads and caches risk register data from YAML file.
/// </summary>
public class YamlRiskRegisterRepository : IRiskRegister
{
    private readonly string _yamlFilePath;
    private readonly RiskRegisterLoader _loader;
    private RiskRegisterData? _cachedData;

    /// <summary>
    /// Initializes a new instance of the <see cref="YamlRiskRegisterRepository"/> class.
    /// </summary>
    /// <param name="yamlFilePath">Path to the risk-register.yaml file.</param>
    public YamlRiskRegisterRepository(string yamlFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(yamlFilePath, nameof(yamlFilePath));
        _yamlFilePath = yamlFilePath;
        _loader = new RiskRegisterLoader();
    }

    /// <inheritdoc/>
    public string Version => GetData().Version;

    /// <inheritdoc/>
    public DateTimeOffset LastUpdated => GetData().LastUpdated;

    /// <inheritdoc/>
    public Task<IReadOnlyList<Risk>> GetAllRisksAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetData().Risks);
    }

    /// <inheritdoc/>
    public Task<Risk?> GetRiskAsync(RiskId id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var risk = GetData().Risks.FirstOrDefault(r => r.RiskId.Value == id.Value);
        return Task.FromResult(risk);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Risk>> GetRisksByCategoryAsync(
        RiskCategory category,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var risks = GetData().Risks
            .Where(r => r.Category == category)
            .ToList();
        return Task.FromResult<IReadOnlyList<Risk>>(risks);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Risk>> GetRisksBySeverityAsync(
        Severity severity,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var risks = GetData().Risks
            .Where(r => r.Severity == severity)
            .ToList();
        return Task.FromResult<IReadOnlyList<Risk>>(risks);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Risk>> SearchRisksAsync(
        string keyword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(keyword, nameof(keyword));

        var lowerKeyword = keyword.ToLowerInvariant();
        var risks = GetData().Risks
            .Where(r =>
                r.Title.Contains(lowerKeyword, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(lowerKeyword, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IReadOnlyList<Risk>>(risks);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Mitigation>> GetAllMitigationsAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetData().Mitigations);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<Mitigation>> GetMitigationsForRiskAsync(
        RiskId riskId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var risk = GetData().Risks.FirstOrDefault(r => r.RiskId.Value == riskId.Value);
        if (risk == null)
        {
            return Task.FromResult<IReadOnlyList<Mitigation>>(new List<Mitigation>());
        }

        return Task.FromResult<IReadOnlyList<Mitigation>>(risk.Mitigations);
    }

    private RiskRegisterData GetData()
    {
        if (_cachedData != null)
        {
            return _cachedData;
        }

        if (!File.Exists(_yamlFilePath))
        {
            throw new FileNotFoundException($"Risk register file not found: {_yamlFilePath}");
        }

        var yamlContent = File.ReadAllText(_yamlFilePath);
        _cachedData = _loader.Parse(yamlContent);
        return _cachedData;
    }
}
