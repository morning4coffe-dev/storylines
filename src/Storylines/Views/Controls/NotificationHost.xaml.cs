using CommunityToolkit.WinUI.Behaviors;

namespace Storylines.Views.Controls;

public sealed partial class NotificationHost : UserControl
{
    private Action _onPersistentClosed;

    public NotificationHost()
    {
        InitializeComponent();
    }

    public void ShowNotification(InfoBarSeverity severity, string title, string message, TimeSpan? duration = null)
    {
        var notification = new Notification
        {
            Severity = severity,
            Title = title ?? string.Empty,
            Message = message ?? string.Empty
        };

        if (duration.HasValue)
            notification.Duration = duration.Value;

        NotificationQueue.Show(notification);
    }

    public void ShowPersistentNotification(PersistentNotificationEvent e)
    {
        PersistentInfoBar.Title = e.Title ?? string.Empty;
        PersistentInfoBar.Message = e.Message ?? string.Empty;
        PersistentInfoBar.Severity = e.Severity;
        PersistentInfoBar.IsClosable = e.IsClosable;
        PersistentInfoBar.Width = e.Width;
        PersistentInfoBar.IconSource = e.IconSource;
        PersistentInfoBar.RequestedTheme = ActualTheme;
        _onPersistentClosed = e.OnClosed;

        PersistentDetailText.Text = e.Detail ?? string.Empty;
        PersistentDetailText.Visibility = string.IsNullOrEmpty(e.Detail)
            ? Visibility.Collapsed
            : Visibility.Visible;

        PersistentProgressBar.IsIndeterminate = e.IsProgressIndeterminate;
        PersistentProgressBar.Value = e.ProgressValue;
        PersistentProgressBar.Visibility = e.HasProgressBar ? Visibility.Visible : Visibility.Collapsed;

        PersistentButtonsPanel.Children.Clear();
        if (e.Buttons?.Count > 0)
        {
            foreach (var btn in e.Buttons)
            {
                var button = new Button { Content = btn.Label };
                var callback = btn.OnClick;
                button.Click += (_, _) => callback?.Invoke();
                PersistentButtonsPanel.Children.Add(button);
            }
            PersistentButtonsPanel.Visibility = Visibility.Visible;
        }
        else
        {
            PersistentButtonsPanel.Visibility = Visibility.Collapsed;
        }

        PersistentInfoBar.Visibility = Visibility.Visible;
        PersistentInfoBar.IsOpen = true;
    }

    public void UpdatePersistentProgress(double value, bool isIndeterminate)
    {
        PersistentProgressBar.IsIndeterminate = isIndeterminate;
        PersistentProgressBar.Value = value;
    }

    public void DismissPersistentNotification()
    {
        PersistentInfoBar.IsOpen = false;
        PersistentInfoBar.Visibility = Visibility.Collapsed;
    }

    private void OnPersistentInfoBarClosed(InfoBar sender, InfoBarClosedEventArgs args)
    {
        PersistentInfoBar.Visibility = Visibility.Collapsed;

        // Only fire the callback for explicit user close, not programmatic dismiss.
        if (args.Reason == InfoBarCloseReason.CloseButton)
        {
            _onPersistentClosed?.Invoke();
        }
        _onPersistentClosed = null;
    }
}