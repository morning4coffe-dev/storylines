using System;
using System.Collections.Generic;

namespace Storylines.Helpers
{
    /// <summary>
    /// Represents a single undoable/redoable action.
    /// </summary>
    public interface IUndoableAction
    {
        /// <summary>Reverts the action.</summary>
        void Undo();

        /// <summary>Re-applies the action.</summary>
        void Redo();

        /// <summary>
        /// Attempts to merge a newer action into this one (e.g. consecutive
        /// keystrokes into a single text-change entry). Returns true if merged.
        /// </summary>
        bool TryMerge(IUndoableAction newer);
    }

    /// <summary>
    /// A bounded undo/redo manager using the Command pattern.
    /// Thread-safety is not required — all calls happen on the UI thread.
    /// </summary>
    public class UndoRedoManager
    {
        private readonly List<IUndoableAction> _undoStack = new List<IUndoableAction>();
        private readonly List<IUndoableAction> _redoStack = new List<IUndoableAction>();
        private readonly int _maxCapacity;
        private int _suppressCount;
        private bool _breakMerge;

        /// <summary>True while an undo or redo operation is in progress.</summary>
        public bool IsExecuting { get; private set; }

        /// <summary>True when recording is suppressed (e.g. during chapter switches).</summary>
        public bool IsSuppressed => _suppressCount > 0;

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        /// <summary>Raised after any push, undo, redo, or clear.</summary>
        public event Action StateChanged;

        public UndoRedoManager(int maxCapacity = 100)
        {
            _maxCapacity = maxCapacity;
        }

        /// <summary>
        /// Records a new action. If the top of the undo stack can merge with
        /// <paramref name="action"/>, the merge happens instead of a push.
        /// Clears the redo stack.
        /// </summary>
        public void Record(IUndoableAction action)
        {
            if (action == null) return;
            if (IsExecuting || IsSuppressed) return;

            // Try to merge with the current top (unless the chain was broken)
            if (!_breakMerge && _undoStack.Count > 0 && _undoStack[_undoStack.Count - 1].TryMerge(action))
            {
                _redoStack.Clear();
                StateChanged?.Invoke();
                return;
            }
            _breakMerge = false;

            // Enforce capacity
            if (_undoStack.Count >= _maxCapacity)
                _undoStack.RemoveAt(0);

            _undoStack.Add(action);
            _redoStack.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>Undoes the most recent action.</summary>
        public void Undo()
        {
            if (_undoStack.Count == 0) return;

            var action = _undoStack[_undoStack.Count - 1];
            _undoStack.RemoveAt(_undoStack.Count - 1);

            IsExecuting = true;
            try
            {
                action.Undo();
            }
            finally
            {
                IsExecuting = false;
            }

            _redoStack.Add(action);
            StateChanged?.Invoke();
        }

        /// <summary>Redoes the most recently undone action.</summary>
        public void Redo()
        {
            if (_redoStack.Count == 0) return;

            var action = _redoStack[_redoStack.Count - 1];
            _redoStack.RemoveAt(_redoStack.Count - 1);

            IsExecuting = true;
            try
            {
                action.Redo();
            }
            finally
            {
                IsExecuting = false;
            }

            _undoStack.Add(action);
            StateChanged?.Invoke();
        }

        /// <summary>Clears both stacks.</summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Returns a disposable scope during which <see cref="Record"/> calls
        /// are silently ignored. Scopes can be nested.
        /// </summary>
        /// <summary>
        /// Prevents the next <see cref="Record"/> call from merging with
        /// the current top of the undo stack.
        /// </summary>
        public void BreakMergeChain()
        {
            _breakMerge = true;
        }

        public IDisposable Suppress()
        {
            _suppressCount++;
            BreakMergeChain();
            return new SuppressScope(this);
        }

        private sealed class SuppressScope : IDisposable
        {
            private readonly UndoRedoManager _owner;
            private bool _disposed;

            public SuppressScope(UndoRedoManager owner) => _owner = owner;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _owner._suppressCount--;
                    _disposed = true;
                }
            }
        }
    }
}
