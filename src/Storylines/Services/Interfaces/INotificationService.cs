
namespace Storylines.Services.Interfaces;

public interface INotificationService
{
    void ShowNotification(NotificationRequest notification);
    void ShowNotification(InfoBarSeverity severity, string title, string message = "");
    void ShowProgressBar(bool isIndeterminate);
    void UpdateProgressBar(int value, ProgressBarState state = ProgressBarState.Normal);
    void HideProgressBar();

    /// <summary>Shows a persistent notification with optional action buttons, detail text, and a progress bar.</summary>
    void ShowPersistentNotification(PersistentNotificationRequest request);

    /// <summary>Updates the progress bar inside the currently visible persistent notification.</summary>
    void UpdatePersistentNotificationProgress(double value, bool isIndeterminate = false);

    /// <summary>Programmatically closes the persistent notification without firing the OnClosed callback.</summary>
    void DismissPersistentNotification();
}

public sealed class NotificationRequest
{
    public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// Describes a persistent notification shown in the dedicated InfoBar slot of NotificationHost.
/// Supports action buttons, a detail line, an inline progress bar, and a custom icon.
/// </summary>
public sealed class PersistentNotificationRequest
{
    public InfoBarSeverity Severity { get; init; } = InfoBarSeverity.Informational;
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    /// <summary>Optional secondary description shown below the message.</summary>
    public string Detail { get; init; } = string.Empty;
    public IconSource IconSource { get; init; }
    public bool IsClosable { get; init; } = true;
    /// <summary>Invoked when the user closes the notification via the close button (not on programmatic dismiss).</summary>
    public Action OnClosed { get; init; }
    public IReadOnlyList<NotificationButton> Buttons { get; init; }
    public bool HasProgressBar { get; init; }
    public bool IsProgressIndeterminate { get; init; }
    public double ProgressValue { get; init; }
    /// <summary>Maximum width of the InfoBar in pixels. Defaults to 440.</summary>
    public int Width { get; init; } = 440;
}

/// <summary>An action button displayed inside a persistent notification.</summary>
public sealed class NotificationButton
{
    public string Label { get; init; } = string.Empty;
    public Action OnClick { get; init; }
}

public enum ProgressBarState
{
    Normal,
    Paused,
    Error,
}
