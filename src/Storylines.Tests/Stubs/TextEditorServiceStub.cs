using Storylines.Models;
using Storylines.Services.Interfaces;
using System.Collections.Generic;

namespace Storylines.Tests.Stubs;

/// <summary>
/// Test double for <see cref="ITextEditorService"/>. Captures inserted text in
/// <see cref="InsertedFragments"/> for assertions without spinning up a real RichEditBox.
/// </summary>
internal sealed class TextEditorServiceStub : ITextEditorService
{
    public List<string> InsertedFragments { get; } = new();
    public string CurrentText { get; private set; } = string.Empty;
    public string SelectedText { get; set; } = string.Empty;
    public int SelectedChapterIndex { get; set; } = -1;
    public int SelectedTextLength { get; set; }
    public bool IsProgrammaticChangeInProgress => false;
    public bool FocusCalled { get; private set; }

    public string GetText(TextFormat format) => CurrentText;
    public string GetSelectedText() => SelectedText;
    public void SetText(TextFormat format, string text) => CurrentText = text ?? string.Empty;
    public void SetText(string rtfText) => SetText(TextFormat.Rtf, rtfText);
    public void Clear() => CurrentText = string.Empty;
    public void Undo() { }
    public void Redo() { }
    public void Focus() => FocusCalled = true;
    public void LoadChapterContent(Chapter chapter) => CurrentText = chapter?.Text ?? string.Empty;
    public void SaveChapterContent(Chapter chapter)
    {
        if (chapter != null) chapter.Text = CurrentText;
    }
    public void InsertTextAtCaret(string text) => InsertedFragments.Add(text);
}
