// src/Acode.Application/Events/IEventPublisher.cs
namespace Acode.Application.Events;

using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Application service for publishing domain events.
/// Used for cross-cutting concerns like logging, metrics, and notifications.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to publish.</typeparam>
    /// <param name="domainEvent">The event instance to publish.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken ct)
        where TEvent : class;
}
