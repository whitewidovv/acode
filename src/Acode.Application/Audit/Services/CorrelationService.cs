namespace Acode.Application.Audit.Services;

using System;
using System.Threading;
using Acode.Domain.Audit;

/// <summary>
/// Service for managing correlation ID scopes using AsyncLocal.
/// Provides automatic propagation of correlation IDs across async operations.
/// </summary>
public sealed class CorrelationService
{
    private static readonly AsyncLocal<CorrelationScope?> CurrentScope = new();

    /// <summary>
    /// Begins a new correlation scope with a new correlation ID.
    /// </summary>
    /// <returns>A disposable correlation scope.</returns>
    public IDisposable BeginCorrelation()
    {
        return BeginCorrelation(CorrelationId.New());
    }

    /// <summary>
    /// Begins a new correlation scope with the specified correlation ID.
    /// </summary>
    /// <param name="correlationId">The correlation ID for this scope.</param>
    /// <returns>A disposable correlation scope.</returns>
    public IDisposable BeginCorrelation(CorrelationId correlationId)
    {
        var scope = new CorrelationScope(correlationId, CurrentScope.Value);
        CurrentScope.Value = scope;
        return scope;
    }

    /// <summary>
    /// Gets the current correlation ID, or null if no scope is active.
    /// </summary>
    /// <returns>The current correlation ID, or null.</returns>
    public CorrelationId? GetCurrentCorrelationId()
    {
        return CurrentScope.Value?.CorrelationId;
    }

    /// <summary>
    /// Represents a correlation ID scope that restores the previous scope when disposed.
    /// </summary>
    private sealed class CorrelationScope : IDisposable
    {
        private readonly CorrelationScope? _previousScope;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CorrelationScope"/> class.
        /// </summary>
        /// <param name="correlationId">The correlation ID for this scope.</param>
        /// <param name="previousScope">The previous scope to restore on disposal.</param>
        public CorrelationScope(CorrelationId correlationId, CorrelationScope? previousScope)
        {
            CorrelationId = correlationId;
            _previousScope = previousScope;
        }

        /// <summary>
        /// Gets the correlation ID for this scope.
        /// </summary>
        public CorrelationId CorrelationId { get; }

        /// <summary>
        /// Disposes the scope and restores the previous scope.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            CurrentScope.Value = _previousScope;
            _disposed = true;
        }
    }
}
