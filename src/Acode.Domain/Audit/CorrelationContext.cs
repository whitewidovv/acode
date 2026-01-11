namespace Acode.Domain.Audit;

/// <summary>
/// Manages correlation ID scope using AsyncLocal.
/// Enables correlation ID to flow through async call chains.
/// </summary>
public sealed class CorrelationContext : IDisposable
{
    private static readonly AsyncLocal<CorrelationId?> CurrentCorrelation = new();
    private readonly CorrelationId? _previousCorrelation;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationContext"/> class.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this scope.</param>
    /// <param name="description">Description of the correlation scope.</param>
    public CorrelationContext(CorrelationId correlationId, string description)
    {
        CorrelationId = correlationId ?? throw new ArgumentNullException(nameof(correlationId));
        Description = description ?? throw new ArgumentNullException(nameof(description));

        // Save previous and set new
        _previousCorrelation = CurrentCorrelation.Value;
        CurrentCorrelation.Value = correlationId;
    }

    /// <summary>
    /// Gets the current correlation ID.
    /// </summary>
    public static CorrelationId? Current => CurrentCorrelation.Value;

    /// <summary>
    /// Gets the correlation ID for this scope.
    /// </summary>
    public CorrelationId CorrelationId { get; }

    /// <summary>
    /// Gets the description of this correlation scope.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Disposes the correlation context and restores previous correlation.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        // Restore previous correlation
        CurrentCorrelation.Value = _previousCorrelation;
        _disposed = true;
    }
}
