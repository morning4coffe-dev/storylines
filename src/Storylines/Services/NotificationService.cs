namespace Storylines.Services;

/// <summary>
/// Event-based implementation of <see cref="INotificationService"/> that publishes
/// <see cref="InAppNotificationEvent"/>, <see cref="ProgressBarEvent"/>, and
/// <see cref="PersistentNotificationEvent"/> through the <see cref="EventAggregator"/>
/// so the view layer can subscribe and handle UI updates without the service needing a
/// direct reference to <c>AppView.current</c> or any page.
/// </summary>
internal sealed class NotificationService : INotificationService
{
    private readonly EventAggregator _events;

    public NotificationService(EventAggregator events)
    {
        _events = events;
    }

    public void ShowNotification(NotificationRequest notification)
    {
        if (notification is null)
            return;

        _events.Publish(new InAppNotificationEvent
        {
            Severity = notification.Severity,
            Title = notification.Title ?? string.Empty,
            Message = notification.Message ?? string.Empty,
            Duration = notification.Duration
        });
    }

    public void ShowNotification(InfoBarSeverity severity, string title, string message = "")
    {
        ShowNotification(new NotificationRequest
        {
            Severity = severity,
            Title = title,
            Message = message ?? string.Empty
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

    public void ShowPersistentNotification(PersistentNotificationRequest request)
    {
        if (request is null)
            return;

        _events.Publish(new PersistentNotificationEvent
        {
            Severity = request.Severity,
            Title = request.Title ?? string.Empty,
            Message = request.Message ?? string.Empty,
            Detail = request.Detail ?? string.Empty,
            IconSource = request.IconSource,
            IsClosable = request.IsClosable,
            OnClosed = request.OnClosed,
            Buttons = request.Buttons,
            HasProgressBar = request.HasProgressBar,
            IsProgressIndeterminate = request.IsProgressIndeterminate,
            ProgressValue = request.ProgressValue,
            Width = request.Width,
        });
    }

    public void UpdatePersistentNotificationProgress(double value, bool isIndeterminate = false)
    {
        _events.Publish(new UpdatePersistentProgressEvent
        {
            Value = value,
            IsIndeterminate = isIndeterminate,
        });
    }

    public void DismissPersistentNotification()
    {
        _events.Publish(new DismissPersistentNotificationEvent());
    }
}
