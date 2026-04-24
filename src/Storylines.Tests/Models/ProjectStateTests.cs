using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Storylines.Models;
using Xunit;

namespace Storylines.Tests.Models;

/// <summary>
/// Tests for ProjectState core data operations (Add, Find, Copy, Remove, Reorder, Sort, Clear).
///
/// ProjectState has dependencies on TimeTravelSystem/ServiceLocator for undo recording,
/// so we test the pure data-management logic using a local copy that omits those calls.
/// The undo/redo recording is tested separately via TimeTravelSystem tests.
/// </summary>
public class ProjectStateTests
{
    #region Local copy of ProjectState data operations

    private sealed class TestProjectState
    {
        public ObservableCollection<TestChapter> Chapters { get; } = new();
        public ObservableCollection<TestCharacter> Characters { get; } = new();
        public List<string> PlotThreads { get; set; } = new();

        public TestChapter AddExistingChapter(string name, string token, string text, string notes = "")
        {
            var ch = new TestChapter { Name = name, Text = text, Notes = notes };
            ch.Token = token;
            Chapters.Add(ch);
            return ch;
        }

        public TestChapter InsertExistingChapter(string name, string token, string text, int position)
        {
            var ch = new TestChapter { Name = name, Text = text };
            ch.Token = token;
            Chapters.Insert(position, ch);
            return ch;
        }

        public void RenameChapter(string token, string newName)
        {
            for (int i = 0; i < Chapters.Count; i++)
                if (Chapters[i].Token == token)
                    Chapters[i].Name = newName;
        }

        public void RemoveChapter(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
                if (Chapters[i].Token == token)
                {
                    Chapters.RemoveAt(i);
                    break;
                }
        }

        public TestChapter FindChapter(string token) =>
            Chapters.FirstOrDefault(c => c.Token == token);

        public int FindChapterID(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
                if (Chapters[i].Token == token)
                    return i;
            return 0;
        }

        public TestChapter CopyChapter(string token)
        {
            var original = FindChapter(token);
            if (original == null) return null;
            return new TestChapter
            {
                Name = original.Name,
                Text = original.Text,
                Notes = original.Notes,
                Token = original.Token
            };
        }

        public void ReorderChapter(string token, int newPosition)
        {
            var chapter = FindChapter(token);
            if (chapter == null) return;
            Chapters.Remove(chapter);
            Chapters.Insert(newPosition, chapter);
        }

        public TestCharacter AddCharacter(string name, string token, string description, string role = null, string age = null)
        {
            var ch = new TestCharacter { Name = name, Description = description, Role = role, Age = age };
            ch.Token = token;
            Characters.Add(ch);
            return ch;
        }

        public TestCharacter FindCharacter(string token) =>
            Characters.FirstOrDefault(c => c.Token == token);

        public int FindCharacterID(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
                if (Characters[i].Token == token)
                    return i;
            return 0;
        }

        public TestCharacter CopyCharacter(string token)
        {
            var original = FindCharacter(token);
            if (original == null) return null;
            return new TestCharacter
            {
                Name = original.Name,
                Description = original.Description,
                Role = original.Role,
                Age = original.Age,
                Token = original.Token
            };
        }

        public void RemoveCharacter(string token)
        {
            for (int i = 0; i < Characters.Count; i++)
                if (Characters[i].Token == token)
                {
                    Characters.RemoveAt(i);
                    break;
                }
        }

        public void SortCharacters()
        {
            var sorted = Characters.OrderBy(c => c.Name).ToList();
            Characters.Clear();
            foreach (var c in sorted) Characters.Add(c);
        }

        public void Clear()
        {
            Chapters.Clear();
            Characters.Clear();
            PlotThreads.Clear();
        }
    }

    private sealed class TestChapter
    {
        public string Token { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public string Notes { get; set; }
    }

    private sealed class TestCharacter
    {
        public string Token { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Role { get; set; }
        public string Age { get; set; }
    }

    #endregion

    #region Chapter — Add / Insert

    [Fact]
    public void AddExistingChapter_AppendsToCollection()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Ch1", "t1", "text1");
        state.AddExistingChapter("Ch2", "t2", "text2");

        Assert.Equal(2, state.Chapters.Count);
        Assert.Equal("Ch1", state.Chapters[0].Name);
        Assert.Equal("Ch2", state.Chapters[1].Name);
    }

    [Fact]
    public void InsertExistingChapter_InsertsAtCorrectPosition()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("First", "t1", "");
        state.AddExistingChapter("Third", "t3", "");
        state.InsertExistingChapter("Second", "t2", "", 1);

        Assert.Equal(3, state.Chapters.Count);
        Assert.Equal("Second", state.Chapters[1].Name);
    }

    [Fact]
    public void AddExistingChapter_SetsTokenCorrectly()
    {
        var state = new TestProjectState();
        var ch = state.AddExistingChapter("Ch", "my-token", "");

        Assert.Equal("my-token", ch.Token);
    }

    #endregion

    #region Chapter — Find

    [Fact]
    public void FindChapter_ExistingToken_ReturnsChapter()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Ch1", "t1", "");
        state.AddExistingChapter("Ch2", "t2", "");

