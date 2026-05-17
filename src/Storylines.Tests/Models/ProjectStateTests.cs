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
        public List<TestPinboardConnection> PinboardConnections { get; } = new();

        public TestChapter AddExistingChapter(
            string name,
            string token,
            string text,
            string notes = "",
            string synopsis = null,
            int? wordCountGoal = null,
            List<string> tags = null,
            double pinboardX = 0,
            double pinboardY = 0,
            TestChapterStatus status = TestChapterStatus.Draft,
            string location = null,
            List<string> plotThreads = null,
            int lastCaretPosition = 0,
            double lastVerticalOffset = 0)
        {
            var ch = new TestChapter
            {
                Name = name,
                Text = text,
                Notes = notes,
                Synopsis = synopsis,
                WordCountGoal = wordCountGoal,
                Tags = tags ?? new List<string>(),
                PinboardX = pinboardX,
                PinboardY = pinboardY,
                Status = status,
                Location = location,
                PlotThreads = plotThreads ?? new List<string>(),
                LastCaretPosition = lastCaretPosition,
                LastVerticalOffset = lastVerticalOffset
            };
            ch.Token = token;
            Chapters.Add(ch);
            return ch;
        }

        public TestChapter InsertExistingChapter(
            string name,
            string token,
            string text,
            int position,
            string notes = "",
            string synopsis = null,
            int? wordCountGoal = null,
            List<string> tags = null,
            double pinboardX = 0,
            double pinboardY = 0,
            TestChapterStatus status = TestChapterStatus.Draft,
            string location = null,
            List<string> plotThreads = null,
            int lastCaretPosition = 0,
            double lastVerticalOffset = 0)
        {
            var ch = new TestChapter
            {
                Name = name,
                Text = text,
                Notes = notes,
                Synopsis = synopsis,
                WordCountGoal = wordCountGoal,
                Tags = tags ?? new List<string>(),
                PinboardX = pinboardX,
                PinboardY = pinboardY,
                Status = status,
                Location = location,
                PlotThreads = plotThreads ?? new List<string>(),
                LastCaretPosition = lastCaretPosition,
                LastVerticalOffset = lastVerticalOffset
            };
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

        /// <summary>
        /// Mirrors ProjectState.RemoveChapter including the PinboardConnections reindexing logic.
        /// </summary>
        public void RemoveChapterWithPinboard(string token)
        {
            for (int i = 0; i < Chapters.Count; i++)
            {
                if (Chapters[i].Token == token)
                {
                    int removedIndex = i;
                    PinboardConnections.RemoveAll(c => c.FromIndex == removedIndex || c.ToIndex == removedIndex);
                    foreach (var conn in PinboardConnections)
                    {
                        if (conn.FromIndex > removedIndex) conn.FromIndex--;
                        if (conn.ToIndex > removedIndex) conn.ToIndex--;
                    }
                    Chapters.RemoveAt(i);
                    break;
                }
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
                Synopsis = original.Synopsis,
                WordCountGoal = original.WordCountGoal,
                Tags = original.Tags?.ToList() ?? new List<string>(),
                PinboardX = original.PinboardX,
                PinboardY = original.PinboardY,
                Status = original.Status,
                Location = original.Location,
                PlotThreads = original.PlotThreads?.ToList() ?? new List<string>(),
                LastCaretPosition = original.LastCaretPosition,
                LastVerticalOffset = original.LastVerticalOffset,
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
        public string Synopsis { get; set; }
        public int? WordCountGoal { get; set; }
        public List<string> Tags { get; set; } = new();
        public double PinboardX { get; set; }
        public double PinboardY { get; set; }
        public TestChapterStatus Status { get; set; }
        public string Location { get; set; }
        public List<string> PlotThreads { get; set; } = new();
        public int LastCaretPosition { get; set; }
        public double LastVerticalOffset { get; set; }
    }

    private enum TestChapterStatus
    {
        Draft,
        InProgress,
        Completed
    }

    private sealed class TestCharacter
    {
        public string Token { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Role { get; set; }
        public string Age { get; set; }
    }

    private sealed class TestPinboardConnection
    {
        public int FromIndex { get; set; }
        public int ToIndex { get; set; }
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
    public void InsertExistingChapter_PreservesRichMetadata()
    {
        var state = new TestProjectState();

        state.InsertExistingChapter(
            "Inserted",
            "t1",
            "text",
            0,
            notes: "note",
            synopsis: "synopsis",
            wordCountGoal: 1200,
            tags: new List<string> { "scene", "mystery" },
            pinboardX: 42,
            pinboardY: 64,
            status: TestChapterStatus.InProgress,
            location: "Archive",
            plotThreads: new List<string> { "Thread A" },
            lastCaretPosition: 18,
            lastVerticalOffset: 21.5);

        var chapter = state.Chapters[0];
        Assert.Equal("note", chapter.Notes);
        Assert.Equal("synopsis", chapter.Synopsis);
        Assert.Equal(1200, chapter.WordCountGoal);
        Assert.Equal(new[] { "scene", "mystery" }, chapter.Tags);
        Assert.Equal(42, chapter.PinboardX);
        Assert.Equal(64, chapter.PinboardY);
        Assert.Equal(TestChapterStatus.InProgress, chapter.Status);
        Assert.Equal("Archive", chapter.Location);
        Assert.Equal(new[] { "Thread A" }, chapter.PlotThreads);
        Assert.Equal(18, chapter.LastCaretPosition);
        Assert.Equal(21.5, chapter.LastVerticalOffset);
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
    public void CopyChapter_CopiesRichMetadataAndCollections()
    {
        var state = new TestProjectState();
        state.AddExistingChapter(
            "Original",
            "t1",
            "some text",
            notes: "a note",
            synopsis: "summary",
            wordCountGoal: 900,
            tags: new List<string> { "tag1" },
            pinboardX: 10,
            pinboardY: 12,
            status: TestChapterStatus.Completed,
            location: "Harbor",
            plotThreads: new List<string> { "plot" },
            lastCaretPosition: 8,
            lastVerticalOffset: 13.5);

        var copy = state.CopyChapter("t1");

        Assert.Equal("summary", copy.Synopsis);
        Assert.Equal(900, copy.WordCountGoal);
        Assert.Equal(new[] { "tag1" }, copy.Tags);
        Assert.Equal(10, copy.PinboardX);
        Assert.Equal(12, copy.PinboardY);
        Assert.Equal(TestChapterStatus.Completed, copy.Status);
        Assert.Equal("Harbor", copy.Location);
        Assert.Equal(new[] { "plot" }, copy.PlotThreads);
        Assert.Equal(8, copy.LastCaretPosition);
        Assert.Equal(13.5, copy.LastVerticalOffset);

        copy.Tags.Add("tag2");
        copy.PlotThreads.Add("plot2");

        Assert.Single(state.Chapters[0].Tags);
        Assert.Single(state.Chapters[0].PlotThreads);
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

    #region Pinboard connection reindexing

    [Fact]
    public void RemoveChapter_ConnectionsReferencingRemovedChapter_AreDeleted()
    {
        // Chapters at indices 0, 1, 2. Remove "B" (index 1).
        // All connections referencing index 1 (as from or to) must be removed.
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");

        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 1 }); // A→B: removed
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 1, ToIndex = 2 }); // B→C: removed
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 2 }); // A→C: survives (reindexed)

        state.RemoveChapterWithPinboard("t2"); // remove "B" at index 1

        Assert.Single(state.PinboardConnections);
        Assert.Equal(0, state.PinboardConnections[0].FromIndex);
        Assert.Equal(1, state.PinboardConnections[0].ToIndex); // C shifted from 2 → 1
    }

    [Fact]
    public void RemoveChapter_IndicesAboveRemoved_AreDecremented()
    {
        // Chapters: A(0), B(1), C(2), D(3). Remove B(1).
        // Connections above index 1 must have their indices decremented by 1.
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");
        state.AddExistingChapter("D", "t4", "");

        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 2 }); // A→C
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 3 }); // A→D
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 2, ToIndex = 3 }); // C→D

        state.RemoveChapterWithPinboard("t2"); // remove B at index 1

        Assert.Equal(3, state.PinboardConnections.Count);
        Assert.Equal(0, state.PinboardConnections[0].FromIndex);
        Assert.Equal(1, state.PinboardConnections[0].ToIndex); // C: 2→1
        Assert.Equal(0, state.PinboardConnections[1].FromIndex);
        Assert.Equal(2, state.PinboardConnections[1].ToIndex); // D: 3→2
        Assert.Equal(1, state.PinboardConnections[2].FromIndex); // C: 2→1
        Assert.Equal(2, state.PinboardConnections[2].ToIndex); // D: 3→2
    }

    [Fact]
    public void RemoveChapter_IndicesBelowRemoved_AreUnchanged()
    {
        // Chapters: A(0), B(1), C(2). Remove C(2).
        // Connection A→B (0→1) must stay exactly the same.
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");
        state.AddExistingChapter("C", "t3", "");

        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 1 }); // A→B (unchanged)
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 2 }); // A→C (removed)

        state.RemoveChapterWithPinboard("t3"); // remove C at index 2

        Assert.Single(state.PinboardConnections);
        Assert.Equal(0, state.PinboardConnections[0].FromIndex);
        Assert.Equal(1, state.PinboardConnections[0].ToIndex);
    }

    [Fact]
    public void RemoveChapter_NoPinboardConnections_RemovesChapterWithoutError()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("A", "t1", "");
        state.AddExistingChapter("B", "t2", "");

        var ex = Record.Exception(() => state.RemoveChapterWithPinboard("t1"));

        Assert.Null(ex);
        Assert.Single(state.Chapters);
        Assert.Equal("B", state.Chapters[0].Name);
    }

    [Fact]
    public void RemoveChapter_LastChapter_ClearsAllConnectionsReferencing()
    {
        var state = new TestProjectState();
        state.AddExistingChapter("Only", "t1", "");
        state.PinboardConnections.Add(new TestPinboardConnection { FromIndex = 0, ToIndex = 0 });

        state.RemoveChapterWithPinboard("t1");

        Assert.Empty(state.Chapters);
        Assert.Empty(state.PinboardConnections);
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
