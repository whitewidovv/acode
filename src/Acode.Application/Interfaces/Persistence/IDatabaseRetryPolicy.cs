// src/Acode.Application/Interfaces/Persistence/IDatabaseRetryPolicy.cs
namespace Acode.Application.Interfaces.Persistence;

/// <summary>
/// Retry policy for transient database errors.
/// Implements exponential backoff with jitter for retryable operations.
/// </summary>
public interface IDatabaseRetryPolicy
{
    /// <summary>
    /// Executes an async operation with retry logic for transient failures.
    /// Only retries if the exception is a DatabaseException with IsTransient = true.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Result of the operation.</returns>
    /// <exception cref="Domain.Exceptions.DatabaseException">If operation fails after all retries or on permanent error.</exception>
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken ct);

    /// <summary>
    /// Executes an async operation with retry logic for transient failures.
    /// Only retries if the exception is a DatabaseException with IsTransient = true.
    /// </summary>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation with retry logic.</returns>
    /// <exception cref="Domain.Exceptions.DatabaseException">If operation fails after all retries or on permanent error.</exception>
    Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken ct);
}
