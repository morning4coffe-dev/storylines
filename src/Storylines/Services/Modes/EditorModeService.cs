using Storylines.Services.Modes.Impl;
using System;
using System.Collections.Generic;

namespace Storylines.Services.Modes
{
    /// <summary>
    /// Tracks the active editor mode and coordinates transitions.
    /// Shell surfaces (MainPage, AppView, ShortcutManager) observe <see cref="ModeChanged"/>
    /// and consume <see cref="Current"/>.<see cref="IEditorMode.Chrome"/> — modes do not
    /// mutate views directly.
    /// </summary>
    public class EditorModeService
    {
        public IEditorMode Current { get; private set; } = EditMode.Instance;

            /// <summary>
            /// All modes available for selection in the mode picker.
            /// Modes that require per-session configuration (e.g. FocusMode) are
            /// represented by factory delegates rather than singleton instances.
            /// </summary>
            public IReadOnlyList<IEditorMode> RegisteredModes { get; } = new List<IEditorMode>
            {
                EditMode.Instance,
                ReadOnlyMode.Instance,
                // FocusMode is configured interactively — not pre-registered here.
            };

        public event Action<IEditorMode> ModeChanged;

        public void Activate(IEditorMode mode)
        {
            if (mode == null) throw new ArgumentNullException(nameof(mode));
            if (ReferenceEquals(mode, Current)) return;

            Current.Leave();
            Current = mode;
            mode.Enter();
            ModeChanged?.Invoke(mode);
        }

        public void Deactivate() => Activate(EditMode.Instance);

        /// <summary>
        /// Attempts to return to edit mode, honoring the current mode's
        /// <see cref="IEditorMode.CanLeave"/> gate. Returns false when blocked
        /// (caller should surface a confirmation UI).
        /// </summary>
        public bool TryLeave()
        {
            if (!Current.CanLeave) return false;
            Deactivate();
            return true;
        }

        public bool IsInMode(string id) => Current.Id == id;
    }
}
