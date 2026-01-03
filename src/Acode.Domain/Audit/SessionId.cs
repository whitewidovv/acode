namespace Acode.Domain.Audit;

/// <summary>
/// Unique identifier for an audit session.
/// </summary>
public sealed record SessionId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SessionId"/> class.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    public SessionId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("SessionId cannot be empty GUID", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new SessionId with a new GUID.
    /// </summary>
    /// <returns>New SessionId.</returns>
    public static SessionId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value.ToString();
}
