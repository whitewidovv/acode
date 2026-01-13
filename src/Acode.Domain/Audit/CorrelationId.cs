namespace Acode.Domain.Audit;

using System.Text.RegularExpressions;
using Acode.Domain.Common;

/// <summary>
/// Correlation identifier linking related audit events.
/// Format: corr_[a-zA-Z0-9]+.
/// </summary>
public sealed partial record CorrelationId
{
    private static readonly Regex FormatPattern = FormatRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationId"/> class.
    /// </summary>
    /// <param name="value">The correlation ID value in format corr_xxx.</param>
    public CorrelationId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("CorrelationId cannot be null or whitespace", nameof(value));
        }

        if (!FormatPattern.IsMatch(value))
        {
            throw new ArgumentException("CorrelationId must match format corr_[a-zA-Z0-9]+", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the correlation ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new CorrelationId with a generated ID.
    /// </summary>
    /// <returns>New CorrelationId.</returns>
    public static CorrelationId New() => new($"corr_{Base62Encoder.Encode(Guid.NewGuid())}");

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value;

    [GeneratedRegex("^corr_[a-zA-Z0-9]+$")]
    private static partial Regex FormatRegex();
}
