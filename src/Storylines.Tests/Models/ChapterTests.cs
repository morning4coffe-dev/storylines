using Xunit;

namespace Storylines.Tests.Models;

public class ChapterTests
{
    [Fact]
    public void SetToken_SetsTokenCorrectly()
    {
        var chapter = new Chapter();
        chapter.SetToken("abc-123");
        Assert.Equal("abc-123", chapter.Token);
    }

    [Fact]
    public void Token_IsReadOnly_CanOnlyBeSetViaSetToken()
    {
        var chapter = new Chapter();
        chapter.SetToken("first");
        chapter.SetToken("second");
        Assert.Equal("second", chapter.Token);
    }

    [Fact]
    public void Name_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.Name = "New Name";

        Assert.Contains("Name", fired);
    }

    [Fact]
    public void Text_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.Text = "Chapter content";

        Assert.Contains("Text", fired);
    }

    [Fact]
    public void Notes_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.Notes = "A note";

        Assert.Contains("Notes", fired);
    }

    [Fact]
    public void Synopsis_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.Synopsis = "A synopsis";

        Assert.Contains("Synopsis", fired);
    }

    [Fact]
    public void WordCountGoal_PropertyChanged_FiresWithCorrectPropertyName()
    {
        var chapter = new Chapter();
        var fired = new List<string>();
        chapter.PropertyChanged += (_, e) => fired.Add(e.PropertyName!);

        chapter.WordCountGoal = 500;

        Assert.Contains("WordCountGoal", fired);
    }

    [Fact]
    public void Name_Update_ReflectsNewValue()
    {
        var chapter = new Chapter { Name = "Old" };
        chapter.Name = "New";
        Assert.Equal("New", chapter.Name);
    }

    [Fact]
    public void WordCountGoal_CanBeNull()
    {
        var chapter = new Chapter { WordCountGoal = 1000 };
        chapter.WordCountGoal = null;
        Assert.Null(chapter.WordCountGoal);
    }

    [Fact]
    public void PropertyChanged_NotFiredWhenNoSubscribers()
    {
        // Verifies no NullReferenceException when there are no subscribers
        var chapter = new Chapter();
        var ex = Record.Exception(() => chapter.Name = "test");
        Assert.Null(ex);
    }
}
