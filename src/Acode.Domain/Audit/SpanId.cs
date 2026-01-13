namespace Acode.Domain.Audit;

using System.Text.RegularExpressions;
using Acode.Domain.Common;

/// <summary>
/// Unique identifier for an audit span.
/// Format: span_[a-zA-Z0-9]+.
/// </summary>
public sealed partial record SpanId
{
    private static readonly Regex FormatPattern = FormatRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="SpanId"/> class.
    /// </summary>
    /// <param name="value">The span ID value in format span_xxx.</param>
    public SpanId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SpanId cannot be null or whitespace", nameof(value));
        }

        if (!FormatPattern.IsMatch(value))
        {
            throw new ArgumentException("SpanId must match format span_[a-zA-Z0-9]+", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the span ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new SpanId with a generated ID.
    /// </summary>
    /// <returns>New SpanId.</returns>
    public static SpanId New() => new($"span_{Base62Encoder.Encode(Guid.NewGuid())}");

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value;

    [GeneratedRegex("^span_[a-zA-Z0-9]+$")]
    private static partial Regex FormatRegex();
}
