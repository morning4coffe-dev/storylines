using Storylines.DataStructures;
using Xunit;

namespace Storylines.Tests.DataStructures;

/// <summary>
/// Tests for PartialStack&lt;T&gt; — the list-backed stack used by TimeTravelChapter
/// for the undo queue. Now linked directly from Scripts/DataStructures/PartialStack.cs.
/// </summary>
public class PartialStackTests
{

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

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var stack = new PartialStack<int>();
        Assert.Equal(0, stack.Count);

        stack.Push(1);
        Assert.Equal(1, stack.Count);

        stack.Push(2);
        Assert.Equal(2, stack.Count);

        stack.Pop();
        Assert.Equal(1, stack.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var stack = new PartialStack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        stack.Clear();

        Assert.Equal(0, stack.Count);
        Assert.Empty(stack.items);
    }

    [Fact]
    public void Clear_ThenPush_WorksNormally()
    {
        var stack = new PartialStack<string>();
        stack.Push("a");
        stack.Clear();
        stack.Push("b");

        Assert.Equal(1, stack.Count);
        Assert.Equal("b", stack.Pop());
    }
}
