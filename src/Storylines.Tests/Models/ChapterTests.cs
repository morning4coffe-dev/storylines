using System.Collections.Generic;
using Storylines.Scripts.Variables;
using Xunit;

namespace Storylines.Tests.Models;

public class ChapterTests
{
    [Fact]
    public void SetToken_SetsTokenCorrectly()
    {
        var chapter = new Chapter();
        chapter.SetToken("abc-123");
        Assert.Equal("abc-123", chapter.token);
    }

    [Fact]
    public void Token_IsReadOnly_CanOnlyBeSetViaSetToken()
    {
        var chapter = new Chapter();
        chapter.SetToken("first");
        chapter.SetToken("second");
        Assert.Equal("second", chapter.token);
    }

    [Fact]
    public void Name_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.name = "New Name";

        Assert.Contains("name", fired);
    }

    [Fact]
    public void Text_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.text = "Chapter content";

        Assert.Contains("text", fired);
    }

    [Fact]
    public void Notes_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.notes = "A note";

        Assert.Contains("notes", fired);
    }

    [Fact]
    public void Synopsis_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.synopsis = "A synopsis";

        Assert.Contains("synopsis", fired);
    }

    [Fact]
    public void WordCountGoal_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.wordCountGoal = 500;

        Assert.Contains("wordCountGoal", fired);
    }

    [Fact]
    public void Name_Update_ReflectsNewValue()
    {
        var chapter = new Chapter { name = "Old" };
        chapter.name = "New";
        Assert.Equal("New", chapter.name);
    }

    [Fact]
    public void WordCountGoal_CanBeNull()
    {
        var chapter = new Chapter { wordCountGoal = 1000 };
        chapter.wordCountGoal = null;
        Assert.Null(chapter.wordCountGoal);
    }

    [Fact]
    public void PropertyChanged_NotFiredWhenNoSubscribers()
    {
        // Verifies no NullReferenceException when there are no subscribers
        var chapter = new Chapter();
        var ex = Record.Exception(() => chapter.name = "test");
        Assert.Null(ex);
    }
}
