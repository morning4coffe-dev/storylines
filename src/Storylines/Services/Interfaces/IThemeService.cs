using System;
using Windows.UI;
using Windows.UI.Xaml;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Centralizes theme and accent-color application across the app. Replaces direct mutation of
    /// hardcoded resource-dictionary entries and per-window theme code so the same service can be
    /// reused after the planned WinUI 3 migration where theme APIs differ.
    /// </summary>
    public interface IThemeService
    {
        /// <summary>
        /// The currently applied accent color. Updates raise <see cref="AccentChanged"/>.
        /// </summary>
        Color AccentColor { get; }

        /// <summary>
        /// The currently applied app element theme (Light, Dark or Default/system).
        /// </summary>
        ElementTheme RequestedTheme { get; }

        /// <summary>
        /// Raised after a successful accent-color change with the new value.
        /// </summary>
        event Action<Color> AccentChanged;

        /// <summary>
        /// Raised after the requested theme has been applied.
        /// </summary>
        event Action<ElementTheme> ThemeChanged;

        /// <summary>
        /// Persist and apply a new accent color across all live windows.
        /// </summary>
        void ApplyAccent(Color color);

        /// <summary>
        /// Persist and apply a new requested theme.
        /// </summary>
        void ApplyTheme(ElementTheme theme);
    }
}
