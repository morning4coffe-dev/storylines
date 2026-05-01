using Microsoft.UI.Xaml.Controls;
using Storylines.Services.Interfaces;

namespace Storylines.Services
{
    /// <summary>
    /// Event-based implementation of <see cref="INotificationService"/> that publishes
    /// <see cref="InAppNotificationEvent"/> and <see cref="ProgressBarEvent"/> through the
    /// <see cref="EventAggregator"/> so the view layer can subscribe and handle UI updates
    /// without the service needing a direct reference to <c>AppView.current</c> or any page.
    /// </summary>
    internal sealed class NotificationService : INotificationService
    {
        private readonly EventAggregator _events;

        public NotificationService(EventAggregator events)
        {
            _events = events;
        }

        public void ShowNotification(InfoBarSeverity severity, string text, string longText = "")
        {
            _events.Publish(new InAppNotificationEvent
            {
                Severity = severity,
                Title = text,
                LongText = longText ?? string.Empty
            });
        }

        public void ShowProgressBar(bool isIndeterminate)
        {
            _events.Publish(new ProgressBarEvent
            {
                Show = true,
                IsIndeterminate = isIndeterminate,
                Value = 0,
                State = ProgressBarEvent.ProgressState.Normal
            });
        }

        public void UpdateProgressBar(int value, ProgressBarState state = ProgressBarState.Normal)
        {
            _events.Publish(new ProgressBarEvent
            {
                Show = true,
                IsIndeterminate = false,
                Value = value,
                State = (ProgressBarEvent.ProgressState)(int)state
            });
        }

        public void HideProgressBar()
        {
            _events.Publish(new ProgressBarEvent { Show = false });
        }
    }
}
