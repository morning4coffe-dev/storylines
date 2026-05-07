using Storylines.Helpers;
using Storylines.Views.Controls;
using Storylines.Views.Pages;
using Storylines.Services.Interfaces;
using Storylines.Models;
using System;
using System.Threading;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Text;

namespace Storylines.Services
{
    /// <summary>
    /// Bridges the ITextEditorService interface to the actual RichEditBox
    /// and ListView UI controls. Registered after UI is ready.
    /// </summary>
    public class TextEditorService : ITextEditorService
    {
        private readonly ProjectState _projectState;
        private readonly WindowContext _windowContext;

        public TextEditorService(ProjectState projectState, WindowContext windowContext)
        {
            _projectState = projectState;
            _windowContext = windowContext;
        }

        private int _programmaticChangeDepth;

        public bool IsProgrammaticChangeInProgress => _programmaticChangeDepth > 0;

        public string GetText(TextFormat format)
        {
            var textBox = _windowContext.ChapterText?.textBox;
            if (textBox is null) return string.Empty;

            var options = format == TextFormat.Rtf
                ? Microsoft.UI.Text.TextGetOptions.FormatRtf
                : Microsoft.UI.Text.TextGetOptions.None;

            textBox.Document.GetText(options, out string text);
            return text;
        }

        public void SetText(TextFormat format, string text)
        {
            var chapterText = _windowContext.ChapterText;
            var textBox = chapterText?.textBox;
            if (textBox is null) return;

            var options = format == TextFormat.Rtf
                ? Microsoft.UI.Text.TextSetOptions.FormatRtf
                : Microsoft.UI.Text.TextSetOptions.None;

            ApplyEditorText(chapterText, GetSelectedChapter(), options, text ?? string.Empty, restoreChapterLocation: false);
        }

        public void Clear()
        {
            SetText(TextFormat.PlainText, string.Empty);
        }

        public void Undo()
        {
            _windowContext.ChapterText?.textBox?.Document?.Undo();
        }

        public void Redo()
        {
            _windowContext.ChapterText?.textBox?.Document?.Redo();
        }

        public int SelectedChapterIndex
        {
            get => _windowContext.ChapterList?.ViewModel?.SelectedIndex
                ?? _windowContext.ChapterList?.listView?.SelectedIndex
                ?? -1;
            set
            {
                if (_windowContext.ChapterList?.ViewModel is not null)
                    _windowContext.ChapterList.ViewModel.SelectedIndex = value;
                else if (_windowContext.ChapterList?.listView is not null)
                    _windowContext.ChapterList.listView.SelectedIndex = value;
            }
        }

        public int SelectedTextLength =>
            _windowContext.ChapterText?.textBox?.Document?.Selection?.Text?.Length ?? 0;

        public void SetText(string rtfText)
        {
            SetText(TextFormat.Rtf, rtfText);
        }

        public void Focus()
        {
            _windowContext.ChapterText?.textBox?.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
        }

        public void LoadChapterContent(Chapter chapter)
        {
            if (chapter is null) return;

            var chapterText = _windowContext.ChapterText;
            var textBox = chapterText?.textBox;
            if (textBox is null) return;

            ApplyEditorText(chapterText, chapter, Microsoft.UI.Text.TextSetOptions.FormatRtf, chapter.Text ?? string.Empty, restoreChapterLocation: true);
        }

        private void ApplyEditorText(ChapterTextBox chapterText, Chapter chapter, TextSetOptions options, string sourceText, bool restoreChapterLocation)
        {
            var textBox = chapterText?.textBox;
            if (textBox is null)
                return;

            Interlocked.Increment(ref _programmaticChangeDepth);

            try
            {
                if (options == Microsoft.UI.Text.TextSetOptions.FormatRtf && string.IsNullOrEmpty(sourceText))
                {
                    textBox.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, string.Empty);
                }
                else
                {
                    textBox.Document.SetText(options, sourceText);
                }

                SyncLoadedChapterText(chapter, textBox, sourceText);
            }
            finally
            {
                ScheduleProgrammaticChangeCompletion(textBox, chapterText, chapter, restoreChapterLocation);
            }
        }

        private static void SyncLoadedChapterText(Chapter chapter, MyRichEditBox textBox, string sourceText)
        {
            if (chapter is null || textBox is null)
                return;

            // RichEditBox normalizes paragraph markers as it loads content.
            // Keep the model aligned before programmatic change suppression ends,
            // otherwise the next TextChanged can look like a user edit.
            textBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string plainText);
            textBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.FormatRtf, out string normalizedRtf);

            var normalizedChapterText = ChapterTextNormalization.NormalizeLoadedChapterText(sourceText, plainText, normalizedRtf);
            if (chapter.Text != normalizedChapterText)
                chapter.Text = normalizedChapterText;
        }

        private void ScheduleProgrammaticChangeCompletion(MyRichEditBox textBox, ChapterTextBox chapterText, Chapter chapter, bool restoreChapterLocation)
        {
            textBox.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                if (restoreChapterLocation)
                    RestoreChapterLocation(chapterText, chapter);

                textBox.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    if (_programmaticChangeDepth > 0)
                        Interlocked.Decrement(ref _programmaticChangeDepth);
                });
            });
        }

        private Chapter GetSelectedChapter()
        {
            var selectedIndex = SelectedChapterIndex;

            if (_projectState?.Chapters is null || selectedIndex < 0 || selectedIndex >= _projectState.Chapters.Count)
                return null;

            return _projectState.Chapters[selectedIndex];
        }

        private static void RestoreChapterLocation(ChapterTextBox chapterText, Chapter chapter)
        {
            var textBox = chapterText?.textBox;
            if (textBox is null || chapter is null)
                return;

            var range = textBox.Document.GetRange(0, TextConstants.MaxUnitCount);
            var caretPosition = Math.Max(0, Math.Min(chapter.LastCaretPosition, range.EndPosition));
            textBox.Document.Selection.SetRange(caretPosition, caretPosition);

            var scrollViewer = chapterText.textBoxScrollViewer;
            if (scrollViewer is null)
                return;

            scrollViewer.UpdateLayout();

            var verticalOffset = Math.Max(0, Math.Min(chapter.LastVerticalOffset, scrollViewer.ScrollableHeight));
            _ = scrollViewer.ChangeView(null, verticalOffset, null, true);
        }

        public void SaveChapterContent(Chapter chapter)
        {
            if (chapter is null) return;

            var textBox = _windowContext.ChapterText?.textBox;
            if (textBox is null) return;

            textBox.Document.GetText(Microsoft.UI.Text.TextGetOptions.FormatRtf, out string rtf);
            chapter.Text = rtf;
        }

        public void InsertTextAtCaret(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var textBox = _windowContext.ChapterText?.textBox;
            if (textBox is null) return;

            var selection = textBox.Document.Selection;
            if (selection is null) return;

            // Replace the current selection (or insert at caret if collapsed) with the new text.
            selection.SetText(Microsoft.UI.Text.TextSetOptions.None, text);

            // Move caret to the end of the inserted text.
            selection.SetRange(selection.EndPosition, selection.EndPosition);
        }
    }
}
