namespace Acode.Domain.Risks;

using System.Text.RegularExpressions;

/// <summary>
/// Value object representing a unique mitigation identifier.
/// Format: MIT-{NUMBER} where NUMBER is 3 digits.
/// Example: MIT-001, MIT-042.
/// </summary>
public sealed record MitigationId
{
    private static readonly Regex FormatRegex = new(
        @"^MIT-(\d{3})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public MitigationId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var match = FormatRegex.Match(value);
        if (!match.Success)
        {
            throw new ArgumentException(
                $"Mitigation ID must be in format MIT-{{NUMBER}} where NUMBER is 3 digits. Got: {value}",
                nameof(value));
        }

        Value = value;
        SequenceNumber = int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    public string Value { get; }

    public int SequenceNumber { get; }

    public override string ToString() => Value;
}
