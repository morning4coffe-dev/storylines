
namespace Storylines.Services.Interfaces;

public interface IDeveloperDiagnosticsService
{
    DeveloperDiagnosticsSnapshot CaptureSnapshot();
    IReadOnlyList<string> GetRecentLogEntries();
}

public sealed class DeveloperDiagnosticsSnapshot
{
    public string CurrentPage { get; init; } = string.Empty;
    public string CurrentTheme { get; init; } = string.Empty;
    public string WindowSize { get; init; } = string.Empty;
    public string CurrentDialog { get; init; } = string.Empty;
    public int InvisibleControlCount { get; init; }
    public IReadOnlyList<DeveloperDiagnosticItem> InvisibleControls { get; init; } = Array.Empty<DeveloperDiagnosticItem>();
    public int AttachedBehaviorCount { get; init; }
    public IReadOnlyList<DeveloperDiagnosticItem> AttachedBehaviors { get; init; } = Array.Empty<DeveloperDiagnosticItem>();
}

public sealed class DeveloperDiagnosticItem
{
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}
