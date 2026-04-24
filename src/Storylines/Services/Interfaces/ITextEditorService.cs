namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Abstracts RichEditBox text operations so that business logic
    /// (SaveSystem, TimeTravelSystem) does not reference UI controls directly.
    /// </summary>
    public interface ITextEditorService
    {
        /// <summary>Gets the current text content in the specified format.</summary>
        string GetText(TextFormat format);

        /// <summary>Sets the text content in the specified format.</summary>
        void SetText(TextFormat format, string text);

        /// <summary>Clears all text content.</summary>
        void Clear();

        /// <summary>Performs an undo operation on the document.</summary>
        void Undo();

        /// <summary>Performs a redo operation on the document.</summary>
        void Redo();

        /// <summary>Gets the currently selected chapter index, or -1 if none.</summary>
        int SelectedChapterIndex { get; set; }

        /// <summary>Sets RTF text content (convenience overload).</summary>
        void SetText(string rtfText);

        /// <summary>Sets keyboard focus to the text editor.</summary>
        void Focus();
    }

    public enum TextFormat
    {
        PlainText,
        Rtf
    }
}
