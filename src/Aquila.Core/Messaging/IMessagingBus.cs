namespace Aquila.Core.Messaging;

/// <summary>
/// Central messaging bus for inter-module communication via publish-subscribe pattern.
/// </summary>
public interface IMessagingBus
{
    /// <summary>
    /// Publish an event to all subscribers.
    /// </summary>
    /// <param name="event">The event to publish.</param>
    /// <returns>Completes when all synchronous handlers have executed.</returns>
    Task Publish<TEvent>(TEvent @event) where TEvent : class;

    /// <summary>
    /// Subscribe to a type of event with a handler.
    /// </summary>
    /// <param name="handler">The handler to invoke when events are published.</param>
    /// <returns>A subscription token that can be used to unsubscribe.</returns>
    SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class;

    /// <summary>
    /// Unsubscribe from a specific event type using a subscription token.
    /// </summary>
    void Unsubscribe<TEvent>(SubscriptionToken token) where TEvent : class;
}

/// <summary>
/// Token representing an event subscription, used for unsubscribing.
/// </summary>
public class SubscriptionToken : IDisposable
{
    private readonly Action _unsubscribeAction;
    private bool _disposed;

    public SubscriptionToken(Action unsubscribeAction)
    {
        _unsubscribeAction = unsubscribeAction;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _unsubscribeAction();
    }
}

// Common event types
namespace Aquila.Core.Messaging.Events;

/// <summary>
/// Published when application is shutting down.
/// </summary>
public class ApplicationShutdownEvent
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// Published when an error occurs that should be logged.
/// </summary>
public class ErrorEvent
{
    public required string Message { get; init; }
    public Exception? Exception { get; init; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// Published when application state changes.
/// </summary>
public class ApplicationStateChangedEvent
{
    public required ApplicationState NewState { get; init; }
    public ApplicationState PreviousState { get; init; }
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public enum ApplicationState
{
    Starting,
    Ready,
    Shutdown
}
