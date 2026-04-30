using System;

namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Cross-context undo/redo coordinator. Replaces the static <c>TimeTravelSystem</c> /
    /// <c>UndoRedoManager</c> helpers so consumers (ViewModels, change recorders) can be
    /// constructor-injected and unit-tested. Concrete implementation lands in Phase 2 cleanup;
    /// this interface defines the contract callers will move to.
    /// </summary>
    public interface IUndoRedoService
    {
        /// <summary>
        /// Whether undo is currently available for the given <paramref name="context"/> (e.g.
        /// <c>"chapters"</c> or <c>"characters"</c>).
        /// </summary>
        bool CanUndo(string context);

        /// <summary>
        /// Whether redo is currently available for the given <paramref name="context"/>.
        /// </summary>
        bool CanRedo(string context);

        /// <summary>
        /// Whether the project has unsaved changes since the last successful save. Replaces the
        /// static <c>TimeTravelSystem.unSavedProgress</c> flag.
        /// </summary>
        bool IsDirty { get; }

        /// <summary>
        /// Mark the project as clean, typically immediately after a successful save.
        /// </summary>
        void MarkClean();

        /// <summary>
        /// Mark the project as dirty (called by change recorders after recording an action).
        /// </summary>
        void MarkDirty();

        /// <summary>
        /// Trigger an undo on the named context. No-op if <see cref="CanUndo"/> is false.
        /// </summary>
        void Undo(string context);

        /// <summary>
        /// Trigger a redo on the named context. No-op if <see cref="CanRedo"/> is false.
        /// </summary>
        void Redo(string context);

        /// <summary>
        /// Raised whenever undo/redo availability changes for any context, with the context
        /// label as payload so listeners can target their refresh.
        /// </summary>
        event Action<string> StateChanged;
    }
}
