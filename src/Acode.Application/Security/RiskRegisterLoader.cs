namespace Acode.Application.Security;

/// <summary>
/// Loads and parses risk register from YAML format.
/// </summary>
public class RiskRegisterLoader
{
    /// <summary>
    /// Parses YAML content into risk register data.
    /// </summary>
    /// <param name="yamlContent">The YAML content to parse.</param>
    /// <returns>Parsed risk register data.</returns>
    /// <exception cref="RiskRegisterParseException">Thrown when YAML parsing fails.</exception>
    /// <exception cref="RiskRegisterValidationException">Thrown when validation fails.</exception>
    public RiskRegisterData Parse(string yamlContent)
    {
        throw new NotImplementedException("RiskRegisterLoader.Parse not yet implemented");
    }
}
