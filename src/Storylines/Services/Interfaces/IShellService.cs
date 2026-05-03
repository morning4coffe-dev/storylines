using System;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Owns app-shell concerns currently scattered across <c>AppView.current</c>: title-bar
    /// configuration, focus root, the currently-open dialog tracker, and theme bridge to the
    /// <c>FrameworkElement</c> root. Centralising this lets ViewModels request shell-level
    /// behaviour without grabbing a static reference to the page tree, and gives us a clean
    /// migration boundary when WinUI 3 replaces the UWP-specific shell APIs.
    /// </summary>
    public interface IShellService
    {
        /// <summary>
        /// Currently-open ContentDialog — used by recovery / save flows that need to suppress
        /// secondary dialogs. <c>null</c> when no dialog is showing.
        /// </summary>
        object CurrentDialog { get; set; }

        /// <summary>
        /// Push a system-level focus to the shell. Used after closing dialogs or transitioning
        /// modes when no specific control should claim focus.
        /// </summary>
        void RequestShellFocus();

        /// <summary>
        /// Raised when the user changes the shell-level theme so toolbar / footer chrome can
        /// re-skin without reaching into the page tree.
        /// </summary>
        event Action ShellThemeChanged;
    }
}
