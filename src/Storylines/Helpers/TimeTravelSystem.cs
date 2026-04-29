using Storylines.Models;
using Storylines.Services;
using Storylines.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace Storylines.Helpers
{
    static class TimeTravelSystem
    {
        private static EventAggregator Events => App.GetService<EventAggregator>();

        public static bool unSavedProgress = false;

        public static void SomethingChanged()
        {
            unSavedProgress = true;
            Events.Publish(new TitleBarUpdateEvent());
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Chapter undo / redo
    // ──────────────────────────────────────────────────────────────────

    public static class TimeTravelChapter
    {
        private static readonly UndoRedoManager _manager = new UndoRedoManager(100);

        private static ProjectState State => App.GetService<ProjectState>();
        private static ITextEditorService TextEditor => App.GetService<ITextEditorService>();
        private static EventAggregator Events => App.GetService<EventAggregator>();

        static TimeTravelChapter()
        {
            _manager.StateChanged += PublishState;
        }

        public enum Changed { Added, Name, Text, Reordered, Removed }

        /// <summary>True while an undo or redo operation is executing.</summary>
        public static bool IsExecuting => _manager.IsExecuting;

        /// <summary>
        /// Suppresses undo recording for the duration of the returned scope.
        /// Also breaks the merge chain so edits after the scope don't merge
        /// with edits before it.
        /// </summary>
        public static IDisposable SuppressRecording() => _manager.Suppress();

        // ── Recording ─────────────────────────────────────────────────

        /// <summary>Record that a chapter was added. Call AFTER the chapter is in the collection.</summary>
        public static void RecordAdded(Chapter chapter, int position)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            var snapshot = State.CopyChapter(chapter.Token);
            _manager.Record(new ChapterAddedAction(snapshot, position));
        }

        /// <summary>Record that a chapter is about to be renamed. Call BEFORE the rename.</summary>
        public static void RecordRename(Chapter chapter, string newName)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            _manager.Record(new ChapterRenamedAction(chapter.Token, chapter.Name, newName));
        }

        /// <summary>Record that a chapter is about to be removed. Call BEFORE the removal.</summary>
        public static void RecordRemoved(Chapter chapter, int position)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            var snapshot = State.CopyChapter(chapter.Token);
            _manager.Record(new ChapterRemovedAction(snapshot, position));
        }

        /// <summary>Record that a chapter is about to be reordered. Call BEFORE the move.</summary>
        public static void RecordReorder(string chapterToken, int oldPosition, int newPosition)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            _manager.Record(new ChapterReorderedAction(chapterToken, oldPosition, newPosition));
        }

        /// <summary>
        /// Record a text change. <paramref name="oldText"/> is the chapter text
        /// before the edit; <paramref name="newText"/> is the text after.
        /// Consecutive text changes to the same chapter are merged automatically.
        /// </summary>
        public static void RecordTextChange(string chapterToken, string oldText, string newText)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            if (oldText == newText) return;
            TimeTravelSystem.SomethingChanged();

            _manager.Record(new ChapterTextChangedAction(chapterToken, oldText, newText));
        }

        public static void Undo() => _manager.Undo();
        public static void Redo() => _manager.Redo();
        public static void ClearUndoAndRedo() => _manager.Clear();

        // ── State publishing ──────────────────────────────────────────

        private static void PublishState()
        {
            Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = _manager.CanUndo,
                CanRedo = _manager.CanRedo,
                Context = "chapters"
            });
        }

        // ── Action classes ────────────────────────────────────────────

        private sealed class ChapterAddedAction : IUndoableAction
        {
            private readonly Chapter _snapshot;
            private readonly int _position;

            public ChapterAddedAction(Chapter snapshot, int position)
            {
                _snapshot = snapshot;
                _position = position;
            }

            public void Undo() => State.RemoveChapter(_snapshot.Token);

            public void Redo()
            {
                int pos = Math.Min(_position, State.Chapters.Count);
                State.InsertExistingChapter(_snapshot.Name, _snapshot.Token, _snapshot.Text, pos, lastCaretPosition: _snapshot.LastCaretPosition, lastVerticalOffset: _snapshot.LastVerticalOffset);
            }

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private sealed class ChapterRemovedAction : IUndoableAction
        {
            private readonly Chapter _snapshot;
            private readonly int _position;

            public ChapterRemovedAction(Chapter snapshot, int position)
            {
                _snapshot = snapshot;
                _position = position;
            }

            public void Undo()
            {
                int pos = Math.Min(_position, State.Chapters.Count);
                State.InsertExistingChapter(_snapshot.Name, _snapshot.Token, _snapshot.Text, pos, lastCaretPosition: _snapshot.LastCaretPosition, lastVerticalOffset: _snapshot.LastVerticalOffset);
            }

            public void Redo() => State.RemoveChapter(_snapshot.Token);

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private sealed class ChapterRenamedAction : IUndoableAction
        {
            private readonly string _token;
            private readonly string _oldName;
            private readonly string _newName;

            public ChapterRenamedAction(string token, string oldName, string newName)
            {
                _token = token;
                _oldName = oldName;
                _newName = newName;
            }

            public void Undo() => SetName(_oldName);
            public void Redo() => SetName(_newName);

            private void SetName(string name)
            {
                var chapter = State.FindChapter(_token);
                if (chapter != null)
                    chapter.Name = name;
            }

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private sealed class ChapterReorderedAction : IUndoableAction
        {
            private readonly string _token;
            private readonly int _oldPosition;
            private readonly int _newPosition;

            public ChapterReorderedAction(string token, int oldPosition, int newPosition)
            {
                _token = token;
                _oldPosition = oldPosition;
                _newPosition = newPosition;
            }

            public void Undo() => MoveChapter(_oldPosition);
            public void Redo() => MoveChapter(_newPosition);

            private void MoveChapter(int targetPosition)
            {
                var chapter = State.FindChapter(_token);
                if (chapter == null) return;

                State.Chapters.Remove(chapter);
                int insertAt = Math.Min(targetPosition, State.Chapters.Count);
                State.Chapters.Insert(insertAt, chapter);
            }

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private sealed class ChapterTextChangedAction : IUndoableAction
        {
            private readonly string _token;
            private readonly string _beforeText;
            private string _afterText;

            public ChapterTextChangedAction(string token, string beforeText, string afterText)
            {
                _token = token;
                _beforeText = beforeText;
                _afterText = afterText;
            }

            public void Undo() => ApplyText(_beforeText);
            public void Redo() => ApplyText(_afterText);

            private void ApplyText(string text)
            {
                var chapter = State.FindChapter(_token);
                if (chapter == null) return;

                chapter.Text = text;

                // If this chapter is currently loaded in the editor, refresh the UI
                int chapterIndex = State.FindChapterID(_token);
                if (TextEditor.SelectedChapterIndex == chapterIndex)
                {
                    TextEditor.LoadChapterContent(chapter);
                    // Sync model with what the editor actually stores.
                    // RichEditBox may normalize RTF (paragraph marks, etc.)
                    // so the round-tripped text can differ from the snapshot.
                    // By syncing here we prevent the next OnTextBox_TextChanged
                    // from seeing a stale mismatch and recording a spurious entry.
                    chapter.Text = TextEditor.GetText(TextFormat.Rtf);
                }
            }

            public bool TryMerge(IUndoableAction newer)
            {
                if (newer is ChapterTextChangedAction textAction && textAction._token == _token)
                {
                    // Only merge small changes (individual keystrokes).
                    // Large differences (paste, cut) become separate undo steps.
                    int stepDiff = Math.Abs((_afterText?.Length ?? 0) - (textAction._afterText?.Length ?? 0));
                    if (stepDiff > 2) return false;

                    // Cap total accumulated change so the user can step back
                    // in roughly word-sized increments.
                    int totalDiff = Math.Abs((textAction._afterText?.Length ?? 0) - (_beforeText?.Length ?? 0));
                    if (totalDiff > 30) return false;

                    _afterText = textAction._afterText;
                    return true;
                }
                return false;
            }
        }
    }

    // ──────────────────────────────────────────────────────────────────
    //  Character undo / redo
    // ──────────────────────────────────────────────────────────────────

    public static class TimeTravelCharacter
    {
        private static readonly UndoRedoManager _manager = new UndoRedoManager(100);

        private static ProjectState State => App.GetService<ProjectState>();
        private static EventAggregator Events => App.GetService<EventAggregator>();
        private static ILogger Logger => App.GetService<ILogger>();

        static TimeTravelCharacter()
        {
            _manager.StateChanged += PublishState;
        }

        public enum Changed { Added, Changed, Removed }

        /// <summary>Record that a character was added. Call AFTER the character is in the collection.</summary>
        public static void RecordAdded(Character character)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            var snapshot = State.CopyCharacter(character.Token);
            _manager.Record(new CharacterAddedAction(snapshot));
        }

        /// <summary>Record that a character is about to be removed. Call BEFORE the removal.</summary>
        public static void RecordRemoved(Character character)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            var snapshot = State.CopyCharacter(character.Token);
            _manager.Record(new CharacterRemovedAction(snapshot));
        }

        /// <summary>
        /// Record that a character is about to be changed. Pass a copy of the
        /// current state (before changes are applied).
        /// Uses a snapshot-swap pattern: each undo/redo captures the current
        /// state before restoring the saved one.
        /// </summary>
        public static void RecordChanged(Character beforeSnapshot)
        {
            if (_manager.IsExecuting || _manager.IsSuppressed) return;
            TimeTravelSystem.SomethingChanged();

            _manager.Record(new CharacterChangedAction(beforeSnapshot));
        }

        public static void Undo() => _manager.Undo();
        public static void Redo() => _manager.Redo();
        public static void ClearUndoAndRedo() => _manager.Clear();

        // ── State publishing ──────────────────────────────────────────

        private static void PublishState()
        {
            Events.Publish(new UndoRedoStateChangedEvent
            {
                CanUndo = _manager.CanUndo,
                CanRedo = _manager.CanRedo,
                Context = "characters"
            });
        }

        // ── Action classes ────────────────────────────────────────────

        private sealed class CharacterAddedAction : IUndoableAction
        {
            private readonly Character _snapshot;

            public CharacterAddedAction(Character snapshot) => _snapshot = snapshot;

            public void Undo() => State.RemoveCharacter(_snapshot.Token);

            public void Redo() => _ = RestoreCharacterAsync(_snapshot, "redo character add");

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private sealed class CharacterRemovedAction : IUndoableAction
        {
            private readonly Character _snapshot;

            public CharacterRemovedAction(Character snapshot) => _snapshot = snapshot;

            public void Undo() => _ = RestoreCharacterAsync(_snapshot, "undo character removal");

            public void Redo() => State.RemoveCharacter(_snapshot.Token);

            public bool TryMerge(IUndoableAction newer) => false;
        }

        private static async System.Threading.Tasks.Task RestoreCharacterAsync(Character snapshot, string operation)
        {
            try
            {
                await State.AddExistingCharacterAsync(
                    snapshot.Name,
                    snapshot.Token,
                    snapshot.Description,
                    snapshot.Picture,
                    snapshot.Role,
                    snapshot.Age,
                    snapshot.Appearance,
                    snapshot.Traits);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to {operation}", ex);
            }
        }

        /// <summary>
        /// Uses a snapshot-swap pattern: the action always holds "the other"
        /// state. Each Undo/Redo captures the current state, then restores
        /// the saved snapshot, so the action toggles between the two states.
        /// </summary>
        private sealed class CharacterChangedAction : IUndoableAction
        {
            private Character _snapshot;

            public CharacterChangedAction(Character beforeSnapshot)
            {
                _snapshot = beforeSnapshot;
            }

            public void Undo() => Swap();
            public void Redo() => Swap();

            private void Swap()
            {
                var chapter = State.FindCharacter(_snapshot.Token);
                if (chapter == null) return;

                int idx = State.FindCharacterID(_snapshot.Token);
                var current = State.CopyCharacter(_snapshot.Token);
                State.Characters[idx] = _snapshot;
                _snapshot = current;

                Events.Publish(new CharacterSelectedEvent { SelectedIndex = idx, HasSelection = true });
            }

            public bool TryMerge(IUndoableAction newer) => false;
        }
    }
}
