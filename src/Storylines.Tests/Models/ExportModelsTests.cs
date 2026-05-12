using Xunit;

namespace Storylines.Tests.Models;

public class ExportModelsTests
{
    [Fact]
    public void Build_CharactersTargetUsesPrimaryCharacterSelections()
    {
        var snapshot = ExportSelectionBuilder.Build(
            ExportTarget.Characters,
            new[]
            {
                new ExportSelectionState("character-1", true),
                new ExportSelectionState("character-2", false),
                new ExportSelectionState("character-3", true),
            },
            new[]
            {
                new ExportSelectionState("ignored-secondary", true)
            });

        Assert.Equal(new[] { "character-1", "character-3" }, snapshot.CharacterIds.ToArray());
        Assert.Empty(snapshot.ChapterIndexes);
        Assert.Empty(snapshot.DialogueCharacterIds);
    }

    [Fact]
    public void Build_DialoguesTargetUsesSelectedChaptersAndCharacterFilter()
    {
        var snapshot = ExportSelectionBuilder.Build(
            ExportTarget.Dialogues,
            new[]
            {
                new ExportSelectionState("chapter-1", true, 1),
                new ExportSelectionState("chapter-2", false, 2),
                new ExportSelectionState("chapter-3", true, 3),
            },
            new[]
            {
                new ExportSelectionState("alice", true),
                new ExportSelectionState("bob", false),
                new ExportSelectionState("charlie", true),
            });

        Assert.Equal(new[] { 1, 3 }, snapshot.ChapterIndexes.ToArray());
        Assert.Empty(snapshot.CharacterIds);
        Assert.Equal(new[] { "alice", "charlie" }, snapshot.DialogueCharacterIds.ToArray());
    }

    [Fact]
    public void Find_ChaptersCapabilityIncludesMarkdown()
    {
        var capability = ExportCapabilityCatalog.Find(ExportTarget.Chapters);

        Assert.NotNull(capability);
        Assert.Equal(
            new[] { ExportFormatId.PlainText, ExportFormatId.RichText, ExportFormatId.Markdown },
            capability.Formats.Select(format => format.Id).ToArray());
    }

    [Fact]
    public void Find_DialoguesCapabilityIncludesMarkdownAndCsv()
    {
        var capability = ExportCapabilityCatalog.Find(ExportTarget.Dialogues);

        Assert.NotNull(capability);
        Assert.Equal(
            new[] { ExportFormatId.PlainText, ExportFormatId.Markdown, ExportFormatId.Csv, ExportFormatId.Json },
            capability.Formats.Select(format => format.Id).ToArray());
    }

    [Fact]
    public void Find_CharactersCapabilityIncludesMarkdownAndJson()
    {
        var capability = ExportCapabilityCatalog.Find(ExportTarget.Characters);

        Assert.NotNull(capability);
        Assert.Equal(
            new[] { ExportFormatId.Markdown, ExportFormatId.Json },
            capability.Formats.Select(format => format.Id).ToArray());
    }
}