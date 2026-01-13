namespace Acode.Domain.Audit;

using System.Text.RegularExpressions;
using Acode.Domain.Common;

/// <summary>
/// Unique identifier for an audit event.
/// Format: evt_[a-zA-Z0-9]+.
/// </summary>
public sealed partial record EventId
{
    private static readonly Regex FormatPattern = FormatRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="EventId"/> class.
    /// </summary>
    /// <param name="value">The event ID value in format evt_xxx.</param>
    public EventId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("EventId cannot be null or whitespace", nameof(value));
        }

        if (!FormatPattern.IsMatch(value))
        {
            throw new ArgumentException("EventId must match format evt_[a-zA-Z0-9]+", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the event ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new EventId with a generated ID.
    /// </summary>
    /// <returns>New EventId.</returns>
    public static EventId New() => new($"evt_{Base62Encoder.Encode(Guid.NewGuid())}");

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value;

    [GeneratedRegex("^evt_[a-zA-Z0-9]+$")]
    private static partial Regex FormatRegex();
}
