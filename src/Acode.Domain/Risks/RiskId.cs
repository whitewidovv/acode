namespace Acode.Domain.Risks;

using System.Text.RegularExpressions;

/// <summary>
/// Value object representing a unique risk identifier.
/// Format: RISK-{CATEGORY}-{NUMBER} where CATEGORY is S|T|R|I|D|E (STRIDE).
/// Example: RISK-S-001, RISK-I-042.
/// </summary>
public sealed record RiskId
{
    private static readonly Regex FormatRegex = new(
        @"^RISK-([STRIDE])-(\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Initializes a new instance of the <see cref="RiskId"/> class.
    /// </summary>
    /// <param name="value">The risk ID string in format RISK-X-NNN.</param>
    /// <exception cref="ArgumentException">Thrown when format is invalid.</exception>
    public RiskId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var match = FormatRegex.Match(value);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"Risk ID must be in format RISK-{{CATEGORY}}-{{NUMBER}} where CATEGORY is S|T|R|I|D|E and NUMBER is 3 digits. Got: {value}",
                nameof(value));
        }

        Value = value;
        Category = MapCategoryLetter(match.Groups[1].Value[0]);
        SequenceNumber = int.Parse(match.Groups[2].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the risk ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the STRIDE category extracted from the risk ID.
    /// </summary>
    public RiskCategory Category { get; }

    /// <summary>
    /// Gets the sequence number extracted from the risk ID.
    /// </summary>
    public int SequenceNumber { get; }

    /// <summary>
    /// Returns the string representation of the risk ID.
    /// </summary>
    /// <returns>The risk ID value.</returns>
    public override string ToString() => Value;

    private static RiskCategory MapCategoryLetter(char letter) => letter switch
    {
        'S' => RiskCategory.Spoofing,
        'T' => RiskCategory.Tampering,
        'R' => RiskCategory.Repudiation,
        'I' => RiskCategory.InformationDisclosure,
        'D' => RiskCategory.DenialOfService,
        'E' => RiskCategory.ElevationOfPrivilege,
        _ => throw new ArgumentException($"Invalid STRIDE category letter: {letter}")
    };
}
