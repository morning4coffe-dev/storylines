using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Storylines.Services
{
    public class EventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<Delegate>> _subscribers = new ConcurrentDictionary<Type, List<Delegate>>();
        private readonly object _lock = new object();

        public void Subscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(type))
                    _subscribers[type] = new List<Delegate>();

                _subscribers[type].Add(handler);
            }
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            var type = typeof(TEvent);
            lock (_lock)
            {
                if (_subscribers.ContainsKey(type))
                    _subscribers[type].Remove(handler);
            }
        }

        public void Publish<TEvent>(TEvent eventData)
        {
            var type = typeof(TEvent);
            Delegate[] snapshot;
            lock (_lock)
            {
                if (!_subscribers.ContainsKey(type))
                    return;
                snapshot = _subscribers[type].ToArray();
            }

            foreach (var handler in snapshot)
            {
                (handler as Action<TEvent>)?.Invoke(eventData);
            }
        }
    }

    #region Event Types

    public class ProgressBarEvent
    {
        public bool Show { get; set; }
        public bool IsIndeterminate { get; set; }
        public int Value { get; set; }
        public ProgressState State { get; set; } = ProgressState.Normal;

        public enum ProgressState { Normal, Paused, Error }
    }

    public class InAppNotificationEvent
    {
        public Microsoft.UI.Xaml.Controls.InfoBarSeverity Severity { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public TimeSpan? Duration { get; set; }
    }

    public class UndoRedoStateChangedEvent
    {
        public bool CanUndo { get; set; }
        public bool CanRedo { get; set; }
        public string Context { get; set; } // "chapters" or "characters"
    }

    public class TitleBarUpdateEvent { }

    public class ToolsStateChangedEvent
    {
        public bool IsStorylinesDocument { get; set; }
    }

    /// <summary>
    /// Published when a chapter is selected or deselected,
    /// so that business logic can react without referencing UI.
    /// </summary>
    public class ChapterSelectedEvent
    {
        public int SelectedIndex { get; set; }
        public bool HasSelection { get; set; }
    }

    /// <summary>
    /// Published when the selected chapter's text should be saved to state.
    /// </summary>
    public class ChapterTextChangedEvent
    {
        public int ChapterIndex { get; set; }
        public string Text { get; set; }
    }

    /// <summary>
    /// Published when a setting changes that requires UI to update.
    /// </summary>
    public class SettingChangedEvent
    {
        public string SettingKey { get; set; }
        public object Value { get; set; }
    }

    /// <summary>
    /// Published when chapter tools should be enabled or disabled.
    /// </summary>
    public class ChapterToolsStateEvent
    {
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Published to request opening/closing the chapter list panel.
    /// </summary>
    public class ToggleChapterListEvent
    {
        public bool Open { get; set; }
        public bool Manually { get; set; }
    }

    /// <summary>
    /// Published when the notes pane should be refreshed.
    /// </summary>
    public class RefreshNotesPaneEvent { }

    /// <summary>
    /// Published by <see cref="WritingSessionService"/> when the today-word-count
    /// or streak changes so the UI can update without polling.
    /// </summary>
    public class SessionStatsUpdatedEvent
    {
        public int TodayWords { get; set; }
        public int StreakDays { get; set; }
    }

    /// <summary>
    /// Published after a project load completes, enabling tools state updates.
    /// </summary>
    public class ProjectLoadedEvent
    {
        public bool IsStorylinesDocument { get; set; }
        public int LastOpenedChapter { get; set; }
    }

    /// <summary>
    /// Published to request the UI to clear the editor and project state.
    /// </summary>
    public class ClearProjectEvent { }

    /// <summary>
    /// Published when a character undo/redo selects a character.
    /// </summary>
    public class CharacterSelectedEvent
    {
        public int SelectedIndex { get; set; }
        public bool HasSelection { get; set; }
    }

    /// <summary>
    /// Published by FocusModeViewModel when the down-bar focus text changes.
    /// </summary>
    public class FocusModeDownBarTextChangedEvent
    {
        public string Text { get; set; }
    }

    /// <summary>
    /// Published by <c>ChapterTextBox</c> whenever the character-format of the current selection
    /// changes. Replaces direct cross-control references to
    /// <c>MainPage.CommandBar.mainBoldButton</c> etc., letting the command bar subscribe and
    /// update its toggle-button states without either control knowing about the other.
    /// </summary>
    public class TextFormattingStateChangedEvent
    {
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsUnderlined { get; set; }
        public bool IsStrikethrough { get; set; }
    }

    #endregion

#if PRIVATE_PLUGINS
    public class BranchingDialogueGraphChangedEvent
    {
        public string ChapterId { get; set; }
        public string GraphId { get; set; }
    }

    public class BranchingDialogueSimulationStateChangedEvent
    {
        public string ChapterId { get; set; }
        public Models.BranchingDialogueSimulationState State { get; set; }
    }
#endif
}
