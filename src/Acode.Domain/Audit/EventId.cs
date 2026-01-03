namespace Acode.Domain.Audit;

/// <summary>
/// Unique identifier for an audit event.
/// </summary>
public sealed record EventId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventId"/> class.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    public EventId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("EventId cannot be empty GUID", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new EventId with a new GUID.
    /// </summary>
    /// <returns>New EventId.</returns>
    public static EventId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value.ToString();
}
