using Xunit;

namespace Storylines.Tests.Helpers;

public class UndoRedoManagerTests
{
    // ── Test helpers ───────────────────────────────────────────────

    private sealed class SetValueAction : IUndoableAction
    {
        private readonly int[] _target;
        private readonly int _before;
        private readonly int _after;

        public int UndoCount { get; private set; }
        public int RedoCount { get; private set; }

        public SetValueAction(int[] target, int before, int after)
        {
            _target = target;
            _before = before;
            _after = after;
        }

        public void Undo() { _target[0] = _before; UndoCount++; }
        public void Redo() { _target[0] = _after; RedoCount++; }
        public bool TryMerge(IUndoableAction newer) => false;
    }

    private sealed class MergeableAction : IUndoableAction
    {
        public string Key { get; }
        public string Before { get; }
        public string After { get; set; }

        public MergeableAction(string key, string before, string after)
        {
            Key = key;
            Before = before;
            After = after;
        }

        public void Undo() { }
        public void Redo() { }

        public bool TryMerge(IUndoableAction newer)
        {
            if (newer is MergeableAction m && m.Key == Key)
            {
                After = m.After;
                return true;
            }
            return false;
        }
    }

    // ── Basic operations ──────────────────────────────────────────

    [Fact]
    public void Initial_State_Is_Empty()
    {
        var mgr = new UndoRedoManager();
        Assert.False(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Record_Enables_CanUndo()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));

        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Undo_Calls_Action_Undo_And_Enables_CanRedo()
    {
        var target = new[] { 0 };
        var action = new SetValueAction(target, 0, 1);
        var mgr = new UndoRedoManager();
        mgr.Record(action);

        mgr.Undo();

        Assert.Equal(0, target[0]);
        Assert.Equal(1, action.UndoCount);
        Assert.False(mgr.CanUndo);
        Assert.True(mgr.CanRedo);
    }

