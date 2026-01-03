namespace Acode.Domain.Audit;

/// <summary>
/// Correlation identifier linking related audit events.
/// </summary>
public sealed record CorrelationId
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationId"/> class.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    public CorrelationId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("CorrelationId cannot be empty GUID", nameof(value));
        }

        Value = value;
    }

    /// <summary>
    /// Gets the GUID value.
    /// </summary>
    public Guid Value { get; }

    /// <summary>
    /// Creates a new CorrelationId with a new GUID.
    /// </summary>
    /// <returns>New CorrelationId.</returns>
    public static CorrelationId New() => new(Guid.NewGuid());

    /// <summary>
    /// Returns the string representation.
    /// </summary>
    /// <returns>String representation.</returns>
    public override string ToString() => Value.ToString();
}
