using Xunit;

namespace Storylines.Tests.Services;

/// <summary>
/// Tests for the EventAggregator pub/sub mechanism.
///
/// EventAggregator.cs also defines WinUI-specific event types (e.g. InAppNotificationEvent
/// which references Microsoft.UI.Xaml.Controls.InfoBarSeverity), so we use a local copy
/// of the class here rather than linking the file. The logic is identical.
/// </summary>
public class EventAggregatorTests
{
    // Local copy of EventAggregator — identical logic to Storylines.Services.EventAggregator
    private sealed class EventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            if (!_subscribers.ContainsKey(type))
                _subscribers[type] = new List<Delegate>();
            _subscribers[type].Add(handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            if (_subscribers.ContainsKey(type))
                _subscribers[type].Remove(handler);
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var type = typeof(TEvent);
            if (_subscribers.ContainsKey(type))
                foreach (var handler in _subscribers[type].ToArray())
                    (handler as Action<TEvent>)?.Invoke(eventData);
        }
    }

    // Simple test event types — no WinUI dependencies
    private record TestEvent(string Message);
    private record OtherEvent(int Value);

    [Fact]
    public void Subscribe_ThenPublish_HandlerReceivesEvent()
    {
        var ea = new EventAggregator();
        TestEvent? received = null;
        ea.Subscribe<TestEvent>(e => received = e);

        ea.Publish(new TestEvent("hello"));

        Assert.NotNull(received);
        Assert.Equal("hello", received.Message);
    }

    [Fact]
    public void Publish_WithNoSubscribers_DoesNotThrow()
    {
        var ea = new EventAggregator();
        var ex = Record.Exception(() => ea.Publish(new TestEvent("nothing")));
        Assert.Null(ex);
    }

    [Fact]
    public void Unsubscribe_StopsHandlerFromReceivingFutureEvents()
    {
        var ea = new EventAggregator();
        var count = 0;
        Action<TestEvent> handler = _ => count++;
        ea.Subscribe(handler);

        ea.Publish(new TestEvent("first"));
        ea.Unsubscribe(handler);
        ea.Publish(new TestEvent("second"));

        Assert.Equal(1, count);
    }

    [Fact]
    public void Unsubscribe_NonRegisteredHandler_DoesNotThrow()
    {
        var ea = new EventAggregator();
        Action<TestEvent> handler = _ => { };
        var ex = Record.Exception(() => ea.Unsubscribe(handler));
        Assert.Null(ex);
    }

    [Fact]
    public void MultipleSubscribers_AllReceiveTheSameEvent()
    {
        var ea = new EventAggregator();
        var received = new List<string>();
        ea.Subscribe<TestEvent>(e => received.Add("A:" + e.Message));
        ea.Subscribe<TestEvent>(e => received.Add("B:" + e.Message));

        ea.Publish(new TestEvent("hi"));

        Assert.Equal(2, received.Count);
        Assert.Contains("A:hi", received);
        Assert.Contains("B:hi", received);
    }

    [Fact]
    public void DifferentEventTypes_DoNotInterfereWithEachOther()
    {
        var ea = new EventAggregator();
        string? lastMessage = null;
        int? lastValue = null;
        ea.Subscribe<TestEvent>(e => lastMessage = e.Message);
        ea.Subscribe<OtherEvent>(e => lastValue = e.Value);

        ea.Publish(new TestEvent("hello"));
        ea.Publish(new OtherEvent(42));

        Assert.Equal("hello", lastMessage);
        Assert.Equal(42, lastValue);
    }

    [Fact]
    public void Publish_UsesSnapshot_SoUnsubscribingDuringHandlerIsStable()
    {
        // Ensures the ToArray() snapshot in Publish prevents a collection-modified exception
        // when a handler unsubscribes itself during invocation.
        var ea = new EventAggregator();
        var count = 0;
        Action<TestEvent>? selfRemovingHandler = null;
        selfRemovingHandler = _ =>
        {
            count++;
            ea.Unsubscribe(selfRemovingHandler!);
        };

        ea.Subscribe(selfRemovingHandler);
        ea.Publish(new TestEvent("x"));
        ea.Publish(new TestEvent("y")); // handler already unsubscribed, should not fire

        Assert.Equal(1, count);
    }

    [Fact]
    public void Subscribe_SameHandlerTwice_FiresTwice()
    {
        // Matches the actual behaviour: no deduplication guard in Subscribe
        var ea = new EventAggregator();
        var count = 0;
        Action<TestEvent> handler = _ => count++;
        ea.Subscribe(handler);
        ea.Subscribe(handler);

        ea.Publish(new TestEvent("x"));

        Assert.Equal(2, count);
    }

    [Fact]
    public void Publish_DataIsPassedUntouched_ToHandler()
    {
        var ea = new EventAggregator();
        OtherEvent? captured = null;
        ea.Subscribe<OtherEvent>(e => captured = e);

        ea.Publish(new OtherEvent(99));

        Assert.NotNull(captured);
        Assert.Equal(99, captured.Value);
    }
}
