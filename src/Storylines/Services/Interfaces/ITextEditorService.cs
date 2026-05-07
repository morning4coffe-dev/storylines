namespace Storylines.Services.Interfaces
{
    /// <summary>
    /// Abstracts RichEditBox text operations so that business logic
    /// (ProjectPersistenceService, TimeTravelSystem) does not reference UI controls directly.
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

        /// <summary>Gets the length of the currently selected text, or 0 if nothing is selected.</summary>
        int SelectedTextLength { get; }

        /// <summary>Returns the currently selected plain-text, or empty string if nothing is selected.</summary>
        string GetSelectedText();

        /// <summary>Sets keyboard focus to the text editor.</summary>
        void Focus();

        /// <summary>
        /// True while the editor is applying content programmatically and UI change
        /// notifications should not be treated as user edits.
        /// </summary>
        bool IsProgrammaticChangeInProgress { get; }

        /// <summary>
        /// Loads a chapter's content into the editor. Handles RTF loading safely
        /// without raw string manipulation.
        /// </summary>
        void LoadChapterContent(Models.Chapter chapter);

        /// <summary>
        /// Saves the current editor content back to the specified chapter model.
        /// </summary>
        void SaveChapterContent(Models.Chapter chapter);

        /// <summary>
        /// Inserts <paramref name="text"/> at the current caret position. Used by the dictation
        /// service to push recognized speech into the editor without the view-model needing to
        /// reach into the RichEditBox directly.
        /// </summary>
        void InsertTextAtCaret(string text);
    }

    public enum TextFormat
    {
        PlainText,
        Rtf
    }
}