    [Fact]
    public void Redo_Calls_Action_Redo()
    {
        var target = new[] { 0 };
        var action = new SetValueAction(target, 0, 1);
        var mgr = new UndoRedoManager();
        mgr.Record(action);

        mgr.Undo();
        mgr.Redo();

        Assert.Equal(1, target[0]);
        Assert.Equal(1, action.RedoCount);
        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Record_After_Undo_Clears_Redo_Stack()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));
        mgr.Undo();
        Assert.True(mgr.CanRedo);

        mgr.Record(new SetValueAction(new[] { 0 }, 0, 2));

        Assert.True(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    // ── Multiple undo/redo ────────────────────────────────────────

    [Fact]
    public void Multiple_Undo_Redo_Restores_Correctly()
    {
        var target = new[] { 0 };
        var mgr = new UndoRedoManager();

        mgr.Record(new SetValueAction(target, 0, 1));
        mgr.Record(new SetValueAction(target, 1, 2));
        mgr.Record(new SetValueAction(target, 2, 3));

        mgr.Undo(); // 3 → 2
        Assert.Equal(2, target[0]);

        mgr.Undo(); // 2 → 1
        Assert.Equal(1, target[0]);

        mgr.Redo(); // 1 → 2
        Assert.Equal(2, target[0]);

        mgr.Redo(); // 2 → 3
        Assert.Equal(3, target[0]);
    }

    // ── Merging ───────────────────────────────────────────────────

    [Fact]
    public void TryMerge_Merges_Compatible_Actions()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new MergeableAction("key1", "a", "ab"));
        mgr.Record(new MergeableAction("key1", "ab", "abc"));

        // Should have merged into one entry
        mgr.Undo();
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void TryMerge_Does_Not_Merge_Different_Keys()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new MergeableAction("key1", "a", "ab"));
        mgr.Record(new MergeableAction("key2", "x", "xy"));

        mgr.Undo();
        Assert.True(mgr.CanUndo); // key1 entry still there
    }

    [Fact]
    public void TryMerge_Updates_After_Value()
    {
        var mgr = new UndoRedoManager();
        var first = new MergeableAction("k", "a", "ab");
        mgr.Record(first);
        mgr.Record(new MergeableAction("k", "ab", "abc"));

        Assert.Equal("abc", first.After);
        Assert.Equal("a", first.Before);
    }

    // ── Capacity ──────────────────────────────────────────────────

    [Fact]
    public void Exceeding_Capacity_Removes_Oldest_Entry()
    {
        var mgr = new UndoRedoManager(maxCapacity: 3);

        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));
        mgr.Record(new SetValueAction(new[] { 0 }, 1, 2));
        mgr.Record(new SetValueAction(new[] { 0 }, 2, 3));
        mgr.Record(new SetValueAction(new[] { 0 }, 3, 4)); // pushes out (0→1)

        int undoCount = 0;
        while (mgr.CanUndo)
        {
            mgr.Undo();
            undoCount++;
        }
        Assert.Equal(3, undoCount);
    }

    // ── Clear ─────────────────────────────────────────────────────

    [Fact]
    public void Clear_Empties_Both_Stacks()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));
        mgr.Undo();

        mgr.Clear();

        Assert.False(mgr.CanUndo);
        Assert.False(mgr.CanRedo);
    }

    // ── Suppress ──────────────────────────────────────────────────

    [Fact]
    public void Suppress_Prevents_Recording()
    {
        var mgr = new UndoRedoManager();

        using (mgr.Suppress())
        {
            mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));
        }

        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void Suppress_Breaks_Merge_Chain()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new MergeableAction("k", "a", "ab"));

        using (mgr.Suppress()) { } // just break the chain

        mgr.Record(new MergeableAction("k", "ab", "abc"));

        // Two separate entries because suppress broke the merge chain
        mgr.Undo();
        Assert.True(mgr.CanUndo);
    }

    [Fact]
    public void BreakMergeChain_Prevents_Next_Merge()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new MergeableAction("k", "a", "ab"));

        mgr.BreakMergeChain();

        mgr.Record(new MergeableAction("k", "ab", "abc"));

        // Two separate entries
        mgr.Undo();
        Assert.True(mgr.CanUndo);
    }

    [Fact]
    public void BreakMergeChain_Only_Affects_Next_Record()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new MergeableAction("k", "a", "ab"));

        mgr.BreakMergeChain();
        mgr.Record(new MergeableAction("k", "ab", "abc")); // not merged (chain broken)

        // The break is consumed; the next merge should work again
        mgr.Record(new MergeableAction("k", "abc", "abcd")); // merged

        mgr.Undo(); // undoes "abc"→"abcd" merged with "ab"→"abc"
        Assert.True(mgr.CanUndo); // original "a"→"ab" still there
        mgr.Undo(); // undoes "a"→"ab"
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void Suppress_Can_Be_Nested()
    {
        var mgr = new UndoRedoManager();

        using (mgr.Suppress())
        {
            using (mgr.Suppress())
            {
                mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));
            }
            // Still suppressed (outer scope)
            mgr.Record(new SetValueAction(new[] { 0 }, 0, 2));
        }

        // No longer suppressed
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 3));

        Assert.True(mgr.CanUndo);
        mgr.Undo();
        Assert.False(mgr.CanUndo); // only 1 entry was recorded
    }

    // ── IsExecuting ───────────────────────────────────────────────

    [Fact]
    public void IsExecuting_Is_True_During_Undo()
    {
        bool wasExecuting = false;
        var mgr = new UndoRedoManager();
        var action = new CallbackAction(
            onUndo: () => wasExecuting = mgr.IsExecuting,
            onRedo: () => { });
        mgr.Record(action);

        mgr.Undo();

        Assert.True(wasExecuting);
        Assert.False(mgr.IsExecuting);
    }

    [Fact]
    public void IsExecuting_Is_True_During_Redo()
    {
        bool wasExecuting = false;
        var mgr = new UndoRedoManager();
        var action = new CallbackAction(
            onUndo: () => { },
            onRedo: () => wasExecuting = mgr.IsExecuting);
        mgr.Record(action);
        mgr.Undo();

        mgr.Redo();

        Assert.True(wasExecuting);
        Assert.False(mgr.IsExecuting);
    }

    [Fact]
    public void Record_During_IsExecuting_Is_Ignored()
    {
        var mgr = new UndoRedoManager();
        var action = new CallbackAction(
            onUndo: () => mgr.Record(new SetValueAction(new[] { 0 }, 0, 99)),
            onRedo: () => { });
        mgr.Record(action);

        mgr.Undo();

        Assert.False(mgr.CanUndo); // the re-entrant Record was ignored
    }

    // ── StateChanged event ────────────────────────────────────────

    [Fact]
    public void StateChanged_Fires_On_Record()
    {
        var mgr = new UndoRedoManager();
        int fired = 0;
        mgr.StateChanged += () => fired++;

        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));

        Assert.Equal(1, fired);
    }

    [Fact]
    public void StateChanged_Fires_On_Undo_And_Redo()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));

        int fired = 0;
        mgr.StateChanged += () => fired++;

        mgr.Undo();
        mgr.Redo();

        Assert.Equal(2, fired);
    }

    [Fact]
    public void StateChanged_Fires_On_Clear()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(new SetValueAction(new[] { 0 }, 0, 1));

        int fired = 0;
        mgr.StateChanged += () => fired++;

        mgr.Clear();

        Assert.Equal(1, fired);
    }

    // ── Edge cases ────────────────────────────────────────────────

    [Fact]
    public void Undo_On_Empty_Stack_Is_No_Op()
    {
        var mgr = new UndoRedoManager();
        mgr.Undo(); // should not throw
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void Redo_On_Empty_Stack_Is_No_Op()
    {
        var mgr = new UndoRedoManager();
        mgr.Redo(); // should not throw
        Assert.False(mgr.CanRedo);
    }

    [Fact]
    public void Record_Null_Action_Is_No_Op()
    {
        var mgr = new UndoRedoManager();
        mgr.Record(null);
        Assert.False(mgr.CanUndo);
    }

    [Fact]
    public void IsExecuting_Resets_Even_If_Undo_Throws()
    {
        var mgr = new UndoRedoManager();
        var action = new CallbackAction(
            onUndo: () => throw new System.InvalidOperationException("test"),
            onRedo: () => { });
        mgr.Record(action);

        Assert.Throws<System.InvalidOperationException>(() => mgr.Undo());
        Assert.False(mgr.IsExecuting);
    }

    // ── Callback action helper ────────────────────────────────────

    private sealed class CallbackAction : IUndoableAction
    {
        private readonly System.Action _onUndo;
        private readonly System.Action _onRedo;

        public CallbackAction(System.Action onUndo, System.Action onRedo)
        {
            _onUndo = onUndo;
            _onRedo = onRedo;
        }

        public void Undo() => _onUndo();
        public void Redo() => _onRedo();
        public bool TryMerge(IUndoableAction newer) => false;
    }
}
