namespace Acode.Domain.Audit;

using System.Text.RegularExpressions;
using Acode.Domain.Common;

/// <summary>
/// Unique identifier for an audit session.
/// Format: sess_[a-zA-Z0-9]+.
/// </summary>
public sealed partial record SessionId
{
    private static readonly Regex FormatPattern = FormatRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="SessionId"/> class.
    /// </summary>
    /// <param name="value">The session ID value in format sess_xxx.</param>
    public SessionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("SessionId cannot be null or whitespace", nameof(value));
        }

        if (!FormatPattern.IsMatch(value))
        {
            throw new ArgumentException("SessionId must match format sess_[a-zA-Z0-9]+", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the session ID value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Creates a new SessionId with a generated ID.
    /// </summary>
    /// <returns>New SessionId.</returns>
    public static SessionId New() => new($"sess_{Base62Encoder.Encode(Guid.NewGuid())}");

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value;

    [GeneratedRegex("^sess_[a-zA-Z0-9]+$")]
    private static partial Regex FormatRegex();
}
