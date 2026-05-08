using Microsoft.UI.Xaml.Controls;
using System;

namespace Storylines.Services.Interfaces
{
    public interface INotificationService
    {
        void ShowNotification(NotificationRequest notification);
        void ShowNotification(InfoBarSeverity severity, string title, string message = "");
        void ShowProgressBar(bool isIndeterminate);
        void UpdateProgressBar(int value, ProgressBarState state = ProgressBarState.Normal);
        void HideProgressBar();
    }

    public sealed class NotificationRequest
    {
        public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public TimeSpan? Duration { get; init; }
    }

    public enum ProgressBarState
    {
        Normal,
        Paused,
        Error,
    }
}
