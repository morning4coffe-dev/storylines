using Storylines.Models.DialogueScript;
using Xunit;

namespace Storylines.Tests.Models.DialogueScript;

public class SuggestionPopupManagerTests
{
    private static readonly SuggestionPopupManager Manager = new SuggestionPopupManager();

    // -------------------------------------------------------------------------
    // No-trigger cases
    // -------------------------------------------------------------------------

    [Fact]
    public void EmptyText_ReturnsNone()
    {
        var r = Manager.Analyze(string.Empty, 0);
        Assert.False(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.None, r.TriggerType);
    }

    [Fact]
    public void CaretOutOfRange_ReturnsNone()
    {
        Assert.False(Manager.Analyze("hello", -1).ShouldShow);
        Assert.False(Manager.Analyze("hello", 999).ShouldShow);
    }

    [Fact]
    public void TopLevelNodeHeader_DoesNotTriggerNodeReference()
    {
        // User is naming a brand-new node — must NOT suggest existing names.
        var text = ":: Mer";
        var r = Manager.Analyze(text, text.Length);
        Assert.NotEqual(SuggestionTriggerType.NodeReference, r.TriggerType);
    }

    [Fact]
    public void CommentLine_NoTrigger()
    {
        var text = "// this is a comment";
        var r = Manager.Analyze(text, text.Length);
        Assert.False(r.ShouldShow);
    }

    [Fact]
    public void ChoiceLineWithoutColons_NoTrigger()
    {
        var text = "-> just a choice";
        var r = Manager.Analyze(text, text.Length);
        Assert.False(r.ShouldShow);
    }

    // -------------------------------------------------------------------------
    // NodeReference triggers
    // -------------------------------------------------------------------------

    [Fact]
    public void IndentedJump_PartialName_TriggersNodeReference()
    {
        var text = "    :: Mer";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.NodeReference, r.TriggerType);
        Assert.Equal("Mer", r.FilterText);
    }

    [Fact]
    public void IndentedJump_EmptyName_TriggersWithEmptyFilter()
    {
        var text = "    :: ";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.NodeReference, r.TriggerType);
        Assert.Equal(string.Empty, r.FilterText);
    }

    [Fact]
    public void InlineJump_OnChoiceLine_TriggersNodeReference()
    {
        var text = "-> Hello there. :: Re";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.NodeReference, r.TriggerType);
        Assert.Equal("Re", r.FilterText);
    }

    [Fact]
    public void NodeReference_TriggerOffsetsAreLineRelative_ToFullText()
    {
        // First line is unrelated; trigger appears on second line.
        var text = "// preamble\n    :: Foo";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal("Foo", r.FilterText);
        // TriggerStart is the start of the partial within the FULL text buffer.
        Assert.Equal(text.Length - "Foo".Length, r.TriggerStart);
        Assert.Equal(text.Length, r.TriggerEnd);
    }

    // -------------------------------------------------------------------------
    // TagReference triggers
    // -------------------------------------------------------------------------

    [Fact]
    public void NodeHeader_PartialTag_TriggersTagReference()
    {
        var text = ":: Intro [#sta";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.TagReference, r.TriggerType);
        Assert.Equal("sta", r.FilterText);
    }

    [Fact]
    public void NodeHeader_SecondTagPartial_TriggersOnLatestHash()
    {
        var text = ":: Intro [#start #vil";
        var r = Manager.Analyze(text, text.Length);
        Assert.Equal(SuggestionTriggerType.TagReference, r.TriggerType);
        Assert.Equal("vil", r.FilterText);
    }

    [Fact]
    public void NodeHeader_AfterClosingBracket_NoTagTrigger()
    {
        var text = ":: Intro [#start] more";
        var r = Manager.Analyze(text, text.Length);
        Assert.NotEqual(SuggestionTriggerType.TagReference, r.TriggerType);
    }

    // -------------------------------------------------------------------------
    // SpeakerReference triggers
    // -------------------------------------------------------------------------

    [Fact]
    public void StartOfLine_PartialName_TriggersSpeakerReference()
    {
        var text = ":: Node\nGu";
        var r = Manager.Analyze(text, text.Length);
        Assert.True(r.ShouldShow);
        Assert.Equal(SuggestionTriggerType.SpeakerReference, r.TriggerType);
        Assert.Equal("Gu", r.FilterText);
    }

    [Fact]
    public void AfterColonOnSameLine_NoSpeakerTrigger()
    {
        var text = "Guard: Hello";
        var r = Manager.Analyze(text, text.Length);
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference, r.TriggerType);
    }

    [Fact]
    public void IndentedLine_NoSpeakerTrigger()
    {
        var text = "    Gu";
        var r = Manager.Analyze(text, text.Length);
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference, r.TriggerType);
    }

    [Fact]
    public void StartOfLine_StructuralPrefix_NoSpeakerTrigger()
    {
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference,
            Manager.Analyze("-> ", 3).TriggerType);
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference,
            Manager.Analyze("//", 2).TriggerType);
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference,
            Manager.Analyze("#tag", 4).TriggerType);
        Assert.NotEqual(SuggestionTriggerType.SpeakerReference,
            Manager.Analyze("@set foo = bar", 14).TriggerType);
    }

    // -------------------------------------------------------------------------
    // Caret position mid-line (not at end)
    // -------------------------------------------------------------------------

    [Fact]
    public void CaretMidWord_TriggersWithUpToCaretFilter()
    {
        // Cursor positioned after "Mer" but before "chant" — should filter on "Mer".
        var text = "    :: Merchant";
        var caret = text.IndexOf("M") + 3; // after "Mer"
        var r = Manager.Analyze(text, caret);
        Assert.Equal(SuggestionTriggerType.NodeReference, r.TriggerType);
        Assert.Equal("Mer", r.FilterText);
    }
}
