using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.DataStructures;

/// <summary>
/// Tests for PartialStack&lt;T&gt; — the list-backed stack used by TimeTravelChapter
/// for the undo queue.
///
/// PartialStack is defined inside TimeTravelSystem.cs which has heavy WinUI/ServiceLocator
/// dependencies, so a local copy is used here. The logic is identical.
/// </summary>
public class PartialStackTests
{
    // Local copy — identical to Storylines.Scripts.Functions.PartialStack<T>
    private sealed class PartialStack<T>
    {
        public List<T> items = new();

        public void Push(T item) => items.Add(item);

        public T Pop()
        {
            if (items.Count > 0)
            {
                T temp = items[items.Count - 1];
                items.RemoveAt(items.Count - 1);
                return temp;
            }
            return default!;
        }
    }

    [Fact]
    public void Push_AddsItemToInternalList()
    {
        var stack = new PartialStack<int>();
        stack.Push(42);
        Assert.Single(stack.items);
        Assert.Equal(42, stack.items[0]);
    }

    [Fact]
    public void Pop_ReturnsLastPushedItem()
    {
        var stack = new PartialStack<string>();
        stack.Push("first");
        stack.Push("second");

        Assert.Equal("second", stack.Pop());
    }

    [Fact]
    public void Pop_RemovesLastItem_LeavingPreviousItems()
    {
        var stack = new PartialStack<string>();
        stack.Push("a");
        stack.Push("b");
        stack.Pop();

        Assert.Single(stack.items);
        Assert.Equal("a", stack.items[0]);
    }

    [Fact]
    public void Pop_EmptyStack_ReturnsDefaultForValueType()
    {
        var stack = new PartialStack<int>();
        Assert.Equal(0, stack.Pop());
    }

    [Fact]
    public void Pop_EmptyStack_ReturnsNullForReferenceType()
    {
        var stack = new PartialStack<string>();
        Assert.Null(stack.Pop());
    }

    [Fact]
    public void PushAndPop_MultipleItems_FollowsLifoOrder()
    {
        var stack = new PartialStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        Assert.Equal(3, stack.Pop());
        Assert.Equal(2, stack.Pop());
        Assert.Equal(1, stack.Pop());
    }

    [Fact]
    public void Items_DirectAccess_AllowsManualClear()
    {
        // TimeTravelChapter uses redoQueue.items.Clear() directly — this verifies that works
        var stack = new PartialStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.items.Clear();

        Assert.Empty(stack.items);
        // Pop on empty stack should not throw
        var ex = Record.Exception(() => stack.Pop());
        Assert.Null(ex);
    }

    [Fact]
    public void Items_DirectAccess_AllowsIndexAssignment()
    {
        // TimeTravelChapter's TryGroupingUndoQueue uses items[i-1] = ... to replace entries
        var stack = new PartialStack<string>();
        stack.Push("original");
        stack.items[0] = "replaced";

        Assert.Equal("replaced", stack.items[0]);
    }

    [Fact]
    public void Items_DirectAccess_AllowsRemoveAt()
    {
        // TryGroupingUndoQueue also calls items.RemoveAt(i)
        var stack = new PartialStack<int>();
        stack.Push(10);
        stack.Push(20);
        stack.Push(30);
        stack.items.RemoveAt(1); // remove the middle item

        Assert.Equal(2, stack.items.Count);
        Assert.Equal(10, stack.items[0]);
        Assert.Equal(30, stack.items[1]);
    }

    [Fact]
    public void Push_NullItem_IsStoredAndReturned()
    {
        var stack = new PartialStack<string?>();
        stack.Push(null);
        Assert.Single(stack.items);
        Assert.Null(stack.Pop());
    }
}
