using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Storylines.Views.Controls
{
    public sealed partial class NotificationHost : UserControl
    {
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
    }
}