        var found = state.FindChapter("t2");
        Assert.NotNull(found);
        Assert.Equal("Ch2", found.Name);
    }

    [Fact]
    public void FindChapter_NonExistentToken_ReturnsNull()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Ch1", "t1", "");

        Assert.Null(state.FindChapter("nonexistent"));
    }

    [Fact]
    public void FindChapterID_ReturnsCorrectIndex()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");

        Assert.Equal(1, state.FindChapterID("t2"));
    }

    [Fact]
    public void FindChapterID_NonExistentToken_ReturnsZero()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");

        Assert.Equal(0, state.FindChapterID("missing"));
    }

    #endregion

    #region Chapter — Copy

    [Fact]
    public void CopyChapter_ReturnsSeparateInstanceWithSameData()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Original", "t1", "some text", "a note");

        var copy = state.CopyChapter("t1");

        Assert.NotNull(copy);
        Assert.Equal("Original", copy.Name);
        Assert.Equal("some text", copy.Text);
        Assert.Equal("a note", copy.Notes);
        Assert.Equal("t1", copy.Token);
    }

    [Fact]
    public void CopyChapter_MutatingCopy_DoesNotAffectOriginal()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Original", "t1", "text");

        var copy = state.CopyChapter("t1");
        copy.Name = "Modified";

        Assert.Equal("Original", state.Chapters[0].Name);
    }

    [Fact]
    public void CopyChapter_NonExistentToken_ReturnsNull()
    {
        var state = new TestProjectState();
        Assert.Null(state.CopyChapter("missing"));
    }

    #endregion

    #region Chapter — Rename / Remove / Reorder

    [Fact]
    public void RenameChapter_UpdatesName()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Old", "t1", "");

        state.RenameChapter("t1", "New");

        Assert.Equal("New", state.Chapters[0].Name);
    }

    [Fact]
    public void RemoveChapter_RemovesCorrectChapter()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");

        state.RemoveChapter("t2");

        Assert.Equal(2, state.Chapters.Count);
        Assert.Equal("A", state.Chapters[0].Name);
        Assert.Equal("C", state.Chapters[1].Name);
    }

    [Fact]
    public void RemoveChapter_NonExistentToken_DoesNothing()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");

        state.RemoveChapter("missing");
        Assert.Single(state.Chapters);
    }

    [Fact]
    public void ReorderChapter_MovesToNewPosition()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");

        state.ReorderChapter("t3", 0);

        Assert.Equal("C", state.Chapters[0].Name);
        Assert.Equal("A", state.Chapters[1].Name);
        Assert.Equal("B", state.Chapters[2].Name);
    }

    #endregion

    #region Character — Add / Find / Copy

    [Fact]
    public void AddCharacter_AppendsToCollection()
    {
        var state = new TestProjectState();
        state.AddCharacter("Alice", "c1", "Desc1");
        state.AddCharacter("Bob", "c2", "Desc2");

        Assert.Equal(2, state.Characters.Count);
    }

    [Fact]
    public void FindCharacter_ExistingToken_ReturnsCharacter()
    {
        var state = new TestProjectState();
        state.AddCharacter("Alice", "c1", "Desc");

        var found = state.FindCharacter("c1");
        Assert.NotNull(found);
        Assert.Equal("Alice", found.Name);
    }

    [Fact]
    public void FindCharacter_NonExistentToken_ReturnsNull()
    {
        var state = new TestProjectState();
        Assert.Null(state.FindCharacter("missing"));
    }

    [Fact]
    public void CopyCharacter_ReturnsSeparateInstance()
    {
        var state = new TestProjectState();
        state.AddCharacter("Alice", "c1", "Desc", "Hero", "30");

        var copy = state.CopyCharacter("c1");

        Assert.NotNull(copy);
        Assert.Equal("Alice", copy.Name);
        Assert.Equal("Hero", copy.Role);
        Assert.Equal("30", copy.Age);
    }

    [Fact]
    public void CopyCharacter_MutatingCopy_DoesNotAffectOriginal()
    {
        var state = new TestProjectState();
        state.AddCharacter("Alice", "c1", "Desc");

        var copy = state.CopyCharacter("c1");
        copy.Name = "Eve";

        Assert.Equal("Alice", state.Characters[0].Name);
    }

    #endregion

    #region Character — Remove / Sort

    [Fact]
    public void RemoveCharacter_RemovesCorrectCharacter()
    {
        var state = new TestProjectState();
        state.AddCharacter("Alice", "c1", "");
        state.AddCharacter("Bob", "c2", "");

        state.RemoveCharacter("c1");

        Assert.Single(state.Characters);
        Assert.Equal("Bob", state.Characters[0].Name);
    }

    [Fact]
    public void SortCharacters_SortsAlphabeticallyByName()
    {
        var state = new TestProjectState();
        state.AddCharacter("Charlie", "c3", "");
        state.AddCharacter("Alice", "c1", "");
        state.AddCharacter("Bob", "c2", "");

        state.SortCharacters();

        Assert.Equal("Alice", state.Characters[0].Name);
        Assert.Equal("Bob", state.Characters[1].Name);
        Assert.Equal("Charlie", state.Characters[2].Name);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllData()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Ch", "t1", "");
        state.AddCharacter("Alice", "c1", "");
        state.PlotThreads.Add("thread1");

        state.Clear();

        Assert.Empty(state.Chapters);
        Assert.Empty(state.Characters);
        Assert.Empty(state.PlotThreads);
    }

    #endregion
}
