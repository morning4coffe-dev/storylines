using System;
using System.Collections.Generic;

namespace Storylines.Scripts.Services
{
    public class EventAggregator
    {
        private readonly Dictionary<Type, List<Delegate>> _subscribers = new Dictionary<Type, List<Delegate>>();

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
            {
                foreach (var handler in _subscribers[type].ToArray())
                {
                    (handler as Action<TEvent>)?.Invoke(eventData);
                }
            }
        }
    }

    #region Event Types

    public class ProgressBarEvent
    {
        public bool Show { get; set; }
        public bool IsIndeterminate { get; set; }
        public int Value { get; set; }
        public ProgressState State { get; set; } = ProgressState.Normal;

        public enum ProgressState { Normal, Paused, Error }
    }

    public class InAppNotificationEvent
    {
        public Microsoft.UI.Xaml.Controls.InfoBarSeverity Severity { get; set; }
        public string Title { get; set; }
        public string LongText { get; set; }
    }

    public class UndoRedoStateChangedEvent
    {
        public bool CanUndo { get; set; }
        public bool CanRedo { get; set; }
        public string Context { get; set; } // "chapters" or "characters"
    }

    public class TitleBarUpdateEvent { }

    public class ToolsStateChangedEvent
    {
        public bool IsStorylinesDocument { get; set; }
    }

    #endregion
}
