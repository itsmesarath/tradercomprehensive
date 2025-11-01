using Serilog;

namespace Aquila.Core.Messaging;

/// <summary>
/// Thread-safe implementation of the messaging bus.
/// </summary>
public class MessagingBus : IMessagingBus
{
    private class Subscription<TEvent> where TEvent : class
    {
        public SubscriptionToken Token { get; }
        public Func<TEvent, Task> Handler { get; }

        public Subscription(SubscriptionToken token, Func<TEvent, Task> handler)
        {
            Token = token;
            Handler = handler;
        }
    }

    private readonly object _lockObj = new();
    private readonly Dictionary<Type, List<object>> _subscriptions;
    private readonly ILogger _logger;

    public MessagingBus()
    {
        _subscriptions = new Dictionary<Type, List<object>>();
        _logger = Log.ForContext<MessagingBus>();
    }

    public async Task Publish<TEvent>(TEvent @event) where TEvent : class
    {
        var eventType = typeof(TEvent);
        List<object> handlers;

        lock (_lockObj)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
                return;

            handlers = new List<object>(subs);
        }

        try
        {
            var tasks = new List<Task>();

            foreach (var subscription in handlers.OfType<Subscription<TEvent>>())
            {
                try
                {
                    tasks.Add(subscription.Handler(@event));
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Error in event handler for {EventType}", eventType.Name);
                }
            }

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error publishing event {EventType}", eventType.Name);
        }
    }

    public SubscriptionToken Subscribe<TEvent>(Func<TEvent, Task> handler) where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);
        var token = new SubscriptionToken(() => Unsubscribe<TEvent>(token));
        var subscription = new Subscription<TEvent>(token, handler);

        lock (_lockObj)
        {
            if (!_subscriptions.TryGetValue(eventType, out var subs))
            {
                subs = new List<object>();
                _subscriptions[eventType] = subs;
            }

            subs.Add(subscription);
        }

        _logger.Debug("Subscription added for event {EventType}", eventType.Name);
        return token;
    }

    public void Unsubscribe<TEvent>(SubscriptionToken token) where TEvent : class
    {
        if (token == null)
            return;

        var eventType = typeof(TEvent);

        lock (_lockObj)
        {
            if (_subscriptions.TryGetValue(eventType, out var subs))
            {
                var toRemove = subs.FirstOrDefault(s =>
                {
                    if (s is Subscription<TEvent> sub)
                        return sub.Token == token;
                    return false;
                });

                if (toRemove != null)
                {
                    subs.Remove(toRemove);
                    _logger.Debug("Subscription removed for event {EventType}", eventType.Name);
                }
            }
        }
    }
}
