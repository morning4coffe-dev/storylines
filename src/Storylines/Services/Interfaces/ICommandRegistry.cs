using System.Windows.Input;

namespace Storylines.Services.Interfaces;

/// <summary>
/// Central registry of invocable application commands. Powers the Command Palette and any
/// future surface (settings shortcut list, help dialog) that needs to enumerate commands.
/// Each feature module registers its commands once at startup; consumers fuzzy-search the
/// registry to look them up.
/// </summary>
public interface ICommandRegistry
{
    /// <summary>
    /// All registered commands, in registration order.
    /// </summary>
    IReadOnlyList<AppCommand> Commands { get; }

    /// <summary>
    /// Register a command. Re-registering the same <see cref="AppCommand.Id"/> replaces the
    /// previous entry so plugins can hot-reload without leaking ghosts.
    /// </summary>
    void Register(AppCommand command);

    /// <summary>
    /// Remove a registered command by id.
    /// </summary>
    bool Unregister(string commandId);

    /// <summary>
    /// Fuzzy-search commands by display name, id, or category. Empty query returns every
    /// command. Designed for the Command Palette: results are scored by match quality.
    /// </summary>
    IReadOnlyList<AppCommand> Search(string query);
}

/// <summary>
/// A single invocable command discoverable through the Command Palette.
/// </summary>
public sealed class AppCommand
{
    public AppCommand(string id, string displayName, string category, ICommand command, string keyboardShortcut = null, string glyph = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Category = category ?? string.Empty;
        Command = command ?? throw new ArgumentNullException(nameof(command));
        KeyboardShortcut = keyboardShortcut;
        Glyph = glyph;
    }

    /// <summary>Stable, unique identifier (e.g. <c>"editor.toggleFocus"</c>).</summary>
    public string Id { get; }

    /// <summary>User-facing label, already localised.</summary>
    public string DisplayName { get; }

    /// <summary>Grouping label shown in the palette (e.g. "Editor", "Speech", "Project").</summary>
    public string Category { get; }

    /// <summary>The executable command. Palette invokes <see cref="ICommand.Execute"/> with <c>null</c> parameter.</summary>
    public ICommand Command { get; }

    /// <summary>Optional human-readable shortcut hint (e.g. <c>"Ctrl+Shift+P"</c>).</summary>
    public string KeyboardShortcut { get; }

    /// <summary>Optional Segoe Fluent Icons glyph code-point for the palette row.</summary>
    public string Glyph { get; }
}
