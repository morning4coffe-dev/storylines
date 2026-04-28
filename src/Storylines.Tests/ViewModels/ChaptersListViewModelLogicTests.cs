using System.Collections.Generic;
using Xunit;

namespace Storylines.Tests.ViewModels;

public class ChaptersListViewModelLogicTests
{
    private enum TestVisibility
    {
        Visible,
        Collapsed
    }

    private sealed class TestChapter
    {
        public string? Token { get; set; }
    }

    private readonly record struct ListState(
        TestVisibility NoChaptersPlaceholderVisibility,
        bool IsExportEnabled,
        bool IsSaveEnabled,
        bool IsSaveCopyEnabled,
        bool IsAddButtonEnabled);

    private static int GetSelectedIndex(IReadOnlyList<TestChapter> chapters, TestChapter? selectedChapter)
    {
        if (selectedChapter == null)
            return -1;

        for (int i = 0; i < chapters.Count; i++)
        {
            if (ReferenceEquals(chapters[i], selectedChapter))
                return i;
        }

        return -1;
    }

    private static TestChapter? ResolveSelectedChapter(IReadOnlyList<TestChapter> chapters, int selectedIndex)
    {
        return selectedIndex >= 0 && selectedIndex < chapters.Count
            ? chapters[selectedIndex]
            : null;
    }

    private static int ClampSelectedIndex(int selectedIndex, int chapterCount)
    {
        if (chapterCount == 0)
            return -1;

        if (selectedIndex >= chapterCount)
            return chapterCount - 1;

        return selectedIndex;
    }

    private static ListState BuildListState(int chapterCount, int characterCount, bool canAdd)
    {
        return new ListState(
            chapterCount > 0 ? TestVisibility.Collapsed : TestVisibility.Visible,
            chapterCount > 0 || characterCount > 0,
            chapterCount > 0,
            chapterCount > 0,
            canAdd);
    }

    [Fact]
    public void GetSelectedIndex_ExistingChapter_ReturnsChapterPosition()
    {
        var chapters = new List<TestChapter>
        {
            new() { Token = "one" },
            new() { Token = "two" },
            new() { Token = "three" }
        };

        Assert.Equal(1, GetSelectedIndex(chapters, chapters[1]));
    }

    [Fact]
    public void GetSelectedIndex_NullChapter_ReturnsMinusOne()
    {
        var chapters = new List<TestChapter> { new() { Token = "one" } };

        Assert.Equal(-1, GetSelectedIndex(chapters, null));
    }

    [Fact]
    public void ResolveSelectedChapter_ValidIndex_ReturnsMatchingChapter()
    {
        var chapters = new List<TestChapter>
        {
            new() { Token = "one" },
            new() { Token = "two" }
        };

        Assert.Same(chapters[1], ResolveSelectedChapter(chapters, 1));
    }

    [Fact]
    public void ResolveSelectedChapter_OutOfRange_ReturnsNull()
    {
        var chapters = new List<TestChapter> { new() { Token = "one" } };

        Assert.Null(ResolveSelectedChapter(chapters, 2));
        Assert.Null(ResolveSelectedChapter(chapters, -1));
    }

    [Fact]
    public void ClampSelectedIndex_WhenListBecomesEmpty_ReturnsMinusOne()
    {
        Assert.Equal(-1, ClampSelectedIndex(3, 0));
    }

    [Fact]
    public void ClampSelectedIndex_WhenListShrinks_ReturnsLastAvailableIndex()
    {
        Assert.Equal(1, ClampSelectedIndex(4, 2));
    }

    [Fact]
    public void BuildListState_WithNoContent_ShowsPlaceholderAndDisablesSave()
    {
        var state = BuildListState(chapterCount: 0, characterCount: 0, canAdd: true);

        Assert.Equal(TestVisibility.Visible, state.NoChaptersPlaceholderVisibility);
        Assert.False(state.IsExportEnabled);
        Assert.False(state.IsSaveEnabled);
        Assert.False(state.IsSaveCopyEnabled);
        Assert.True(state.IsAddButtonEnabled);
    }

    [Fact]
    public void BuildListState_WithCharactersOnly_EnablesExportWithoutSave()
    {
        var state = BuildListState(chapterCount: 0, characterCount: 2, canAdd: true);

        Assert.Equal(TestVisibility.Visible, state.NoChaptersPlaceholderVisibility);
        Assert.True(state.IsExportEnabled);
        Assert.False(state.IsSaveEnabled);
        Assert.False(state.IsSaveCopyEnabled);
    }

    [Fact]
    public void BuildListState_UsesCanAddForAddButtonState()
    {
        var state = BuildListState(chapterCount: 3, characterCount: 0, canAdd: false);

        Assert.Equal(TestVisibility.Collapsed, state.NoChaptersPlaceholderVisibility);
        Assert.True(state.IsExportEnabled);
        Assert.True(state.IsSaveEnabled);
        Assert.True(state.IsSaveCopyEnabled);
        Assert.False(state.IsAddButtonEnabled);
    }
}